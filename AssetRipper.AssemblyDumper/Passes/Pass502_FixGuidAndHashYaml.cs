using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;
using AssetRipper.Primitives;
using AssetRipper.Yaml;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass502_FixGuidAndHashYaml
	{
		public static void DoPass()
		{
			foreach (TypeDefinition guidType in SharedState.Instance.SubclassGroups["GUID"].Types)
			{
				MethodDefinition toStringMethod = guidType.AddGuidToStringOverride();
				guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease)).FixGuidYaml(toStringMethod);
				guidType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor)).FixGuidYaml(toStringMethod);
			}
			TypeDefinition helperType = SharedState.Instance.InjectHelperType(typeof(HashHelper));
			foreach (TypeDefinition hashType in SharedState.Instance.SubclassGroups["Hash128"].Types)
			{
				MethodDefinition releaseMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
				MethodDefinition editorMethod = hashType.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
				releaseMethod.FixHashYaml(helperType);
				editorMethod.FixHashYaml(helperType);
				hashType.AddHashToStringOverride(helperType);
			}
		}

		private static void FixGuidYaml(this MethodDefinition method, MethodDefinition toStringMethod)
		{
			IMethodDefOrRef scalarNodeConstructor = SharedState.Instance.Importer.ImportMethod<YamlScalarNode>(m => 
			{
				return m.IsConstructor
					&& m.Parameters.Count == 1
					&& m.Parameters[0].ParameterType is CorLibTypeSignature signature
					&& signature.ElementType == ElementType.String;
			});
			method.CilMethodBody!.LocalVariables.Clear();
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, toStringMethod);
			processor.Add(CilOpCodes.Newobj, scalarNodeConstructor);
			processor.Add(CilOpCodes.Ret);
		}

		private static MethodDefinition AddGuidToStringOverride(this TypeDefinition type)
		{
			MethodDefinition method = type.AddMethod(nameof(object.ToString), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, type.Module!.CorLibTypeFactory.String);
			MethodDefinition conversionMethod = type.Methods.Single(m => m.Name == "op_Implicit");
			ITypeDefOrRef commonRef = SharedState.Instance.Importer.ImportType<UnityGuid>();
			IMethodDefOrRef toStringMethod = SharedState.Instance.Importer.ImportMethod<UnityGuid>(m => m.Name == nameof(UnityGuid.ToString));

			CilInstructionCollection processor = method.GetProcessor();
			CilLocalVariable local = processor.AddLocalVariable(commonRef.ToTypeSignature());
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Stloc, local);
			processor.Add(CilOpCodes.Ldloca, local);
			processor.Add(CilOpCodes.Call, toStringMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();

			return method;
		}

		private static void FixHashYaml(this MethodDefinition method, TypeDefinition helperType)
		{
			TypeDefinition type = method.DeclaringType!;
			IMethodDefOrRef exportMethod = helperType.Methods.Single(m => m.Name == nameof(HashHelper.ExportYaml));
			method.CilMethodBody!.LocalVariables.Clear();
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Clear();
			processor.AddLoadAllHashFields(type);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Callvirt, type.Methods.Single(m => m.Name == "get_SerializedVersion"));
			processor.Add(CilOpCodes.Call, exportMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static MethodDefinition AddHashToStringOverride(this TypeDefinition type, TypeDefinition helperType)
		{
			MethodDefinition method = type.AddMethod(nameof(object.ToString), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, type.Module!.CorLibTypeFactory.String);
			IMethodDefOrRef helperMethod = helperType.Methods.Single(m => m.Name == nameof(HashHelper.ToString));

			CilInstructionCollection processor = method.GetProcessor();
			processor.AddLoadAllHashFields(type);
			processor.Add(CilOpCodes.Call, helperMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();

			return method;
		}

		private static void AddLoadAllHashFields(this CilInstructionCollection processor, TypeDefinition type)
		{
			for (int i = 0; i < 16; i++)
			{
				FieldDefinition field = type.GetFieldByName(GetHashFieldName(i));
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
			}
		}

		private static string GetHashFieldName(int i)
		{
			return i < 10 ? $"m_Bytes__{i}" : $"m_Bytes_{i}";
		}
	}
}
