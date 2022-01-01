using AssemblyDumper.Utils;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.IO.Endian;
using AssetRipper.Core.Project;
using AssetRipper.Core.YAML;

namespace AssemblyDumper
{
	public static class CommonTypeGetter
	{
		public static AssemblyDefinition CommonAssembly { get; set; }
		private static ModuleDefinition generatedModule;

		//Reading
		public static ITypeDefOrRef AssetReaderDefinition => SharedState.Importer.ImportCommonType<AssetReader>();
		public static ITypeDefOrRef EndianReaderDefinition => SharedState.Importer.ImportCommonType<EndianReader>();
		public static ITypeDefOrRef EndianReaderExtensionsDefinition => SharedState.Importer.ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.EndianReaderExtensions));
		public static ITypeDefOrRef AssetReaderExtensionsDefinition => SharedState.Importer.ImportCommonType(typeof(AssetRipper.Core.IO.Extensions.AssetReaderExtensions));

		//Writing
		public static ITypeDefOrRef AssetWriterDefinition => SharedState.Importer.ImportCommonType<AssetWriter>();

		//Yaml Export
		public static ITypeDefOrRef IExportContainerDefinition => SharedState.Importer.ImportCommonType<IExportContainer>();
		public static ITypeDefOrRef YAMLNodeDefinition => SharedState.Importer.ImportCommonType<YAMLNode>();
		public static ITypeDefOrRef YAMLMappingNodeDefinition => SharedState.Importer.ImportCommonType<YAMLMappingNode>();
		public static ITypeDefOrRef YAMLSequenceNodeDefinition => SharedState.Importer.ImportCommonType<YAMLSequenceNode>();
		public static ITypeDefOrRef YAMLScalarNodeDefinition => SharedState.Importer.ImportCommonType<YAMLScalarNode>();
		public static IMethodDefOrRef YAMLMappingNodeConstructor => SharedState.Importer.ImportCommonConstructor<YAMLMappingNode>();
		public static IMethodDefOrRef YAMLSequenceNodeConstructor => SharedState.Importer.ImportCommonConstructor<YAMLSequenceNode>(1);

		//Generics
		public static ITypeDefOrRef AssetDictionaryType => SharedState.Importer.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");
		public static ITypeDefOrRef NullableKeyValuePair => SharedState.Importer.ImportCommonType("AssetRipper.Core.IO.NullableKeyValuePair`2");

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

		public static ITypeDefOrRef ImportCommonType(this ReferenceImporter importer, string typeFullName) => importer.ImportType(LookupCommonType(typeFullName));
		public static ITypeDefOrRef ImportCommonType(this ReferenceImporter importer, System.Type type) => importer.ImportType(LookupCommonType(type));
		public static ITypeDefOrRef ImportCommonType<T>(this ReferenceImporter importer) => importer.ImportType(LookupCommonType<T>());

		public static IMethodDefOrRef ImportCommonMethod(this ReferenceImporter importer, string typeFullName, Func<MethodDefinition, bool> filter)
		{
			return importer.ImportMethod(LookupCommonMethod(typeFullName, filter));
		}
		public static IMethodDefOrRef ImportCommonMethod(this ReferenceImporter importer, System.Type type, Func<MethodDefinition, bool> filter)
		{
			return importer.ImportMethod(LookupCommonMethod(type, filter));
		}
		public static IMethodDefOrRef ImportCommonMethod<T>(this ReferenceImporter importer, Func<MethodDefinition, bool> filter)
		{
			return importer.ImportMethod(LookupCommonMethod<T>(filter));
		}

		/// <summary>
		/// Import the default constructor for this type
		/// </summary>
		public static IMethodDefOrRef ImportCommonConstructor<T>(this ReferenceImporter importer) => importer.ImportCommonConstructor(typeof(T).FullName, 0);
		public static IMethodDefOrRef ImportCommonConstructor<T>(this ReferenceImporter importer, int numParameters) => importer.ImportCommonConstructor(typeof(T).FullName, numParameters);
		public static IMethodDefOrRef ImportCommonConstructor(this ReferenceImporter importer, string typeFullName) => importer.ImportCommonConstructor(typeFullName, 0);
		public static IMethodDefOrRef ImportCommonConstructor(this ReferenceImporter importer, string typeFullName, int numParameters)
		{
			return importer.ImportMethod(LookupCommonType(typeFullName).GetConstructor(numParameters));
		}
	}
}