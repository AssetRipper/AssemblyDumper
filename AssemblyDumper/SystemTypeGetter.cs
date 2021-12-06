using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Utils;
using Mono.Cecil;

namespace AssemblyDumper
{
	public static class SystemTypeGetter
	{
		public static AssemblyDefinition RuntimeAssembly { get; set; }
		public static AssemblyDefinition CollectionsAssembly { get; set; }

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

		public static TypeReference Int8 { get; private set; }
		public static TypeReference UInt8 { get; private set; }
		public static TypeReference Int16 { get; private set; }
		public static TypeReference UInt16 { get; private set; }
		public static TypeReference Int32 { get; private set; }
		public static TypeReference UInt32 { get; private set; }
		public static TypeReference Int64 { get; private set; }
		public static TypeReference UInt64 { get; private set; }
		public static TypeReference String { get; private set; }
		public static TypeReference Dictionary { get; private set; }
		public static TypeReference List { get; private set; }
		public static TypeReference Type { get; private set; }
		public static TypeReference Void { get; private set; }
		public static TypeReference Object { get; private set; }
		public static TypeReference BinaryReader { get; private set; }
		public static MethodReference NotSupportedExceptionConstructor { get; private set; }

		public static void Initialize(ModuleDefinition module)
		{
			Int8 = module.ImportSystemType("System.SByte");
			UInt8 = module.ImportSystemType("System.Byte");
			Int16 = module.ImportSystemType("System.Int16");
			UInt16 = module.ImportSystemType("System.UInt16");
			Int32 = module.ImportSystemType("System.Int32");
			UInt32 = module.ImportSystemType("System.UInt32");
			Int64 = module.ImportSystemType("System.Int64");
			UInt64 = module.ImportSystemType("System.UInt64");
			String = module.ImportSystemType("System.String");
			Dictionary = module.ImportSystemType("System.Collections.Generic.Dictionary`2");
			List = module.ImportSystemType("System.Collections.Generic.List`1");
			Type = module.ImportSystemType("System.Type");
			Void = module.ImportSystemType("System.Void");
			Object = module.ImportSystemType("System.Object");
			BinaryReader = module.ImportSystemType("System.IO.BinaryReader");
			NotSupportedExceptionConstructor = module.ImportSystemDefaultConstructor("System.NotSupportedException");
		}

		public static MethodReference ImportSystemDefaultConstructor(this ModuleDefinition module, string typeFullName)
		{
			TypeDefinition type = LookupSystemType(typeFullName) ?? throw new Exception($"{typeFullName} not found in the system assemblies");
			return module.ImportReference(type.GetDefaultConstructor());
		}

		public static TypeDefinition LookupSystemType(string typeFullName)
		{
			return RuntimeAssembly.MainModule.GetType(typeFullName)
				?? CollectionsAssembly.MainModule.GetType(typeFullName);
		}
		public static TypeDefinition LookupSystemType(Type type) => LookupSystemType(type.FullName);
		public static TypeDefinition LookupSystemType<T>() => LookupSystemType(typeof(T));

		public static MethodDefinition LookupSystemMethod(string typeFullName, Func<MethodDefinition, bool> filter)
		{
			TypeDefinition type = LookupSystemType(typeFullName) ?? throw new Exception($"{typeFullName} not found in the system assemblies");
			return type.Methods.Single(filter);
		}
		public static MethodDefinition LookupSystemMethod(Type type, Func<MethodDefinition, bool> filter) => LookupSystemMethod(type.FullName, filter);
		public static MethodDefinition LookupSystemMethod<T>(Func<MethodDefinition, bool> filter) => LookupSystemMethod(typeof(T), filter);

		public static TypeReference ImportSystemType(this ModuleDefinition module, string typeFullName) => module.ImportReference(LookupSystemType(typeFullName));
		public static TypeReference ImportSystemType(this ModuleDefinition module, System.Type type) => module.ImportReference(LookupSystemType(type));
		public static TypeReference ImportSystemType<T>(this ModuleDefinition module) => module.ImportReference(LookupSystemType<T>());

		public static MethodReference ImportSystemMethod(this ModuleDefinition module, string typeFullName, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupSystemMethod(typeFullName, filter));
		}
		public static MethodReference ImportSystemMethod(this ModuleDefinition module, System.Type type, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupSystemMethod(type, filter));
		}
		public static MethodReference ImportSystemMethod<T>(this ModuleDefinition module, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupSystemMethod<T>(filter));
		}

		public static TypeReference GetPrimitiveType(this ModuleDefinition module, string cppPrimitiveName) =>
			CppPrimitivesToCSharpPrimitives.TryGetValue(cppPrimitiveName, out var csPrimitiveName)
				? module.ImportSystemType($"System.{csPrimitiveName}")
				: null;
	}
}