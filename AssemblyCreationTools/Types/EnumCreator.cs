namespace AssetRipper.AssemblyCreationTools.Types
{
	public static class EnumCreator
	{
		public static TypeDefinition CreateFromExisting<T>(AssemblyBuilder builder, string @namespace, string name) where T : Enum
		{
			EnumUnderlyingType underlyingType = Enum.GetUnderlyingType(typeof(T)).ToEnumUnderlyingType();
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, underlyingType);
			foreach (int item in Enum.GetValues(typeof(T)))
			{
				definition.AddEnumField(item.ToString(), item);
			}

			return definition;
		}

		public static TypeDefinition CreateFromDictionary(AssemblyBuilder builder, string @namespace, string name, IEnumerable<KeyValuePair<string, int>> fields)
		{
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, EnumUnderlyingType.Int32);
			foreach (KeyValuePair<string, int> pair in fields)
			{
				definition.AddEnumField(pair.Key, pair.Value);
			}

			return definition;
		}

		public static TypeDefinition CreateFromArray(AssemblyBuilder builder, string @namespace, string name, string[] fields)
		{
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, EnumUnderlyingType.Int32);
			for (int i = 0; i < fields.Length; i++)
			{
				definition.AddEnumField(fields[i], i);
			}

			return definition;
		}

		private static void AddEnumValue(this TypeDefinition typeDefinition, AssemblyBuilder builder, EnumUnderlyingType underlyingType)
		{
			FieldSignature fieldSignature = new FieldSignature(underlyingType.ToTypeSignature(builder));
			FieldDefinition fieldDef = new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName, fieldSignature);
			typeDefinition.Fields.Add(fieldDef);
		}

		private static void AddEnumField(this TypeDefinition typeDefinition, string name, int value)
		{
			FieldSignature fieldSignature = new FieldSignature(typeDefinition.ToTypeSignature());
			FieldDefinition fieldDef = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, fieldSignature);
			fieldDef.Constant = Constant.FromValue(value);
			typeDefinition.Fields.Add(fieldDef);
		}

		private static TypeDefinition CreateEmptyEnum(AssemblyBuilder builder, string @namespace, string name, EnumUnderlyingType underlyingType)
		{
			ITypeDefOrRef enumReference = builder.Importer.ImportType(typeof(Enum));
			TypeDefinition definition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Sealed, enumReference);
			builder.Module.TopLevelTypes.Add(definition);
			definition.AddEnumValue(builder, underlyingType);
			return definition;
		}
	}
}