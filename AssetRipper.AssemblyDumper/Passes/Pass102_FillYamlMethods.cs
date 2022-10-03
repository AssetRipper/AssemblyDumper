using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core;
using AssetRipper.Core.IO;
using AssetRipper.Core.IO.Extensions;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using AssetRipper.Core.Project;
using AssetRipper.Yaml;
using AssetRipper.Yaml.Extensions;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass102_FillYamlMethods
	{
		private const string ExportYamlRelease = nameof(UnityAssetBase.ExportYamlRelease);
		private const string ExportYamlEditor = nameof(UnityAssetBase.ExportYamlEditor);
		private static string ExportYamlMethod => emittingRelease ? ExportYamlRelease : ExportYamlEditor;

		/// <summary>
		/// Uses original names for robustness and clarity
		/// </summary>
		/// <remarks>
		/// These fields are excluded from meta files even though they don't have flags indicating that.
		/// </remarks>
		private static readonly HashSet<string> AdditionalFieldsToSkipInImporters = new()
		{
			"m_ObjectHideFlags",
			"m_ExtensionPtr",
			"m_PrefabParentObject",
			"m_CorrespondingSourceObject",
			"m_PrefabInternal",
			"m_PrefabAsset",
			"m_PrefabInstance",
		};

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static IMethodDefOrRef addSerializedVersionMethod;
		private static IMethodDefOrRef mappingAddMethod;
		private static IMethodDefOrRef sequenceAddMethod;
		private static IMethodDefOrRef mappingNodeConstructor;
		private static IMethodDefOrRef sequenceNodeConstructor;
		private static IMethodDefOrRef byteArrayToYamlMethod;
		private static IMethodDefOrRef addTypelessDataMethod;
		private static ITypeDefOrRef yamlNodeReference;
		private static ITypeDefOrRef yamlMappingNodeReference;
		private static ITypeDefOrRef yamlScalarNodeReference;
		private static ITypeDefOrRef yamlSequenceNodeReference;
		private static ITypeDefOrRef exportContainerReference;

		private static ITypeDefOrRef assetDictionaryReference;
		private static TypeDefinition assetDictionaryDefinition;
		private static ITypeDefOrRef assetListReference;
		private static TypeDefinition assetListDefinition;
		private static ITypeDefOrRef keyValuePairReference;
		private static TypeDefinition keyValuePairDefinition;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private static readonly Dictionary<string, IMethodDescriptor> methodDictionary = new();

		private static readonly SignatureComparer signatureComparer = new()
		{
			AcceptNewerAssemblyVersionNumbers = true,
			IgnoreAssemblyVersionNumbers = true
		};
		private static CilInstructionLabel DummyInstructionLabel { get; } = new CilInstructionLabel();

		private static bool emittingRelease = true;

		private static void Initialize()
		{
			addSerializedVersionMethod = SharedState.Instance.Importer.ImportMethod(typeof(YamlSerializedVersionExtensions), 
				m => m.Name == nameof(YamlSerializedVersionExtensions.AddSerializedVersion));

			mappingAddMethod = SharedState.Instance.Importer.ImportMethod<YamlMappingNode>(
				m => m.Name == nameof(YamlMappingNode.Add) &&
				m.Parameters.Count == 2 &&
				m.Parameters[0].ParameterType.Name == nameof(YamlNode) &&
				m.Parameters[1].ParameterType.Name == nameof(YamlNode));

			byteArrayToYamlMethod = SharedState.Instance.Importer.ImportMethod(typeof(YamlArrayExtensions), 
				m => m.Name == nameof(YamlArrayExtensions.ExportYaml) && m.Parameters.Count == 1);

			addTypelessDataMethod = SharedState.Instance.Importer.ImportMethod(typeof(YamlArrayExtensions), 
				m => m.Name == nameof(YamlArrayExtensions.AddTypelessData));

			sequenceAddMethod = SharedState.Instance.Importer.ImportMethod<YamlSequenceNode>(
				m => m.Name == nameof(YamlSequenceNode.Add) &&
				m.Parameters.Count == 1 &&
				m.Parameters[0].ParameterType.Name == nameof(YamlNode));

			mappingNodeConstructor = SharedState.Instance.Importer.ImportDefaultConstructor<YamlMappingNode>();
			sequenceNodeConstructor = SharedState.Instance.Importer.ImportConstructor<YamlSequenceNode>(1);

			yamlNodeReference = SharedState.Instance.Importer.ImportType<YamlNode>();
			yamlMappingNodeReference = SharedState.Instance.Importer.ImportType<YamlMappingNode>();
			yamlScalarNodeReference = SharedState.Instance.Importer.ImportType<YamlScalarNode>();
			yamlSequenceNodeReference = SharedState.Instance.Importer.ImportType<YamlSequenceNode>();

			exportContainerReference = SharedState.Instance.Importer.ImportType<IExportContainer>();

			assetDictionaryReference = SharedState.Instance.Importer.ImportType(typeof(AssetDictionary<,>));
			assetListReference = SharedState.Instance.Importer.ImportType(typeof(AssetList<>));
			keyValuePairReference = SharedState.Instance.Importer.ImportType(typeof(AccessPairBase<,>));

			assetDictionaryDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>))!;
			assetListDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetList<>))!;
			keyValuePairDefinition = SharedState.Instance.Importer.LookupType(typeof(AccessPairBase<,>))!;
		}

		public static void DoPass()
		{
			methodDictionary.Clear();
			Initialize();
			emittingRelease = false;
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					bool isImporter = instance.InheritsFromType(1003);//The id for AssetImporter
					instance.FillEditorMethod(isImporter);
				}
			}
			CreateHelperClassForWriteMethods();
			methodDictionary.Clear();

			emittingRelease = true;
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					bool isImporter = instance.InheritsFromType(1003);//The id for AssetImporter
					instance.FillReleaseMethod(isImporter);
				}
			}
			CreateHelperClassForWriteMethods();
			methodDictionary.Clear();
		}

		private static void CreateHelperClassForWriteMethods()
		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.HelpersNamespace, $"{ExportYamlMethod}Methods");
			type.IsPublic = false;
			//type.Methods.Add(readAssetListDefinition!);
			//type.Methods.Add(readAssetDictionaryDefinition!);
			foreach ((string _, IMethodDescriptor method) in methodDictionary.OrderBy(pair => pair.Key))
			{
				if (method is MethodDefinition methodDefinition && methodDefinition.DeclaringType is null)
				{
					type.Methods.Add(methodDefinition);
				}
			}
			Console.WriteLine($"\t{type.Methods.Count} {ExportYamlMethod} helper methods");
		}

		private static void FillEditorMethod(this GeneratedClassInstance instance, bool isImporter)
		{
			instance.Type.FillMethod(ExportYamlEditor, instance.Class.EditorRootNode, instance.VersionRange.Start, isImporter);
		}

		private static void FillReleaseMethod(this GeneratedClassInstance instance, bool isImporter)
		{
			instance.Type.FillMethod(ExportYamlRelease, instance.Class.ReleaseRootNode, instance.VersionRange.Start, isImporter);
		}

		private static bool GetActualIgnoreInMetaFiles(this UniversalNode node)
		{
			return node.IgnoreInMetaFiles || AdditionalFieldsToSkipInImporters.Contains(node.OriginalName);
		}

		private static void FillMethod(this TypeDefinition type, string methodName, UniversalNode? rootNode, UnityVersion version, bool isImporter)
		{
			MethodDefinition method = type.Methods.First(m => m.Name == methodName);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable resultNode = processor.AddLocalVariable(yamlMappingNodeReference.ToTypeSignature());
			processor.Add(CilOpCodes.Newobj, mappingNodeConstructor);
			processor.Add(CilOpCodes.Stloc, resultNode);

			if (rootNode is not null)
			{
				processor.MaybeEmitFlowMappingStyle(rootNode, resultNode);
				processor.AddAddSerializedVersion(resultNode, rootNode.Version);
				foreach (UniversalNode unityNode in rootNode.SubNodes)
				{
					if (!isImporter || !unityNode.GetActualIgnoreInMetaFiles())
					{
						FieldDefinition field = type.GetFieldByName(unityNode.Name, true);
						if (unityNode.NodeType == NodeType.TypelessData)
						{
							processor.Add(CilOpCodes.Ldloc, resultNode);
							processor.Add(CilOpCodes.Ldstr, unityNode.OriginalName);
							processor.Add(CilOpCodes.Ldarg_0);
							processor.Add(CilOpCodes.Ldfld, field);
							processor.Add(CilOpCodes.Call, addTypelessDataMethod);
						}
						else
						{
							IMethodDescriptor fieldExportMethod = GetOrMakeMethod(unityNode, field.Signature!.FieldType, version);
							processor.Add(CilOpCodes.Ldloc, resultNode);
							processor.AddScalarNodeForString(unityNode.OriginalName);
							processor.Add(CilOpCodes.Ldarg_0);//this
							processor.Add(CilOpCodes.Ldfld, field);
							processor.Add(CilOpCodes.Ldarg_1);//container
							processor.AddCall(fieldExportMethod);
							processor.Add(CilOpCodes.Call, mappingAddMethod);
						}
					}
				}
			}

			processor.Add(CilOpCodes.Ldloc, resultNode);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddAddSerializedVersion(this CilInstructionCollection processor, CilLocalVariable yamlMappingNode, int version)
		{
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.Add(CilOpCodes.Ldc_I4, version);
			processor.Add(CilOpCodes.Call, addSerializedVersionMethod);
		}

		private static IMethodDescriptor GetOrMakeMethod(UniversalNode node, TypeSignature type, UnityVersion version)
		{
			string uniqueName = UniqueNameFactory.GetYamlName(node, version);
			if (methodDictionary.TryGetValue(uniqueName, out IMethodDescriptor? method))
			{
				return method;
			}

			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition typeDefinition = subclassGroup.GetTypeForVersion(version);
				Debug.Assert(signatureComparer.Equals(typeDefinition.ToTypeSignature(), type));
				method = typeDefinition.GetMethodByName(ExportYamlMethod);
				methodDictionary.Add(uniqueName, method);
				return method;
			}

			switch (node.NodeType)
			{
				case NodeType.Vector:
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode elementTypeNode = arrayNode.SubNodes[1];
						if (type is GenericInstanceTypeSignature genericSignature)
						{
							Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetList<int>)}`1");
							Debug.Assert(genericSignature.TypeArguments.Count == 1);
							method = MakeListMethod(uniqueName, elementTypeNode, genericSignature.TypeArguments[0], version);
						}
						else
						{
							SzArrayTypeSignature arrayType = (SzArrayTypeSignature)type;
							TypeSignature elementType = arrayType.BaseType;
							method = MakeArrayMethod(uniqueName, elementTypeNode, elementType, version);
						}
					}
					break;
				case NodeType.Map:
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode pairNode = arrayNode.SubNodes[1];
						GenericInstanceTypeSignature genericMapSignature = (GenericInstanceTypeSignature)type;
						Debug.Assert(genericMapSignature.GenericType.Name == $"{nameof(AssetDictionary<int, int>)}`2");
						Debug.Assert(genericMapSignature.TypeArguments.Count == 2);
						GenericInstanceTypeSignature genericPairSignature = keyValuePairReference.MakeGenericInstanceType(genericMapSignature.TypeArguments[0], genericMapSignature.TypeArguments[1]);
						method = MakeDictionaryMethod(uniqueName, genericMapSignature, pairNode, genericPairSignature, version);
					}
					break;
				case NodeType.Pair:
					{
						UniversalNode firstTypeNode = node.SubNodes[0];
						UniversalNode secondTypeNode = node.SubNodes[1];
						GenericInstanceTypeSignature genericSignature = (GenericInstanceTypeSignature)type;
						Debug.Assert(genericSignature.GenericType.Name?.ToString() is $"{nameof(AssetPair<int, int>)}`2" or $"{nameof(AccessPairBase<int, int>)}`2");
						Debug.Assert(genericSignature.TypeArguments.Count == 2);
						method = MakePairMethod(uniqueName, firstTypeNode, genericSignature.TypeArguments[0], secondTypeNode, genericSignature.TypeArguments[1], version);
					}
					break;
				case NodeType.Array:
					{
						UniversalNode elementTypeNode = node.SubNodes[1];
						if (type is GenericInstanceTypeSignature genericSignature)
						{
							Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetList<int>)}`1");
							Debug.Assert(genericSignature.TypeArguments.Count == 1);
							method = MakeListMethod(uniqueName, elementTypeNode, genericSignature.TypeArguments[0], version);
						}
						else
						{
							SzArrayTypeSignature arrayType = (SzArrayTypeSignature)type;
							TypeSignature elementType = arrayType.BaseType;
							method = MakeArrayMethod(uniqueName, elementTypeNode, elementType, version);
						}
					}
					break;
				case NodeType.TypelessData:
					{
						throw new NotSupportedException();
					}
				default:
					method = MakePrimitiveMethod(uniqueName, node, type);
					break;
			}

			methodDictionary.Add(uniqueName, method);
			return method;
		}

		private static IMethodDescriptor MakeDictionaryMethod(string uniqueName, GenericInstanceTypeSignature genericDictType, UniversalNode pairNode, GenericInstanceTypeSignature genericPairType, UnityVersion version)
		{
			IMethodDescriptor pairExportMethod = GetOrMakeMethod(pairNode, genericPairType, version);

			MethodDefinition method = NewMethod(uniqueName, genericDictType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable sequenceLocal = new CilLocalVariable(yamlSequenceNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(sequenceLocal);
			processor.Add(CilOpCodes.Ldc_I4, 1); //SequenceStyle.BlockCurve
			processor.Add(CilOpCodes.Newobj, sequenceNodeConstructor);
			processor.Add(CilOpCodes.Stloc, sequenceLocal);

			//Get length of dictionary
			processor.Add(CilOpCodes.Ldarg_0);
			MethodDefinition getCountDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>))!.Properties.Single(m => m.Name == nameof(AssetDictionary<int, int>.Count)).GetMethod!;
			IMethodDefOrRef getCountReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictType, getCountDefinition);
			processor.Add(CilOpCodes.Call, getCountReference);

			//Make local and store length in it
			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Make an i
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction unconditionalBranchInstruction = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read key + value, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetLabel = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create the dummy instruction to jump back to

			//Export Yaml
			MethodDefinition getItemDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>))!.Methods.Single(m => m.Name == nameof(AssetDictionary<int, int>.GetPair));
			IMethodDefOrRef getItemReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictType, getItemDefinition);

			CilLocalVariable pairExportLocal = processor.AddLocalVariable(yamlNodeReference.ToTypeSignature());
			processor.Add(CilOpCodes.Ldarg_0); //Load Dictionary
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Call, getItemReference); //Get the i_th key value pair
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(pairExportMethod); //Emit yaml node for that key value pair
			processor.Add(CilOpCodes.Stloc, pairExportLocal);

			processor.Add(CilOpCodes.Ldloc, sequenceLocal);
			processor.Add(CilOpCodes.Ldloc, pairExportLocal);
			processor.Add(CilOpCodes.Call, sequenceAddMethod); //Call the add method

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel? loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
			unconditionalBranchInstruction.Operand = loopConditionStartLabel;

			processor.Add(CilOpCodes.Ldloc, sequenceLocal);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();

			return method;
		}

		private static IMethodDescriptor MakePairMethod(string uniqueName, UniversalNode keyNode, TypeSignature keySignature, UniversalNode valueNode, TypeSignature valueSignature, UnityVersion version)
		{
			GenericInstanceTypeSignature pairType = keyValuePairReference.MakeGenericInstanceType(keySignature, valueSignature);
			bool firstIsScalar = keyNode.NodeType.IsPrimitive()
				|| keyNode.TypeName == Pass002_RenameSubnodes.Utf8StringName
				|| keyNode.TypeName == Pass002_RenameSubnodes.GuidName;

			MethodDefinition getKeyDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AccessPairBase<,>), m => m.Name == "get_Key");
			IMethodDefOrRef getKeyReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairType, getKeyDefinition);
			MethodDefinition getValueDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AccessPairBase<,>), m => m.Name == "get_Value");
			IMethodDefOrRef getValueReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairType, getValueDefinition);

			IMethodDescriptor keyExportMethod = GetOrMakeMethod(keyNode, keySignature, version);
			IMethodDescriptor valueExportMethod = GetOrMakeMethod(valueNode, valueSignature, version);

			MethodDefinition method = NewMethod(uniqueName, pairType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable localVariableForOutputNode = processor.AddLocalVariable(yamlMappingNodeReference.ToTypeSignature());
			processor.Add(CilOpCodes.Newobj, mappingNodeConstructor);
			processor.Add(CilOpCodes.Stloc, localVariableForOutputNode);

			if (firstIsScalar)
			{
				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);

				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, getKeyReference);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(keyExportMethod);

				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, getValueReference);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(valueExportMethod);

				processor.Add(CilOpCodes.Call, mappingAddMethod);
			}
			else
			{
				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
				processor.AddScalarNodeForString("first");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, getKeyReference);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(keyExportMethod);
				processor.Add(CilOpCodes.Call, mappingAddMethod);

				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
				processor.AddScalarNodeForString("second");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, getValueReference);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(valueExportMethod);
				processor.Add(CilOpCodes.Call, mappingAddMethod);
			}

			processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();

			return method;
		}

		private static IMethodDescriptor MakeListMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version)
		{
			GenericInstanceTypeSignature genericListType = assetListReference!.MakeGenericInstanceType(elementType);
			MethodDefinition method = NewMethod(uniqueName, genericListType);
			CilInstructionCollection processor = method.GetProcessor();
			IMethodDescriptor elementExportMethod = GetOrMakeMethod(elementTypeNode, elementType, version);
			processor.FillArrayListMethod(elementExportMethod, genericListType, elementType);
			return method;
		}

		private static IMethodDescriptor MakeArrayMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version)
		{
			SzArrayTypeSignature arrayType = elementType.MakeSzArrayType();
			MethodDefinition method = NewMethod(uniqueName, arrayType);
			CilInstructionCollection processor = method.GetProcessor();
			if (elementType is CorLibTypeSignature corLibTypeSignature && corLibTypeSignature.ElementType == ElementType.U1)
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, byteArrayToYamlMethod);
				processor.Add(CilOpCodes.Ret);
			}
			else
			{
				IMethodDescriptor elementExportMethod = GetOrMakeMethod(elementTypeNode, elementType, version);
				processor.FillArrayListMethod(elementExportMethod, arrayType, elementType);
			}
			return method;
		}

		private static void FillArrayListMethod(this CilInstructionCollection processor, IMethodDescriptor elementExportMethod, TypeSignature arrayType, TypeSignature elementType)
		{
			CilLocalVariable sequenceNode = processor.AddLocalVariable(yamlSequenceNodeReference.ToTypeSignature());
			processor.Add(CilOpCodes.Ldc_I4, 0); //SequenceStyle.Block
			processor.Add(CilOpCodes.Newobj, sequenceNodeConstructor);
			processor.Add(CilOpCodes.Stloc, sequenceNode);

			//Make local and store length in it
			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Ldarg_0); //Load array
			if (arrayType is SzArrayTypeSignature)
			{
				processor.Add(CilOpCodes.Ldlen); //Get length
			}
			else
			{
				processor.Add(CilOpCodes.Call, GetAssetListCountMethod(elementType)); //Get count
			}

			processor.Add(CilOpCodes.Stloc, countLocal); //Store it

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction? unconditionalBranchInstruction = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetLabel = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Do stuff at index i
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			if (arrayType is SzArrayTypeSignature)
			{
				processor.AddLoadElement(elementType);
			}
			else
			{
				processor.Add(CilOpCodes.Call, GetAssetListGetItemMethod(elementType));
			}

			CilLocalVariable elementLocal = processor.AddLocalVariable(yamlNodeReference.ToTypeSignature());
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(elementExportMethod);
			processor.Add(CilOpCodes.Stloc, elementLocal);

			processor.Add(CilOpCodes.Ldloc, sequenceNode);
			processor.Add(CilOpCodes.Ldloc, elementLocal);
			processor.Add(CilOpCodes.Call, sequenceAddMethod);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
			unconditionalBranchInstruction.Operand = loopConditionStartLabel;

			processor.Add(CilOpCodes.Ldloc, sequenceNode);
			processor.Add(CilOpCodes.Ret);
		}

		private static IMethodDescriptor MakePrimitiveMethod(string uniqueName, UniversalNode node, TypeSignature type)
		{
			MethodDefinition method = NewMethod(uniqueName, type);
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddScalarNodeForLoadedPrimitiveValue(node.NodeType);
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDefOrRef GetAssetListCountMethod(TypeSignature typeArgument)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == $"get_{nameof(AssetList<int>.Count)}");
			GenericInstanceTypeSignature assetListTypeSignature = SharedState.Instance.Importer.ImportType(typeof(AssetList<>)).MakeGenericInstanceType(typeArgument);
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetListTypeSignature, method);
		}

		private static IMethodDefOrRef GetAssetListGetItemMethod(TypeSignature typeArgument)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == $"get_Item");
			GenericInstanceTypeSignature assetListTypeSignature = SharedState.Instance.Importer.ImportType(typeof(AssetList<>)).MakeGenericInstanceType(typeArgument);
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetListTypeSignature, method);
		}

		private static void AddScalarNodeForString(this CilInstructionCollection processor, string name)
		{
			processor.Add(CilOpCodes.Ldstr, name);
			processor.Add(CilOpCodes.Newobj, GetScalarNodeConstructor(SharedState.Instance.Importer.String));
		}

		private static void AddScalarNodeForLoadedPrimitiveValue(this CilInstructionCollection processor, NodeType nodeType)
		{
			processor.Add(CilOpCodes.Newobj, GetScalarNodeConstructor(nodeType.ToPrimitiveTypeSignature()));
		}

		private static IMethodDefOrRef GetScalarNodeConstructor(TypeSignature parameterType)
		{
			return TryGetScalarNodeConstructor(parameterType) ?? throw new InvalidOperationException($"Could not find a scalar node constructor for {parameterType.FullName}");
		}

		private static readonly Dictionary<TypeSignature, IMethodDefOrRef?> scalarNodeConstructorCache = new Dictionary<TypeSignature, IMethodDefOrRef?>();

		private static IMethodDefOrRef? TryGetScalarNodeConstructor(TypeSignature parameterType)
		{
			if (scalarNodeConstructorCache.TryGetValue(parameterType, out IMethodDefOrRef? constructor))
			{
				return constructor;
			}
			else
			{
				TypeDefinition scalarType = SharedState.Instance.Importer.LookupType<YamlScalarNode>()
				?? throw new Exception("Could not find the yaml scalar node type");
				MethodDefinition? constructorDefinition = scalarType.Methods.SingleOrDefault(m =>
					m.IsConstructor &&
					m.Parameters.Count == 1 &&
					m.Parameters[0].ParameterType.FullName == parameterType.FullName);
				constructor = constructorDefinition == null ? null : SharedState.Instance.Importer.UnderlyingImporter.ImportMethod(constructorDefinition);
				scalarNodeConstructorCache.Add(parameterType, constructor);
				return constructor;
			}

		}

		private static void MaybeEmitFlowMappingStyle(this CilInstructionCollection processor, UniversalNode rootNode, CilLocalVariable yamlMappingNode)
		{
			if (((TransferMetaFlags)rootNode.MetaFlag).IsTransferUsingFlowMappingStyle())
			{
				IMethodDefOrRef setter = SharedState.Instance.Importer.ImportMethod<YamlMappingNode>(m => m.Name == "set_Style");
				processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
				processor.Add(CilOpCodes.Ldc_I4, (int)MappingStyle.Flow);
				processor.Add(CilOpCodes.Call, setter);
			}
		}

		private static MethodDefinition NewMethod(string uniqueName, TypeSignature parameter)
		{
			MethodSignature methodSignature = MethodSignature.CreateStatic(yamlNodeReference.ToTypeSignature());
			MethodDefinition method = new MethodDefinition($"{ExportYamlMethod}_{uniqueName}", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, methodSignature);
			method.CilMethodBody = new CilMethodBody(method);
			method.AddParameter(parameter, "value");
			method.AddParameter(exportContainerReference.ToTypeSignature(), "container");
			method.AddExtensionAttribute(SharedState.Instance.Importer);
			return method;
		}

		private static CilInstruction AddCall(this CilInstructionCollection processor, IMethodDescriptor method)
		{
			return method is MethodDefinition definition && definition.IsStatic
				? processor.Add(CilOpCodes.Call, method)
				: processor.Add(CilOpCodes.Callvirt, method);
		}
	}
}
