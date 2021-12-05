using AssemblyDumper.Utils;
using Mono.Cecil;
using System;
using System.Linq;

namespace AssemblyDumper
{
	public static class CommonTypeGetter
	{
		public static AssemblyDefinition CommonAssembly { get; set; }
		private static ModuleDefinition generatedModule;

		//Reading
		public static TypeReference AssetReaderDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Asset.AssetReader>();
		public static TypeReference EndianReaderDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Endian.EndianReader>();
		public static TypeReference EndianReaderExtensionsDefinition => generatedModule.ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.EndianReaderExtensions));
		public static TypeReference AssetReaderExtensionsDefinition => generatedModule.ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.AssetReaderExtensions));

		//Writing
		public static TypeReference AssetWriterDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Asset.AssetWriter>();

		//Yaml Export
		public static TypeReference IExportContainerDefinition => generatedModule.ImportCommonType<AssetRipper.Core.Project.Collections.IExportCollection>();
		public static TypeReference YAMLNodeDefinition => generatedModule.ImportCommonType<AssetRipper.Core.YAML.YAMLNode>();
		public static TypeReference YAMLMappingNodeReference => generatedModule.ImportCommonType<AssetRipper.Core.YAML.YAMLMappingNode>();
		public static MethodReference YAMLMappingNodeConstructor => generatedModule.ImportCommonConstructor<AssetRipper.Core.YAML.YAMLMappingNode>();

		//Generics
		public static TypeReference AssetDictionaryType => generatedModule.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");

		public static void Initialize(ModuleDefinition module)
		{
			generatedModule = module;
		}

		public static TypeDefinition LookupCommonType(string typeFullName) => CommonAssembly.MainModule.GetType(typeFullName);
		public static TypeDefinition LookupCommonType(Type type) => LookupCommonType(type.FullName);
		public static TypeDefinition LookupCommonType<T>() => LookupCommonType(typeof(T));

		public static MethodDefinition LookupCommonMethod(string typeFullName, Func<MethodDefinition, bool> filter) => LookupCommonType(typeFullName).Methods.Single(filter);
		public static MethodDefinition LookupCommonMethod(Type type, Func<MethodDefinition, bool> filter) => LookupCommonMethod(type.FullName, filter);
		public static MethodDefinition LookupCommonMethod<T>(Func<MethodDefinition, bool> filter) => LookupCommonMethod(typeof(T), filter);

		public static TypeReference ImportCommonType(this ModuleDefinition module, string typeFullName) => module.ImportReference(LookupCommonType(typeFullName));
		public static TypeReference ImportCommonType(this ModuleDefinition module, System.Type type) => module.ImportReference(LookupCommonType(type));
		public static TypeReference ImportCommonType<T>(this ModuleDefinition module) => module.ImportReference(LookupCommonType<T>());

		public static MethodReference ImportCommonMethod(this ModuleDefinition module, string typeFullName, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupCommonMethod(typeFullName, filter));
		}
		public static MethodReference ImportCommonMethod(this ModuleDefinition module, System.Type type, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupCommonMethod(type, filter));
		}
		public static MethodReference ImportCommonMethod<T>(this ModuleDefinition module, Func<MethodDefinition, bool> filter)
		{
			return module.ImportReference(LookupCommonMethod<T>(filter));
		}

		/// <summary>
		/// Import the default constructor for this type
		/// </summary>
		public static MethodReference ImportCommonConstructor<T>(this ModuleDefinition module) => module.ImportCommonConstructor(typeof(T).FullName, 0);
		public static MethodReference ImportCommonConstructor<T>(this ModuleDefinition module, int numParameters) => module.ImportCommonConstructor(typeof(T).FullName, numParameters);
		public static MethodReference ImportCommonConstructor(this ModuleDefinition module, string typeFullName) => module.ImportCommonConstructor(typeFullName, 0);
		public static MethodReference ImportCommonConstructor(this ModuleDefinition module, string typeFullName, int numParameters)
		{
			return module.ImportReference(LookupCommonType(typeFullName).GetConstructor(numParameters));
		}
	}
}