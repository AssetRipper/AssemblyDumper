using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class FieldUtils
	{
		public static List<FieldDefinition> GetAllFieldsInTypeAndBase(TypeDefinition type)
		{
			if (type == null)
				return new();

			var ret = type.Fields.ToList();

			ret.AddRange(GetAllFieldsInTypeAndBase(type.BaseType?.Resolve()));

			return ret;
		}
	}
}