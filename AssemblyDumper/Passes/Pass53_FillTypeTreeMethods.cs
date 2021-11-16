using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TypeTree;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass53_FillTypeTreeMethods
	{
		private static TypeReference typeTreeNode;
		private static MethodReference typeTreeNodeConstructor;
		private static GenericInstanceType typeTreeNodeList;
		private static MethodReference typeTreeNodeListConstructor;
		private static MethodReference listAddMethod;
		public static void DoPass()
		{
			Logger.Info("Pass 53: Fill Type Tree Methods");

			typeTreeNode = SharedState.Module.ImportCommonType<TypeTreeNode>();
			typeTreeNodeConstructor = SharedState.Module.ImportCommonConstructor<TypeTreeNode>(8);
			typeTreeNodeList = SystemTypeGetter.List.MakeGenericInstanceType(typeTreeNode);
			typeTreeNodeListConstructor = MethodUtils.MakeConstructorOnGenericType(typeTreeNodeList, 0);
			listAddMethod = MethodUtils.MakeMethodOnGenericType(typeTreeNodeList.Resolve().Methods.First(m => m.Name == "Add"), typeTreeNodeList);

			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeYamlMethod = type.Methods.First(m => m.Name == "MakeEditorTypeTreeNodes");
				var releaseModeYamlMethod = type.Methods.First(m => m.Name == "MakeReleaseTypeTreeNodes");

				var editorModeBody = editorModeYamlMethod.Body = new(editorModeYamlMethod);
				var releaseModeBody = releaseModeYamlMethod.Body = new(releaseModeYamlMethod);

				var editorModeProcessor = editorModeBody.GetILProcessor();
				var releaseModeProcessor = releaseModeBody.GetILProcessor();
				
				//Logger.Info($"Generating the editor read method for {name}");
				if (klass.EditorRootNode == null)
				{
					editorModeProcessor.EmitNotSupportedException();
				}
				else
				{
					editorModeProcessor.EmitTypeTreeCreation(klass.EditorRootNode);
				}

				//Logger.Info($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode == null)
				{
					releaseModeProcessor.EmitNotSupportedException();
				}
				else
				{
					releaseModeProcessor.EmitTypeTreeCreation(klass.ReleaseRootNode);
				}

				editorModeBody.Optimize();
				releaseModeBody.Optimize();
			}
		}

		private static void EmitNotSupportedException(this ILProcessor processor)
		{
			processor.Emit(OpCodes.Newobj, SystemTypeGetter.NotSupportedExceptionConstructor);
			processor.Emit(OpCodes.Throw);
		}

		private static void EmitTypeTreeCreation(this ILProcessor processor, UnityNode rootNode)
		{
			processor.Body.InitLocals = true;
			VariableDefinition resultVariable = new(typeTreeNodeList);
			processor.Body.Variables.Add(resultVariable);

			processor.Emit(OpCodes.Newobj, typeTreeNodeListConstructor);
			processor.Emit(OpCodes.Stloc, resultVariable);

			processor.EmitTreeNodesRecursively(resultVariable, rootNode, 0);

			processor.Emit(OpCodes.Ldloc, resultVariable);
			processor.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits all the type tree nodes recursively
		/// </summary>
		/// <param name="processor">The IL processor emitting the code</param>
		/// <param name="listVariable">The local list variable that type tree nodes are added to</param>
		/// <param name="node">The Unity node being emitted as a type tree node</param>
		/// <param name="currentIndex">The index of the emitted tree node relative to the root node</param>
		/// <returns>The relative index of the next tree node to be emitted</returns>
		private static int EmitTreeNodesRecursively(this ILProcessor processor, VariableDefinition listVariable, UnityNode node, int currentIndex)
		{
			processor.EmitSingleTreeNode(listVariable, node, currentIndex);
			currentIndex++;
			foreach (var subNode in node.SubNodes)
			{
				currentIndex = processor.EmitTreeNodesRecursively(listVariable, subNode, currentIndex);
			}
			return currentIndex;
		}

		private static void EmitSingleTreeNode(this ILProcessor processor, VariableDefinition listVariable, UnityNode node, int currentIndex)
		{
			//For the add method at the end
			processor.Emit(OpCodes.Ldloc, listVariable);

			processor.Emit(OpCodes.Ldstr, node.TypeName);
			processor.Emit(OpCodes.Ldstr, node.Name);

			//Level
			processor.Emit(OpCodes.Ldarg_1);//depth
			processor.Emit(OpCodes.Ldc_I4, (int)node.Level);//Because of recalculation in Pass 4, the root node is always zero
			processor.Emit(OpCodes.Add);

			processor.Emit(OpCodes.Ldc_I4, node.ByteSize);

			//Index
			processor.Emit(OpCodes.Ldarg_2);//starting index
			processor.Emit(OpCodes.Ldc_I4, currentIndex);
			processor.Emit(OpCodes.Add);

			processor.Emit(OpCodes.Ldc_I4, node.Version);
			processor.Emit(OpCodes.Ldc_I4, (int)node.TypeFlags);
			processor.Emit(OpCodes.Ldc_I4, node.MetaFlag);
			processor.Emit(OpCodes.Newobj, typeTreeNodeConstructor);

			processor.Emit(OpCodes.Call, listAddMethod);
		}
	}
}
