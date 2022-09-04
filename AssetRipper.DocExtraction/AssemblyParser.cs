using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.DocExtraction;

public static class AssemblyParser
{
	private static readonly HashSet<string?> ClassBlackList = new()
	{
		"System.Attribute",
		"System.Exception",
		"System.IO.Stream",
		"UnityEditor.Build.BuildPlayerProcessor",
		"UnityEditor.Experimental.AssetsModifiedProcessor",
		"UnityEditor.AssetModificationProcessor",
		"UnityEditor.AssetPostprocessor",
		"UnityEditor.Editor",
		"UnityEditor.EditorWindow",
		"UnityEngine.PropertyAttribute",
	};

	public static void ExtractDocumentationFromAssembly(
		string dllPath,
		Dictionary<string, string> typeSummaries,
		Dictionary<string, string> fieldSummaries,
		Dictionary<string, string> propertySummaries,
		Dictionary<string, ClassDocumentation> classDictionary,
		Dictionary<string, EnumDocumentation> enumDictionary,
		Dictionary<string, StructDocumentation> structDictionary)
	{
		ModuleDefinition module = ModuleDefinition.FromFile(dllPath);
		foreach (TypeDefinition type in module.TopLevelTypes)
		{
			string typeFullName = type.FullName;
			if (!type.IsPublic || ClassBlackList.Contains(typeFullName) || ClassBlackList.Contains(type.BaseType?.FullName))
			{
			}
			else if (type.IsEnum)
			{
				EnumDocumentation enumDocumentation = AddEnumDocumentation(typeSummaries, fieldSummaries, type, typeFullName);
				enumDictionary.Add(typeFullName, enumDocumentation);
			}
			else if (type.IsValueType)
			{
				StructDocumentation structDocumentation = AddStructDocumentation(typeSummaries, fieldSummaries, propertySummaries, type, typeFullName);
				structDictionary.Add(typeFullName, structDocumentation);
			}
			else if (!type.IsInterface && !type.IsStatic())
			{
				ClassDocumentation classDocumentation = AddClassDocumetation(typeSummaries, fieldSummaries, propertySummaries, type, typeFullName);
				classDictionary.Add(typeFullName, classDocumentation);
			}
		}
	}

	private static ClassDocumentation AddClassDocumetation(Dictionary<string, string> typeSummaries, Dictionary<string, string> fieldSummaries, Dictionary<string, string> propertySummaries, TypeDefinition type, string typeFullName)
	{
		ClassDocumentation classDocumentation = new()
		{
			Name = type.Name ?? throw new NullReferenceException("Name cannot be null"),
			Namespace = type.Namespace,
			BaseName = type.BaseType?.Name,
			BaseNamespace = type.BaseType?.Namespace,
			DocumentationString = typeSummaries.TryGetValue(typeFullName),
			ObsoleteMessage = type.GetObsoleteMessage(),
			NativeName = type.GetNativeClass(),
		};

		foreach (FieldDefinition field in type.Fields)
		{
			if (field.IsPublic && !field.IsStatic)
			{
				string fieldName = field.Name ?? throw new NullReferenceException("Field Name cannot be null");
				DataMemberDocumentation fieldDocumentation = new()
				{
					Name = fieldName,
					TypeName = field.Signature?.FieldType.Name ?? throw new NullReferenceException("Field Signature cannot be null"),
					TypeNamespace = field.Signature?.FieldType.Namespace,
					DocumentationString = fieldSummaries.TryGetValue($"{typeFullName}.{field.Name}"),
					ObsoleteMessage = field.GetObsoleteMessage(),
					NativeName = field.GetNativeName(),
				};
				classDocumentation.DataMembers.Add(fieldName, fieldDocumentation);
			}
		}

		foreach (PropertyDefinition property in type.Properties)
		{
			if (property.IsPublic() && !property.IsStatic() && !property.HasParameters())
			{
				string propertyName = property.Name ?? throw new NullReferenceException("Property Name cannot be null");
				DataMemberDocumentation propertyDocumentation = new()
				{
					Name = propertyName,
					TypeName = property.Signature?.ReturnType?.Name ?? throw new NullReferenceException("Property Type cannot be null"),
					TypeNamespace = property.Signature?.ReturnType?.Namespace,
					DocumentationString = propertySummaries.TryGetValue($"{typeFullName}.{property.Name}"),
					ObsoleteMessage = property.GetObsoleteMessage(),
					NativeName = property.GetNativeName() ?? property.GetNativeProperty(),
				};
				classDocumentation.DataMembers.Add(propertyName, propertyDocumentation);
			}
		}

		return classDocumentation;
	}

