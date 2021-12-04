using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Passes
{
	public static class Pass49_CreateEmptyMethods
	{
		private const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 49: Creating empty methods on generated types");

			var typeTreeNodeRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TypeTree.TypeTreeNode>();
			var typeTreeNodeListRef = SystemTypeGetter.List.MakeGenericInstanceType(typeTreeNodeRef);

			TypeReference commonPPtrTypeRef = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			TypeReference unityObjectBaseInterfaceRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			GenericInstanceType unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef);
			TypeReference ienumerableTypeRef = SharedState.Module.ImportSystemType("System.Collections.Generic.IEnumerable`1");
			GenericInstanceType enumerablePPtrsRef = ienumerableTypeRef.MakeGenericInstanceType(unityObjectBasePPtrRef);
			TypeReference dependencyContextRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.DependencyContext>();

			foreach (TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				var releaseReadDef = new MethodDefinition("ReadRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseReadDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));

				var editorReadDef = new MethodDefinition("ReadEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorReadDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));

				var releaseWriteDef = new MethodDefinition("WriteRelease", OverrideMethodAttributes, SystemTypeGetter.Void);
				releaseWriteDef.Parameters.Add(new("writer", ParameterAttributes.None, CommonTypeGetter.AssetWriterDefinition));

				var editorWriteDef = new MethodDefinition("WriteEditor", OverrideMethodAttributes, SystemTypeGetter.Void);
				editorWriteDef.Parameters.Add(new("writer", ParameterAttributes.None, CommonTypeGetter.AssetWriterDefinition));

				var releaseYamlDef = new MethodDefinition("ExportYAMLRelease", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				releaseYamlDef.Parameters.Add(new("container", ParameterAttributes.None, CommonTypeGetter.IExportContainerDefinition));

				var editorYamlDef = new MethodDefinition("ExportYAMLEditor", OverrideMethodAttributes, CommonTypeGetter.YAMLNodeDefinition);
				editorYamlDef.Parameters.Add(new("container", ParameterAttributes.None, CommonTypeGetter.IExportContainerDefinition));

				var releaseTypeTreeDef = new MethodDefinition("MakeReleaseTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				releaseTypeTreeDef.Parameters.Add(new("depth", ParameterAttributes.None, SystemTypeGetter.Int32));
				releaseTypeTreeDef.Parameters.Add(new("startingIndex", ParameterAttributes.None, SystemTypeGetter.Int32));

				var editorTypeTreeDef = new MethodDefinition("MakeEditorTypeTreeNodes", OverrideMethodAttributes, typeTreeNodeListRef);
				editorTypeTreeDef.Parameters.Add(new("depth", ParameterAttributes.None, SystemTypeGetter.Int32));
				editorTypeTreeDef.Parameters.Add(new("startingIndex", ParameterAttributes.None, SystemTypeGetter.Int32));

				var fetchDependenciesDef = new MethodDefinition("FetchDependencies", OverrideMethodAttributes, enumerablePPtrsRef);
				fetchDependenciesDef.Parameters.Add(new("context", ParameterAttributes.None, dependencyContextRef));

				type.Methods.Add(releaseReadDef);
				type.Methods.Add(editorReadDef);
				type.Methods.Add(releaseWriteDef);
				type.Methods.Add(editorWriteDef);
				type.Methods.Add(releaseYamlDef);
				type.Methods.Add(editorYamlDef);
				type.Methods.Add(releaseTypeTreeDef);
				type.Methods.Add(editorTypeTreeDef);
				type.Methods.Add(fetchDependenciesDef);
			}
		}
	}
}