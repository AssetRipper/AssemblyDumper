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
				AddDocumentationToType(group.Interface, documentationDictionary);
				foreach (TypeDefinition type in group.Types)
				{
					AddDocumentationToType(type, documentationDictionary);
				}
			}
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
