using AssemblyDumper.Utils;
using Mono.Cecil;

namespace AssemblyDumper
{
	public static class SystemTypeGetter
	{
		public static AssemblyDefinition Assembly { get; set; }

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

		public static TypeReference Int8(ModuleDefinition module) => module.ImportSystemType("System.SByte");
		public static TypeReference UInt8(ModuleDefinition module) => module.ImportSystemType("System.Byte");
		public static TypeReference Int16(ModuleDefinition module) => module.ImportSystemType("System.Int16");
		public static TypeReference UInt16(ModuleDefinition module) => module.ImportSystemType("System.UInt16");
		public static TypeReference Int32(ModuleDefinition module) => module.ImportSystemType("System.Int32");
		public static TypeReference UInt32(ModuleDefinition module) => module.ImportSystemType("System.UInt32");
		public static TypeReference Int64(ModuleDefinition module) => module.ImportSystemType("System.Int64");
		public static TypeReference UInt64(ModuleDefinition module) => module.ImportSystemType("System.UInt64");
		public static TypeReference String(ModuleDefinition module) => module.ImportSystemType("System.String");

		public static TypeReference ImportSystemType(this ModuleDefinition module, string typeFullName)
		{
			return module.ImportReference(LookupSystemType(typeFullName));
		}

		public static MethodReference ImportDefaultConstructor(this ModuleDefinition module, string typeFullName)
		{
			return module.ImportReference(LookupSystemType(typeFullName).GetDefaultConstructor());
		}

		public static TypeDefinition LookupSystemType(string typeFullName) => Assembly.MainModule.GetType(typeFullName);


	}
}