	private static EnumDocumentation AddEnumDocumentation(Dictionary<string, string> typeSummaries, Dictionary<string, string> fieldSummaries, TypeDefinition type, string typeFullName)
	{
		FieldDefinition valueField = type.Fields[0]; //value field is always first
		if (valueField.IsStatic)
		{
			throw new Exception("Value field can't be static");
		}

		EnumDocumentation enumDocumentation = new()
		{
			ElementType = ((CorLibTypeSignature)valueField.Signature!.FieldType).ElementType,
			IsFlagsEnum = type.HasAttribute("System", nameof(FlagsAttribute)),
			Name = type.Name ?? throw new NullReferenceException("Type Name cannot be null"),
			Namespace = type.Namespace,
			DocumentationString = typeSummaries.TryGetValue(typeFullName),
			ObsoleteMessage = type.GetObsoleteMessage(),
			NativeName = null,//NativeClassAttribute isn't valid on enums
		};

		for (int i = 1; i < type.Fields.Count; i++)
		{
			FieldDefinition enumField = type.Fields[i];
			if (enumField.IsStatic)
			{
				string enumFieldName = enumField.Name ?? throw new NullReferenceException("Field Name cannot be null");
				EnumMemberDocumentation memberDocumentation = new()
				{
					Name = enumFieldName,
					Value = enumField.Constant!.ConvertToLong(),
					DocumentationString = fieldSummaries.TryGetValue($"{typeFullName}.{enumField.Name}"),
					ObsoleteMessage = enumField.GetObsoleteMessage(),
					NativeName = enumField.GetNativeName(),
				};
				enumDocumentation.Members.Add(enumFieldName, memberDocumentation);
			}
			else
			{
				throw new Exception("Enum field must be static");
			}
		}

		return enumDocumentation;
	}

	private static StructDocumentation AddStructDocumentation(Dictionary<string, string> typeSummaries, Dictionary<string, string> fieldSummaries, Dictionary<string, string> propertySummaries, TypeDefinition type, string typeFullName)
	{
		StructDocumentation structDocumentation = new()
		{
			Name = type.Name ?? throw new NullReferenceException("Name cannot be null"),
			Namespace = type.Namespace,
			DocumentationString = typeSummaries.TryGetValue(typeFullName),
			ObsoleteMessage = type.GetObsoleteMessage(),
			NativeName = type.GetNativeClass(),
		};

		foreach (FieldDefinition field in type.Fields)
		{
			if (field.IsPublic && !field.IsStatic)
			{
				string fieldName = field.Name ?? throw new NullReferenceException("Field Name cannot be null");
				DataMemberDocumentation fieldDocumentation = new()
				{
					Name = fieldName,
					TypeName = field.Signature?.FieldType.Name ?? throw new NullReferenceException("Field Signature cannot be null"),
					TypeNamespace = field.Signature?.FieldType.Namespace,
					DocumentationString = fieldSummaries.TryGetValue($"{typeFullName}.{field.Name}"),
					ObsoleteMessage = field.GetObsoleteMessage(),
					NativeName = field.GetNativeName(),
				};
				structDocumentation.DataMembers.Add(fieldName, fieldDocumentation);
			}
		}

		foreach (PropertyDefinition property in type.Properties)
		{
			if (property.IsPublic() && !property.IsStatic() && !property.HasParameters())
			{
				string propertyName = property.Name ?? throw new NullReferenceException("Property Name cannot be null");
				DataMemberDocumentation propertyDocumentation = new()
				{
					Name = propertyName,
					TypeName = property.Signature?.ReturnType?.Name ?? throw new NullReferenceException("Property Type cannot be null"),
					TypeNamespace = property.Signature?.ReturnType?.Namespace,
					DocumentationString = propertySummaries.TryGetValue($"{typeFullName}.{property.Name}"),
					ObsoleteMessage = property.GetObsoleteMessage(),
					NativeName = property.GetNativeName() ?? property.GetNativeProperty(),
				};
				structDocumentation.DataMembers.Add(propertyName, propertyDocumentation);
			}
		}

		return structDocumentation;
	}
}