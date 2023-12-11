using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.AST;
using AssetRipper.Assets;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Traversal;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass108_WalkMethods
{
	private enum State
	{
		Release,
		Editor,
		Standard,
	}

	private static State CurrentState { get; set; } = State.Standard;

	private static string MethodName => CurrentState switch
	{
		State.Release => nameof(IUnityAssetBase.WalkRelease),
		State.Editor => nameof(IUnityAssetBase.WalkEditor),
		_ => nameof(IUnityAssetBase.WalkStandard),
	};

	private static bool IsUsable(FieldNode node) => CurrentState switch
	{
		State.Release => !node.Property.IsEditorOnly && !node.Property.IsInjected,
		State.Editor => !node.Property.IsReleaseOnly && !node.Property.IsInjected,
		_ => true,
	};

	private static string GetName(FieldNode node) => CurrentState switch
	{
		State.Release or State.Editor => node.Property.OriginalFieldName!,
		_ => node.Field.Name!,
	};
#nullable disable
	private static TypeSignature assetWalkerType;

	private static IMethodDefOrRef enterAssetMethod;
	private static IMethodDefOrRef exitAssetMethod;

	private static IMethodDefOrRef enterFieldMethod;
	private static IMethodDefOrRef exitFieldMethod;

	private static IMethodDefOrRef enterListMethod;
	private static IMethodDefOrRef exitListMethod;

	private static IMethodDefOrRef enterDictionaryMethod;
	private static IMethodDefOrRef exitDictionaryMethod;

	private static IMethodDefOrRef enterPairMethod;
	private static IMethodDefOrRef exitPairMethod;

	private static IMethodDefOrRef visitPrimitiveMethod;
	private static IMethodDefOrRef visitPPtrMethod;

	private static TypeDefinition helperClass;
#nullable enable
	private static Dictionary<TypeSignature, IMethodDescriptor> MethodDictionary { get; } = new(SignatureComparer.Default);

	public static void DoPass()
	{
		Initialize();
		foreach (State state in (ReadOnlySpan<State>)[State.Release, State.Editor, State.Standard])
		{
			CurrentState = state;
			MethodDictionary.Clear();

			CreateEmptyMethods();

			helperClass = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.HelpersNamespace, MethodName + "Methods");
			helperClass.IsPublic = false;

			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					TypeNode rootNode = new(instance);

					TypeDefinition type = instance.Type;
					TypeSignature typeSignature = type.ToTypeSignature();

					CilInstructionCollection processor = type.GetMethodByName(MethodName).GetProcessor();

					if (group.IsPPtr)
					{
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Callvirt, visitPPtrMethod.MakeGenericInstanceMethod(typeSignature, Pass080_PPtrConversions.PPtrsToParameters[type].ToTypeSignature()));
						processor.Add(CilOpCodes.Ret);
					}
					else
					{
						CilInstructionLabel returnLabel = new();

						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Callvirt, enterAssetMethod.MakeGenericInstanceMethod(typeSignature));
						processor.Add(CilOpCodes.Brfalse, returnLabel);

						foreach (FieldNode fieldNode in rootNode.Children.Where(IsUsable))
						{
							CilInstructionLabel finishLabel = new();

							string fieldName = GetName(fieldNode);

							processor.Add(CilOpCodes.Ldarg_1);
							processor.Add(CilOpCodes.Ldstr, fieldName);
							processor.Add(CilOpCodes.Callvirt, enterFieldMethod.MakeGenericInstanceMethod(typeSignature));
							processor.Add(CilOpCodes.Brfalse, finishLabel);

							processor.Add(CilOpCodes.Ldarg_0);
							processor.Add(CilOpCodes.Ldfld, fieldNode.Field);
							processor.Add(CilOpCodes.Ldarg_1);
							processor.AddCall(GetOrMakeMethod(fieldNode.Child));

							processor.Add(CilOpCodes.Ldarg_1);
							processor.Add(CilOpCodes.Ldstr, fieldName);
							processor.Add(CilOpCodes.Callvirt, exitFieldMethod.MakeGenericInstanceMethod(typeSignature));

							finishLabel.Instruction = processor.Add(CilOpCodes.Nop);
						}

						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Callvirt, exitAssetMethod.MakeGenericInstanceMethod(typeSignature));

						returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					}
				}
			}
		}
	}

	private static IMethodDescriptor GetOrMakeMethod(Node node)
	{
		if (MethodDictionary.TryGetValue(node.TypeSignature, out IMethodDescriptor? cachedMethod))
		{
			return cachedMethod;
		}

		IMethodDescriptor result;
		switch (node)
		{
			case PrimitiveNode:
				{
					MethodDefinition method = NewMethod(node);
					CilInstructionCollection processor = method.GetProcessor();
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, visitPrimitiveMethod.MakeGenericInstanceMethod(node.TypeSignature));
					processor.Add(CilOpCodes.Ret);
					result = method;
				}
				break;
			case ListNode listNode:
				{
					MethodDefinition method = NewMethod(node);
					CilInstructionCollection processor = method.GetProcessor();

					CilInstructionLabel returnLabel = new();

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, enterListMethod.MakeGenericInstanceMethod(listNode.Child.TypeSignature));
					processor.Add(CilOpCodes.Brfalse, returnLabel);

					{
						MethodDefinition getCountDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == "get_Count");
						IMethodDefOrRef getCountReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, listNode.TypeSignature, getCountDefinition);
						MethodDefinition getItemDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == "get_Item");
						IMethodDefOrRef getItemReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, listNode.TypeSignature, getItemDefinition);

						IMethodDescriptor elementMethod = GetOrMakeMethod(listNode.Child);

						//Make local and store length in it
						CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
						processor.Add(CilOpCodes.Ldarg_0); //Load array
						processor.Add(CilOpCodes.Call, getCountReference); //Get count

						processor.Add(CilOpCodes.Stloc, countLocal); //Store it

						//Make an i
						CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
						processor.Owner.LocalVariables.Add(iLocal); //Add to method
						processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
						processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

						//Create an empty, unconditional branch which will jump down to the loop condition.
						//This converts the do..while loop into a for loop.
						CilInstructionLabel loopConditionStartLabel = new();
						processor.Add(CilOpCodes.Br, loopConditionStartLabel);

						//Now we just read pair, increment i, compare against count, and jump back to here if it's less
						ICilLabel jumpTargetLabel = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

						//Do stuff at index i
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldloc, iLocal);
						processor.Add(CilOpCodes.Call, getItemReference);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.AddCall(elementMethod);

						//Increment i
						processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
						processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
						processor.Add(CilOpCodes.Add); //Add 
						processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

						//Jump to start of loop if i < count
						loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Nop);
						processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
						processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
						processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
					}

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, exitListMethod.MakeGenericInstanceMethod(listNode.Child.TypeSignature));

					returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					result = method;
				}
				break;
			case DictionaryNode dictionaryNode:
				{
					MethodDefinition method = NewMethod(node);
					CilInstructionCollection processor = method.GetProcessor();

					CilInstructionLabel returnLabel = new();

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, enterDictionaryMethod.MakeGenericInstanceMethod([.. dictionaryNode.TypeSignature.TypeArguments]));
					processor.Add(CilOpCodes.Brfalse, returnLabel);

					{
						MethodDefinition getCountDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == "get_Count");
						IMethodDefOrRef getCountReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, dictionaryNode.TypeSignature, getCountDefinition);
						MethodDefinition getItemDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == "GetPair");
						IMethodDefOrRef getItemReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, dictionaryNode.TypeSignature, getItemDefinition);

						IMethodDescriptor pairMethod = GetOrMakeMethod(dictionaryNode.Child);

						//Make local and store length in it
						CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
						processor.Add(CilOpCodes.Ldarg_0); //Load array
						processor.Add(CilOpCodes.Call, getCountReference); //Get count

						processor.Add(CilOpCodes.Stloc, countLocal); //Store it

						//Make an i
						CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
						processor.Owner.LocalVariables.Add(iLocal); //Add to method
						processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
						processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

						//Create an empty, unconditional branch which will jump down to the loop condition.
						//This converts the do..while loop into a for loop.
						CilInstructionLabel loopConditionStartLabel = new();
						processor.Add(CilOpCodes.Br, loopConditionStartLabel);

						//Now we just read pair, increment i, compare against count, and jump back to here if it's less
						ICilLabel jumpTargetLabel = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

						//Do stuff at index i
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldloc, iLocal);
						processor.Add(CilOpCodes.Call, getItemReference);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.AddCall(pairMethod);

						//Increment i
						processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
						processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
						processor.Add(CilOpCodes.Add); //Add 
						processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

						//Jump to start of loop if i < count
						loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Nop);
						processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
						processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
						processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
					}

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, exitDictionaryMethod.MakeGenericInstanceMethod([.. dictionaryNode.TypeSignature.TypeArguments]));

					returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					result = method;
				}
				break;
			case PairNode pairNode:
				{
					MethodDefinition method = NewMethod(node);
					CilInstructionCollection processor = method.GetProcessor();

					CilInstructionLabel returnLabel = new();

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, enterPairMethod.MakeGenericInstanceMethod([.. pairNode.TypeSignature.TypeArguments]));
					processor.Add(CilOpCodes.Brfalse, returnLabel);

					{
						MethodDefinition getKeyDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetPair<,>), m => m.Name == "get_Key");
						IMethodDefOrRef getKeyReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairNode.TypeSignature, getKeyDefinition);
						MethodDefinition getValueDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetPair<,>), m => m.Name == "get_Value");
						IMethodDefOrRef getValueReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairNode.TypeSignature, getValueDefinition);

						IMethodDescriptor keyMethod = GetOrMakeMethod(pairNode.Key);
						IMethodDescriptor valueMethod = GetOrMakeMethod(pairNode.Value);

						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Call, getKeyReference);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.AddCall(keyMethod);

						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Call, getValueReference);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.AddCall(valueMethod);
					}

					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, exitPairMethod.MakeGenericInstanceMethod([.. pairNode.TypeSignature.TypeArguments]));

					returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					result = method;
				}
				break;
			case KeyNode keyNode:
				return GetOrMakeMethod(keyNode.Child);
			case ValueNode valueNode:
				return GetOrMakeMethod(valueNode.Child);
			default:
				{
					MethodDefinition method = NewMethod(node);
					CilInstructionCollection processor = method.GetProcessor();
					processor.Add(CilOpCodes.Ret);
					result = method;
				}
				break;
		}
		MethodDictionary.Add(node.TypeSignature, result);
		return result;

		static MethodDefinition NewMethod(Node node)
		{
			MethodDefinition method = helperClass.AddMethod(
				UniqueNameFactory.MakeUniqueName(node.TypeSignature),
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
				SharedState.Instance.Importer.Void);
			method.AddParameter(node.TypeSignature, "value");
			method.AddParameter(assetWalkerType, "walker");
			return method;
		}
	}

	private static void Initialize()
	{
		assetWalkerType = SharedState.Instance.Importer.ImportType<AssetWalker>().ToTypeSignature();

		enterAssetMethod = ImportWalkerMethod(nameof(AssetWalker.EnterAsset));
		exitAssetMethod = ImportWalkerMethod(nameof(AssetWalker.ExitAsset));

		enterFieldMethod = ImportWalkerMethod(nameof(AssetWalker.EnterField));
		exitFieldMethod = ImportWalkerMethod(nameof(AssetWalker.ExitField));

		enterListMethod = ImportWalkerMethod(nameof(AssetWalker.EnterList));
		exitListMethod = ImportWalkerMethod(nameof(AssetWalker.ExitList));

		enterDictionaryMethod = ImportWalkerMethod(nameof(AssetWalker.EnterDictionary));
		exitDictionaryMethod = ImportWalkerMethod(nameof(AssetWalker.ExitDictionary));

		enterPairMethod = ImportWalkerMethod(nameof(AssetWalker.EnterPair));
		exitPairMethod = ImportWalkerMethod(nameof(AssetWalker.ExitPair));

		visitPrimitiveMethod = ImportWalkerMethod(nameof(AssetWalker.VisitPrimitive));
		visitPPtrMethod = ImportWalkerMethod(nameof(AssetWalker.VisitPPtr));
	}

	private static void CreateEmptyMethods()
	{
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (TypeDefinition type in group.Types)
			{
				MethodDictionary.Add(type.ToTypeSignature(), AddMethod(type, MethodName, assetWalkerType));
			}
		}

		static MethodDefinition AddMethod(TypeDefinition type, string methodName, TypeSignature assetWalkerType)
		{
			MethodDefinition method = type.AddMethod(methodName, Pass063_CreateEmptyMethods.OverrideMethodAttributes, SharedState.Instance.Importer.Void);
			method.AddParameter(assetWalkerType, "walker");
			return method;
		}
	}

	private static IMethodDefOrRef ImportWalkerMethod(string methodName)
	{
		return SharedState.Instance.Importer.ImportMethod<AssetWalker>(m =>
		{
			return m.Name == methodName && (methodName != nameof(AssetWalker.VisitPPtr) || m.GenericParameters.Count == 2);
		});
	}

	private static CilInstruction AddCall(this CilInstructionCollection processor, IMethodDescriptor method)
	{
		return method is MethodDefinition { IsStatic: true }
			? processor.Add(CilOpCodes.Call, method)
			: processor.Add(CilOpCodes.Callvirt, method);
	}
}