using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass70_FixPPtrYaml
	{
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 70: Fix PPtr Yaml Export");

			foreach (string name in SharedState.ClassDictionary.Keys)
			{
				if (name.StartsWith("PPtr<"))
				{
					string parameterTypeName = name.Substring(5, name.LastIndexOf('>') - 5);
					TypeDefinition parameterType = SharedState.TypeDictionary[parameterTypeName];
					TypeDefinition pptrType = SharedState.TypeDictionary[name];
					FixYaml(pptrType, parameterType);
				}
			}
		}

		private static void FixYaml(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			MethodReference exportGeneric = SharedState.Module.ImportCommonMethod("AssetRipper.Core.Classes.Misc.PPtr", m => m.Name == "ExportYAML");
			GenericInstanceMethod commonExportReference = new GenericInstanceMethod(exportGeneric);
			commonExportReference.GenericArguments.Add(parameterType);
			MethodDefinition releaseYamlMethod = pptrType.Methods.Single(m => m.Name == "ExportYAMLRelease");
			MethodDefinition editorYamlMethod = pptrType.Methods.Single(m => m.Name == "ExportYAMLEditor");

			FixMethod(releaseYamlMethod, commonExportReference);
			FixMethod(editorYamlMethod, commonExportReference);
		}

		private static void FixMethod(MethodDefinition yamlMethod, MethodReference exportMethod)
		{
			FieldDefinition fileID = yamlMethod.DeclaringType.Fields.Single(f => f.Name == "m_FileID");
			FieldDefinition pathID = yamlMethod.DeclaringType.Fields.Single(f => f.Name == "m_PathID");
			yamlMethod.Body.Instructions.Clear();
			ILProcessor processor = yamlMethod.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_1);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, fileID);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, pathID);
			processor.Emit(OpCodes.Call, exportMethod);
			processor.Emit(OpCodes.Ret);
		}
	}
}
