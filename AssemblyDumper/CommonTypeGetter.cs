using AsmResolver.DotNet;
using AssemblyDumper.Utils;
using System;
using System.Linq;

namespace AssemblyDumper
{
	public static class CommonTypeGetter
	{
		public static AssemblyDefinition CommonAssembly { get; set; }
		private static ModuleDefinition generatedModule;

		//Reading
		public static ITypeDefOrRef AssetReaderDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Asset.AssetReader>();
		public static ITypeDefOrRef EndianReaderDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Endian.EndianReader>();
		public static ITypeDefOrRef EndianReaderExtensionsDefinition => ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.EndianReaderExtensions));
		public static ITypeDefOrRef AssetReaderExtensionsDefinition => ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.AssetReaderExtensions));

		//Writing
		public static ITypeDefOrRef AssetWriterDefinition => generatedModule.ImportCommonType<AssetRipper.Core.IO.Asset.AssetWriter>();

		//Yaml Export
		public static ITypeDefOrRef IExportContainerDefinition => generatedModule.ImportCommonType<AssetRipper.Core.Project.IExportContainer>();
		public static ITypeDefOrRef YAMLNodeDefinition => generatedModule.ImportCommonType<AssetRipper.Core.YAML.YAMLNode>();
		public static ITypeDefOrRef YAMLMappingNodeDefinition => generatedModule.ImportCommonType<AssetRipper.Core.YAML.YAMLMappingNode>();
		public static ITypeDefOrRef YAMLSequenceNodeDefinition => generatedModule.ImportCommonType<AssetRipper.Core.YAML.YAMLSequenceNode>();
		public static IMethodDefOrRef YAMLMappingNodeConstructor => generatedModule.ImportCommonConstructor<AssetRipper.Core.YAML.YAMLMappingNode>();
		public static IMethodDefOrRef YAMLSequenceNodeConstructor => generatedModule.ImportCommonConstructor<AssetRipper.Core.YAML.YAMLSequenceNode>(1);

		//Generics
		public static ITypeDefOrRef AssetDictionaryType => generatedModule.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");
		public static ITypeDefOrRef NullableKeyValuePair => generatedModule.ImportCommonType("AssetRipper.Core.IO.NullableKeyValuePair`2");

		public static void Initialize(ModuleDefinition module)
		{
			generatedModule = module;
		}

		public static TypeDefinition LookupCommonType(string typeFullName) => CommonAssembly.ManifestModule.TopLevelTypes.SingleOrDefault(t => t.GetTypeFullName() == typeFullName);
		public static TypeDefinition LookupCommonType(Type type) => LookupCommonType(type.FullName);
		public static TypeDefinition LookupCommonType<T>() => LookupCommonType(typeof(T));

		public static MethodDefinition LookupCommonMethod(string typeFullName, Func<MethodDefinition, bool> filter) => LookupCommonType(typeFullName).Methods.Single(filter);
		public static MethodDefinition LookupCommonMethod(Type type, Func<MethodDefinition, bool> filter) => LookupCommonMethod(type.FullName, filter);
		public static MethodDefinition LookupCommonMethod<T>(Func<MethodDefinition, bool> filter) => LookupCommonMethod(typeof(T), filter);

		public static ITypeDefOrRef ImportCommonType(this ModuleDefinition module, string typeFullName) => SharedState.Importer.ImportType(LookupCommonType(typeFullName));
		public static ITypeDefOrRef ImportCommonType(System.Type type) => SharedState.Importer.ImportType(LookupCommonType(type));
		public static ITypeDefOrRef ImportCommonType<T>(this ModuleDefinition module) => SharedState.Importer.ImportType(LookupCommonType<T>());

		public static IMethodDefOrRef ImportCommonMethod(this ModuleDefinition module, string typeFullName, Func<MethodDefinition, bool> filter)
		{
			return SharedState.Importer.ImportMethod(LookupCommonMethod(typeFullName, filter));
		}
		public static IMethodDefOrRef ImportCommonMethod(this ModuleDefinition module, System.Type type, Func<MethodDefinition, bool> filter)
		{
			return SharedState.Importer.ImportMethod(LookupCommonMethod(type, filter));
		}
		public static IMethodDefOrRef ImportCommonMethod<T>(this ModuleDefinition module, Func<MethodDefinition, bool> filter)
		{
			return SharedState.Importer.ImportMethod(LookupCommonMethod<T>(filter));
		}

		/// <summary>
		/// Import the default constructor for this type
		/// </summary>
		public static IMethodDefOrRef ImportCommonConstructor<T>(this ModuleDefinition module) => module.ImportCommonConstructor(typeof(T).FullName, 0);
		public static IMethodDefOrRef ImportCommonConstructor<T>(this ModuleDefinition module, int numParameters) => module.ImportCommonConstructor(typeof(T).FullName, numParameters);
		public static IMethodDefOrRef ImportCommonConstructor(this ModuleDefinition module, string typeFullName) => module.ImportCommonConstructor(typeFullName, 0);
		public static IMethodDefOrRef ImportCommonConstructor(this ModuleDefinition module, string typeFullName, int numParameters)
		{
			return SharedState.Importer.ImportMethod(LookupCommonType(typeFullName).GetConstructor(numParameters));
		}
	}
}