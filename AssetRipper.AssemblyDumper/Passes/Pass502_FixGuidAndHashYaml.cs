using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.IO.Files;
using AssetRipper.Yaml;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass502_FixGuidAndHashYaml
	{
		public static void DoPass()
		{
			foreach (TypeDefinition guidType in SharedState.Instance.SubclassGroups["GUID"].Types)
			{
				guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease)).FixGuidYaml();
				guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor)).FixGuidYaml();
			}
			InjectHelper(out TypeDefinition helperType);
			foreach (TypeDefinition hashType in SharedState.Instance.SubclassGroups["Hash128"].Types)
			{
				MethodDefinition releaseMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
				MethodDefinition editorMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
				releaseMethod.FixHashYaml(helperType);
				editorMethod.FixHashYaml(helperType);
			}
		}

		private static void FixGuidYaml(this MethodDefinition method)
		{
			MethodDefinition conversionMethod = method.DeclaringType!.Methods.Single(m => m.Name == "op_Implicit");
			ITypeDefOrRef commonRef = SharedState.Instance.Importer.ImportType<UnityGUID>();
			IMethodDefOrRef toStringMethod = SharedState.Instance.Importer.ImportMethod<UnityGUID>(m => m.Name == nameof(UnityGUID.ToString));
			IMethodDefOrRef scalarNodeConstructor = SharedState.Instance.Importer.ImportMethod<YamlScalarNode>(m => 
			{
				return m.IsConstructor
					&& m.Parameters.Count == 1
					&& m.Parameters[0].ParameterType is CorLibTypeSignature signature
					&& signature.ElementType == ElementType.String;
			});
			method.CilMethodBody!.LocalVariables.Clear();
			CilLocalVariable local = new CilLocalVariable(commonRef.ToTypeSignature());
			method.CilMethodBody.LocalVariables.Add(local);
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Stloc, local);
			processor.Add(CilOpCodes.Ldloca, local);
			processor.Add(CilOpCodes.Call, toStringMethod);
			processor.Add(CilOpCodes.Newobj, scalarNodeConstructor);
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

		private static void FixHashYaml(this MethodDefinition method, TypeDefinition helperType)
		{
			TypeDefinition type = method.DeclaringType!;
			IMethodDefOrRef exportMethod = helperType.Methods.Single(m => m.Name == nameof(HashHelper.ExportYaml));
			method.CilMethodBody!.LocalVariables.Clear();
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			for (int i = 0; i < 16; i++)
			{
				FieldDefinition field = type.GetFieldByName(GetHashFieldName(i));
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
			}
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static string GetHashFieldName(int i)
		{
			return i < 10 ? $"m_Bytes__{i}" : $"m_Bytes_{i}";
		}

		private static void InjectHelper(out TypeDefinition helperType)
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(HashHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			foreach (TypeDefinition type in result.ClonedTopLevelTypes)
			{
				type.Namespace = SharedState.HelpersNamespace;
				SharedState.Instance.Module.TopLevelTypes.Add(type);
			}
			helperType = result.ClonedTopLevelTypes.Single(t => t.Name == nameof(HashHelper));
		}
	}
}
