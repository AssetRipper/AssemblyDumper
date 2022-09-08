using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core.Classes;
using AssetRipper.Core.Interfaces;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass300_HasNameInterface
	{
		private const string Utf8PropertyName = "Name";
		private const string StringPropertyName = nameof(IHasNameString.NameString);

		private static string Utf8StringName => Pass002_RenameSubnodes.Utf8StringName;

		public static void DoPass()
		{
			TypeSignature utf8StringSignature = SharedState.Instance.SubclassGroups[Utf8StringName].Instances.Single().Type.ToTypeSignature();
			TypeDefinition hasNameInterface = MakeHasNameInterface(utf8StringSignature);
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				DoPassOnGroup(group, hasNameInterface, utf8StringSignature);
			}
		}

		private static TypeDefinition MakeHasNameInterface(TypeSignature utf8StringSignature)
		{
			TypeDefinition @interface = InterfaceCreator.CreateEmptyInterface(SharedState.Instance.Module, SharedState.InterfacesNamespace, "IHasName");
			@interface.AddGetterProperty(Utf8PropertyName, InterfaceUtils.InterfacePropertyDeclaration, utf8StringSignature);
			@interface.AddInterfaceImplementation<IHasNameString>(SharedState.Instance.Importer);
			return @interface;
		}

		private static void DoPassOnGroup(ClassGroupBase group, TypeDefinition hasNameInterface, TypeSignature utf8StringSignature)
		{
			if (group.Types.All(t => t.TryGetNameField(true, out var _)))
			{
				TypeDefinition groupInterface = group.Interface;
				groupInterface.AddInterfaceImplementation(hasNameInterface);
				if (groupInterface.Properties.Any(p => p.Name == Utf8PropertyName))
				{
					throw new Exception("Interface already has a name property");
				}

				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetNameField(false, out FieldDefinition? field))
					{
						type.ImplementNameProperties(field, utf8StringSignature);
					}
				}
			}
			else
			{
				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetNameField(false, out FieldDefinition? field))
					{
						type.AddInterfaceImplementation(hasNameInterface);
						type.ImplementNameProperties(field, utf8StringSignature);
					}
				}
			}
		}

		private static void ImplementNameProperties(this TypeDefinition type, FieldDefinition field, TypeSignature utf8StringSignature)
		{
			if (!type.Properties.Any(p => p.Name == Utf8PropertyName))
			{
				type.ImplementGetterProperty(Utf8PropertyName, InterfaceUtils.InterfacePropertyImplementation, utf8StringSignature, field);
			}
			if (!type.Properties.Any(p => p.Name == StringPropertyName))
			{
				type.ImplementStringProperty(StringPropertyName, InterfaceUtils.InterfacePropertyImplementation, field);
			}
		}

		private static PropertyDefinition ImplementStringProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, FieldDefinition field)
		{
			PropertyDefinition property = type.AddFullProperty(propertyName, methodAttributes, SharedState.Instance.Importer.String);

			IMethodDefOrRef getRef = SharedState.Instance.Importer.ImportMethod<Utf8StringBase>(m => m.Name == $"get_{nameof(Utf8StringBase.String)}");
			CilInstructionCollection getProcessor = property.GetMethod!.CilMethodBody!.Instructions;
			getProcessor.Add(CilOpCodes.Ldarg_0);
			getProcessor.Add(CilOpCodes.Ldfld, field);
			getProcessor.Add(CilOpCodes.Call, getRef);
			getProcessor.Add(CilOpCodes.Ret);

			IMethodDefOrRef setRef = SharedState.Instance.Importer.ImportMethod<Utf8StringBase>(m => m.Name == $"set_{nameof(Utf8StringBase.String)}");
			CilInstructionCollection setProcessor = property.SetMethod!.CilMethodBody!.Instructions;
			setProcessor.Add(CilOpCodes.Ldarg_0);
			setProcessor.Add(CilOpCodes.Ldfld, field);
			setProcessor.Add(CilOpCodes.Ldarg_1);
			setProcessor.Add(CilOpCodes.Call, setRef);
			setProcessor.Add(CilOpCodes.Ret);

			return property;
		}

		private static bool TryGetNameField(this TypeDefinition type, bool checkBaseTypes, [NotNullWhen(true)] out FieldDefinition? field)
		{
			field = type.TryGetFieldByName("m_Name", checkBaseTypes);
			return field?.Signature?.FieldType.Name == Utf8StringName;
		}
	}
}
