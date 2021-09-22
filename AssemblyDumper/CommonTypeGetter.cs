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

		public static TypeReference DebugOnlyAttributeDefinition { get; private set; }
		public static MethodReference DebugOnlyAttributeConstructor { get; private set; }

		public static TypeReference HideInEditorAttributeDefinition { get; private set; }
		public static MethodReference HideInEditorAttributeConstructor { get; private set; }

		public static TypeReference NotEditableAttributeDefinition { get; private set; }
		public static MethodReference NotEditableAttributeConstructor { get; private set; }

		public static TypeReference StrongPPtrAttributeDefinition { get; private set; }
		public static MethodReference StrongPPtrAttributeConstructor { get; private set; }

		public static TypeReference TreatAsBooleanAttributeDefinition { get; private set; }
		public static MethodReference TreatAsBooleanAttributeConstructor { get; private set; }

		public static TypeReference AlignBytesAttributeDefinition { get; private set; }
		public static MethodReference AlignBytesAttributeConstructor { get; private set; }

		public static TypeReference ChildAlignsBytesAttributeDefinition { get; private set; }
		public static MethodReference ChildAlignsBytesAttributeConstructor { get; private set; }

		public static TypeReference RegisterAssemblyAttributeDefinition { get; private set; }
		public static MethodReference RegisterAssemblyAttributeConstructor { get; private set; }

		public static TypeReference RegisterAssetTypeAttributeDefinition { get; private set; }
		public static MethodReference RegisterAssetTypeAttributeConstructor { get; private set; }

		public static TypeReference UnityObjectBaseDefinition { get; private set; }
		public static TypeReference UnityVersionDefinition { get; private set; }


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

			DebugOnlyAttributeDefinition = module.ImportCommonType<DebugOnlyAttribute>();
			DebugOnlyAttributeConstructor = module.ImportCommonConstructor<DebugOnlyAttribute>();

			HideInEditorAttributeDefinition = module.ImportCommonType<HideInEditorAttribute>();
			HideInEditorAttributeConstructor = module.ImportCommonConstructor<HideInEditorAttribute>();

			NotEditableAttributeDefinition = module.ImportCommonType<NotEditableAttribute>();
			NotEditableAttributeConstructor = module.ImportCommonConstructor<NotEditableAttribute>();

			StrongPPtrAttributeDefinition = module.ImportCommonType<StrongPPtrAttribute>();
			StrongPPtrAttributeConstructor = module.ImportCommonConstructor<StrongPPtrAttribute>();

			TreatAsBooleanAttributeDefinition = module.ImportCommonType<TreatAsBooleanAttribute>();
			TreatAsBooleanAttributeConstructor = module.ImportCommonConstructor<TreatAsBooleanAttribute>();

			AlignBytesAttributeDefinition = module.ImportCommonType<AlignBytesAttribute>();
			AlignBytesAttributeConstructor = module.ImportCommonConstructor<AlignBytesAttribute>();

			ChildAlignsBytesAttributeDefinition = module.ImportCommonType<ChildAlignsBytesAttribute>();
			ChildAlignsBytesAttributeConstructor = module.ImportCommonConstructor<ChildAlignsBytesAttribute>();

			RegisterAssemblyAttributeDefinition = module.ImportCommonType<RegisterAssemblyAttribute>();
			RegisterAssemblyAttributeConstructor = module.ImportCommonConstructor<RegisterAssemblyAttribute>(1);

			RegisterAssetTypeAttributeDefinition = module.ImportCommonType<RegisterAssetTypeAttribute>();
			RegisterAssetTypeAttributeConstructor = module.ImportCommonConstructor<RegisterAssetTypeAttribute>(3);

			UnityObjectBaseDefinition = module.ImportCommonType<UnityObjectBase>();
			UnityVersionDefinition = module.ImportCommonType<AssetRipper.Core.Parser.Files.UnityVersion>();
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
