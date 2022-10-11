using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;
using AssetRipper.IO.Files.SerializedFiles.Parser.TypeTrees;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass900_FillTypeTreeMethods
	{
		private const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static ITypeDefOrRef typeTreeNode;
		private static IMethodDefOrRef typeTreeNodeConstructor;
		private static GenericInstanceTypeSignature typeTreeNodeList;
		private static IMethodDefOrRef typeTreeNodeListConstructor;
		private static IMethodDefOrRef listAddMethod;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static bool generateEmptyMethods = true;

		public static void DoPass()
		{
			typeTreeNode = SharedState.Instance.Importer.ImportType<TypeTreeNode>();
			typeTreeNodeConstructor = SharedState.Instance.Importer.ImportConstructor<TypeTreeNode>(8);
			typeTreeNodeList = SharedState.Instance.Importer.ImportType(typeof(List<>)).MakeGenericInstanceType(typeTreeNode.ToTypeSignature());
			typeTreeNodeListConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, typeTreeNodeList, 0);
			listAddMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				typeTreeNodeList,
				SharedState.Instance.Importer.LookupMethod(typeof(List<>), m => m.Name == "Add"));

			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				if (group.ID == 129) //PlayerSettings
				{
					continue;
				}

				foreach (GeneratedClassInstance instance in group.Instances)
				{
					TypeDefinition type = instance.Type;
					UniversalClass klass = instance.Class;

					MethodDefinition editorModeMethod = type.AddMethod(nameof(UnityAssetBase.MakeEditorTypeTreeNodes), OverrideMethodAttributes, typeTreeNodeList);
					editorModeMethod.AddParameter(SharedState.Instance.Importer.Int32, "depth");
					editorModeMethod.AddParameter(SharedState.Instance.Importer.Int32, "startingIndex");

					MethodDefinition releaseModeMethod = type.AddMethod(nameof(UnityAssetBase.MakeReleaseTypeTreeNodes), OverrideMethodAttributes, typeTreeNodeList);
					releaseModeMethod.AddParameter(SharedState.Instance.Importer.Int32, "depth");
					releaseModeMethod.AddParameter(SharedState.Instance.Importer.Int32, "startingIndex");

					CilMethodBody editorModeBody = editorModeMethod.CilMethodBody!;
					CilMethodBody releaseModeBody = releaseModeMethod.CilMethodBody!;

					CilInstructionCollection editorModeProcessor = editorModeBody.Instructions;
					CilInstructionCollection releaseModeProcessor = releaseModeBody.Instructions;

					//Console.WriteLine($"Generating the editor read method for {name}");
					if (klass.EditorRootNode == null || generateEmptyMethods)
					{
						editorModeProcessor.AddNotSupportedException();
					}
					else
					{
						editorModeProcessor.AddTypeTreeCreation(klass.EditorRootNode);
					}

					//Console.WriteLine($"Generating the release read method for {name}");
					if (klass.ReleaseRootNode == null || generateEmptyMethods)
					{
						releaseModeProcessor.AddNotSupportedException();
					}
					else
					{
						releaseModeProcessor.AddTypeTreeCreation(klass.ReleaseRootNode);
					}

					editorModeProcessor.OptimizeMacros();
					releaseModeProcessor.OptimizeMacros();
				}
			}
		}

		private static void AddTypeTreeCreation(this CilInstructionCollection processor, UniversalNode rootNode)
		{
			processor.Add(CilOpCodes.Newobj, typeTreeNodeListConstructor);

			processor.AddTreeNodesRecursively(rootNode, 0, 0);

			processor.Add(CilOpCodes.Ret);
		}

		/// <summary>
		/// Emits all the type tree nodes recursively
		/// </summary>
		/// <param name="processor">The IL processor emitting the code</param>
		/// <param name="listVariable">The local list variable that type tree nodes are added to</param>
		/// <param name="node">The Unity node being emitted as a type tree node</param>
		/// <param name="currentIndex">The index of the emitted tree node relative to the root node</param>
		/// <returns>The relative index of the next tree node to be emitted</returns>
		private static int AddTreeNodesRecursively(this CilInstructionCollection processor, UniversalNode node, int currentIndex, int currentLevel)
		{
			processor.AddSingleTreeNode(node, currentIndex, currentLevel);
			currentIndex++;
			foreach (UniversalNode subNode in node.SubNodes)
			{
				currentIndex = processor.AddTreeNodesRecursively(subNode, currentIndex, currentLevel + 1);
			}
			return currentIndex;
		}

		private static void AddSingleTreeNode(this CilInstructionCollection processor, UniversalNode node, int currentIndex, int currentLevel)
		{
			//For the add method at the end
			processor.Add(CilOpCodes.Dup);

			processor.Add(CilOpCodes.Ldstr, node.OriginalTypeName);
			processor.Add(CilOpCodes.Ldstr, node.OriginalName);

			//Level
			processor.Add(CilOpCodes.Ldarg_1);//depth
			processor.Add(CilOpCodes.Ldc_I4, currentLevel);//Root node is level zero
			processor.Add(CilOpCodes.Add);

			processor.Add(CilOpCodes.Ldc_I4, -1);//Byte size not included

			//Index
			processor.Add(CilOpCodes.Ldarg_2);//starting index
			processor.Add(CilOpCodes.Ldc_I4, currentIndex);
			processor.Add(CilOpCodes.Add);

			processor.Add(CilOpCodes.Ldc_I4, node.Version);
			processor.Add(CilOpCodes.Ldc_I4, 0);//Type flags not included
			processor.Add(CilOpCodes.Ldc_I4, unchecked((int)node.MetaFlag));
			processor.Add(CilOpCodes.Newobj, typeTreeNodeConstructor);

			processor.Add(CilOpCodes.Call, listAddMethod);
		}
	}
}
