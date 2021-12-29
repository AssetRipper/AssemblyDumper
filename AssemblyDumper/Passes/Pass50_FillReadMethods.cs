using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass50_FillReadMethods
	{
		private static ITypeDefOrRef AssetDictionaryType { get; set; }
		private static CilInstructionLabel DummyInstructionLabel { get; } = new CilInstructionLabel();

		public static void DoPass()
		{
			Console.WriteLine("Pass 50: Filling read methods");

			AssetDictionaryType = SharedState.Importer.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");

			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeReadMethod = type.Methods.First(m => m.Name == "ReadEditor");
				var releaseModeReadMethod = type.Methods.First(m => m.Name == "ReadRelease");

				var editorModeBody = editorModeReadMethod.CilMethodBody;
				var releaseModeBody = releaseModeReadMethod.CilMethodBody;
				
				var editorModeProcessor = editorModeBody.Instructions;
				var releaseModeProcessor = releaseModeBody.Instructions;

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

				editorModeProcessor.Add(CilOpCodes.Ret);
				releaseModeProcessor.Add(CilOpCodes.Ret);

				editorModeProcessor.OptimizeMacros();
				releaseModeProcessor.OptimizeMacros();
			}
		}

		private static void AddLoadToProcessor(UnityNode node, CilInstructionCollection processor, List<FieldDefinition> fields)
		{
			//Get field
			var field = fields.SingleOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Owner.Owner.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			ReadFieldContent(node, processor, field);
		}

		private static void ReadFieldContent(UnityNode node, CilInstructionCollection processor, FieldDefinition field)
		{
			if (SharedState.TypeDictionary.TryGetValue(node.TypeName, out var fieldType))
			{
				ReadAssetType(node, processor, field, fieldType, 0);
				return;
			}

			switch (node.TypeName)
			{
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

		private static void MaybeAlignBytes(UnityNode node, CilInstructionCollection processor)
		{
			if (((TransferMetaFlags)node.MetaFlag).IsAlignBytes())
			{
				//Load reader
				processor.Add(CilOpCodes.Ldarg_1);

				//Get ReadAsset
				var alignMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "AlignStream");

				//Call it
				processor.Add(CilOpCodes.Call, SharedState.Importer.ImportMethod(alignMethod));
			}
		}

		private static void ReadPrimitiveType(UnityNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth)
		{
			//Primitives
			var csPrimitiveTypeName = SystemTypeGetter.CppPrimitivesToCSharpPrimitives[node.TypeName];
			var csPrimitiveType = SystemTypeGetter.GetCppPrimitiveTypeSignature(node.TypeName);

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

			if (primitiveReadMethod == null)
				throw new Exception($"Missing a read method for {csPrimitiveTypeName} in {processor.Owner.Owner.DeclaringType}");

			//Load this
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0);

			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			if (arrayDepth == 1)//Read{Primitive}Array has an allowAlignment parameter
			{
				processor.Add(CilOpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call read method
			processor.Add(CilOpCodes.Call, SharedState.Importer.ImportMethod(primitiveReadMethod));

			//Store result in field
			if (field != null)
				processor.Add(CilOpCodes.Stfld, field);

			//Maybe Align Bytes
			//Note: string has its own alignment built-in. That's why this doesn't appear to align strings
			MaybeAlignBytes(node, processor);
		}

		/// <summary>
		/// Complex field type, IAssetReadable, call read
		/// </summary>
		private static void ReadAssetType(UnityNode node, CilInstructionCollection processor, FieldDefinition field, TypeDefinition fieldType, int arrayDepth)
		{
			//Load "this" for field store later
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0);

			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			//Get ReadAsset
			MethodDefinition readMethod = arrayDepth switch
			{
				0 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAsset"),
				1 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArray" && m.Parameters.Count == 1),
				2 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArrayArray" && m.Parameters.Count == 1),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadAssetType does not support array depth '{arrayDepth}'"),
			};

			//Make generic ReadAsset<T>
			var genericInst = MethodUtils.MakeGenericInstanceMethod(readMethod, fieldType.ToTypeSignature());

			if (arrayDepth > 0)//ReadAssetArray and ReadAssetArrayArray have an allowAlignment parameter
			{
				processor.Add(CilOpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call it
			processor.Add(CilOpCodes.Call, genericInst);

			//Store result in field
			if (field != null)
				processor.Add(CilOpCodes.Stfld, field);

			//Maybe Align Bytes
			MaybeAlignBytes(node, processor);
		}

		private static void ReadByteArray(UnityNode node, CilInstructionCollection processor, FieldDefinition field)
		{
			//Load "this" for field store later
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0);

			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			//Get ReadAsset
			var readMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadByteArray");

			//Call it
			processor.Add(CilOpCodes.Call, SharedState.Importer.ImportMethod(readMethod));

			//Store result in field
			if (field != null)
				processor.Add(CilOpCodes.Stfld, field);

			//Maybe Align Bytes
			//warning: this will generate incorrect reads
			//there will be a double alignment from the endian reader aligning itself
			MaybeAlignBytes(node, processor);
		}

		private static void ReadVector(UnityNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth)
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

		private static void ReadArray(UnityNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth)
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
							throw new($"ReadArray does not support Pair arrays with a depth > 2. Found in {node.Name} (field {field}) of {processor.Owner.Owner.DeclaringType}");

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

		private static void ReadPairArray(CilInstructionCollection processor, FieldDefinition field, UnityNode listTypeNode)
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

			var arrayType = genericKvp.MakeSzArrayType();

			//Read length of array
			var intReader = SharedState.Importer.ImportMethod(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.Single(m => m.Name == "ReadInt32"));
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Newarr, genericKvp.ToTypeDefOrRef()); //Create new array of kvp with given count
			var arrayLocal = new CilLocalVariable(arrayType); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local
			
			//Make an i
			var iLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create a label for a dummy instruction to jump back to
			var jumpTargetLabel = new CilInstructionLabel();

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			jumpTargetLabel.Instruction = processor.Add(CilOpCodes.Nop); //Create a dummy instruction to jump back to

			//Read element at index i of array
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			ReadPair(listTypeNode, processor, null); //Read the pair
			processor.Add(CilOpCodes.Stelem, genericKvp.ToTypeDefOrRef()); //Store in array

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			//Now just store field
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0); //Load this
			
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array

			if (field != null)
				processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static void ReadPairArrayArray(CilInstructionCollection processor, FieldDefinition field, UnityNode listTypeNode)
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

			var arrayType = genericKvp.MakeSzArrayType();

			//Read length of array
			var intReader = SharedState.Importer.ImportMethod(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.Single(m => m.Name == "ReadInt32"));
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Newarr, arrayType.ToTypeDefOrRef()); //Create new array of arrays of kvps with given count
			var arrayLocal = new CilLocalVariable(arrayType.MakeSzArrayType()); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make an i
			var iLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			var jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read element at index i of array
			ReadPairArray(processor, null, listTypeNode); //Read the array of pairs
			var pairArrayLocal = new CilLocalVariable(arrayType); //Create local for array of pairs
			processor.Owner.LocalVariables.Add(pairArrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, pairArrayLocal); //Store pair array
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array of arrays local
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldloc, pairArrayLocal); //Load array of pairs
			processor.Add(CilOpCodes.Stelem, arrayType.ToTypeDefOrRef()); //Store in array

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			//Now just store field
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0); //Load this
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array

			if (field != null)
				processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static void ReadDictionaryArray(UnityNode node, CilInstructionCollection processor, FieldDefinition field)
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
			var arrayType = dictType.MakeSzArrayType(); //cursed. that is all.

			//Read length of array
			var intReader = SharedState.Importer.ImportMethod(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.Single(m => m.Name == "ReadInt32"));
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Newarr, dictType.ToTypeDefOrRef()); //Create new array of dictionaries with given count
			var arrayLocal = new CilLocalVariable(arrayType); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make an i
			var iLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			var jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read element at index i of array
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			ReadDictionary(dictNode, processor, null);
			processor.Add(CilOpCodes.Stelem, dictType.ToTypeDefOrRef()); //Store in array

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			//Now just store field
			processor.Add(CilOpCodes.Ldarg_0); //Load this
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array
			processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static void ReadDictionary(UnityNode node, CilInstructionCollection processor, FieldDefinition field)
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
			var addMethod = MethodUtils.MakeMethodOnGenericType(genericDictType, genericDictType.Resolve().Methods.Single(m => m.Name == "Add" && m.Parameters.Count == 2));

			//Read length of array
			var intReader = SharedState.Importer.ImportMethod(CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.Single(m => m.Name == "ReadInt32"));
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, intReader); //Call int reader

			//Make local and store length in it
			var countLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty dict and local for it
			processor.Add(CilOpCodes.Newobj, genericDictCtor); //Create new dictionary
			var dictLocal = new CilLocalVariable(genericDictType); //Create local
			processor.Owner.LocalVariables.Add(dictLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, dictLocal); //Store dict in local

			//Make an i
			var iLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read key + value, increment i, compare against count, and jump back to here if it's less
			var jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read ith key-value pair of dict 
			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load dict local
			ReadFieldContent(node.SubNodes[0].SubNodes[1].SubNodes[0], processor, null); //Load first
			ReadFieldContent(node.SubNodes[0].SubNodes[1].SubNodes[1], processor, null); //Load second
			processor.Add(CilOpCodes.Call, addMethod); //Call Add(TKey, TValue)

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			var loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			//Now just store field
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0); //Load this

			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load dict

			if (field != null)
				processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static void ReadPair(UnityNode node, CilInstructionCollection processor, FieldDefinition field)
		{
			//Read one, read two, construct tuple, store field
			//Passing a null field to any of the Read generators causes no field store or this load to be emitted
			//Which is just what we want
			var first = node.SubNodes[0];
			var second = node.SubNodes[1];

			//Load this for later usage
			if (field != null)
				processor.Add(CilOpCodes.Ldarg_0);

			//Load the left side of the pair
			ReadFieldContent(first, processor, null);

			//Load the right side of the pair
			ReadFieldContent(second, processor, null);

			var genericKvp = GenericTypeResolver.ResolvePairType(first, second);

			var genericCtor = MethodUtils.MakeConstructorOnGenericType(genericKvp, 2);

			//Call constructor
			processor.Add(CilOpCodes.Newobj, genericCtor);

			//Store in field if desired
			if (field != null)
				processor.Add(CilOpCodes.Stfld, field);
		}
	}
}
