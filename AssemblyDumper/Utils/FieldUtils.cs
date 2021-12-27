using AsmResolver.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class FieldUtils
	{
		public static List<FieldDefinition> GetAllFieldsInTypeAndBase(this TypeDefinition type)
		{
			if (type == null)
				return new();

			var ret = type.Fields.ToList();

			ret.AddRange(GetAllFieldsInTypeAndBase(type.BaseType?.Resolve()));

			return ret;
		}

		public static FieldDefinition GetFieldByName(this TypeDefinition type, string fieldName)
		{
			return type.Fields.Single(field => field.Name == fieldName);
		}

		public static bool TryGetFieldByName(this TypeDefinition type, string fieldName, out FieldDefinition field)
		{
			field = type.Fields.SingleOrDefault(field => field.Name == fieldName);
			return field != null;
		}
	}
}