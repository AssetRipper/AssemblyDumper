using System.Collections.Generic;
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

        public static TypeReference Int8(ModuleDefinition module) => module.ImportSystemType("System.SByte");
        public static TypeReference UInt8(ModuleDefinition module) => module.ImportSystemType("System.Byte");
        public static TypeReference Int16(ModuleDefinition module) => module.ImportSystemType("System.Int16");
        public static TypeReference UInt16(ModuleDefinition module) => module.ImportSystemType("System.UInt16");
        public static TypeReference Int32(ModuleDefinition module) => module.ImportSystemType("System.Int32");
        public static TypeReference UInt32(ModuleDefinition module) => module.ImportSystemType("System.UInt32");
        public static TypeReference Int64(ModuleDefinition module) => module.ImportSystemType("System.Int64");
        public static TypeReference UInt64(ModuleDefinition module) => module.ImportSystemType("System.UInt64");
        public static TypeReference String(ModuleDefinition module) => module.ImportSystemType("System.String");
        public static TypeReference Dictionary(ModuleDefinition module) => module.ImportSystemType("System.Collections.Generic.Dictionary`2");
        public static TypeReference KeyValuePair(ModuleDefinition module) => module.ImportSystemType("System.Collections.Generic.KeyValuePair`2");

        public static TypeReference ImportSystemType(this ModuleDefinition module, string typeFullName)
        {
            return module.ImportReference(LookupSystemType(typeFullName));
        }

        public static MethodReference ImportDefaultConstructor(this ModuleDefinition module, string typeFullName)
        {
            return module.ImportReference(LookupSystemType(typeFullName).GetDefaultConstructor());
        }

        public static TypeDefinition LookupSystemType(string typeFullName) => RuntimeAssembly.MainModule.GetType(typeFullName) ?? CollectionsAssembly.MainModule.GetType(typeFullName);

        public static TypeReference GetPrimitiveType(this ModuleDefinition module, string cppPrimitiveName) =>
            CppPrimitivesToCSharpPrimitives.TryGetValue(cppPrimitiveName, out var csPrimitiveName)
                ? module.ImportSystemType($"System.{csPrimitiveName}")
                : null;
    }
}