using AssemblyDumper.Utils;
using AssetRipper.Core.Interfaces;
using AssetRipper.Core.Parser.Asset;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TypeTree;

namespace AssemblyDumper.Passes
{
	public static class Pass49_CreateEmptyMethods
	{
		private const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 49: Creating empty methods on generated types");

			var typeTreeNodeRef = SharedState.Importer.ImportCommonType<TypeTreeNode>();
			var typeTreeNodeListRef = SystemTypeGetter.List.MakeGenericInstanceType(typeTreeNodeRef.ToTypeSignature());

			ITypeDefOrRef commonPPtrTypeRef = SharedState.Importer.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			ITypeDefOrRef unityObjectBaseInterfaceRef = SharedState.Importer.ImportCommonType<IUnityObjectBase>();
			GenericInstanceTypeSignature unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef.ToTypeSignature());
			ITypeDefOrRef ienumerableTypeRef = SharedState.Importer.ImportSystemType("System.Collections.Generic.IEnumerable`1");
			GenericInstanceTypeSignature enumerablePPtrsRef = ienumerableTypeRef.MakeGenericInstanceType(unityObjectBasePPtrRef);
			ITypeDefOrRef dependencyContextRef = SharedState.Importer.ImportCommonType<DependencyContext>();

			foreach (TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				var releaseReadDef = type.AddMethod("ReadRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseReadDef.AddParameter("reader", CommonTypeGetter.AssetReaderDefinition);

				var editorReadDef = type.AddMethod("ReadEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorReadDef.AddParameter("reader", CommonTypeGetter.AssetReaderDefinition);

				var releaseWriteDef = type.AddMethod("WriteRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseWriteDef.AddParameter("writer", CommonTypeGetter.AssetWriterDefinition);

				var editorWriteDef = type.AddMethod("WriteEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorWriteDef.AddParameter("writer", CommonTypeGetter.AssetWriterDefinition);

				var releaseYamlDef = type.AddMethod("ExportYAMLRelease", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				releaseYamlDef.AddParameter("container", CommonTypeGetter.IExportContainerDefinition);

				var editorYamlDef = type.AddMethod("ExportYAMLEditor", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				editorYamlDef.AddParameter("container", CommonTypeGetter.IExportContainerDefinition);

				var releaseTypeTreeDef = type.AddMethod("MakeReleaseTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				releaseTypeTreeDef.AddParameter("depth", SystemTypeGetter.Int32);
				releaseTypeTreeDef.AddParameter("startingIndex", SystemTypeGetter.Int32);

				var editorTypeTreeDef = type.AddMethod("MakeEditorTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				editorTypeTreeDef.AddParameter("depth", SystemTypeGetter.Int32);
				editorTypeTreeDef.AddParameter("startingIndex", SystemTypeGetter.Int32);

				var fetchDependenciesDef = type.AddMethod("FetchDependencies", OverrideMethodAttributes, enumerablePPtrsRef);
				fetchDependenciesDef.AddParameter("context", dependencyContextRef);
			}
		}
	}
}