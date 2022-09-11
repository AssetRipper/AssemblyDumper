﻿using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static partial class InterfaceDocumenter
	{
		[Flags]
		private enum FieldPresense : byte
		{
			Normal = 0,
			SometimesReleaseOnly = 1,
			SometimesEditorOnly = 2,
			AlwaysReleaseOnly = 4,
			AlwaysEditorOnly = 8,
		}

		private static void AddInterfacePropertyDocumentation(ClassGroupBase group)
		{
			Dictionary<PropertyDefinition, FieldPresense> interfacePropertyData = GetReleaseAndEditorOnlyData(group);

			foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
			{
				AddInterfacePropertyDocumentation(
					interfaceProperty.Definition,
					interfaceProperty.HasMethod is not null,
					interfaceProperty.Definition.IsValueType(),
					interfacePropertyData[interfaceProperty.Definition]);

				if (interfaceProperty.HasMethod is not null)
				{
					string versionString = interfaceProperty.PresentRange.GetString();
					DocumentationHandler.AddMethodDefinitionLine(interfaceProperty.HasMethod, versionString);
					DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty.Definition, versionString);
				}
			}
		}

		private static void AddInterfacePropertyDocumentation(PropertyDefinition property, bool hasMethodExists, bool isValueType, FieldPresense presense)
		{
			string str = hasMethodExists
				? isValueType
					? "Maybe absent"
					: "Maybe null"
				: isValueType
					? "Not absent"
					: "Not null";

			DocumentationHandler.AddPropertyDefinitionLine(property, str);

			if ((presense & FieldPresense.AlwaysReleaseOnly) != 0)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Release Only");
			}
			else if ((presense & FieldPresense.SometimesReleaseOnly) != 0)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Sometimes Release Only");
			}
			if ((presense & FieldPresense.AlwaysEditorOnly) != 0)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Editor Only");
			}
			else if ((presense & FieldPresense.SometimesEditorOnly) != 0)
			{
				DocumentationHandler.AddPropertyDefinitionLine(property, "Sometimes Editor Only");
			}
		}

		/// <summary>
		/// Property : Release Only, Editor Only
		/// </summary>
		private static Dictionary<PropertyDefinition, FieldPresense> GetReleaseAndEditorOnlyData(ClassGroupBase group)
		{
			Dictionary<string, List<KeyValuePair<GeneratedClassInstance, string?>>> propDictionary = new();
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				foreach (ClassProperty classProperty in instance.Properties)
				{
					List<KeyValuePair<GeneratedClassInstance, string?>> propList = propDictionary.GetOrAdd(classProperty.Definition.Name!);
					propList.Add(new KeyValuePair<GeneratedClassInstance, string?>(instance, classProperty.BackingField?.Name));
				}
			}

			Dictionary<PropertyDefinition, FieldPresense> result = new();
			foreach (PropertyDefinition property in group.Interface.Properties)
			{
				List<KeyValuePair<GeneratedClassInstance, string?>> propList = propDictionary[property.Name!.ToString()];

				bool sometimesReleaseOnly = propList.Any(pair => IsFieldReleaseOnly(pair.Key, pair.Value));
				bool releaseOnly = sometimesReleaseOnly && propList.All(pair => IsFieldAbsentOrReleaseOnly(pair.Key, pair.Value));

				bool sometimesEditorOnly = propList.Any(pair => IsFieldEditorOnly(pair.Key, pair.Value));
				bool editorOnly = sometimesEditorOnly && propList.All(pair => IsFieldAbsentOrEditorOnly(pair.Key, pair.Value));

				FieldPresense presense = FieldPresense.Normal;
				if (releaseOnly)
				{
					presense |= FieldPresense.AlwaysReleaseOnly;
				}
				if (sometimesReleaseOnly)
				{
					presense |= FieldPresense.SometimesReleaseOnly;
				}
				if (editorOnly)
				{
					presense |= FieldPresense.AlwaysEditorOnly;
				}
				if (sometimesEditorOnly)
				{
					presense |= FieldPresense.SometimesEditorOnly;
				}

				result[property] = presense;
			}
			return result;
		}

		private static UniversalNode? GetReleaseFieldByName(this GeneratedClassInstance instance, string fieldName)
		{
			return instance.Class.ReleaseRootNode?.SubNodes.SingleOrDefault(n => n.Name == fieldName);
		}

		private static UniversalNode? GetEditorFieldByName(this GeneratedClassInstance instance, string fieldName)
		{
			return instance.Class.EditorRootNode?.SubNodes.SingleOrDefault(n => n.Name == fieldName);
		}

		private static bool IsFieldAbsentOrEditorOnly(this GeneratedClassInstance instance, string? fieldName)
		{
			return fieldName is null || instance.GetReleaseFieldByName(fieldName) is null;
		}

		private static bool IsFieldAbsentOrReleaseOnly(this GeneratedClassInstance instance, string? fieldName)
		{
			return fieldName is null || instance.GetEditorFieldByName(fieldName) is null;
		}

		private static bool IsFieldEditorOnly(this GeneratedClassInstance instance, string? fieldName)
		{
			return fieldName is not null
				&& instance.GetReleaseFieldByName(fieldName) is null
				&& instance.GetEditorFieldByName(fieldName) is not null;
		}

		private static bool IsFieldReleaseOnly(this GeneratedClassInstance instance, string? fieldName)
		{
			return fieldName is not null
				&& instance.GetEditorFieldByName(fieldName) is null
				&& instance.GetReleaseFieldByName(fieldName) is not null;
		}
	}
}
