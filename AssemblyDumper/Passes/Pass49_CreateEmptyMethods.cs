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

			ITypeDefOrRef? typeTreeNodeRef = SharedState.Importer.ImportCommonType<TypeTreeNode>();
			GenericInstanceTypeSignature? typeTreeNodeListRef = SystemTypeGetter.List.MakeGenericInstanceType(typeTreeNodeRef.ToTypeSignature());

			ITypeDefOrRef commonPPtrTypeRef = SharedState.Importer.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			ITypeDefOrRef unityObjectBaseInterfaceRef = SharedState.Importer.ImportCommonType<IUnityObjectBase>();
			GenericInstanceTypeSignature unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef.ToTypeSignature());
			ITypeDefOrRef ienumerableTypeRef = SharedState.Importer.ImportSystemType("System.Collections.Generic.IEnumerable`1");
			GenericInstanceTypeSignature enumerablePPtrsRef = ienumerableTypeRef.MakeGenericInstanceType(unityObjectBasePPtrRef);
			ITypeDefOrRef dependencyContextRef = SharedState.Importer.ImportCommonType<DependencyContext>();

			foreach (TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				MethodDefinition? releaseReadDef = type.AddMethod("ReadRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseReadDef.AddParameter("reader", CommonTypeGetter.AssetReaderDefinition);

				MethodDefinition? editorReadDef = type.AddMethod("ReadEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorReadDef.AddParameter("reader", CommonTypeGetter.AssetReaderDefinition);

				MethodDefinition? releaseWriteDef = type.AddMethod("WriteRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseWriteDef.AddParameter("writer", CommonTypeGetter.AssetWriterDefinition);

				MethodDefinition? editorWriteDef = type.AddMethod("WriteEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorWriteDef.AddParameter("writer", CommonTypeGetter.AssetWriterDefinition);

				MethodDefinition? releaseYamlDef = type.AddMethod("ExportYAMLRelease", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				releaseYamlDef.AddParameter("container", CommonTypeGetter.IExportContainerDefinition);

				MethodDefinition? editorYamlDef = type.AddMethod("ExportYAMLEditor", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				editorYamlDef.AddParameter("container", CommonTypeGetter.IExportContainerDefinition);

				MethodDefinition? releaseTypeTreeDef = type.AddMethod("MakeReleaseTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				releaseTypeTreeDef.AddParameter("depth", SystemTypeGetter.Int32);
				releaseTypeTreeDef.AddParameter("startingIndex", SystemTypeGetter.Int32);

				MethodDefinition? editorTypeTreeDef = type.AddMethod("MakeEditorTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				editorTypeTreeDef.AddParameter("depth", SystemTypeGetter.Int32);
				editorTypeTreeDef.AddParameter("startingIndex", SystemTypeGetter.Int32);

				MethodDefinition? fetchDependenciesDef = type.AddMethod("FetchDependencies", OverrideMethodAttributes, enumerablePPtrsRef);
				fetchDependenciesDef.AddParameter("context", dependencyContextRef);
			}
		}
	}
}