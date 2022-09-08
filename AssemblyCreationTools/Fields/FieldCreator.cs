namespace AssetRipper.AssemblyCreationTools.Fields
{
	public static class FieldCreator
	{
		public static FieldDefinition AddField(
			this TypeDefinition type,
			TypeSignature fieldType,
			string fieldName,
			bool isStatic = false,
			FieldVisibility visibility = FieldVisibility.Public)
		{
			FieldAttributes attributes = visibility.ToAttributes() | (isStatic ? FieldAttributes.Static : default);
			FieldDefinition field = new FieldDefinition(fieldName, attributes, new FieldSignature(fieldType));
			type.Fields.Add(field);
			return field;
		}

		public static FieldDefinition AddConstantField(
			this TypeDefinition type,
			TypeSignature fieldType,
			string fieldName,
			Constant constant,
			FieldVisibility visibility = FieldVisibility.Public)
		{
			FieldDefinition field = new FieldDefinition(
				fieldName,
				FieldAttributes.Static |
				FieldAttributes.Literal |
				FieldAttributes.HasDefault |
				visibility.ToAttributes(),
				new FieldSignature(fieldType));
			field.Constant = constant;
			type.Fields.Add(field);
			return field;
		}
	}
}
