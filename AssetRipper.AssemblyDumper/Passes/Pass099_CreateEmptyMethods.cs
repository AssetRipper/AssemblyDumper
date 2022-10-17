using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.Export.Dependencies;
using AssetRipper.Assets.IO.Reading;
using AssetRipper.Assets.IO.Writing;
using AssetRipper.Assets.Metadata;
using AssetRipper.Yaml;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass099_CreateEmptyMethods
	{
		private const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			TypeSignature commonPPtrTypeRef = SharedState.Instance.Importer.ImportTypeSignature(typeof(PPtr<>));
			TypeSignature unityObjectBaseInterfaceRef = SharedState.Instance.Importer.ImportTypeSignature<IUnityObjectBase>();
			GenericInstanceTypeSignature unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef);
			TypeSignature ienumerableTypeRef = SharedState.Instance.Importer.ImportTypeSignature(typeof(IEnumerable<>));
			GenericInstanceTypeSignature enumerablePPtrsRef = ienumerableTypeRef.MakeGenericInstanceType(unityObjectBasePPtrRef);
			TypeSignature dependencyContextRef = SharedState.Instance.Importer.ImportTypeSignature<DependencyContext>();
			TypeSignature assetReaderRef = SharedState.Instance.Importer.ImportTypeSignature<AssetReader>();
			TypeSignature assetWriterRef = SharedState.Instance.Importer.ImportTypeSignature<AssetWriter>();
			TypeSignature exportContainerInterfaceRef = SharedState.Instance.Importer.ImportTypeSignature<IExportContainer>();
			TypeSignature yamlNodeRef = SharedState.Instance.Importer.ImportTypeSignature<YamlNode>();

			foreach (TypeDefinition type in SharedState.Instance.AllNonInterfaceTypes)
			{
				MethodDefinition? releaseReadDef = type.AddMethod(nameof(UnityAssetBase.ReadRelease), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
				releaseReadDef.AddParameter(assetReaderRef, "reader");

				MethodDefinition? editorReadDef = type.AddMethod(nameof(UnityAssetBase.ReadEditor), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
				editorReadDef.AddParameter(assetReaderRef, "reader");

				MethodDefinition? releaseWriteDef = type.AddMethod(nameof(UnityAssetBase.WriteRelease), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
				releaseWriteDef.AddParameter(assetWriterRef, "writer");

				MethodDefinition? editorWriteDef = type.AddMethod(nameof(UnityAssetBase.WriteEditor), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
				editorWriteDef.AddParameter(assetWriterRef, "writer");

				MethodDefinition? releaseYamlDef = type.AddMethod(nameof(UnityAssetBase.ExportYamlRelease), OverrideMethodAttributes, yamlNodeRef);
				releaseYamlDef.AddParameter(exportContainerInterfaceRef, "container");

				MethodDefinition? editorYamlDef = type.AddMethod(nameof(UnityAssetBase.ExportYamlEditor), OverrideMethodAttributes, yamlNodeRef);
				editorYamlDef.AddParameter(exportContainerInterfaceRef, "container");

				MethodDefinition? fetchDependenciesDef = type.AddMethod(nameof(UnityAssetBase.FetchDependencies), OverrideMethodAttributes, enumerablePPtrsRef);
				fetchDependenciesDef.AddParameter(dependencyContextRef, "context");

				type.AddMethod(nameof(UnityAssetBase.Reset), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
			}
		}
	}
}