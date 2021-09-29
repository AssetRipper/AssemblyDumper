using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Utils
{
	public static class FieldUtils
	{
		public static FieldDefinition AddIntField(this TypeDefinition typeDefinition, string name)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition(name, FieldAttributes.Public, SystemTypeGetter.Int32);
			typeDefinition.Fields.Add(fieldDef);
			return fieldDef;
		}

		public static FieldDefinition AddIntStaticField(this TypeDefinition typeDefinition, string name)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, SystemTypeGetter.Int32);
			typeDefinition.Fields.Add(fieldDef);
			return fieldDef;
		}

		public static FieldDefinition AddByteField(this TypeDefinition typeDefinition, string name)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition(name, FieldAttributes.Public, SystemTypeGetter.UInt8);
			typeDefinition.Fields.Add(fieldDef);
			return fieldDef;
		}

		public static FieldDefinition AddByteArrayField(this TypeDefinition typeDefinition, string name)
		{
			var module = typeDefinition.Module;
			var fieldDef = new FieldDefinition(name, FieldAttributes.Public, SystemTypeGetter.UInt8.MakeArrayType());
			typeDefinition.Fields.Add(fieldDef);
			return fieldDef;
		}
	}
}