using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass203_OffsetPtrImplicitConversions
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			foreach ((string name, SubclassGroup group) in SharedState.Instance.SubclassGroups)
			{
				if (name.StartsWith("OffsetPtr"))
				{
					foreach (TypeDefinition type in group.Types)
					{
						type.AddImplicitConversion();
					}
				}
			}
		}

		private static void AddImplicitConversion(this TypeDefinition type)
		{
			FieldDefinition field = type.GetField();

			MethodDefinition implicitMethod = type.AddMethod("op_Implicit", ConversionAttributes, field.Signature!.FieldType);
			implicitMethod.AddParameter(type.ToTypeSignature(), "value");

			CilInstructionCollection processor = implicitMethod.CilMethodBody!.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Ret);
		}

		private static FieldDefinition GetField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == "m_Data");
		}
	}
}
