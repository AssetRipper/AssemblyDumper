using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.IO.Files;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass201_GuidConversionOperators
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
		public static void DoPass()
		{
			foreach (TypeDefinition type in SharedState.Instance.SubclassGroups["GUID"].Types)
			{
				AddImplicitConversion(type);
				AddExplicitConversion(type);
			}
		}

		private static void AddImplicitConversion(TypeDefinition guidType)
		{
			ITypeDefOrRef commonGuidType = SharedState.Instance.Importer.ImportType<UnityGUID>();
			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportConstructor<UnityGUID>(4);

			FieldDefinition data0 = guidType.Fields.Single(field => field.Name == "m_Data_0_");
			FieldDefinition data1 = guidType.Fields.Single(field => field.Name == "m_Data_1_");
			FieldDefinition data2 = guidType.Fields.Single(field => field.Name == "m_Data_2_");
			FieldDefinition data3 = guidType.Fields.Single(field => field.Name == "m_Data_3_");

			MethodDefinition implicitMethod = guidType.AddMethod("op_Implicit", ConversionAttributes, commonGuidType.ToTypeSignature());
			implicitMethod.AddParameter(guidType.ToTypeSignature(), "value");

			implicitMethod.CilMethodBody!.InitializeLocals = true;
			CilInstructionCollection processor = implicitMethod.CilMethodBody.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, data0);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, data1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, data2);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, data3);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddExplicitConversion(TypeDefinition guidType)
		{
			ITypeDefOrRef commonGuidType = SharedState.Instance.Importer.ImportType<UnityGUID>();
			IMethodDefOrRef constructor = guidType.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0 && !m.IsStatic);

			FieldDefinition data0 = guidType.Fields.Single(field => field.Name == "m_Data_0_");
			FieldDefinition data1 = guidType.Fields.Single(field => field.Name == "m_Data_1_");
			FieldDefinition data2 = guidType.Fields.Single(field => field.Name == "m_Data_2_");
			FieldDefinition data3 = guidType.Fields.Single(field => field.Name == "m_Data_3_");

			IMethodDefOrRef getData0 = SharedState.Instance.Importer.ImportMethod<UnityGUID>(m => m.Name == $"get_{nameof(UnityGUID.Data0)}");
			IMethodDefOrRef getData1 = SharedState.Instance.Importer.ImportMethod<UnityGUID>(m => m.Name == $"get_{nameof(UnityGUID.Data1)}");
			IMethodDefOrRef getData2 = SharedState.Instance.Importer.ImportMethod<UnityGUID>(m => m.Name == $"get_{nameof(UnityGUID.Data2)}");
			IMethodDefOrRef getData3 = SharedState.Instance.Importer.ImportMethod<UnityGUID>(m => m.Name == $"get_{nameof(UnityGUID.Data3)}");

			MethodDefinition explicitMethod = guidType.AddMethod("op_Explicit", ConversionAttributes, guidType.ToTypeSignature());
			Parameter parameter = explicitMethod.AddParameter(commonGuidType.ToTypeSignature(), "value");

			CilInstructionCollection processor = explicitMethod.CilMethodBody!.Instructions;

			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga, parameter);
			processor.Add(CilOpCodes.Call, getData0);
			processor.Add(CilOpCodes.Stfld, data0);
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga, parameter);
			processor.Add(CilOpCodes.Call, getData1);
			processor.Add(CilOpCodes.Stfld, data1);
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga, parameter);
			processor.Add(CilOpCodes.Call, getData2);
			processor.Add(CilOpCodes.Stfld, data2);
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga, parameter);
			processor.Add(CilOpCodes.Call, getData3);
			processor.Add(CilOpCodes.Stfld, data3);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
