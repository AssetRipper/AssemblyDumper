using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core;
using AssetRipper.Core.Classes.Misc;

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
						TypeDefinition parameterType = Pass080_PPtrConversions.GetParameterTypeDefinition(group.Name);
						FixYaml(instance.Type, parameterType);
					}
				}
			}
		}

		private static void FixYaml(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			Func<MethodDefinition, bool> filter = m => m.Name == nameof(PPtr.ExportYaml) && m.Parameters.Count == 2;
			IMethodDefOrRef exportGeneric = SharedState.Instance.Importer.ImportMethod(typeof(PPtr), filter);
			MethodSpecification commonExportReference = MethodUtils.MakeGenericInstanceMethod(SharedState.Instance.Importer, exportGeneric, parameterType.ToTypeSignature());
			
			MethodDefinition releaseYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
			MethodDefinition editorYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));

			FixMethod(releaseYamlMethod, commonExportReference);
			FixMethod(editorYamlMethod, commonExportReference);
		}

		private static void FixMethod(MethodDefinition yamlMethod, MethodSpecification exportMethod)
		{
			CilInstructionCollection processor = yamlMethod.CilMethodBody!.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
