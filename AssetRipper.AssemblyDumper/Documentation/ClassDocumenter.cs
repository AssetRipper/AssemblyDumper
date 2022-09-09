using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using AssetRipper.Core.Utils;
using AssetRipper.DocExtraction.DataStructures;
using AssetRipper.DocExtraction.MetaData;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class ClassDocumenter
	{
		public static void AddClassDocumentation(GeneratedClassInstance instance)
		{
			if (instance.History is not null)
			{
				AddDocumentationFromHistory(instance, instance.History, SharedState.Instance.HistoryFile);
			}

			AddClassTypeDocumentation(instance);

			if (instance.Class.EditorRootNode is not null || instance.Class.ReleaseRootNode is not null)
			{
				foreach ((PropertyDefinition property, string? fieldName) in instance.PropertiesToFields)
				{
					AddPropertyDocumentation(instance, property, fieldName);
				}
			}
		}

		private static void AddClassTypeDocumentation(GeneratedClassInstance instance)
		{
			if (instance.ID >= 0)
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"Type ID: {instance.ID}");
			}

			if (instance.Type.Name != instance.Class.GetOriginalTypeName())
			{
				DocumentationHandler.AddTypeDefinitionLine(instance.Type, $"Original Name: \"{instance.Class.GetOriginalTypeName().EscapeXml()}\"");
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
			DocumentationHandler.AddTypeDefinitionLine(instance.Type, UnityVersionRangeUtils.GetUnityVersionRangeString(instance.VersionRange));
		}

		private static void AddPropertyDocumentation(GeneratedClassInstance instance, PropertyDefinition property, string? fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Not present in this version range");
				return;
			}

			UniversalNode? releaseNode = instance.GetReleaseFieldByName(fieldName);
			UniversalNode? editorNode = instance.GetEditorFieldByName(fieldName);
			UniversalNode mainNode = releaseNode ?? editorNode ?? throw new Exception($"In {instance.Name}, could not find nodes for {fieldName}");

			DocumentationHandler.AddPropertyDefinitionLine(property, $"Field name: {fieldName}");

			if (mainNode.Name != mainNode.OriginalName)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, $"Original field name: \"{XmlUtils.EscapeXmlInvalidCharacters(mainNode.OriginalName)}\"");
			}

			if (mainNode.TypeName != mainNode.OriginalTypeName)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, $"Original Type: \"{XmlUtils.EscapeXmlInvalidCharacters(mainNode.OriginalTypeName)}\"");
			}

			DocumentationHandler.AddPropertyDefinitionLine(property, $"Ascii Crc: {CrcUtils.CalculateDigestAscii(mainNode.OriginalName)}");

			if (releaseNode is null)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Editor Only");
			}
			else
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, $"Release Flags: {GetMetaFlagString(releaseNode.MetaFlag)}");
			}

			if (editorNode is null)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Release Only");
			}
			else
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, $"Editor Flags: {GetMetaFlagString(editorNode.MetaFlag)}");
			}
		}

		private static UniversalNode? GetReleaseFieldByName(this GeneratedClassInstance instance, string fieldName)
		{
			return instance.Class.ReleaseRootNode?.SubNodes.SingleOrDefault(n => n.Name == fieldName);
		}

		private static UniversalNode? GetEditorFieldByName(this GeneratedClassInstance instance, string fieldName)
		{
			return instance.Class.EditorRootNode?.SubNodes.SingleOrDefault(n => n.Name == fieldName);
		}

		private static string GetMetaFlagString(uint flag)
		{
			return string.Join(" | ", ((TransferMetaFlags)flag).Split());
		}

		private static void AddDocumentationFromHistory(GeneratedClassInstance instance, ComplexTypeHistory history, HistoryFile historyFile)
		{
			VersionedListDocumenter.AddSet(instance.Type, history.NativeName.GetSubList(instance.VersionRange), "Native Name: ");
			VersionedListDocumenter.AddSet(instance.Type, history.DocumentationString.GetSubList(instance.VersionRange), "Summary: ");
			VersionedListDocumenter.AddList(instance.Type, history.ObsoleteMessage.GetSubList(instance.VersionRange), "Obsolete Message: ");

			foreach ((_, DataMemberHistory memberHistory) in history.GetAllMembers(instance.VersionRange.Start, historyFile))
			{
				string fieldName = Passes.Pass002_RenameSubnodes.GetValidFieldName(memberHistory.Name);
				if (instance.FieldsToProperties.TryGetValue(fieldName, out PropertyDefinition? property))
				{
					VersionedList<string> nativeNameSubList = memberHistory.NativeName.GetSubList(instance.VersionRange);
					VersionedList<FullName> managedTypeSubList = memberHistory.TypeFullName.GetSubList(instance.VersionRange);
					VersionedList<string> docStringSubList = memberHistory.DocumentationString.GetSubList(instance.VersionRange);
					VersionedList<string> obsoleteMessageSubList = memberHistory.ObsoleteMessage.GetSubList(instance.VersionRange);

					VersionedListDocumenter.AddSet(property, nativeNameSubList, "Native Name: ");
					VersionedListDocumenter.AddList(property, managedTypeSubList, "Managed Type: ");
					VersionedListDocumenter.AddSet(property, docStringSubList, "Summary: ");
					VersionedListDocumenter.AddList(property, obsoleteMessageSubList, "Obsolete Message: ");

					if (instance.Type.TryGetFieldByName(fieldName, out FieldDefinition? field))
					{
						VersionedListDocumenter.AddSet(field, nativeNameSubList, "Native Name: ");
						VersionedListDocumenter.AddList(field, managedTypeSubList, "Managed Type: ");
						VersionedListDocumenter.AddSet(field, docStringSubList, "Summary: ");
						VersionedListDocumenter.AddList(field, obsoleteMessageSubList, "Obsolete Message: ");
					}
				}
			}
		}
	}
}
