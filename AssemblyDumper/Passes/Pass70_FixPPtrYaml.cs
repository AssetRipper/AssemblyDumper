using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDumper.Utils;
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
			IMethodDefOrRef exportGeneric = SharedState.Module.ImportCommonMethod("AssetRipper.Core.Classes.Misc.PPtr", m => m.Name == "ExportYAML");
			MethodSpecification commonExportReference = MethodUtils.MakeGenericInstanceMethod(exportGeneric, parameterType.ToTypeSignature());
			
			MethodDefinition releaseYamlMethod = pptrType.Methods.Single(m => m.Name == "ExportYAMLRelease");
			MethodDefinition editorYamlMethod = pptrType.Methods.Single(m => m.Name == "ExportYAMLEditor");

			FixMethod(releaseYamlMethod, commonExportReference);
			FixMethod(editorYamlMethod, commonExportReference);
		}

		private static void FixMethod(MethodDefinition yamlMethod, MethodSpecification exportMethod)
		{
			FieldDefinition fileID = yamlMethod.DeclaringType.Fields.Single(f => f.Name == "m_FileID");
			FieldDefinition pathID = yamlMethod.DeclaringType.Fields.Single(f => f.Name == "m_PathID");
			CilInstructionCollection processor = yamlMethod.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, fileID);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, pathID);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
