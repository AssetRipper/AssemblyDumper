using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass23_OffsetPtrImplicitConversions
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 23: OffsetPtr Implicit Conversions");

			foreach ((string name, TypeDefinition type) in SharedState.TypeDictionary)
			{
				if (name.StartsWith("OffsetPtr"))
				{
					type.AddImplicitConversion();
				}
			}
		}

		private static void AddImplicitConversion(this TypeDefinition type)
		{
			FieldDefinition field = type.GetField();
			MethodDefinition method = new MethodDefinition("op_Implicit", ConversionAttributes, field.FieldType);
			type.Methods.Add(method);

			var value = new ParameterDefinition("value", ParameterAttributes.None, type);
			method.Parameters.Add(value);

			var processor = method.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, field);
			processor.Emit(OpCodes.Ret);
		}

		private static FieldDefinition GetField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == "data");
		}
	}
}
