using AssetRipper.Core;
using AssetRipper.Core.Classes.Misc;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass502_FixGuidAndHashYaml
	{
		public static void DoPass()
		{
			foreach (TypeDefinition guidType in SharedState.Instance.SubclassGroups["GUID"].Types)
			{
				MethodDefinition releaseMethod = guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
				MethodDefinition editorMethod = guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
				releaseMethod.FixGuidYaml<UnityGUID>();
				editorMethod.FixGuidYaml<UnityGUID>();
			}
			foreach (TypeDefinition hashType in SharedState.Instance.SubclassGroups["Hash128"].Types)
			{
				MethodDefinition releaseMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
				MethodDefinition editorMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
				releaseMethod.FixHashYaml<Hash128>();
				editorMethod.FixHashYaml<Hash128>();
			}
		}

		private static void FixGuidYaml<T>(this MethodDefinition method)
		{
			MethodDefinition conversionMethod = method.DeclaringType!.Methods.Single(m => m.Name == "op_Implicit");
			ITypeDefOrRef commonRef = SharedState.Instance.Importer.ImportType<T>();
			IMethodDefOrRef exportMethod = SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == nameof(UnityAssetBase.ExportYaml));
			method.CilMethodBody!.InitializeLocals = true;
			method.CilMethodBody.LocalVariables.Clear();
			CilLocalVariable local = new CilLocalVariable(commonRef.ToTypeSignature());
			method.CilMethodBody.LocalVariables.Add(local);
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Stloc, local);
			processor.Add(CilOpCodes.Ldloca, local);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void FixHashYaml<T>(this MethodDefinition method)
		{
			MethodDefinition conversionMethod = method.DeclaringType!.Methods.Single(m => m.Name == "op_Explicit");
			ITypeDefOrRef commonRef = SharedState.Instance.Importer.ImportType<T>();
			IMethodDefOrRef exportMethod = SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == nameof(UnityAssetBase.ExportYaml));
			method.CilMethodBody!.LocalVariables.Clear();
			CilLocalVariable local = new CilLocalVariable(commonRef.ToTypeSignature());
			method.CilMethodBody.LocalVariables.Add(local);
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}
	}
}
