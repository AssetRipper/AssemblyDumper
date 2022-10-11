using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass500_FixPPtrYaml
	{
		public static void DoPass()
		{
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.Name.StartsWith("PPtr_"))
				{
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						TypeDefinition parameterType = Pass080_PPtrConversions.PPtrsToParameters[instance.Type];
						int parameterClassID = SharedState.Instance.TypesToGroups[parameterType].ID;
						FixYaml(instance.Type, parameterType, parameterClassID);
					}
				}
			}
		}

		private static void FixYaml(TypeDefinition pptrType, TypeDefinition parameterType, int parameterClassID)
		{
			Func<MethodDefinition, bool> filter = m => m.Name == nameof(PPtrExtensions.ExportYaml) && m.Parameters.Count == 3;
			IMethodDefOrRef exportGeneric = SharedState.Instance.Importer.ImportMethod(typeof(PPtrExtensions), filter);
			MethodSpecification commonExportReference = MethodUtils.MakeGenericInstanceMethod(SharedState.Instance.Importer, exportGeneric, parameterType.ToTypeSignature());

			MethodDefinition releaseYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
			MethodDefinition editorYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));

			FixMethod(releaseYamlMethod, commonExportReference, parameterClassID);
			FixMethod(editorYamlMethod, commonExportReference, parameterClassID);
		}

		private static void FixMethod(MethodDefinition yamlMethod, MethodSpecification exportMethod, int parameterClassID)
		{
			CilInstructionCollection processor = yamlMethod.CilMethodBody!.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldc_I4, parameterClassID);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}
	}
}
