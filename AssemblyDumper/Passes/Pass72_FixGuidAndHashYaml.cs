using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AssetRipper.Core.Classes.Misc;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass72_FixGuidAndHashYaml
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 72: Fix Guid and Hash Yaml Export");
			if(SharedState.TypeDictionary.TryGetValue("GUID", out TypeDefinition guidType))
			{
				MethodDefinition releaseMethod = guidType.Methods.Single(m => m.Name == "ExportYAMLRelease");
				MethodDefinition editorMethod = guidType.Methods.Single(m => m.Name == "ExportYAMLEditor");
				releaseMethod.FixYaml<UnityGUID>();
				editorMethod.FixYaml<UnityGUID>();
			}
			if (SharedState.TypeDictionary.TryGetValue("Hash128", out TypeDefinition hashType))
			{
				MethodDefinition releaseMethod = hashType.Methods.Single(m => m.Name == "ExportYAMLRelease");
				MethodDefinition editorMethod = hashType.Methods.Single(m => m.Name == "ExportYAMLEditor");
				releaseMethod.FixYaml<Hash128>();
				editorMethod.FixYaml<Hash128>();
			}
		}

		private static void FixYaml<T>(this MethodDefinition method)
		{
			MethodDefinition conversionMethod = method.DeclaringType.Methods.Single(m => m.Name == "op_Implicit");
			ITypeDefOrRef commonRef = SharedState.Importer.ImportCommonType<T>();
			IMethodDefOrRef exportMethod = SharedState.Importer.ImportCommonMethod<T>(m => m.Name == "ExportYAML");
			method.CilMethodBody.InitializeLocals = true;
			method.CilMethodBody.LocalVariables.Clear();
			CilLocalVariable local = new CilLocalVariable(commonRef.ToTypeSignature());
			method.CilMethodBody.LocalVariables.Add(local);
			var processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Stloc, local);
			processor.Add(CilOpCodes.Ldloca, local);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
