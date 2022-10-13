using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Passes;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class InjectedDocumenter
	{
		public static void AddDocumentation()
		{
			Dictionary<int, Dictionary<string, string>> classSummaries = new()
			{
				{ 1032 , new()
					{
						{ Pass507_InjectedProperties.TargetSceneName , "The scene this asset references." },
					}
				},
				{ 28 , new()
					{
						{ Pass507_InjectedProperties.TerrainDataName , "The terrain data that references this texture." },
					}
				},
				{ 4 , new()
					{
						{ "RootOrder_C4" , $"The index of this {SeeXmlTagGenerator.MakeCRefForClassInterface(4)} in its father's children." },
					}
				},
			};
			Dictionary<string, Dictionary<string, string>> subClassSummaries = new()
			{
				{ "SubMesh" , new()
					{
						{ "FirstByte" , "Offset in the index buffer." },
						{ "FirstVertex" , "Offset in the vertex list." },
					}
				},
			};
			AddDocumentationForDictionaries(classSummaries, subClassSummaries);
		}

		private static void AddDocumentationForDictionaries(Dictionary<int, Dictionary<string, string>> classSummaries, Dictionary<string, Dictionary<string, string>> subClassSummaries)
		{
			foreach ((int id, Dictionary<string, string> documentationDictionary) in classSummaries)
			{
				ClassGroup group = SharedState.Instance.ClassGroups[id];
				AddDocumentationToGroup(group, documentationDictionary);
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					AddDocumentationToInstance(instance, documentationDictionary);
				}
			}
			foreach ((string subClass, Dictionary<string, string> documentationDictionary) in subClassSummaries)
			{
				SubclassGroup group = SharedState.Instance.SubclassGroups[subClass];
				AddDocumentationToGroup(group, documentationDictionary);
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					AddDocumentationToInstance(instance, documentationDictionary);
				}
				
			}
		}

		private static void AddDocumentationToInstance(GeneratedClassInstance instance, Dictionary<string, string> documentationDictionary)
		{
			TypeDefinition type = instance.Type;
			foreach ((string propertyName, string summary) in documentationDictionary)
			{
				ClassProperty? classProperty = instance.Properties.FirstOrDefault(p => p.Definition.Name == propertyName);
				if (classProperty is not null)
				{
					DocumentationHandler.AddPropertyDefinitionLine(classProperty.Definition, summary);
					if (classProperty.SpecialDefinition is not null)
					{
						DocumentationHandler.AddPropertyDefinitionLine(classProperty.SpecialDefinition, summary);
					}
					if (classProperty.BackingField is not null)
					{
						DocumentationHandler.AddFieldDefinitionLine(classProperty.BackingField, summary);
					}
				}
				else
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

		private static void AddDocumentationToGroup(ClassGroupBase group, Dictionary<string, string> documentationDictionary)
		{
			foreach ((string propertyName, string summary) in documentationDictionary)
			{
				InterfaceProperty? classProperty = group.InterfaceProperties.FirstOrDefault(p => p.Definition.Name == propertyName);
				if (classProperty is not null)
				{
					DocumentationHandler.AddPropertyDefinitionLine(classProperty.Definition, summary);
					if (classProperty.SpecialDefinition is not null)
					{
						DocumentationHandler.AddPropertyDefinitionLine(classProperty.SpecialDefinition, summary);
					}
				}
				else
				{
					PropertyDefinition property = group.Interface.Properties.First(p => p.Name == propertyName);
					DocumentationHandler.AddPropertyDefinitionLine(property, summary);
				}
			}
		}
	}
}
