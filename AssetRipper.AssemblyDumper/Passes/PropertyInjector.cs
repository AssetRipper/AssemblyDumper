using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class PropertyInjector
	{
		public static void InjectFullProperty(ClassGroupBase group, TypeSignature propertySignature, string propertyName, bool nullable)
		{
			PropertyDefinition interfaceProperty = group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertySignature);
			if (nullable)
			{
				interfaceProperty.AddNullableAttributesForMaybeNull();
			}

			foreach (TypeDefinition type in group.Types)
			{
				FieldDefinition field = type.AddField(propertySignature, $"m_{propertyName}");
				PropertyDefinition property = type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, null, field);
				if (nullable)
				{
					field.AddNullableAttributesForMaybeNull();
					property.AddNullableAttributesForMaybeNull();
				}
			}
		}
	}
}
