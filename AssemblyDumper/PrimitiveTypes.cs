using AssetRipper.AssemblyCreationTools;

namespace AssetRipper.AssemblyDumper
{
	public static class PrimitiveTypes
	{
		public readonly static string[] primitivesAndGenerics =
		{
			"Array",
			"bool",
			"char",
			"double",
			"float",
			"int",
			"list",
			"long long", //long in C#
			"map",
			"pair",
			"set",
			"short",
			"SInt16",
			"SInt32",
			"SInt64",
			"SInt8",
			"staticvector",
			"string",
			"TypelessData", //byte[]
			"UInt16",
			"UInt32",
			"UInt64",
			"UInt8",
			"unsigned int",
			"unsigned long long",
			"unsigned short",
			"Type*", //int32
			"vector",
			"void"
		};

		public readonly static string[] primitiveNames =
		{
			"bool",
			"char",
			"double",
			"float",
			"int",
			"long long", //long in C#
			"short",
			"SInt16",
			"SInt32",
			"SInt64",
			"SInt8",
			"string",
			"TypelessData", //byte[]
			"UInt16",
			"UInt32",
			"UInt64",
			"UInt8",
			"unsigned int",
			"unsigned long long",
			"unsigned short",
			"Type*", //int32
			"void"
		};

		public readonly static string[] generics =
		{
			"Array",
			"first",
			"list",
			"map",
			"pair",
			"second",
			"set",
			"staticvector",
			"vector"
		};

		public static readonly Dictionary<string, string> CppPrimitivesToCSharpPrimitives = new()
		{
			{ "bool", "Boolean" },
			{ "char", "Byte" },
			{ "double", "Double" },
			{ "float", "Single" },
			{ "int", "Int32" },
			{ "long long", "Int64" },
			{ "short", "Int16" },
			{ "SInt16", "Int16" },
			{ "SInt32", "Int32" },
			{ "SInt64", "Int64" },
			{ "SInt8", "SByte" },
			{ "string", "String" },
			{ "UInt16", "UInt16" },
			{ "UInt32", "UInt32" },
			{ "UInt64", "UInt64" },
			{ "UInt8", "Byte" },
			{ "unsigned int", "UInt32" },
			{ "unsigned long long", "UInt64" },
			{ "unsigned short", "UInt16" },
			{ "Type*", "Int32" } //TODO Verify
		};

		public static readonly string[] primitiveNamesCsharp = new string[]
		{
			"void",
			"bool",
			"byte",
			"sbyte",
			"short",
			"ushort",
			"int",
			"uint",
			"long",
			"ulong",
			"float",
			"double",
			"decimal",
		};

		private static CorLibTypeSignature? GetCSharpPrimitiveTypeSignature(this CachedReferenceImporter importer, string cppPrimitiveName) => cppPrimitiveName switch
		{
			"Boolean" => importer.Boolean,
			"SByte" => importer.Int8,
			"Int16" => importer.Int16,
			"Int32" => importer.Int32,
			"Int64" => importer.Int64,
			"Byte" => importer.UInt8,
			"UInt16" => importer.UInt16,
			"UInt32" => importer.UInt32,
			"UInt64" => importer.UInt64,
			"Single" => importer.Single,
			"Double" => importer.Double,
			"String" => importer.String,
			_ => null,
		};

		/// <summary>
		/// Gets the type signature for a cpp primitive type
		/// </summary>
		/// <param name="cppPrimitiveName">The name of a cpp primitive, ie long long</param>
		/// <remarks>
		/// Note: The type tree dumps only contain cpp primitive names
		/// </remarks>
		/// <returns>The CorLibTypeSignature associated with that cpp name, or null if it can't be found</returns>
		public static CorLibTypeSignature? GetCppPrimitiveTypeSignature(this CachedReferenceImporter importer, string cppPrimitiveName) =>
			CppPrimitivesToCSharpPrimitives.TryGetValue(cppPrimitiveName, out string? csPrimitiveName)
				? importer.GetCSharpPrimitiveTypeSignature(csPrimitiveName)
				: null;
	}
}
