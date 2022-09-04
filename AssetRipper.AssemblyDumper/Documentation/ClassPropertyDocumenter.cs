using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using AssetRipper.Core.Utils;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class ClassPropertyDocumenter
	{
		public static void AddClassPropertyDocumentation(GeneratedClassInstance instance)
		{
			if (instance.Class.EditorRootNode is null && instance.Class.ReleaseRootNode is null)
			{
				//Console.WriteLine($"Skipping documentation for {instance} because both root nodes are null");
				return;
			}
			foreach ((PropertyDefinition property, string? fieldName) in instance.PropertiesToFields)
			{
				AddPropertyDocumentation(instance, property, fieldName);
			}
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
	}
}
