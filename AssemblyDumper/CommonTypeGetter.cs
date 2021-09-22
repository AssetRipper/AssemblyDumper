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


		public static void Initialize(ModuleDefinition module)
		{
			ByteSizeAttributeDefinition = module.ImportCommonType<ByteSizeAttribute>();
			ByteSizeAttributeConstructor = module.ImportConstructor<ByteSizeAttribute>(1);

			EditorOnlyAttributeDefinition = module.ImportCommonType<EditorOnlyAttribute>();
			EditorOnlyAttributeConstructor = module.ImportConstructor<EditorOnlyAttribute>();

			StrippedAttributeDefinition = module.ImportCommonType<StrippedAttribute>();
			StrippedAttributeConstructor = module.ImportConstructor<StrippedAttribute>();

			PersistentIDAttributeDefinition = module.ImportCommonType<PersistentIDAttribute>();
			PersistentIDAttributeConstructor = module.ImportConstructor<PersistentIDAttribute>(1);

			ReleaseOnlyAttributeDefinition = module.ImportCommonType<ReleaseOnlyAttribute>();
			ReleaseOnlyAttributeConstructor = module.ImportConstructor<ReleaseOnlyAttribute>();

			DebugOnlyAttributeDefinition = module.ImportCommonType<DebugOnlyAttribute>();
			DebugOnlyAttributeConstructor = module.ImportConstructor<DebugOnlyAttribute>();

			HideInEditorAttributeDefinition = module.ImportCommonType<HideInEditorAttribute>();
			HideInEditorAttributeConstructor = module.ImportConstructor<HideInEditorAttribute>();

			NotEditableAttributeDefinition = module.ImportCommonType<NotEditableAttribute>();
			NotEditableAttributeConstructor = module.ImportConstructor<NotEditableAttribute>();

			StrongPPtrAttributeDefinition = module.ImportCommonType<StrongPPtrAttribute>();
			StrongPPtrAttributeConstructor = module.ImportConstructor<StrongPPtrAttribute>();

			TreatAsBooleanAttributeDefinition = module.ImportCommonType<TreatAsBooleanAttribute>();
			TreatAsBooleanAttributeConstructor = module.ImportConstructor<TreatAsBooleanAttribute>();

			AlignBytesAttributeDefinition = module.ImportCommonType<AlignBytesAttribute>();
			AlignBytesAttributeConstructor = module.ImportConstructor<AlignBytesAttribute>();

			ChildAlignsBytesAttributeDefinition = module.ImportCommonType<ChildAlignsBytesAttribute>();
			ChildAlignsBytesAttributeConstructor = module.ImportConstructor<ChildAlignsBytesAttribute>();

			RegisterAssemblyAttributeDefinition = module.ImportCommonType<RegisterAssemblyAttribute>();
			RegisterAssemblyAttributeConstructor = module.ImportConstructor<RegisterAssemblyAttribute>(1);

			RegisterAssetTypeAttributeDefinition = module.ImportCommonType<RegisterAssetTypeAttribute>();
			RegisterAssetTypeAttributeConstructor = module.ImportConstructor<RegisterAssetTypeAttribute>(3);

			UnityObjectBaseDefinition = module.ImportCommonType<UnityObjectBase>();
		}

		public static TypeReference ImportCommonType(this ModuleDefinition module, string typeFullName)
		{
			return module.ImportReference(LookupCommonType(typeFullName));
		}

		public static TypeReference ImportCommonType<T>(this ModuleDefinition module) => module.ImportCommonType(typeof(T).FullName);

		private static MethodReference ImportConstructor<T>(this ModuleDefinition module) => module.ImportConstructor(typeof(T).FullName, 0);
		private static MethodReference ImportConstructor(this ModuleDefinition module, string typeFullName) => module.ImportConstructor(typeFullName, 0);
		private static MethodReference ImportConstructor<T>(this ModuleDefinition module, int numParameters) => module.ImportConstructor(typeof(T).FullName, numParameters);
		private static MethodReference ImportConstructor(this ModuleDefinition module, string typeFullName, int numParameters)
		{
			return module.ImportReference(LookupCommonType(typeFullName).GetConstructor(numParameters));
		}

		private static TypeDefinition LookupCommonType(string typeFullName) => Assembly.MainModule.GetType(typeFullName);
	}
}
