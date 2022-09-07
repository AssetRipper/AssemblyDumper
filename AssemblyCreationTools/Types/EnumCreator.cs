namespace AssetRipper.AssemblyCreationTools.Types
{
	public static class EnumCreator
	{
		private static TypeDefinition CreateFromExisting<T>(AssemblyBuilder builder, string? @namespace, string name) where T : Enum
		{
			EnumUnderlyingType underlyingType = Enum.GetUnderlyingType(typeof(T)).ToEnumUnderlyingType();
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, underlyingType);
			foreach (long item in Enum.GetValues(typeof(T)))
			{
				definition.AddEnumField(item.ToString(), item, underlyingType);
			}

			return definition;
		}

		public static TypeDefinition CreateFromDictionary(AssemblyBuilder builder, string? @namespace, string name, IEnumerable<KeyValuePair<string, long>> fields, EnumUnderlyingType underlyingType = EnumUnderlyingType.Int32)
		{
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, underlyingType);
			foreach (KeyValuePair<string, long> pair in fields)
			{
				definition.AddEnumField(pair.Key, pair.Value, underlyingType);
			}

			return definition;
		}

		public static TypeDefinition CreateFromArray(AssemblyBuilder builder, string? @namespace, string name, string[] fields, EnumUnderlyingType underlyingType = EnumUnderlyingType.Int32)
		{
			TypeDefinition definition = CreateEmptyEnum(builder, @namespace, name, underlyingType);
			for (int i = 0; i < fields.Length; i++)
			{
				definition.AddEnumField(fields[i], i, underlyingType);
			}

			return definition;
		}

		private static void AddEnumValue(this TypeDefinition typeDefinition, AssemblyBuilder builder, EnumUnderlyingType underlyingType)
		{
			FieldSignature fieldSignature = new FieldSignature(underlyingType.ToTypeSignature(builder));
			FieldDefinition fieldDef = new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName, fieldSignature);
			typeDefinition.Fields.Add(fieldDef);
		}

		private static void AddEnumField(this TypeDefinition typeDefinition, string name, long value, EnumUnderlyingType underlyingType)
		{
			FieldSignature fieldSignature = new FieldSignature(typeDefinition.ToTypeSignature());
			FieldDefinition fieldDef = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, fieldSignature);
			fieldDef.Constant = FromValue(value, underlyingType);
			typeDefinition.Fields.Add(fieldDef);
		}

		private static TypeDefinition CreateEmptyEnum(AssemblyBuilder builder, string? @namespace, string name, EnumUnderlyingType underlyingType)
		{
			ITypeDefOrRef enumReference = builder.Importer.ImportType(typeof(Enum));
			TypeDefinition definition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Sealed, enumReference);
			builder.Module.TopLevelTypes.Add(definition);
			definition.AddEnumValue(builder, underlyingType);
			return definition;
		}

		private static Constant FromValue(long value, EnumUnderlyingType underlyingType)
		{
			return underlyingType switch
			{
				EnumUnderlyingType.Int8 => Constant.FromValue((sbyte)value),
				EnumUnderlyingType.UInt8 => Constant.FromValue((byte)value),
				EnumUnderlyingType.Int16 => Constant.FromValue((short)value),
				EnumUnderlyingType.UInt16 => Constant.FromValue((ushort)value),
				EnumUnderlyingType.Int32 => Constant.FromValue((int)value),
				EnumUnderlyingType.UInt32 => Constant.FromValue((uint)value),
				EnumUnderlyingType.Int64 => Constant.FromValue(value),
				EnumUnderlyingType.UInt64 => Constant.FromValue(unchecked((ulong)value)),
				_ => throw new ArgumentOutOfRangeException(nameof(underlyingType)),
			};
		}
	}
}