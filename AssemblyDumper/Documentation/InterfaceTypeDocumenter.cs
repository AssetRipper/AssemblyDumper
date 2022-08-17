namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class InterfaceTypeDocumenter
	{
		public static void AddInterfaceTypeDocumentation(ClassGroupBase group)
		{
			if (group is ClassGroup classGroup)
			{
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, $"Interface for the {string.Join(", ", classGroup.Names)} classes.");
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, $"Type ID: {classGroup.ID}");
			}
			else
			{
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, $"Interface for the {group.Name} classes.");
			}
			if (group.Instances.All(instance => instance.Class.IsReleaseOnly))
			{
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, "Release Only");
			}
			if (group.Instances.All(instance => instance.Class.IsEditorOnly))
			{
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, "Editor Only");
			}
			DocumentationHandler.AddTypeDefinitionLine(group.Interface, GetSerializedVersionString(group));
			DocumentationHandler.AddTypeDefinitionLine(group.Interface, GetUnityVersionString(group));
		}

		private static string GetUnityVersionString(ClassGroupBase group)
		{
			return group.Instances
				.Select(instance => instance.VersionRange)
				.GetUnionedRanges()
				.GetString();
		}

		private static string GetSerializedVersionString(ClassGroupBase group)
		{
			group.GetSerializedVersions(out int minimum, out int maximum);
			return minimum == maximum 
				? $"Serialized Version: {minimum}" 
				: $"Serialized Versions: {minimum} to {maximum}";
		}
	}
}
