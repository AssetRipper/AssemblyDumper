using System.Diagnostics.CodeAnalysis;

namespace AssemblyDumper.Utils
{
	public static class FieldUtils
	{
		public static List<FieldDefinition> GetAllFieldsInTypeAndBase(this TypeDefinition? type)
		{
			if (type == null)
				return new();

			List<FieldDefinition>? ret = type.Fields.ToList();

			ret.AddRange(GetAllFieldsInTypeAndBase(type.BaseType?.Resolve()));

			return ret;
		}

		public static FieldDefinition GetFieldByName(this TypeDefinition type, string fieldName)
		{
			return type.TryGetFieldByName(fieldName)
				?? throw new Exception($"{type.FullName} doesn't have a {fieldName} field");
		}

		public static bool TryGetFieldByName(this TypeDefinition type, string fieldName, [NotNullWhen(true)][MaybeNullWhen(false)] out FieldDefinition? field)
		{
			field = type.TryGetFieldByName(fieldName);
			return field != null;
		}

		public static FieldDefinition? TryGetFieldByName(this TypeDefinition type, string fieldName)
		{
			return type.Fields.SingleOrDefault(field => field.Name == fieldName)
				?? type.TryGetBaseTypeDefinition()?.TryGetFieldByName(fieldName);
		}

		private static TypeDefinition? TryGetBaseTypeDefinition(this TypeDefinition type)
		{
			return type.BaseType as TypeDefinition;
		}
	}
}