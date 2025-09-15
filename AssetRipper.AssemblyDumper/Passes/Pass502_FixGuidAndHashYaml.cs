using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass502_FixGuidAndHashYaml
	{
		public static void DoPass()
		{
			foreach (TypeDefinition guidType in SharedState.Instance.SubclassGroups["GUID"].Types)
			{
				MethodDefinition toStringMethod = guidType.AddGuidToStringOverride();
			}
			TypeDefinition helperType = SharedState.Instance.InjectHelperType(typeof(HashHelper));
			foreach (TypeDefinition hashType in SharedState.Instance.SubclassGroups["Hash128"].Types)
			{
				hashType.AddHashToStringOverride(helperType);
			}
		}

		private static MethodDefinition AddGuidToStringOverride(this TypeDefinition type)
		{
			MethodDefinition method = type.AddMethod(nameof(object.ToString), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, type.DeclaringModule!.CorLibTypeFactory.String);
			MethodDefinition conversionMethod = type.Methods.Single(m => m.Name == "op_Implicit");
			ITypeDefOrRef commonRef = SharedState.Instance.Importer.ImportType<UnityGuid>();
			IMethodDefOrRef toStringMethod = SharedState.Instance.Importer.ImportMethod<UnityGuid>(m => m.Name == nameof(UnityGuid.ToString) && m.Parameters.Count == 0);

			CilInstructionCollection processor = method.GetInstructions();
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

		private static MethodDefinition AddHashToStringOverride(this TypeDefinition type, TypeDefinition helperType)
		{
			MethodDefinition method = type.AddMethod(nameof(object.ToString), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, type.DeclaringModule!.CorLibTypeFactory.String);
			IMethodDefOrRef helperMethod = helperType.Methods.Single(m => m.Name == nameof(HashHelper.ToString));

			CilInstructionCollection processor = method.GetInstructions();
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
