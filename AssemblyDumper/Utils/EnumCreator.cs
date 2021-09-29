using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace AssemblyDumper.Utils
{
	public static class EnumCreator
	{
		public static TypeDefinition CreateFromExisting<T>(AssemblyDefinition assembly, string @namespace, string name) where T : Enum
		{
			TypeDefinition definition = CreateEmptyEnum(assembly, @namespace, name);
			foreach (T item in Enum.GetValues(typeof(T)))
			{
				definition.AddEnumField(item.ToString(), item);
			}

			return definition;
		}

		public static TypeDefinition CreateFromDictionary(AssemblyDefinition assembly, string @namespace, string name, Dictionary<string, int> fields)
		{
			TypeDefinition definition = CreateEmptyEnum(assembly, @namespace, name);
			foreach (var pair in fields)
			{
				definition.AddEnumField(pair.Key, pair.Value);
			}

			return definition;
		}

		public static TypeDefinition CreateFromArray(AssemblyDefinition assembly, string @namespace, string name, string[] fields)
		{
			TypeDefinition definition = CreateEmptyEnum(assembly, @namespace, name);
			for (int i = 0; i < fields.Length; i++)
			{
				definition.AddEnumField(fields[i], i);
			}

			return definition;
		}

		public static TypeDefinition CreateTest(AssemblyDefinition assembly)
		{
			return CreateFromArray(assembly, SharedState.ExamplesNamespace, "TestEnum", new string[] { "Test1", "Test2", "Test3", "Test4" });
		}

		private static void AddEnumValue(this TypeDefinition typeDefinition)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, SystemTypeGetter.Int32);
			typeDefinition.Fields.Add(fieldDef);
		}

		private static void AddEnumField(this TypeDefinition typeDefinition, string name, object value)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal, typeDefinition);
			fieldDef.Constant = value;
			typeDefinition.Fields.Add(fieldDef);
		}

		private static TypeDefinition CreateEmptyEnum(AssemblyDefinition assembly, string @namespace, string name)
		{
			var module = assembly.MainModule;
			var enumReference = module.ImportSystemType("System.Enum");
			TypeDefinition definition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Sealed, enumReference);
			module.Types.Add(definition);
			definition.AddEnumValue();
			return definition;
		}
	}
}