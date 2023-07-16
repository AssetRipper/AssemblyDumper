using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.IO.Serialization;
using AssetRipper.Assets.IO.Writing;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Endian;
using AssetRipper.Yaml;
using System.Text.Json.Nodes;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass060_CreateEmptyMethods
	{
		public const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			TypeSignature commonPPtrTypeRef = SharedState.Instance.Importer.ImportTypeSignature(typeof(PPtr<>));
			TypeSignature unityObjectBaseInterfaceRef = SharedState.Instance.Importer.ImportTypeSignature<IUnityObjectBase>();
			GenericInstanceTypeSignature unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef);
			TypeSignature refEndianSpanReaderRef = SharedState.Instance.Importer.ImportTypeSignature(typeof(EndianSpanReader)).MakeByReferenceType();
			TypeSignature assetWriterRef = SharedState.Instance.Importer.ImportTypeSignature<AssetWriter>();
			TypeSignature exportContainerInterfaceRef = SharedState.Instance.Importer.ImportTypeSignature<IExportContainer>();
			TypeSignature yamlNodeRef = SharedState.Instance.Importer.ImportTypeSignature<YamlNode>();
			TypeSignature jsonNodeRef = SharedState.Instance.Importer.ImportTypeSignature<JsonNode>();
			TypeSignature serializerRef = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetSerializer>();
			TypeSignature serializationOptionsRef = SharedState.Instance.Importer.ImportTypeSignature<SerializationOptions>();
			TypeSignature deserializerRef = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetDeserializer>();
			TypeSignature deserializationOptionsRef = SharedState.Instance.Importer.ImportTypeSignature<DeserializationOptions>();

			foreach (TypeDefinition type in SharedState.Instance.AllNonInterfaceTypes)
			{
				type.AddMethod(nameof(UnityAssetBase.ReadRelease), OverrideMethodAttributes, SharedState.Instance.Importer.Void)
					.AddParameter(refEndianSpanReaderRef, "reader");

				type.AddMethod(nameof(UnityAssetBase.ReadEditor), OverrideMethodAttributes, SharedState.Instance.Importer.Void)
					.AddParameter(refEndianSpanReaderRef, "reader");

				type.AddMethod(nameof(UnityAssetBase.WriteRelease), OverrideMethodAttributes, SharedState.Instance.Importer.Void)
					.AddParameter(assetWriterRef, "writer");

				type.AddMethod(nameof(UnityAssetBase.WriteEditor), OverrideMethodAttributes, SharedState.Instance.Importer.Void)
					.AddParameter(assetWriterRef, "writer");

				type.AddMethod(nameof(UnityAssetBase.ExportYamlRelease), OverrideMethodAttributes, yamlNodeRef)
					.AddParameter(exportContainerInterfaceRef, "container");

				type.AddMethod(nameof(UnityAssetBase.ExportYamlEditor), OverrideMethodAttributes, yamlNodeRef)
					.AddParameter(exportContainerInterfaceRef, "container");

				type.AddSerializeMethod(nameof(UnityAssetBase.SerializeReleaseFields), jsonNodeRef, serializerRef, serializationOptionsRef);

				type.AddSerializeMethod(nameof(UnityAssetBase.SerializeEditorFields), jsonNodeRef, serializerRef, serializationOptionsRef);

				type.AddSerializeMethod(nameof(UnityAssetBase.SerializeAllFields), jsonNodeRef, serializerRef, serializationOptionsRef);

				MethodDefinition deserializeMethod = type.AddMethod(nameof(UnityAssetBase.Deserialize), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
				deserializeMethod.AddParameter(jsonNodeRef, "node");
				deserializeMethod.AddParameter(deserializerRef, "deserializer");
				deserializeMethod.AddParameter(deserializationOptionsRef, "options");

				type.AddMethod(nameof(UnityAssetBase.Reset), OverrideMethodAttributes, SharedState.Instance.Importer.Void);
			}
		}

		private static void AddSerializeMethod(this TypeDefinition type, string methodName, TypeSignature jsonNodeRef, TypeSignature serializerRef, TypeSignature serializationOptionsRef)
		{
			MethodDefinition method = type.AddMethod(methodName, OverrideMethodAttributes, jsonNodeRef);
			method.AddParameter(serializerRef, "serializer");
			method.AddParameter(serializationOptionsRef, "options");
		}
	}
}