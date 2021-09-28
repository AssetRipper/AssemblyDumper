using AssemblyDumper.Utils;
using AssetRipper.Core;
using AssetRipper.Core.Attributes;
using Mono.Cecil;

namespace AssemblyDumper
{
	public static class CommonTypeGetter
	{
		public static AssemblyDefinition Assembly { get; set; }
		//Attributes
		public static TypeReference ByteSizeAttributeDefinition { get; private set; }
		public static MethodReference ByteSizeAttributeConstructor { get; private set; }

		public static TypeReference EditorOnlyAttributeDefinition { get; private set; }
		public static MethodReference EditorOnlyAttributeConstructor { get; private set; }

		public static TypeReference StrippedAttributeDefinition { get; private set; }
		public static MethodReference StrippedAttributeConstructor { get; private set; }

		public static TypeReference PersistentIDAttributeDefinition { get; private set; }
		public static MethodReference PersistentIDAttributeConstructor { get; private set; }

		public static TypeReference ReleaseOnlyAttributeDefinition { get; private set; }
		public static MethodReference ReleaseOnlyAttributeConstructor { get; private set; }

		public static TypeReference FixedLengthAttributeDefinition { get; private set; }
		public static MethodReference FixedLengthAttributeConstructor { get; private set; }

		public static TypeReference EditorMetaFlagsAttributeDefinition { get; private set; }
		public static MethodReference EditorMetaFlagsAttributeConstructor { get; private set; }

		public static TypeReference ReleaseMetaFlagsAttributeDefinition { get; private set; }
		public static MethodReference ReleaseMetaFlagsAttributeConstructor { get; private set; }

		public static TypeReference RegisterAssemblyAttributeDefinition { get; private set; }
		public static MethodReference RegisterAssemblyAttributeConstructor { get; private set; }

		public static TypeReference RegisterAssetTypeAttributeDefinition { get; private set; }
		public static MethodReference RegisterAssetTypeAttributeConstructor { get; private set; }

		public static TypeReference UnityObjectBaseDefinition { get; private set; }
		public static TypeReference UnityAssetBaseDefinition { get; private set; }
		public static TypeReference UnityVersionDefinition { get; private set; }
		public static TypeReference TransferMetaFlagsDefinition { get; private set; }
		
		public static TypeReference AssetReaderDefinition { get; private set; }
		public static TypeReference EndianReaderDefinition { get; private set; }
		public static TypeReference AssetWriterDefinition { get; private set; }


		public static void Initialize(ModuleDefinition module)
		{
			ByteSizeAttributeDefinition = module.ImportCommonType<ByteSizeAttribute>();
			ByteSizeAttributeConstructor = module.ImportCommonConstructor<ByteSizeAttribute>(1);

			EditorOnlyAttributeDefinition = module.ImportCommonType<EditorOnlyAttribute>();
			EditorOnlyAttributeConstructor = module.ImportCommonConstructor<EditorOnlyAttribute>();

			StrippedAttributeDefinition = module.ImportCommonType<StrippedAttribute>();
			StrippedAttributeConstructor = module.ImportCommonConstructor<StrippedAttribute>();

			PersistentIDAttributeDefinition = module.ImportCommonType<PersistentIDAttribute>();
			PersistentIDAttributeConstructor = module.ImportCommonConstructor<PersistentIDAttribute>(1);

			ReleaseOnlyAttributeDefinition = module.ImportCommonType<ReleaseOnlyAttribute>();
			ReleaseOnlyAttributeConstructor = module.ImportCommonConstructor<ReleaseOnlyAttribute>();

			FixedLengthAttributeDefinition = module.ImportCommonType<FixedLengthAttribute>();
			FixedLengthAttributeConstructor = module.ImportCommonConstructor<FixedLengthAttribute>(1);

			EditorMetaFlagsAttributeDefinition = module.ImportCommonType<EditorMetaFlagsAttribute>();
			EditorMetaFlagsAttributeConstructor = module.ImportCommonConstructor<EditorMetaFlagsAttribute>(1);

			ReleaseMetaFlagsAttributeDefinition = module.ImportCommonType<ReleaseMetaFlagsAttribute>();
			ReleaseMetaFlagsAttributeConstructor = module.ImportCommonConstructor<ReleaseMetaFlagsAttribute>(1);

			RegisterAssemblyAttributeDefinition = module.ImportCommonType<RegisterAssemblyAttribute>();
			RegisterAssemblyAttributeConstructor = module.ImportCommonConstructor<RegisterAssemblyAttribute>(1);

			RegisterAssetTypeAttributeDefinition = module.ImportCommonType<RegisterAssetTypeAttribute>();
			RegisterAssetTypeAttributeConstructor = module.ImportCommonConstructor<RegisterAssetTypeAttribute>(3);

			UnityObjectBaseDefinition = module.ImportCommonType<UnityObjectBase>();
			UnityAssetBaseDefinition = module.ImportCommonType<UnityAssetBase>();
			UnityVersionDefinition = module.ImportCommonType<AssetRipper.Core.Parser.Files.UnityVersion>();
			TransferMetaFlagsDefinition = module.ImportCommonType<AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TransferMetaFlags>();

			AssetReaderDefinition = module.ImportCommonType<AssetRipper.Core.IO.Asset.AssetReader>();
			EndianReaderDefinition = module.ImportCommonType<AssetRipper.Core.IO.Endian.EndianReader>();
			AssetWriterDefinition = module.ImportCommonType<AssetRipper.Core.IO.Asset.AssetWriter>();
		}

		public static TypeReference ImportCommonType(this ModuleDefinition module, string typeFullName)
		{
			return module.ImportReference(LookupCommonType(typeFullName));
		}

		public static TypeReference ImportCommonType<T>(this ModuleDefinition module) => module.ImportCommonType(typeof(T).FullName);

		public static MethodReference ImportCommonConstructor<T>(this ModuleDefinition module) => module.ImportCommonConstructor(typeof(T).FullName, 0);
		public static MethodReference ImportCommonConstructor(this ModuleDefinition module, string typeFullName) => module.ImportCommonConstructor(typeFullName, 0);
		public static MethodReference ImportCommonConstructor<T>(this ModuleDefinition module, int numParameters) => module.ImportCommonConstructor(typeof(T).FullName, numParameters);
		public static MethodReference ImportCommonConstructor(this ModuleDefinition module, string typeFullName, int numParameters)
		{
			return module.ImportReference(LookupCommonType(typeFullName).GetConstructor(numParameters));
		}

		private static TypeDefinition LookupCommonType(string typeFullName) => Assembly.MainModule.GetType(typeFullName);
	}
}
