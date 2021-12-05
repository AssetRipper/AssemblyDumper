using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Passes
{
	public static class Pass50_FillReadMethods
	{
		private static TypeReference AssetDictionaryType { get; set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 50: Filling read methods");

			AssetDictionaryType = SharedState.Module.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");

			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeReadMethod = type.Methods.First(m => m.Name == "ReadEditor");
				var releaseModeReadMethod = type.Methods.First(m => m.Name == "ReadRelease");

				var editorModeBody = editorModeReadMethod.Body = new(editorModeReadMethod);
				var releaseModeBody = releaseModeReadMethod.Body = new(releaseModeReadMethod);

				var editorModeProcessor = editorModeBody.GetILProcessor();
				var releaseModeProcessor = releaseModeBody.GetILProcessor();

				var fields = FieldUtils.GetAllFieldsInTypeAndBase(type);

				//Console.WriteLine($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (var unityNode in klass.EditorRootNode.SubNodes)
					{
						AddLoadToProcessor(unityNode, editorModeProcessor, fields);
					}
				}

				//Console.WriteLine($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode != null)
				{
					foreach (var unityNode in klass.ReleaseRootNode.SubNodes)
					{
						AddLoadToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}

				editorModeProcessor.Emit(OpCodes.Ret);
				releaseModeProcessor.Emit(OpCodes.Ret);

				editorModeBody.Optimize();
				releaseModeBody.Optimize();
			}
		}

		private static void AddLoadToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields)
		{
			//Get field
			var field = fields.SingleOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			ReadFieldContent(node, processor, field);
		}

		private static void ReadFieldContent(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			if (SharedState.TypeDictionary.TryGetValue(node.TypeName, out var fieldType))
			{
				ReadAssetType(node, processor, field, fieldType, 0);
				return;
			}

			switch (node.TypeName)
			{
				//TODO
				case "vector":
				case "set":
				case "staticvector":
					ReadVector(node, processor, field, 1);
					return;
				case "map":
					ReadDictionary(node, processor, field);
					return;
				case "pair":
					ReadPair(node, processor, field);
					return;
				case "TypelessData": //byte array
					ReadByteArray(node, processor, field);
					return;
				case "Array":
					ReadArray(node, processor, field, 1);
					return;
			}

			ReadPrimitiveType(node, processor, field, 0);
		}

		private static void MaybeAlignBytes(UnityNode node, ILProcessor processor)
		{
			if (((TransferMetaFlags)node.MetaFlag).IsAlignBytes())
			{
				//Load reader
				processor.Emit(OpCodes.Ldarg_1);

				//Get ReadAsset
				var alignMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "AlignStream");

				//Call it
				processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(alignMethod));
			}
		}

		private static void ReadPrimitiveType(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			//Primitives
			var csPrimitiveTypeName = SystemTypeGetter.CppPrimitivesToCSharpPrimitives[node.TypeName];
			var csPrimitiveType = processor.Body.Method.DeclaringType.Module.GetPrimitiveType(node.TypeName);

			//Read
			MethodDefinition primitiveReadMethod = arrayDepth switch
			{
				0 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}") //String
				     ?? CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}")
				     ?? SystemTypeGetter.LookupSystemType("System.IO.BinaryReader").Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}"), //Byte, SByte, and Boolean
				1 => CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}Array" && m.Parameters.Count == 1),
				2 => CommonTypeGetter.EndianReaderExtensionsDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}ArrayArray"),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadPrimitiveType does not support array depth '{arrayDepth}'"),
			};

			primitiveReadMethod ??= SystemTypeGetter.LookupSystemType("System.IO.BinaryReader").Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}");

			if (primitiveReadMethod == null)
				throw new Exception($"Missing a read method for {csPrimitiveTypeName} in {processor.Body.Method.DeclaringType}");

			//Load this
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);

			if (arrayDepth == 1)//Read{Primitive}Array has an allowAlignment parameter
			{
				processor.Emit(OpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call read method
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(primitiveReadMethod));

			//Store result in field
			if (field != null)
				processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
			//Note: string has its own alignment built-in. That's why this doesn't appear to align strings
			MaybeAlignBytes(node, processor);
		}

		/// <summary>
		/// Complex field type, IAssetReadable, call read
		/// </summary>
		private static void ReadAssetType(UnityNode node, ILProcessor processor, FieldDefinition field, TypeDefinition fieldType, int arrayDepth)
		{
			//Load "this" for field store later
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);

			//Get ReadAsset
			MethodDefinition readMethod = arrayDepth switch
			{
				0 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAsset"),
				1 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArray" && m.Parameters.Count == 1),
				2 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArrayArray" && m.Parameters.Count == 1),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadAssetType does not support array depth '{arrayDepth}'"),
			};

			//Make generic ReadAsset<T>
			var genericInst = new GenericInstanceMethod(readMethod);
			genericInst.GenericArguments.Add(processor.Body.Method.Module.ImportReference(fieldType));

			if (arrayDepth > 0)//ReadAssetArray and ReadAssetArrayArray have an allowAlignment parameter
			{
				processor.Emit(OpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call it
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(genericInst));

			//Store result in field
			if (field != null)
				processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
			MaybeAlignBytes(node, processor);
		}

		private static void ReadByteArray(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			//Load "this" for field store later
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);

			//Get ReadAsset
			var readMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadByteArray");

			//Call it
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(readMethod));

			//Store result in field
			if (field != null)
				processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
			//warning: this will generate incorrect reads
			//there will be a double alignment from the endian reader aligning itself
			MaybeAlignBytes(node, processor);
		}

		private static void ReadVector(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			var listTypeNode = node.SubNodes[0];
			if (listTypeNode.TypeName is "Array")
			{
				ReadArray(listTypeNode, processor, field, arrayDepth);
			}
			else
			{
				throw new ArgumentException($"Invalid subnode for {node.TypeName}", nameof(node));
			}

			//warning: this will generate incorrect reads
			//there will be a double alignment from the endian reader aligning itself
			MaybeAlignBytes(node, processor);
		}

		private static void ReadArray(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			var listTypeNode = node.SubNodes[1];
			if (SharedState.TypeDictionary.TryGetValue(listTypeNode.TypeName, out var fieldType))
			{
				ReadAssetType(listTypeNode, processor, field, fieldType, arrayDepth);
			}
			else
				switch (listTypeNode.TypeName)
				{
					case "vector" or "set" or "staticvector":
						ReadVector(listTypeNode, processor, field, arrayDepth + 1);
						break;
					case "Array":
						ReadArray(listTypeNode, processor, field, arrayDepth + 1);
						break;
					case "map":
						if (arrayDepth > 1)
							throw new("ReadArray does not support dictionary arrays with a depth > 1. Found in {node.Name} (field {field}) of {processor.Body.Method.DeclaringType}");

						ReadDictionaryArray(node, processor, field);
						break;
					case "pair":
						if (arrayDepth > 2)
							throw new($"ReadArray does not support Pair arrays with a depth > 2. Found in {node.Name} (field {field}) of {processor.Body.Method.DeclaringType}");

						if (arrayDepth == 2)
						{
							ReadPairArrayArray(processor, field, listTypeNode);
							break;
						}

						ReadPairArray(processor, field, listTypeNode);
						break;
					default:
						ReadPrimitiveType(listTypeNode, processor, field, arrayDepth);
						break;
				}

			MaybeAlignBytes(node, processor);
		}

		private static void ReadPairArray(ILProcessor processor, FieldDefinition field, UnityNode listTypeNode)
		{
			//Strategy:
			//Read Count
			//Make array of size count
			//For i = 0 .. count
			//	Read pair, store in array
			//Store array in field

			//Resolve things we'll need
			var first = listTypeNode.SubNodes[0];
			var second = listTypeNode.SubNodes[1];
			var genericKvp = GenericTypeResolver.ResolvePairType(first, second);

			var arrayType = genericKvp.MakeArrayType();

			//Read length of array
			var intReader = processor.Body.Method.Module.ImportReference(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadInt32"));
			processor.Emit(OpCodes.Ldarg_1); //Load reader
			processor.Emit(OpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Emit(OpCodes.Ldloc, countLocal); //Load count
			processor.Emit(OpCodes.Newarr, genericKvp); //Create new array of kvp with given count
			var arrayLocal = new VariableDefinition(arrayType); //Create local
			processor.Body.Variables.Add(arrayLocal); //Add to method
			processor.Emit(OpCodes.Stloc, arrayLocal); //Store array in local

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

			//Read element at index i of array
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array local
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i local
			ReadPair(listTypeNode, processor, null); //Read the pair
			processor.Emit(OpCodes.Stelem_Any, genericKvp); //Store in array

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

			//Now just store field
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0); //Load this
			
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array

			if (field != null)
				processor.Emit(OpCodes.Stfld, field); //Store field
		}

		private static void ReadPairArrayArray(ILProcessor processor, FieldDefinition field, UnityNode listTypeNode)
		{
			//Strategy:
			//Read Count
			//Make array of size count
			//For i = 0 .. count
			//	Read array of pairs, store in array of arrays
			//Store array in field

			//Resolve things we'll need
			var first = listTypeNode.SubNodes[0];
			var second = listTypeNode.SubNodes[1];
			var genericKvp = GenericTypeResolver.ResolvePairType(first, second);

			var arrayType = genericKvp.MakeArrayType();

			//Read length of array
			var intReader = processor.Body.Method.Module.ImportReference(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadInt32"));
			processor.Emit(OpCodes.Ldarg_1); //Load reader
			processor.Emit(OpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Emit(OpCodes.Ldloc, countLocal); //Load count
			processor.Emit(OpCodes.Newarr, arrayType); //Create new array of arrays of kvps with given count
			var arrayLocal = new VariableDefinition(arrayType.MakeArrayType()); //Create local
			processor.Body.Variables.Add(arrayLocal); //Add to method
			processor.Emit(OpCodes.Stloc, arrayLocal); //Store array in local

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

			//Read element at index i of array
			ReadPairArray(processor, null, listTypeNode); //Read the array of pairs
			var pairArrayLocal = new VariableDefinition(arrayType); //Create local for array of pairs
			processor.Body.Variables.Add(pairArrayLocal); //Add to method
			processor.Emit(OpCodes.Stloc, pairArrayLocal); //Store pair array
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array of arrays local
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i local
			processor.Emit(OpCodes.Ldloc, pairArrayLocal); //Load array of pairs
			processor.Emit(OpCodes.Stelem_Any, arrayType); //Store in array

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

			//Now just store field
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0); //Load this
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array

			if (field != null)
				processor.Emit(OpCodes.Stfld, field); //Store field
		}

		private static void ReadDictionaryArray(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			//you know the drill
			//read count
			//make empty array
			//for i = 0 .. count 
			//  read an entire bloody dictionary
			//set field

			//we need an array type, so let's get that
			var dictNode = node.SubNodes[1];
			var dictType = GenericTypeResolver.ResolveDictionaryType(dictNode);
			var arrayType = dictType.MakeArrayType(); //cursed. that is all.

			//Read length of array
			var intReader = processor.Body.Method.Module.ImportReference(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadInt32"));
			processor.Emit(OpCodes.Ldarg_1); //Load reader
			processor.Emit(OpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Emit(OpCodes.Ldloc, countLocal); //Load count
			processor.Emit(OpCodes.Newarr, dictType); //Create new array of dictionaries with given count
			var arrayLocal = new VariableDefinition(arrayType); //Create local
			processor.Body.Variables.Add(arrayLocal); //Add to method
			processor.Emit(OpCodes.Stloc, arrayLocal); //Store array in local

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

			//Read element at index i of array
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array local
			processor.Emit(OpCodes.Ldloc, iLocal); //Load i local
			ReadDictionary(dictNode, processor, null);
			processor.Emit(OpCodes.Stelem_Any, dictType); //Store in array

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

			//Now just store field
			processor.Emit(OpCodes.Ldarg_0); //Load this
			processor.Emit(OpCodes.Ldloc, arrayLocal); //Load array
			processor.Emit(OpCodes.Stfld, field); //Store field
		}

		private static void ReadDictionary(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			//Strategy:
			//Read Count
			//Make dictionary
			//For i = 0 .. count
			//	Read key, read value, store in dict
			//Store dict in field

			//Resolve things we'll need
			var genericDictType = GenericTypeResolver.ResolveDictionaryType(node);
			var genericDictCtor = MethodUtils.MakeConstructorOnGenericType(genericDictType, 0);
			var addMethod = MethodUtils.MakeMethodOnGenericType(genericDictType.Resolve().Methods.Single(m => m.Name == "Add" && m.Parameters.Count == 2), genericDictType);

			//Read length of array
			var intReader = processor.Body.Method.Module.ImportReference(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadInt32"));
			processor.Emit(OpCodes.Ldarg_1); //Load reader
			processor.Emit(OpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new VariableDefinition(SystemTypeGetter.Int32); //Create local
			processor.Body.Variables.Add(countLocal); //Add to method
			processor.Emit(OpCodes.Stloc, countLocal); //Store count in it

			//Create empty dict and local for it
			processor.Emit(OpCodes.Newobj, genericDictCtor); //Create new dictionary
			var dictLocal = new VariableDefinition(genericDictType); //Create local
			processor.Body.Variables.Add(dictLocal); //Add to method
			processor.Emit(OpCodes.Stloc, dictLocal); //Store dict in local

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

			//Read ith key-value pair of dict 
			processor.Emit(OpCodes.Ldloc, dictLocal); //Load dict local
			ReadFieldContent(node.SubNodes[0].SubNodes[1].SubNodes[0], processor, null); //Load first
			ReadFieldContent(node.SubNodes[0].SubNodes[1].SubNodes[1], processor, null); //Load second
			processor.Emit(OpCodes.Call, addMethod); //Call Add(TKey, TValue)

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

			//Now just store field
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0); //Load this

			processor.Emit(OpCodes.Ldloc, dictLocal); //Load dict

			if (field != null)
				processor.Emit(OpCodes.Stfld, field); //Store field
		}

		private static void ReadPair(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			//Read one, read two, construct tuple, store field
			//Passing a null field to any of the Read generators causes no field store or this load to be emitted
			//Which is just what we want
			var first = node.SubNodes[0];
			var second = node.SubNodes[1];

			//Load this for later usage
			if (field != null)
				processor.Emit(OpCodes.Ldarg_0);

			//Load the left side of the pair
			ReadFieldContent(first, processor, null);

			//Load the right side of the pair
			ReadFieldContent(second, processor, null);

			var genericKvp = GenericTypeResolver.ResolvePairType(first, second);

			var genericCtor = MethodUtils.MakeConstructorOnGenericType(genericKvp, 2);

			//Call constructor
			processor.Emit(OpCodes.Newobj, genericCtor);

			//Store in field if desired
			if (field != null)
				processor.Emit(OpCodes.Stfld, field);
		}
	}
}