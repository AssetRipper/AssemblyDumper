using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass52_FillYamlMethods
	{
		private static MethodReference addSerializedVersionMethod;
		private static MethodReference mappingAddMethod;
		private static MethodReference sequenceAddMethod;
		private static MethodReference byteArrayToYamlMethod;

		private static bool emittingRelease = true;

		private static void Initialize()
		{
			addSerializedVersionMethod = SharedState.Module.ImportCommonMethod("AssetRipper.Core.IO.Extensions.SerializedVersionYAMLExtensions", m => m.Name == "AddSerializedVersion");
			mappingAddMethod = SharedState.Module.ImportCommonMethod<AssetRipper.Core.YAML.YAMLMappingNode>(
				m => m.Name == "Add" &&
				m.Parameters.Count == 2 &&
				m.Parameters[0].ParameterType.Name == nameof(AssetRipper.Core.YAML.YAMLNode) &&
				m.Parameters[1].ParameterType.Name == nameof(AssetRipper.Core.YAML.YAMLNode));
			byteArrayToYamlMethod = SharedState.Module.ImportCommonMethod("AssetRipper.Core.YAML.Extensions.ArrayYAMLExtensions",
				m => m.Name == "ExportYAML" &&
				m.Parameters.Count == 1);
			sequenceAddMethod = SharedState.Module.ImportCommonMethod<AssetRipper.Core.YAML.YAMLSequenceNode>(
				m => m.Name == "Add" &&
				m.Parameters.Count == 1 &&
				m.Parameters[0].ParameterType.Name == nameof(AssetRipper.Core.YAML.YAMLNode));
		}

		public static void DoPass()
		{
			Console.WriteLine("Pass 52: Filling yaml methods");
			Initialize();
			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				var type = SharedState.TypeDictionary[name];
				List<FieldDefinition> fields = FieldUtils.GetAllFieldsInTypeAndBase(type).Distinct().ToList();
				emittingRelease = false;
				type.FillEditorMethod(klass, fields);
				emittingRelease = true;
				type.FillReleaseMethod(klass, fields);
			}
		}

		private static void FillEditorMethod(this TypeDefinition type, UnityClass klass, List<FieldDefinition> fields)
		{
			var editorModeYamlMethod = type.Methods.Single(m => m.Name == "ExportYAMLEditor");
			editorModeYamlMethod.FillMethod(klass.EditorRootNode, fields);
		}

		private static void FillReleaseMethod(this TypeDefinition type, UnityClass klass, List<FieldDefinition> fields)
		{
			var releaseModeYamlMethod = type.Methods.Single(m => m.Name == "ExportYAMLRelease");
			releaseModeYamlMethod.FillMethod(klass.ReleaseRootNode, fields);
		}

		private static void FillMethod(this MethodDefinition method, UnityNode rootNode, List<FieldDefinition> fields)
		{
			var body = method.Body = new(method);
			var processor = body.GetILProcessor();

			body.InitLocals = true;
			VariableDefinition resultNode = new VariableDefinition(CommonTypeGetter.YAMLMappingNodeDefinition);
			body.Variables.Add(resultNode);
			processor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLMappingNodeConstructor);
			processor.Emit(OpCodes.Stloc, resultNode);

			//Console.WriteLine($"Generating the release read method for {name}");
			if (rootNode != null)
			{
				processor.MaybeEmitFlowMappingStyle(rootNode, resultNode);
				processor.EmitAddSerializedVersion(resultNode, rootNode.Version);
				foreach (var unityNode in rootNode.SubNodes)
				{
					AddExportToProcessor(unityNode, processor, fields, resultNode);
				}
			}

			processor.Emit(OpCodes.Ldloc, resultNode);
			processor.Emit(OpCodes.Ret);
			body.Optimize();
		}

		private static void EmitAddSerializedVersion(this ILProcessor processor, VariableDefinition yamlMappingNode, int version)
		{
			processor.Emit(OpCodes.Ldloc, yamlMappingNode);
			processor.Emit(OpCodes.Ldc_I4, version);
			processor.Emit(OpCodes.Call, addSerializedVersionMethod);
		}

		private static void AddExportToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields, VariableDefinition yamlMappingNode)
		{
			//Get field
			FieldDefinition field = fields.SingleOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			//ExportFieldContent(node, processor, field, yamlMappingNode);
			processor.EmitExportFieldContent(node, field, yamlMappingNode);
		}

		private static void EmitExportFieldContent(this ILProcessor processor, UnityNode node, FieldDefinition field, VariableDefinition yamlMappingNode)
		{
			processor.EmitNodeForField(node, field);
			VariableDefinition fieldNode = new VariableDefinition(CommonTypeGetter.YAMLNodeDefinition);
			processor.Body.Variables.Add(fieldNode);
			processor.Emit(OpCodes.Stloc, fieldNode);

			processor.Emit(OpCodes.Ldloc, yamlMappingNode);
			processor.EmitScalarNodeForString(node.Name);
			processor.Emit(OpCodes.Ldloc, fieldNode);
			processor.Emit(OpCodes.Call, mappingAddMethod);
		}

		private static void EmitNodeForField(this ILProcessor processor, UnityNode node, FieldDefinition field)
		{
			processor.EmitLoadField(field);
			processor.EmitNodeForLoadedValue(node);
		}

		private static void EmitNodeForLoadedValue(this ILProcessor processor, UnityNode node)
		{
			if (SharedState.TypeDictionary.TryGetValue(node.TypeName, out TypeDefinition typeDefinition))
			{
				processor.EmitNodeForLoadedAssetValue(typeDefinition);
				return;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					processor.EmitNodeForLoadedVector(node);
					return;
				case "map":
					processor.EmitNodeForLoadedDictionary(node);
					//processor.Emit(OpCodes.Pop);
					//processor.EmitScalarNodeForString("test");
					return;
				case "pair":
					processor.EmitNodeForLoadedPair(node);
					return;
				case "TypelessData": //byte array
					processor.Emit(OpCodes.Call, byteArrayToYamlMethod);
					return;
				case "Array":
					processor.EmitNodeForLoadedArray(node);
					return;
				default:
					processor.EmitScalarNodeForLoadedPrimitiveType(node.TypeName);
					return;
			}
		}

		private static void EmitLoadField(this ILProcessor processor, FieldDefinition field)
		{
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, field);
		}

		private static void EmitScalarNodeForString(this ILProcessor processor, string name)
		{
			processor.Emit(OpCodes.Ldstr, name);
			processor.Emit(OpCodes.Newobj, GetScalarNodeConstructor(SystemTypeGetter.String));
		}

		private static void EmitScalarNodeForLoadedPrimitiveType(this ILProcessor processor, string typeName)
		{
			TypeReference type = SharedState.Module.GetPrimitiveType(typeName) ?? throw new ArgumentException(nameof(typeName));
			processor.Emit(OpCodes.Newobj, GetScalarNodeConstructor(type));
		}

		private static void EmitNodeForLoadedAssetValue(this ILProcessor processor, TypeDefinition type)
		{
			MethodDefinition exportMethod = type.GetYamlExportMethod();
			processor.Emit(OpCodes.Ldarg_1); //IExportCollection container
			processor.Emit(OpCodes.Call, exportMethod);
		}

		private static void EmitNodeForLoadedPair(this ILProcessor processor, UnityNode pairNode)
		{
			UnityNode firstSubNode = pairNode.SubNodes[0];
			UnityNode secondSubNode = pairNode.SubNodes[1];
			GenericInstanceType pairType = GenericTypeResolver.ResolvePairType(firstSubNode, secondSubNode);
			bool firstIsScalar = SharedState.Module.GetPrimitiveType(firstSubNode.TypeName) != null;

			MethodDefinition getKeyDefinition = CommonTypeGetter.LookupCommonMethod("AssetRipper.Core.IO.NullableKeyValuePair`2", m => m.Name == "get_Key");
			MethodReference getKeyReference = SharedState.Module.ImportReference(MethodUtils.MakeMethodOnGenericType(getKeyDefinition, pairType));
			MethodDefinition getValueDefinition = CommonTypeGetter.LookupCommonMethod("AssetRipper.Core.IO.NullableKeyValuePair`2", m => m.Name == "get_Value");
			MethodReference getValueReference = SharedState.Module.ImportReference(MethodUtils.MakeMethodOnGenericType(getValueDefinition, pairType));

			VariableDefinition pair = new VariableDefinition(pairType);
			processor.Body.Variables.Add(pair);
			processor.Emit(OpCodes.Stloc, pair);
			
			VariableDefinition mappingLocal = new VariableDefinition(CommonTypeGetter.YAMLMappingNodeDefinition);
			processor.Body.Variables.Add(mappingLocal);
			processor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLMappingNodeConstructor);
			processor.Emit(OpCodes.Stloc, mappingLocal);

			if (firstIsScalar)
			{
				processor.Emit(OpCodes.Ldloc, pair);
				processor.Emit(OpCodes.Call, getKeyReference);
				processor.EmitNodeForLoadedValue(firstSubNode);

				VariableDefinition firstExportLocal = new VariableDefinition(CommonTypeGetter.YAMLNodeDefinition);
				processor.Body.Variables.Add(firstExportLocal);
				processor.Emit(OpCodes.Stloc, firstExportLocal);

				processor.Emit(OpCodes.Ldloc, pair);
				processor.Emit(OpCodes.Call, getValueReference);
				processor.EmitNodeForLoadedValue(secondSubNode);

				VariableDefinition secondExportLocal = new VariableDefinition(CommonTypeGetter.YAMLNodeDefinition);
				processor.Body.Variables.Add(secondExportLocal);
				processor.Emit(OpCodes.Stloc, secondExportLocal);

				processor.Emit(OpCodes.Ldloc, mappingLocal);
				processor.Emit(OpCodes.Ldloc, firstExportLocal);
				processor.Emit(OpCodes.Ldloc, secondExportLocal);
				processor.Emit(OpCodes.Call, mappingAddMethod);

				processor.Emit(OpCodes.Ldloc, mappingLocal);
			}
			else
			{
				processor.Emit(OpCodes.Ldloc, mappingLocal);
				processor.EmitScalarNodeForString("first");
				processor.Emit(OpCodes.Ldloc, pair);
				processor.Emit(OpCodes.Call, getKeyReference);
				processor.EmitNodeForLoadedValue(firstSubNode);
				processor.Emit(OpCodes.Call, mappingAddMethod);

				processor.Emit(OpCodes.Ldloc, mappingLocal);
				processor.EmitScalarNodeForString("second");
				processor.Emit(OpCodes.Ldloc, pair);
				processor.Emit(OpCodes.Call, getValueReference);
				processor.EmitNodeForLoadedValue(secondSubNode);
				processor.Emit(OpCodes.Call, mappingAddMethod);

				processor.Emit(OpCodes.Ldloc, mappingLocal);
			}
		}

		private static void EmitNodeForLoadedVector(this ILProcessor processor, UnityNode vectorNode)
		{
			processor.EmitNodeForLoadedArray(vectorNode.SubNodes[0]);
		}

		private static void EmitNodeForLoadedArray(this ILProcessor processor, UnityNode arrayNode)
		{
			UnityNode elementNode = arrayNode.SubNodes[1];
			ArrayType arrayType = GenericTypeResolver.ResolveArrayType(arrayNode);

			TypeReference elementType = arrayType.ElementType;
			if (elementType is not ArrayType && elementType.FullName == "System.Byte")
			{
				processor.Emit(OpCodes.Call, byteArrayToYamlMethod);
				return;
			}

			VariableDefinition array = new VariableDefinition(arrayType);
			processor.Body.Variables.Add(array);
			processor.Emit(OpCodes.Stloc, array);

			VariableDefinition sequenceNode = new VariableDefinition(CommonTypeGetter.YAMLSequenceNodeDefinition);
			processor.Body.Variables.Add(sequenceNode);
			processor.Emit(OpCodes.Ldc_I4, 0); //SequenceStyle.Block
			processor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLSequenceNodeConstructor);
			processor.Emit(OpCodes.Stloc, sequenceNode);

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Ldloc, array); //Load array
			processor.Emit(OpCodes.Ldlen); //Get length
			processor.Emit(OpCodes.Stloc, countLocal); //Store it

			//Make an i
			var iLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(iLocal); //Add to method
			processor.Emit(OpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Emit(OpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
			processor.Append(unconditionalBranch);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			var jumpTarget = processor.Create(OpCodes.Nop); //Create a dummy instruction to jump back to
			processor.Append(jumpTarget); //Add it to the method body

			//Do stuff at index i
			processor.Emit(OpCodes.Ldloc, array);
			processor.Emit(OpCodes.Ldloc, iLocal);
			processor.EmitLoadElement(elementType);
			processor.EmitNodeForLoadedValue(elementNode);
			VariableDefinition elementLocal = new VariableDefinition(CommonTypeGetter.YAMLNodeDefinition);
			processor.Body.Variables.Add(elementLocal);
			processor.Emit(OpCodes.Stloc, elementLocal);

			processor.Emit(OpCodes.Ldloc, sequenceNode);
			processor.Emit(OpCodes.Ldloc, elementLocal);
			processor.Emit(OpCodes.Call, sequenceAddMethod);

			//Increment i
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i local
			processor.Emit(OpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Emit(OpCodes.Add); //Add 
			processor.Emit(OpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStart = processor.Create(OpCodes.Ldloc, iLocal); //Load i
			processor.Append(loopConditionStart);
			processor.Emit(OpCodes.Ldloc, countLocal); //Load count
			processor.Emit(OpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStart;

			processor.Emit(OpCodes.Ldloc, sequenceNode);
		}

		private static void EmitNodeForLoadedDictionary(this ILProcessor processor, UnityNode dictionaryNode)
		{
			UnityNode pairNode = dictionaryNode.SubNodes[0].SubNodes[1];
			UnityNode firstNode = dictionaryNode.SubNodes[0].SubNodes[1].SubNodes[0];
			UnityNode secondNode = dictionaryNode.SubNodes[0].SubNodes[1].SubNodes[1];

			var genericDictType = GenericTypeResolver.ResolveDictionaryType(dictionaryNode);
			var genericPairType = GenericTypeResolver.ResolvePairType(pairNode);
			var dictLocal = new VariableDefinition(genericDictType); //Create local
			processor.Body.Variables.Add(dictLocal); //Add to method
			processor.Emit(OpCodes.Stloc, dictLocal); //Store dict in local

			VariableDefinition sequenceLocal = new VariableDefinition(CommonTypeGetter.YAMLSequenceNodeDefinition);
			processor.Body.Variables.Add(sequenceLocal);
			processor.Emit(OpCodes.Ldc_I4, 1); //SequenceStyle.BlockCurve
			processor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLSequenceNodeConstructor);
			processor.Emit(OpCodes.Stloc, sequenceLocal);

			//Get length of dictionary
			processor.Emit(OpCodes.Ldloc, dictLocal);
			MethodDefinition getCountDefinition = SystemTypeGetter.LookupSystemType("System.Collections.Generic.List`1").Methods.Single(m => m.Name == "get_Count");
			MethodReference getCountReference = SharedState.Module.ImportReference(MethodUtils.MakeMethodReferenceOnGenericType(getCountDefinition, genericPairType));
			processor.Emit(OpCodes.Call, getCountReference);

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Stloc, countLocal); //Store count in it

			//Make an i
			var iLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(iLocal); //Add to method
			processor.Emit(OpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Emit(OpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
			processor.Append(unconditionalBranch);

			//Now we just read key + value, increment i, compare against count, and jump back to here if it's less
			var jumpTarget = processor.Create(OpCodes.Nop); //Create a dummy instruction to jump back to
			processor.Append(jumpTarget); //Add it to the method body

			//Export Yaml
			MethodDefinition getItemDefinition = SystemTypeGetter.LookupSystemType("System.Collections.Generic.List`1").Methods.Single(m => m.Name == "get_Item");
			MethodReference getItemReference = SharedState.Module.ImportReference(MethodUtils.MakeMethodReferenceOnGenericType(getItemDefinition, genericPairType));
			processor.Emit(OpCodes.Ldloc, dictLocal); //Load Dictionary
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i
			processor.Emit(OpCodes.Call, getItemReference); //Get the i_th key value pair
			processor.EmitNodeForLoadedPair(pairNode); //Emit yaml node for that key value pair

			VariableDefinition pairExportLocal = new VariableDefinition(CommonTypeGetter.YAMLNodeDefinition);
			processor.Body.Variables.Add(pairExportLocal);
			processor.Emit(OpCodes.Stloc, pairExportLocal);

			processor.Emit(OpCodes.Ldloc, sequenceLocal);
			processor.Emit(OpCodes.Ldloc, pairExportLocal); 
			processor.Emit(OpCodes.Call, sequenceAddMethod); //Call the add method

			//Increment i
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i local
			processor.Emit(OpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Emit(OpCodes.Add); //Add 
			processor.Emit(OpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStart = processor.Create(OpCodes.Ldloc, iLocal); //Load i
			processor.Append(loopConditionStart);
			processor.Emit(OpCodes.Ldloc, countLocal); //Load count
			processor.Emit(OpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStart;

			//Load sequence node for next use
			processor.Emit(OpCodes.Ldloc, sequenceLocal);
		}

		private static MethodDefinition GetYamlExportMethod(this TypeDefinition type)
		{
			if (emittingRelease)
				return type.Methods.Single(m => m.Name == "ExportYAMLRelease");
			else
				return type.Methods.Single(m => m.Name == "ExportYAMLEditor");
		}

		private static MethodReference GetScalarNodeConstructor(TypeReference parameterType)
		{
			return TryGetScalarNodeConstructor(parameterType) ?? throw new InvalidOperationException($"Could not find a scalar node constructor for {parameterType.FullName}");
		}

		private static MethodReference TryGetScalarNodeConstructor(TypeReference parameterType)
		{
			TypeDefinition scalarType = CommonTypeGetter.LookupCommonType<AssetRipper.Core.YAML.YAMLScalarNode>()
				?? throw new Exception("Could not find the yaml scalar node type");
			MethodDefinition result = scalarType.Methods.SingleOrDefault(m => 
				m.IsConstructor && 
				m.Parameters.Count == 1 && 
				m.Parameters[0].ParameterType.FullName == parameterType.FullName);
			return result == null ? null : SharedState.Module.ImportReference(result);
		}

		private static void MaybeEmitFlowMappingStyle(this ILProcessor processor, UnityNode rootNode, VariableDefinition yamlMappingNode)
		{
			if (((TransferMetaFlags)rootNode.MetaFlag).IsTransferUsingFlowMappingStyle())
			{
				MethodReference setter = SharedState.Module.ImportCommonMethod<AssetRipper.Core.YAML.YAMLMappingNode>(m => m.Name == "set_Style");
				processor.Emit(OpCodes.Ldloc, yamlMappingNode);
				processor.Emit(OpCodes.Ldc_I4, (int)AssetRipper.Core.YAML.MappingStyle.Flow);
				processor.Emit(OpCodes.Call, setter);
			}
		}

		private static void EmitLoadElement(this ILProcessor processor, TypeReference elementType)
		{
			if(elementType is ArrayType)
			{
				processor.Emit(OpCodes.Ldelem_Ref);
				return;
			}

			string elementTypeName = elementType.FullName;
			switch (elementTypeName)
			{
				case "System.Boolean":
					processor.Emit(OpCodes.Ldelem_U1);
					return;
				case "System.SByte":
					processor.Emit(OpCodes.Ldelem_I1);
					return;
				case "System.Byte":
					processor.Emit(OpCodes.Ldelem_U1);
					return;
				case "System.Int16":
					processor.Emit(OpCodes.Ldelem_I2);
					return;
				case "System.UInt16":
					processor.Emit(OpCodes.Ldelem_U2);
					return;
				case "System.Int32":
					processor.Emit(OpCodes.Ldelem_I4);
					return;
				case "System.UInt32":
					processor.Emit(OpCodes.Ldelem_U4);
					return;
				case "System.Int64":
					processor.Emit(OpCodes.Ldelem_I8);
					return;
				case "System.UInt64":
					throw new NotSupportedException();
				case "System.Single":
					processor.Emit(OpCodes.Ldelem_R4);
					return;
				case "System.Double":
					processor.Emit(OpCodes.Ldelem_R8);
					return;
				default:
					processor.Emit(OpCodes.Ldelem_Ref);
					return;
			}
		}
	}
}
