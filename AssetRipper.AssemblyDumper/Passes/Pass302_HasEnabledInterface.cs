using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass302_HasEnabledInterface
	{
		public static void DoPass()
		{
			TypeDefinition hasEnabledInterface = MakeHasEnabledInterface();
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values)
			{
				DoPassOnGroup(group, hasEnabledInterface);
			}
		}

		private static TypeDefinition MakeHasEnabledInterface()
		{
			TypeDefinition @interface = InterfaceCreator.CreateEmptyInterface(SharedState.Instance.Module, SharedState.InterfacesNamespace, "IHasEnabled");
			@interface.AddFullProperty("Enabled", InterfaceUtils.InterfacePropertyDeclaration, SharedState.Instance.Importer.Boolean);
			return @interface;
		}

		private static void DoPassOnGroup(ClassGroup group, TypeDefinition hasEnabledInterface)
		{
			if (group.Types.All(t => t.TryGetEnabledField(out var _)))
			{
				group.Interface.AddInterfaceImplementation(hasEnabledInterface);

				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetEnabledField(out FieldDefinition? field))
					{
						if (type.Properties.Any(p => p.Name == "Enabled"))
						{
							throw new Exception("Already had an Enabled Property");
						}
						else
						{
							type.ImplementEnabledProperty(field);
						}
					}
					else
					{
						throw new Exception("Should never happen");
					}
				}
			}
			else
			{
				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetEnabledField(out FieldDefinition? field))
					{
						type.AddInterfaceImplementation(hasEnabledInterface);
						if (type.Properties.Any(p => p.Name == "Enabled"))
						{
							throw new Exception("Already had an Enabled Property");
						}
						else
						{
							type.ImplementEnabledProperty(field);
						}
					}
				}
			}
		}

		private static void ImplementEnabledProperty(this TypeDefinition type, FieldDefinition field)
		{
			string? fieldTypeName = field.GetFieldTypeName();
			if (fieldTypeName == "Boolean")
			{
				type.ImplementFullProperty("Enabled", InterfaceUtils.InterfacePropertyImplementation, SharedState.Instance.Importer.Boolean, field);
			}
			else if (fieldTypeName == "Byte")
			{
				type.ImplementBooleanPropertyForByteField("Enabled", field);
			}
			else
			{
				throw new NotSupportedException($"FieldType: {fieldTypeName}");
			}
		}

		private static bool TryGetEnabledField(this TypeDefinition type, [NotNullWhen(true)] out FieldDefinition? field)
		{
			field = type.TryGetFieldByName("m_Enabled");
			string? fieldTypeName = field?.GetFieldTypeName();
			return fieldTypeName == "Boolean" || fieldTypeName == "Byte";
		}

		private static string? GetFieldTypeName(this FieldDefinition field) => field.Signature?.FieldType.Name;

		private static void ImplementBooleanPropertyForByteField(this TypeDefinition type, string propertyName, FieldDefinition field)
		{
			PropertyDefinition property = type.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, SharedState.Instance.Importer.Boolean);
			CilInstructionCollection getProcessor = property.GetMethod!.CilMethodBody!.Instructions;
			getProcessor.Add(CilOpCodes.Ldarg_0);
			getProcessor.Add(CilOpCodes.Ldfld, field);
			getProcessor.Add(CilOpCodes.Ldc_I4_0);
			getProcessor.Add(CilOpCodes.Cgt_Un);
			getProcessor.Add(CilOpCodes.Ret);
			getProcessor.OptimizeMacros();

			CilInstructionCollection setProcessor = property.SetMethod!.CilMethodBody!.Instructions;
			CilInstructionLabel jumpTrue = new CilInstructionLabel();
			CilInstructionLabel jumpFalse = new CilInstructionLabel();
			setProcessor.Add(CilOpCodes.Ldarg_0);
			setProcessor.Add(CilOpCodes.Ldarg_1);
			setProcessor.Add(CilOpCodes.Brtrue, jumpTrue);
			setProcessor.Add(CilOpCodes.Ldc_I4_0);
			setProcessor.Add(CilOpCodes.Br, jumpFalse);
			jumpTrue.Instruction = setProcessor.Add(CilOpCodes.Ldc_I4_1);
			jumpFalse.Instruction = setProcessor.Add(CilOpCodes.Stfld, field);
			setProcessor.Add(CilOpCodes.Ret);
			setProcessor.OptimizeMacros();
		}
	}
}
