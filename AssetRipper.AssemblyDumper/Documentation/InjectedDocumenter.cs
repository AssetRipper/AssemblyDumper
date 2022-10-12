using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class InjectedDocumenter
	{
		private static readonly Dictionary<string, Dictionary<string, string>> subClassSummaries = new()
		{
			{ "SubMesh" , new()
				{
					{ "FirstByte" , "Offset in the index buffer." },
					{ "FirstVertex" , "Offset in the vertex list." },
				}
			},
		};

		public static void AddDocumentation()
		{
			foreach ((string subClass, Dictionary<string, string> documentationDictionary) in subClassSummaries)
			{
				SubclassGroup group = SharedState.Instance.SubclassGroups[subClass];
				AddDocumentationToType(group.Interface, documentationDictionary);
				foreach (TypeDefinition type in group.Types)
				{
					AddDocumentationToType(type, documentationDictionary);
				}
			}
		}

		private static void AddDocumentationToType(TypeDefinition type, Dictionary<string, string> documentationDictionary)
		{
			foreach ((string propertyName, string summary) in documentationDictionary)
			{
				PropertyDefinition property = type.Properties.First(p => p.Name == propertyName);
				DocumentationHandler.AddPropertyDefinitionLine(property, summary);
				if (type.TryGetFieldByName($"m_{propertyName}", out FieldDefinition? field))
				{
					DocumentationHandler.AddFieldDefinitionLine(field, summary);
				}
			}
		}
	}
}
