﻿using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Passes;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class InjectedDocumenter
	{
		public static void AddDocumentation()
		{
			Dictionary<int, string> classTypeSummaries = new()
			{
			};
			Dictionary<int, Dictionary<string, string>> classPropertySummaries = new()
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
						{ "RootOrder_C4" , $"The index of this {SeeXmlTagGenerator.MakeCRefForClassInterface(4)} in its father's children. If a transform has no father, its root order should be 0." },
					}
				},
			};
			Dictionary<string, string> subClassTypeSummaries = new()
			{
				{ "SceneObjectIdentifier" , $"A subset of {SeeXmlTagGenerator.MakeHRef(@"https://docs.unity3d.com/ScriptReference/GlobalObjectId.html", "GlobalObjectId")}." },
			};
			Dictionary<string, Dictionary<string, string>> subClassPropertySummaries = new()
			{
				{ "SceneObjectIdentifier" , new()
					{
						{ "TargetObject" , $"The local file ID of the object." },
						{ "TargetPrefab" , "The prefab instance id of the object. For normal game objects, this prefab id is 0." },
						{ Pass508_LazySceneObjectIdentifier.TargetObjectName , "An object in the scene to be referenced. If not null, it will replace TargetObject during Yaml export." },
						{ Pass508_LazySceneObjectIdentifier.TargetPrefabName , "A prefab instance to be referenced. If not null, it will replace TargetPrefab during Yaml export." },
					}
				},
				{ "SubMesh" , new()
					{
						{ "FirstByte" , "Offset in the index buffer." },
						{ "FirstVertex" , "Offset in the vertex list." },
					}
				},
			};
			AddDocumentationForDictionaries(classTypeSummaries, classPropertySummaries, subClassTypeSummaries, subClassPropertySummaries);
		}

		private static void AddDocumentationForDictionaries(
			Dictionary<int, string> classTypeSummaries,
			Dictionary<int, Dictionary<string, string>> classPropertySummaries,
			Dictionary<string, string> subClassTypeSummaries,
			Dictionary<string, Dictionary<string, string>> subClassPropertySummaries)
		{
			foreach ((int id, string summary) in classTypeSummaries)
			{
				ClassGroup group = SharedState.Instance.ClassGroups[id];
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, summary);
				foreach (TypeDefinition type in group.Types)
				{
					DocumentationHandler.AddTypeDefinitionLine(type, summary);
				}
			}
			foreach ((int id, Dictionary<string, string> documentationDictionary) in classPropertySummaries)
			{
				ClassGroup group = SharedState.Instance.ClassGroups[id];
				AddDocumentationToGroup(group, documentationDictionary);
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					AddDocumentationToInstance(instance, documentationDictionary);
				}
			}
			foreach ((string subClass, string summary) in subClassTypeSummaries)
			{
				SubclassGroup group = SharedState.Instance.SubclassGroups[subClass];
				DocumentationHandler.AddTypeDefinitionLine(group.Interface, summary);
				foreach (TypeDefinition type in group.Types)
				{
					DocumentationHandler.AddTypeDefinitionLine(type, summary);
				}
			}
			foreach ((string subClass, Dictionary<string, string> documentationDictionary) in subClassPropertySummaries)
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
