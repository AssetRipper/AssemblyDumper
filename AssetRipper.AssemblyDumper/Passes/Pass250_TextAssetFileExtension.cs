using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass250_TextAssetFileExtension
	{
		public static void DoPass()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[49];
			AddNullableStringProperty(group, "FileExtension");
		}

		private static void AddNullableStringProperty(ClassGroup group, string propertyName)
		{
			PropertyDefinition interfaceProperty = group.Interface.AddFullProperty(
				propertyName,
				InterfaceUtils.InterfacePropertyDeclaration,
				SharedState.Instance.Importer.String);
			interfaceProperty.AddNullableAttributesForMaybeNull();

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				PropertyDefinition instanceProperty = instance.Type.ImplementFullAutoProperty(
					propertyName,
					InterfaceUtils.InterfacePropertyImplementation,
					SharedState.Instance.Importer.String,
					SharedState.Instance.Importer,
					out FieldDefinition field);
				instanceProperty.AddNullableAttributesForMaybeNull();
				field.AddNullableAttribute(NullableAnnotation.MaybeNull);
			}
		}
	}
}
