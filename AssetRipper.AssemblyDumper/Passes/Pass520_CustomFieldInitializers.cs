using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass520_CustomFieldInitializers
	{
		private static readonly List<(ClassGroupBase, string, ElementType, IConvertible)> definitions = new();

		public static void DoPass()
		{
			GenerateDefinitions();
			ModifyGroups();
			definitions.Clear();
		}

		private static void GenerateDefinitions()
		{
			Dictionary<int, ClassGroup> objectClassDictionary = SharedState.Instance.ClassGroups;
			Dictionary<string, SubclassGroup> subClassDictionary = SharedState.Instance.SubclassGroups;

			//Texture2D
			AddDefinition(objectClassDictionary[28], "m_AlphaIsTransparency", ElementType.Boolean, true);
		}

		private static void AddDefinition(ClassGroupBase group, string fieldName, ElementType elementType, IConvertible value)
		{
			definitions.Add((group, fieldName, elementType, value));
		}

		private static void ModifyGroups()
		{
			foreach ((ClassGroupBase group, string fieldName, ElementType elementType, IConvertible value) in definitions)
			{
				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetFieldByName(fieldName, out FieldDefinition? field, false))
					{
						CorLibTypeSignature? fieldType = field.Signature?.FieldType as CorLibTypeSignature;
						if (fieldType is not null && fieldType.ElementType == elementType)
						{
							type.AppendInitializerToConstructors(field, elementType, value);
						}
					}
				}
			}
		}

		private static void AppendInitializerToConstructors(this TypeDefinition type, FieldDefinition field, ElementType elementType, IConvertible value)
		{
			foreach (MethodDefinition constructor in type.GetInstanceConstructors())
			{
				constructor.GetProcessor().AppendInitializer(field, elementType, value);
			}
		}

		private static void AppendInitializer(this CilInstructionCollection processor, FieldDefinition field, ElementType elementType, IConvertible value)
		{
			processor.Pop();//the return instruction
			processor.Add(CilOpCodes.Ldarg_0);
			switch (elementType)
			{
				case ElementType.Boolean:
					processor.Add(CilOpCodes.Ldc_I4, (bool)value ? 1 : 0);
					break;
				case ElementType.Char:
					processor.Add(CilOpCodes.Ldc_I4, (char)value);
					break;
				case ElementType.I1:
					processor.Add(CilOpCodes.Ldc_I4, (sbyte)value);
					break;
				case ElementType.I2:
					processor.Add(CilOpCodes.Ldc_I4, (short)value);
					break;
				case ElementType.I4:
					processor.Add(CilOpCodes.Ldc_I4, (int)value);
					break;
				case ElementType.I8:
					processor.Add(CilOpCodes.Ldc_I8, (long)value);
					break;
				case ElementType.U1:
					processor.Add(CilOpCodes.Ldc_I4, (byte)value);
					break;
				case ElementType.U2:
					processor.Add(CilOpCodes.Ldc_I4, (ushort)value);
					break;
				case ElementType.U4:
					processor.Add(CilOpCodes.Ldc_I4, unchecked((int)(uint)value));
					break;
				case ElementType.U8:
					processor.Add(CilOpCodes.Ldc_I8, (ulong)value);
					break;
				case ElementType.R4:
					processor.Add(CilOpCodes.Ldc_R4, (float)value);
					break;
				case ElementType.R8:
					processor.Add(CilOpCodes.Ldc_R8, (double)value);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(elementType), $"{nameof(ElementType)} {elementType} not supported");
			}
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}
	}
}
