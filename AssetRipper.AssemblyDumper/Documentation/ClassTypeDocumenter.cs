using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class ClassTypeDocumenter
	{
		public static void AddClassTypeDocumentation(GeneratedClassInstance instance)
		{
			if (instance.Type.Name != instance.Class.GetOriginalTypeName())
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"Original Name: \"{XmlUtils.EscapeXmlInvalidCharacters(instance.Class.GetOriginalTypeName())}\"");
			}

			if (instance.ID >= 0)
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"Type ID: {instance.ID}");
			}

			DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"Serialized Version: {instance.GetSerializedVersion()}");

			if (instance.Class.IsReleaseOnly)
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, "Release Only");
			}

			if (instance.Class.IsEditorOnly)
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, "Editor Only");
			}

			if (instance.Class.IsStripped)
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, "Stripped");
			}
			DocumentationHandler.AddTypeDefinitionLine(instance.Type, GetUnityVersionRangeString(instance.VersionRange));
		}

		private static string GetUnityVersionRangeString(Range<UnityVersion> range)
		{
			string start = range.Start == UnityVersion.MinVersion || range.Start == SharedState.Instance.MinVersion
				? "Min"
				: range.Start.ToString();
			string end = range.End == UnityVersion.MaxVersion
				? "Max"
				: range.End.ToString();
			return $"{start} to {end}";
		}
	}
}
