using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.DataStructures;
using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass055_CreateEnumProperties
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
				{
					if (interfaceProperty.History is not null && interfaceProperty.History.TypeFullName.Count == 1)
					{
						string fullName = interfaceProperty.History.TypeFullName[0].Value.ToString();
						if (Pass040_AddEnums.EnumDictionary.TryGetValue(fullName, out (TypeDefinition, EnumHistory) tuple))
						{
							(TypeDefinition type, EnumHistory enumHistory) = tuple;
							ElementType enumElementType = ((CorLibTypeSignature)type.GetFieldByName("value__").Signature!.FieldType).ElementType;
							string propertyName = $"{interfaceProperty.Definition.Name}E";

							interfaceProperty.SpecialDefinition = group.Interface.AddFullProperty(
								propertyName,
								InterfaceUtils.InterfacePropertyDeclaration,
								type.ToTypeSignature());

							foreach (ClassProperty classProperty in interfaceProperty.Implementations)
							{
								if (classProperty.BackingField?.Signature?.FieldType is CorLibTypeSignature fieldTypeSignature
									&& fieldTypeSignature.ElementType.IsFixedSizeInteger())
								{
									classProperty.SpecialDefinition = classProperty.Class.Type.AddFullProperty(
										propertyName,
										InterfaceUtils.InterfacePropertyImplementation,
										type.ToTypeSignature());
									classProperty.SpecialDefinition.GetMethod!.GetProcessor().FillGetter(classProperty.BackingField, fieldTypeSignature.ElementType, enumElementType);
									classProperty.SpecialDefinition.SetMethod!.GetProcessor().FillSetter(classProperty.BackingField, fieldTypeSignature.ElementType, enumElementType);
								}
								else
								{
									classProperty.SpecialDefinition = classProperty.Class.Type.ImplementFullProperty(
										propertyName,
										InterfaceUtils.InterfacePropertyImplementation,
										type.ToTypeSignature(),
										null);
								}
							}
						}
					}
				}
			}
		}

		private static CilInstruction? AddConversion(this CilInstructionCollection processor, ElementType from, ElementType to)
		{
			if (from == to)
			{
				return null;
			}

			CilOpCode opCode = to switch
			{
				//ElementType.I1 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I1 : CilOpCodes.Conv_Ovf_I1_Un,
				//ElementType.U1 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U1 : CilOpCodes.Conv_Ovf_U1_Un,
				//ElementType.I2 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I2 : CilOpCodes.Conv_Ovf_I2_Un,
				//ElementType.U2 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U2 : CilOpCodes.Conv_Ovf_U2_Un,
				//ElementType.I4 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I4 : CilOpCodes.Conv_Ovf_I4_Un,
				//ElementType.U4 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U4 : CilOpCodes.Conv_Ovf_U4_Un,
				//ElementType.I8 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I8 : CilOpCodes.Conv_Ovf_I8_Un,
				//ElementType.U8 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U8 : CilOpCodes.Conv_Ovf_U8_Un,
				ElementType.I1 => CilOpCodes.Conv_I1,
				ElementType.U1 => CilOpCodes.Conv_U1,
				ElementType.I2 => CilOpCodes.Conv_I2,
				ElementType.U2 => CilOpCodes.Conv_U2,
				ElementType.I4 => CilOpCodes.Conv_I4,
				ElementType.U4 => CilOpCodes.Conv_U4,
				ElementType.I8 => CilOpCodes.Conv_I8,
				ElementType.U8 => CilOpCodes.Conv_U8,
				_ => throw new ArgumentOutOfRangeException(nameof(to)),
			};

			return processor.Add(opCode);
		}

		private static void FillGetter(this CilInstructionCollection processor, FieldDefinition field, ElementType fieldType, ElementType enumType)
		{
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.AddConversion(fieldType, enumType);
			processor.Add(CilOpCodes.Ret);
		}

		private static void FillSetter(this CilInstructionCollection processor, FieldDefinition field, ElementType fieldType, ElementType enumType)
		{
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddConversion(enumType, fieldType);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
