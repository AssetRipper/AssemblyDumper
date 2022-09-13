using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core;
using AssetRipper.Core.IO;
using AssetRipper.Core.IO.Extensions;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using AssetRipper.Yaml;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass102_FillYamlMethods
	{
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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static CilInstructionLabel DummyInstructionLabel { get; } = new CilInstructionLabel();

		private static bool emittingRelease = true;

		private static void Initialize()
		{
			Func<MethodDefinition, bool> filter = m => m.Name == nameof(YamlSerializedVersionExtensions.AddSerializedVersion);
			addSerializedVersionMethod = SharedState.Instance.Importer.ImportMethod(typeof(YamlSerializedVersionExtensions), filter);

			mappingAddMethod = SharedState.Instance.Importer.ImportMethod<YamlMappingNode>(
				m => m.Name == nameof(YamlMappingNode.Add) &&
				m.Parameters.Count == 2 &&
				m.Parameters[0].ParameterType.Name == nameof(YamlNode) &&
				m.Parameters[1].ParameterType.Name == nameof(YamlNode));

			Func<MethodDefinition, bool> filter1 = m => m.Name == "ExportYaml" && m.Parameters.Count == 1;
			byteArrayToYamlMethod = SharedState.Instance.Importer.ImportMethod(typeof(AssetRipper.Yaml.Extensions.YamlArrayExtensions), filter1);

			addTypelessDataMethod = SharedState.Instance.Importer.ImportMethod(typeof(AssetRipper.Yaml.Extensions.YamlArrayExtensions), m => m.Name == "AddTypelessData");

			sequenceAddMethod = SharedState.Instance.Importer.ImportMethod<YamlSequenceNode>(
				m => m.Name == "Add" &&
				m.Parameters.Count == 1 &&
				m.Parameters[0].ParameterType.Name == nameof(YamlNode));

			mappingNodeConstructor = SharedState.Instance.Importer.ImportDefaultConstructor<YamlMappingNode>();
			sequenceNodeConstructor = SharedState.Instance.Importer.ImportConstructor<YamlSequenceNode>(1);

			yamlNodeReference = SharedState.Instance.Importer.ImportType<YamlNode>();
			yamlMappingNodeReference = SharedState.Instance.Importer.ImportType<YamlMappingNode>();
			yamlScalarNodeReference = SharedState.Instance.Importer.ImportType<YamlScalarNode>();
			yamlSequenceNodeReference = SharedState.Instance.Importer.ImportType<YamlSequenceNode>();
		}

		public static void DoPass()
		{
			Initialize();
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					bool isImporter = instance.InheritsFromType(1003);//The id for AssetImporter
					Dictionary<string, FieldDefinition> fields = instance.Type.GetAllFieldsInTypeAndBase().ToDictionary(f => f.Name!.Value, f => f);
					emittingRelease = false;
					instance.FillEditorMethod(fields, isImporter);
					emittingRelease = true;
					instance.FillReleaseMethod(fields, isImporter);
				}
			}
		}

		private static void FillEditorMethod(this GeneratedClassInstance instance, Dictionary<string, FieldDefinition> fields, bool isImporter)
		{
			MethodDefinition editorModeYamlMethod = instance.Type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
			editorModeYamlMethod.FillMethod(instance.Class.EditorRootNode, fields, instance.VersionRange.Start, isImporter);
		}

		private static void FillReleaseMethod(this GeneratedClassInstance instance, Dictionary<string, FieldDefinition> fields, bool isImporter)
		{
			MethodDefinition editorModeYamlMethod = instance.Type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
			editorModeYamlMethod.FillMethod(instance.Class.EditorRootNode, fields, instance.VersionRange.Start, isImporter);
		}

		private static void FillMethod(this MethodDefinition method, UniversalNode? rootNode, Dictionary<string, FieldDefinition> fields, UnityVersion version, bool isImporter)
		{
			CilMethodBody body = method.CilMethodBody!;
			CilInstructionCollection processor = body.Instructions;

			body.InitializeLocals = true;
			CilLocalVariable resultNode = new CilLocalVariable(yamlMappingNodeReference.ToTypeSignature());
			body.LocalVariables.Add(resultNode);
			processor.Add(CilOpCodes.Newobj, mappingNodeConstructor);
			processor.Add(CilOpCodes.Stloc, resultNode);

			//Console.WriteLine($"Generating the release read method for {name}");
			if (rootNode != null)
			{
				processor.MaybeEmitFlowMappingStyle(rootNode, resultNode);
				processor.AddAddSerializedVersion(resultNode, rootNode.Version);
				foreach (UniversalNode unityNode in rootNode.SubNodes)
				{
					if (!isImporter
						|| (!unityNode.IgnoreInMetaFiles && !AdditionalFieldsToSkipInImporters.Contains(unityNode.OriginalName)))
					{
						AddExportToProcessor(unityNode, processor, fields, resultNode, version);
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

		private static void AddExportToProcessor(UniversalNode node, CilInstructionCollection processor, Dictionary<string, FieldDefinition> fields, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			//Get field
			fields.TryGetValue(node.Name, out FieldDefinition? field);

			if (field == null)
			{
				throw new Exception($"Field {node.Name} cannot be found in {processor.Owner.Owner.DeclaringType} (fields are {string.Join(", ", fields.Values.Select(f => f.Name))})");
			}

			processor.AddExportFieldContent(node, field, yamlMappingNode, version);
		}

		private static void AddExportFieldContent(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition typeDefinition = subclassGroup.GetTypeForVersion(version);
				processor.AddExportAssetField(node, field, yamlMappingNode, typeDefinition);
				return;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					processor.AddExportVectorField(node, field, yamlMappingNode, version);
					return;
				case "map":
					processor.AddExportDictionaryField(node, field, yamlMappingNode, version);
					return;
				case "pair":
					processor.AddExportPairField(node, field, yamlMappingNode, version);
					return;
				case "TypelessData": //byte array
					processor.AddExportTypelessDataField(node, field, yamlMappingNode);
					return;
				case "Array":
					processor.AddExportArrayField(node, field, yamlMappingNode, version);
					return;
				default:
					processor.AddExportPrimitiveField(node, field, yamlMappingNode);
					return;
			}
		}

		private static void AddLocalForLoadedValue(this CilInstructionCollection processor, UniversalNode node, out CilLocalVariable localVariable, UnityVersion version)
		{
			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition typeDefinition = subclassGroup.GetTypeForVersion(version);
				processor.AddLocalForLoadedAssetValue(typeDefinition, out localVariable);
				return;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					processor.AddLocalForLoadedVector(node, version, out localVariable);
					return;
				case "map":
					processor.AddLocalForLoadedDictionary(node, version, out localVariable);
					return;
				case "pair":
					processor.AddLocalForLoadedPair(node, out localVariable, version);
					return;
				case "TypelessData": //byte array
									 //processor.AddLocalForLoadedByteArray(out localVariable);
									 //return;
					throw new NotSupportedException("TypelessData");
				case "Array":
					processor.AddLocalForLoadedArray(node, version, out localVariable);
					return;
				default:
					processor.AddLocalForLoadedPrimitiveValue(node.TypeName, out localVariable);
					return;
			}
		}

		private static void AddExportAssetField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode, TypeDefinition typeDefinition)
		{
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.AddScalarNodeForString(node.OriginalName);
			processor.AddLoadField(field);
			processor.AddNodeForLoadedAssetValue(typeDefinition);
			processor.Add(CilOpCodes.Call, mappingAddMethod);
		}

		private static void AddExportPrimitiveField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode)
		{
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.AddScalarNodeForString(node.OriginalName);
			processor.AddLoadField(field);
			processor.AddScalarNodeForLoadedPrimitiveValue(node.TypeName);
			processor.Add(CilOpCodes.Call, mappingAddMethod);
		}

		private static void AddNodeForLoadedAssetValue(this CilInstructionCollection processor, TypeDefinition type)
		{
			MethodDefinition exportMethod = type.GetYamlExportMethod();
			processor.Add(CilOpCodes.Ldarg_1); //IExportCollection container
			processor.Add(CilOpCodes.Call, exportMethod);
		}

		private static void AddLocalForLoadedAssetValue(this CilInstructionCollection processor, TypeDefinition type, out CilLocalVariable localVariable)
		{
			processor.AddNodeForLoadedAssetValue(type);
			localVariable = new CilLocalVariable(yamlNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(localVariable);
			processor.Add(CilOpCodes.Stloc, localVariable);
		}

		private static void AddExportPairField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			processor.AddLoadField(field);
			processor.AddLocalForLoadedPair(node, out CilLocalVariable pairNodeLocal, version);
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.AddScalarNodeForString(node.OriginalName);
			processor.Add(CilOpCodes.Ldloc, pairNodeLocal);
			processor.Add(CilOpCodes.Call, mappingAddMethod);
		}

		private static void AddLocalForLoadedPair(this CilInstructionCollection processor, UniversalNode pairNode, out CilLocalVariable localVariableForOutputNode, UnityVersion version)
		{
			UniversalNode firstSubNode = pairNode.SubNodes[0];
			UniversalNode secondSubNode = pairNode.SubNodes[1];
			GenericInstanceTypeSignature pairType = GenericTypeResolver.ResolvePairType(firstSubNode, secondSubNode, version);
			bool firstIsScalar = SharedState.Instance.Importer.GetCppPrimitiveTypeSignature(firstSubNode.TypeName) != null
				|| firstSubNode.TypeName == Pass002_RenameSubnodes.Utf8StringName
				|| firstSubNode.TypeName == Pass002_RenameSubnodes.GuidName;

			MethodDefinition getKeyDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetRipper.Core.IO.NullableKeyValuePair<,>), m => m.Name == "get_Key");
			IMethodDefOrRef getKeyReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairType, getKeyDefinition);
			MethodDefinition getValueDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetRipper.Core.IO.NullableKeyValuePair<,>), m => m.Name == "get_Value");
			IMethodDefOrRef getValueReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, pairType, getValueDefinition);

			CilLocalVariable pair = new CilLocalVariable(pairType);
			processor.Owner.LocalVariables.Add(pair);
			processor.Add(CilOpCodes.Stloc, pair);

			localVariableForOutputNode = new CilLocalVariable(yamlMappingNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(localVariableForOutputNode);
			processor.Add(CilOpCodes.Newobj, mappingNodeConstructor);
			processor.Add(CilOpCodes.Stloc, localVariableForOutputNode);

			if (firstIsScalar)
			{
				processor.Add(CilOpCodes.Ldloc, pair);
				processor.Add(CilOpCodes.Call, getKeyReference);
				processor.AddLocalForLoadedValue(firstSubNode, out CilLocalVariable firstExportLocal, version);

				processor.Add(CilOpCodes.Ldloc, pair);
				processor.Add(CilOpCodes.Call, getValueReference);
				processor.AddLocalForLoadedValue(secondSubNode, out CilLocalVariable secondExportLocal, version);

				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
				processor.Add(CilOpCodes.Ldloc, firstExportLocal);
				processor.Add(CilOpCodes.Ldloc, secondExportLocal);
				processor.Add(CilOpCodes.Call, mappingAddMethod);
			}
			else
			{
				processor.Add(CilOpCodes.Ldloc, pair);
				processor.Add(CilOpCodes.Call, getKeyReference);
				processor.AddLocalForLoadedValue(firstSubNode, out CilLocalVariable? local1, version);
				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
				processor.AddScalarNodeForString("first");
				processor.Add(CilOpCodes.Ldloc, local1);
				processor.Add(CilOpCodes.Call, mappingAddMethod);

				processor.Add(CilOpCodes.Ldloc, pair);
				processor.Add(CilOpCodes.Call, getValueReference);
				processor.AddLocalForLoadedValue(secondSubNode, out CilLocalVariable? local2, version);
				processor.Add(CilOpCodes.Ldloc, localVariableForOutputNode);
				processor.AddScalarNodeForString("second");
				processor.Add(CilOpCodes.Ldloc, local2);
				processor.Add(CilOpCodes.Call, mappingAddMethod);
			}
		}

		private static void AddExportVectorField(this CilInstructionCollection processor, UniversalNode vectorNode, FieldDefinition field, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			//This cloning is necessary to prevent the code from using "Array" instead of the actual name
			UniversalNode? arrayNode = vectorNode.SubNodes[0].DeepClone();
			arrayNode.Name = vectorNode.Name;
			arrayNode.OriginalName = vectorNode.OriginalName;
			processor.AddExportArrayField(arrayNode, field, yamlMappingNode, version);
		}

		private static void AddLocalForLoadedVector(this CilInstructionCollection processor, UniversalNode vectorNode, UnityVersion version, out CilLocalVariable localVariable)
		{
			//This cloning is necessary to prevent the code from using "Array" instead of the actual name
			UniversalNode? arrayNode = vectorNode.SubNodes[0].DeepClone();
			arrayNode.Name = vectorNode.Name;
			arrayNode.OriginalName = vectorNode.OriginalName;
			processor.AddLocalForLoadedArray(arrayNode, version, out localVariable);
		}

		private static void AddExportTypelessDataField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode)
		{
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.Add(CilOpCodes.Ldstr, node.OriginalName);
			processor.AddLoadField(field);
			processor.Add(CilOpCodes.Call, addTypelessDataMethod);
		}

		private static void AddExportByteArrayField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode)
		{
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.AddScalarNodeForString(node.OriginalName);
			processor.AddLoadField(field);
			processor.Add(CilOpCodes.Call, byteArrayToYamlMethod);
			processor.Add(CilOpCodes.Call, mappingAddMethod);
		}

		private static void AddLocalForLoadedByteArray(this CilInstructionCollection processor, out CilLocalVariable localVariable)
		{
			processor.Add(CilOpCodes.Call, byteArrayToYamlMethod);
			localVariable = new CilLocalVariable(yamlNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(localVariable);
			processor.Add(CilOpCodes.Stloc, localVariable);
		}

		private static void AddExportArrayField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			TypeSignature arrayType = GenericTypeResolver.ResolveArrayType(node, version);
			TypeSignature elementType = GetElementTypeFromArrayType(arrayType);
			if (elementType is not SzArrayTypeSignature && elementType.FullName == "System.Byte")
			{
				processor.AddExportByteArrayField(node, field, yamlMappingNode);
			}
			else
			{
				processor.AddLoadField(field);
				processor.AddLocalForLoadedArray(node, version, out CilLocalVariable sequenceNode);
				processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
				processor.AddScalarNodeForString(node.OriginalName);
				processor.Add(CilOpCodes.Ldloc, sequenceNode);
				processor.Add(CilOpCodes.Call, mappingAddMethod);
			}
		}

		private static TypeSignature GetElementTypeFromArrayType(TypeSignature arrayType)
		{
			if (arrayType is SzArrayTypeSignature szArrayTypeSignature)
			{
				return szArrayTypeSignature.BaseType;
			}
			else if (arrayType is GenericInstanceTypeSignature genericInstanceTypeSignature)
			{
				return genericInstanceTypeSignature.TypeArguments.Single();
			}
			else
			{
				throw new NotSupportedException($"Arrays can't use the type signature type: {arrayType.GetType()}");
			}
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

		private static void AddLocalForLoadedArray(this CilInstructionCollection processor, UniversalNode arrayNode, UnityVersion version, out CilLocalVariable sequenceNode)
		{
			UniversalNode elementNode = arrayNode.SubNodes[1];
			TypeSignature arrayType = GenericTypeResolver.ResolveArrayType(arrayNode, version);

			bool isArray = arrayType is SzArrayTypeSignature;
			TypeSignature elementType = GetElementTypeFromArrayType(arrayType);

			if (elementType is not SzArrayTypeSignature && elementType.FullName == "System.Byte")
			{
				throw new NotSupportedException("Byte arrays not supported");
			}

			CilLocalVariable array = new CilLocalVariable(arrayType);
			processor.Owner.LocalVariables.Add(array);
			processor.Add(CilOpCodes.Stloc, array);

			sequenceNode = new CilLocalVariable(yamlSequenceNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(sequenceNode);
			processor.Add(CilOpCodes.Ldc_I4, 0); //SequenceStyle.Block
			processor.Add(CilOpCodes.Newobj, sequenceNodeConstructor);
			processor.Add(CilOpCodes.Stloc, sequenceNode);

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Ldloc, array); //Load array
			if (isArray)
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
			processor.Add(CilOpCodes.Ldloc, array);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			if (isArray)
			{
				processor.AddLoadElement(elementType.ToTypeDefOrRef());
			}
			else
			{
				processor.Add(CilOpCodes.Call, GetAssetListGetItemMethod(elementType));
			}

			processor.AddLocalForLoadedValue(elementNode, out CilLocalVariable elementLocal, version);

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
		}

		private static void AddExportDictionaryField(this CilInstructionCollection processor, UniversalNode node, FieldDefinition field, CilLocalVariable yamlMappingNode, UnityVersion version)
		{
			processor.AddLoadField(field);
			processor.AddLocalForLoadedDictionary(node, version, out CilLocalVariable dictionaryNodeLocal);
			processor.Add(CilOpCodes.Ldloc, yamlMappingNode);
			processor.AddScalarNodeForString(node.OriginalName);
			processor.Add(CilOpCodes.Ldloc, dictionaryNodeLocal);
			processor.Add(CilOpCodes.Call, mappingAddMethod);
		}

		private static void AddLocalForLoadedDictionary(this CilInstructionCollection processor, UniversalNode dictionaryNode, UnityVersion version, out CilLocalVariable sequenceLocal)
		{
			UniversalNode pairNode = dictionaryNode.SubNodes[0].SubNodes[1];
			UniversalNode firstNode = dictionaryNode.SubNodes[0].SubNodes[1].SubNodes[0];
			UniversalNode secondNode = dictionaryNode.SubNodes[0].SubNodes[1].SubNodes[1];

			GenericInstanceTypeSignature genericDictType = GenericTypeResolver.ResolveDictionaryType(dictionaryNode, version);
			GenericInstanceTypeSignature genericPairType = GenericTypeResolver.ResolvePairType(pairNode, version);
			CilLocalVariable? dictLocal = new CilLocalVariable(genericDictType); //Create local
			processor.Owner.LocalVariables.Add(dictLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, dictLocal); //Store dict in local

			sequenceLocal = new CilLocalVariable(yamlSequenceNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(sequenceLocal);
			processor.Add(CilOpCodes.Ldc_I4, 1); //SequenceStyle.BlockCurve
			processor.Add(CilOpCodes.Newobj, sequenceNodeConstructor);
			processor.Add(CilOpCodes.Stloc, sequenceLocal);

			//Get length of dictionary
			processor.Add(CilOpCodes.Ldloc, dictLocal);
			MethodDefinition getCountDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetRipper.Core.IO.AssetDictionary<,>))!.Properties.Single(m => m.Name == "Count").GetMethod!;
			IMethodDefOrRef getCountReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictType, getCountDefinition);
			processor.Add(CilOpCodes.Call, getCountReference);

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
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
			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load Dictionary
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Call, getItemReference); //Get the i_th key value pair
			processor.AddLocalForLoadedPair(pairNode, out CilLocalVariable pairExportLocal, version); //Emit yaml node for that key value pair

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
		}

		private static void AddLoadField(this CilInstructionCollection processor, FieldDefinition field)
		{
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
		}

		private static void AddScalarNodeForString(this CilInstructionCollection processor, string name)
		{
			processor.Add(CilOpCodes.Ldstr, name);
			processor.Add(CilOpCodes.Newobj, GetScalarNodeConstructor(SharedState.Instance.Importer.String));
		}

		private static void AddScalarNodeForLoadedPrimitiveValue(this CilInstructionCollection processor, string typeName)
		{
			CorLibTypeSignature? type = SharedState.Instance.Importer.GetCppPrimitiveTypeSignature(typeName) ?? throw new ArgumentException(nameof(typeName));
			processor.Add(CilOpCodes.Newobj, GetScalarNodeConstructor(type));
		}

		private static void AddLocalForLoadedPrimitiveValue(this CilInstructionCollection processor, string typeName, out CilLocalVariable localVariable)
		{
			processor.AddScalarNodeForLoadedPrimitiveValue(typeName);
			localVariable = new CilLocalVariable(yamlScalarNodeReference.ToTypeSignature());
			processor.Owner.LocalVariables.Add(localVariable);
			processor.Add(CilOpCodes.Stloc, localVariable);
		}

		private static MethodDefinition GetYamlExportMethod(this TypeDefinition type)
		{
			if (emittingRelease)
			{
				return type.Methods.Single(m => m.Name == "ExportYamlRelease");
			}
			else
			{
				return type.Methods.Single(m => m.Name == "ExportYamlEditor");
			}
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

		private static void AddLoadElement(this CilInstructionCollection processor, ITypeDefOrRef elementType)
		{
			if (elementType is SzArrayTypeSignature)
			{
				processor.Add(CilOpCodes.Ldelem_Ref);
				return;
			}

			string elementTypeName = elementType.FullName;
			switch (elementTypeName)
			{
				case "System.Boolean":
					processor.Add(CilOpCodes.Ldelem_U1);
					return;
				case "System.SByte":
					processor.Add(CilOpCodes.Ldelem_I1);
					return;
				case "System.Byte":
					processor.Add(CilOpCodes.Ldelem_U1);
					return;
				case "System.Int16":
					processor.Add(CilOpCodes.Ldelem_I2);
					return;
				case "System.UInt16":
					processor.Add(CilOpCodes.Ldelem_U2);
					return;
				case "System.Int32":
					processor.Add(CilOpCodes.Ldelem_I4);
					return;
				case "System.UInt32":
					processor.Add(CilOpCodes.Ldelem_U4);
					return;
				case "System.Int64":
					processor.Add(CilOpCodes.Ldelem_I8);
					return;
				case "System.UInt64":
					throw new NotSupportedException();
				case "System.Single":
					processor.Add(CilOpCodes.Ldelem_R4);
					return;
				case "System.Double":
					processor.Add(CilOpCodes.Ldelem_R8);
					return;
				default:
					processor.Add(CilOpCodes.Ldelem_Ref);
					return;
			}
		}
	}
}
