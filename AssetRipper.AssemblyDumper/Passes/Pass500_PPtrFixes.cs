using AssetRipper.AssemblyDumper.Documentation;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass500_PPtrFixes
	{
		public static void DoPass()
		{
			static bool filter(MethodDefinition m) => m.Name == nameof(PPtrHelper.ExportYaml) && m.Parameters.Count == 3;
			IMethodDefOrRef genericExportMethod = SharedState.Instance.InjectHelperType(typeof(PPtrHelper)).Methods.Single(filter);

			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.IsPPtr)
				{
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						TypeDefinition parameterType = Pass080_PPtrConversions.PPtrsToParameters[instance.Type];

						//Yaml
						{
							IMethodDescriptor exportMethod = genericExportMethod.MakeGenericInstanceMethod(parameterType.ToTypeSignature());
							int parameterClassID = SharedState.Instance.TypesToGroups[parameterType].ID;
							FixYaml(instance.Type, exportMethod, parameterClassID);
						}

						//DebuggerDisplay
						{
							string parameterTypeName = group.Name.Substring("PPtr_".Length);
							instance.Type.AddDebuggerDisplayAttribute($"{parameterTypeName} FileID: {{FileID}} PathID: {{PathID}}");
						}

						//Documentation
						{
							DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"{SeeXmlTagGenerator.MakeCRef(typeof(PPtr))} for {SeeXmlTagGenerator.MakeCRef(parameterType)}");
						}
					}
				}
			}
		}

		private static void FixYaml(TypeDefinition pptrType, IMethodDescriptor exportMethod, int parameterClassID)
		{
			MethodDefinition releaseYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
			MethodDefinition editorYamlMethod = pptrType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));

			FixMethod(releaseYamlMethod, exportMethod, parameterClassID);
			FixMethod(editorYamlMethod, exportMethod, parameterClassID);
		}

		private static void FixMethod(MethodDefinition yamlMethod, IMethodDescriptor exportMethod, int parameterClassID)
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
