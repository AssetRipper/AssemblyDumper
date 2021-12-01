using AssetRipper.Core.Classes.Misc;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass21_GuidImplicitConversion
	{
		public static void DoPass()
		{
			Logger.Info("Pass 21: GUID Implicit Conversion");
			if(SharedState.TypeDictionary.TryGetValue("GUID", out TypeDefinition guidType))
			{
				AddGuidConversion(guidType);
			}
		}

		private static void AddGuidConversion(TypeDefinition guidType)
		{
			TypeReference commonGuidType = SharedState.Module.ImportCommonType<UnityGUID>();
			MethodReference constructor = SharedState.Module.ImportCommonConstructor<UnityGUID>(4);

			FieldDefinition data0 = guidType.Fields.Single(field => field.Name == "data[0]");
			FieldDefinition data1 = guidType.Fields.Single(field => field.Name == "data[1]");
			FieldDefinition data2 = guidType.Fields.Single(field => field.Name == "data[2]");
			FieldDefinition data3 = guidType.Fields.Single(field => field.Name == "data[3]");

			var implicitMethod = new MethodDefinition("op_Implicit", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, SystemTypeGetter.Void);
			implicitMethod.ReturnType = commonGuidType;
			guidType.Methods.Add(implicitMethod);
			implicitMethod.Body.InitLocals = true;
			var processor = implicitMethod.Body.GetILProcessor();

			var value = new ParameterDefinition("value", ParameterAttributes.None, guidType);
			implicitMethod.Parameters.Add(value);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, data0);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, data1);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, data2);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, data3);
			processor.Emit(OpCodes.Newobj, constructor);
			processor.Emit(OpCodes.Ret);
		}
	}
}
