using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using AssetRipper.Core.Classes.Misc;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass21_GuidImplicitConversion
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 21: GUID Implicit Conversion");
			if(SharedState.TypeDictionary.TryGetValue("GUID", out TypeDefinition guidType))
			{
				AddGuidConversion(guidType);
			}
		}

		private static void AddGuidConversion(TypeDefinition guidType)
		{
			ITypeDefOrRef commonGuidType = SharedState.Importer.ImportCommonType<UnityGUID>();
			IMethodDefOrRef constructor = SharedState.Importer.ImportCommonConstructor<UnityGUID>(4);

			FieldDefinition data0 = guidType.Fields.Single(field => field.Name == "data_0_");
			FieldDefinition data1 = guidType.Fields.Single(field => field.Name == "data_1_");
			FieldDefinition data2 = guidType.Fields.Single(field => field.Name == "data_2_");
			FieldDefinition data3 = guidType.Fields.Single(field => field.Name == "data_3_");

			MethodDefinition implicitMethod = guidType.AddMethod("op_Implicit", ConversionAttributes, commonGuidType);
			implicitMethod.AddParameter("value", guidType);

			implicitMethod.CilMethodBody.InitializeLocals = true;
			var processor = implicitMethod.CilMethodBody.Instructions;

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
	}
}
