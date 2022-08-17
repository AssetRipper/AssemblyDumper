using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core.IO;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.IO.Extensions;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using AssetRipper.IO.Endian;
using System.IO;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass100_FillReadMethods
	{
		private static IMethodDefOrRef? alignStreamMethod;
		private static IMethodDefOrRef? readByteArrayMethod;
		private static IMethodDefOrRef? readInt32Method;
		private static TypeDefinition? assetReaderDefinition;
		private static ITypeDefOrRef? assetReaderReference;
		private static TypeDefinition? endianReaderDefinition;
		private static ITypeDefOrRef? endianReaderReference;
		private static TypeDefinition? binaryReaderDefinition;
		private static ITypeDefOrRef? binaryReaderReference;
		private static TypeDefinition? endianReaderExtensionsDefinition;
		private static CilInstructionLabel DummyInstructionLabel { get; } = new CilInstructionLabel();

		public static void DoPass()
		{
			Initialize();
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach(GeneratedClassInstance instance in group.Instances)
				{
					DoPassOnInstance(instance);
				}
			}
		}

		private static void Initialize()
		{
			alignStreamMethod = SharedState.Instance.Importer.ImportMethod<EndianReader>(m => m.Name == nameof(EndianReader.AlignStream));
			readByteArrayMethod = SharedState.Instance.Importer.ImportMethod<EndianReader>(m => m.Name == nameof(EndianReader.ReadByteArray) && m.Parameters.Count == 0);
			readInt32Method = SharedState.Instance.Importer.ImportMethod<EndianReader>(m => m.Name == nameof(EndianReader.ReadInt32));
			assetReaderDefinition = SharedState.Instance.Importer.LookupType<AssetReader>();
			assetReaderReference = SharedState.Instance.Importer.ImportType<AssetReader>();
			endianReaderDefinition = SharedState.Instance.Importer.LookupType<EndianReader>();
			endianReaderReference = SharedState.Instance.Importer.ImportType<EndianReader>();
			binaryReaderDefinition = SharedState.Instance.Importer.LookupType<BinaryReader>();
			binaryReaderReference = SharedState.Instance.Importer.ImportType<BinaryReader>();
			endianReaderExtensionsDefinition = SharedState.Instance.Importer.LookupType(typeof(EndianReaderExtensions));
		}

		private static void DoPassOnInstance(GeneratedClassInstance instance)
		{
			MethodDefinition editorModeReadMethod = instance.Type.Methods.Single(m => m.Name == "ReadEditor");
			MethodDefinition releaseModeReadMethod = instance.Type.Methods.Single(m => m.Name == "ReadRelease");

			CilMethodBody editorModeBody = editorModeReadMethod.CilMethodBody!;
			CilMethodBody releaseModeBody = releaseModeReadMethod.CilMethodBody!;

			CilInstructionCollection editorModeProcessor = editorModeBody.Instructions;
			CilInstructionCollection releaseModeProcessor = releaseModeBody.Instructions;

			Dictionary<string, FieldDefinition> fields = instance.Type.GetAllFieldsInTypeAndBase().ToDictionary(f => f.Name!.Value, f => f);

			//Console.WriteLine($"Generating the editor read method for {name}");
			if (instance.Class.EditorRootNode != null)
			{
				foreach (UniversalNode unityNode in instance.Class.EditorRootNode.SubNodes)
				{
					AddLoadToProcessor(unityNode, editorModeProcessor, fields, instance.VersionRange.Start);
				}
			}

			//Console.WriteLine($"Generating the release read method for {name}");
			if (instance.Class.ReleaseRootNode != null)
			{
				foreach (UniversalNode unityNode in instance.Class.ReleaseRootNode.SubNodes)
				{
					AddLoadToProcessor(unityNode, releaseModeProcessor, fields, instance.VersionRange.Start);
				}
			}

			editorModeProcessor.Add(CilOpCodes.Ret);
			releaseModeProcessor.Add(CilOpCodes.Ret);

			editorModeProcessor.OptimizeMacros();
			releaseModeProcessor.OptimizeMacros();
		}

		private static void AddLoadToProcessor(UniversalNode node, CilInstructionCollection processor, Dictionary<string, FieldDefinition> fields, UnityVersion version)
		{
			//Get field
			fields.TryGetValue(node.Name, out FieldDefinition? field);

			if (field == null)
			{
				throw new Exception($"Field {node.Name} cannot be found in {processor.Owner.Owner.DeclaringType} (fields are {string.Join(", ", fields.Values.Select(f => f.Name))})");
			}

			ReadFieldContent(node, processor, field, version);
		}

		private static void ReadFieldContent(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, UnityVersion version)
		{
			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition fieldType = subclassGroup.GetTypeForVersion(version);
				ReadAssetTypeToField(node, processor, field, fieldType, 0);
				return;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					ReadVectorToField(node, processor, field, 1, version);
					return;
				case "map":
					ReadDictionaryToField(node, processor, field, version);
					return;
				case "pair":
					ReadPairToField(node, processor, field, version);
					return;
				case "TypelessData": //byte array
					ReadByteArrayToField(node, processor, field);
					return;
				case "Array":
					ReadArrayToField(node, processor, field, 1, version);
					return;
			}

			ReadPrimitiveTypeToField(node, processor, field, 0);
		}

		private static CilLocalVariable ReadContentToLocal(UniversalNode node, CilInstructionCollection processor, UnityVersion version)
		{
			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition fieldType = subclassGroup.GetTypeForVersion(version);
				return ReadAssetTypeToLocal(node, processor, fieldType, 0);
			}

			return node.TypeName switch
			{
				"vector" or "set" or "staticvector" => ReadVectorToLocal(node, processor, 1, version),
				"map" => ReadDictionaryToLocal(node, processor, version),
				"pair" => ReadPairToLocal(node, processor, version),
				"TypelessData" => ReadByteArrayToLocal(node, processor),
				"Array" => ReadArrayToLocal(node, processor, 1, version),
				_ => ReadPrimitiveTypeToLocal(node, processor, 0),
			};
		}

		private static void MaybeAlignBytes(UniversalNode node, CilInstructionCollection processor)
		{
			if (((TransferMetaFlags)node.MetaFlag).IsAlignBytes())
			{
				//Load reader
				processor.Add(CilOpCodes.Ldarg_1);

				//Call it
				processor.Add(CilOpCodes.Call, alignStreamMethod!);
			}
		}

		private static void ReadPrimitiveTypeToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth)
		{
			//Load this
			processor.Add(CilOpCodes.Ldarg_0);

			ReadPrimitiveTypeWithoutAligning(node, processor, arrayDepth);

			//Store result in field
			processor.Add(CilOpCodes.Stfld, field);

			MaybeAlignBytes(node, processor);
		}

		private static CilLocalVariable ReadPrimitiveTypeToLocal(UniversalNode node, CilInstructionCollection processor, int arrayDepth)
		{
			TypeSignature fieldType = ReadPrimitiveTypeWithoutAligning(node, processor, arrayDepth);
			CilLocalVariable local = new CilLocalVariable(fieldType);
			processor.Owner.LocalVariables.Add(local);
			processor.Add(CilOpCodes.Stloc, local);
			MaybeAlignBytes(node, processor);
			return local;
		}

		private static TypeSignature ReadPrimitiveTypeWithoutAligning(UniversalNode node, CilInstructionCollection processor, int arrayDepth)
		{
			//Primitives
			string csPrimitiveTypeName = PrimitiveTypes.CppPrimitivesToCSharpPrimitives[node.TypeName];
			CorLibTypeSignature csPrimitiveType = SharedState.Instance.Importer.GetCppPrimitiveTypeSignature(node.TypeName) ?? throw new Exception();

			//Read
			MethodDefinition? primitiveReadMethod = arrayDepth switch
			{
				0 => assetReaderDefinition?.Methods.SingleOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}") //String
				     ?? endianReaderDefinition?.Methods.SingleOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}")
				     ?? binaryReaderDefinition?.Methods.SingleOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}"), //Byte, SByte, and Boolean
				1 => endianReaderDefinition?.Methods.SingleOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}Array" && m.Parameters.Count == 1),
				2 => endianReaderExtensionsDefinition?.Methods.SingleOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}ArrayArray"),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadPrimitiveType does not support array depth '{arrayDepth}'"),
			};

			if (primitiveReadMethod == null)
			{
				throw new Exception($"Missing a read method for {csPrimitiveTypeName} in {processor.Owner.Owner.DeclaringType}");
			}

			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			if (arrayDepth == 1)//Read{Primitive}Array has an allowAlignment parameter
			{
				processor.Add(CilOpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call read method
			processor.Add(CilOpCodes.Callvirt, SharedState.Instance.Importer.UnderlyingImporter.ImportMethod(primitiveReadMethod));

			return arrayDepth switch
			{
				0 => csPrimitiveType,
				1 => csPrimitiveType.MakeSzArrayType(),
				2 => csPrimitiveType.MakeSzArrayType().MakeSzArrayType(),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth)),
			};
		}

		private static void ReadAssetTypeToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, TypeDefinition fieldType, int arrayDepth)
		{
			//Load "this" for field store later
			processor.Add(CilOpCodes.Ldarg_0);

			ReadAssetTypeWithoutAligning(node, processor, fieldType, arrayDepth);

			//Store result in field
			processor.Add(CilOpCodes.Stfld, field);

			//Maybe Align Bytes
			MaybeAlignBytes(node, processor);
		}

		private static CilLocalVariable ReadAssetTypeToLocal(UniversalNode node, CilInstructionCollection processor, TypeDefinition fieldBaseType, int arrayDepth)
		{
			TypeSignature fieldSignature = ReadAssetTypeWithoutAligning(node, processor, fieldBaseType, arrayDepth);
			CilLocalVariable local = new CilLocalVariable(fieldSignature);
			processor.Owner.LocalVariables.Add(local);
			processor.Add(CilOpCodes.Stloc, local);
			MaybeAlignBytes(node, processor);
			return local;
		}

		private static TypeSignature ReadAssetTypeWithoutAligning(UniversalNode node, CilInstructionCollection processor, TypeDefinition fieldBaseType, int arrayDepth)
		{
			TypeSignature fieldBaseSignature = fieldBaseType.ToTypeSignature();
			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			//Get ReadAsset
			MethodDefinition readMethod = arrayDepth switch
			{
				0 => assetReaderDefinition!.Methods.First(m => m.Name == "ReadAsset"),
				1 => assetReaderDefinition!.Methods.First(m => m.Name == "ReadAssetList" && m.Parameters.Count == 1),
				2 => assetReaderDefinition!.Methods.First(m => m.Name == "ReadAssetListList" && m.Parameters.Count == 1),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadAssetType does not support array depth '{arrayDepth}'"),
			};

			//Make generic ReadAsset<T>
			MethodSpecification genericInst = MethodUtils.MakeGenericInstanceMethod(SharedState.Instance.Importer, readMethod, fieldBaseSignature);

			if (arrayDepth > 0)//ReadAssetArray and ReadAssetArrayArray have an allowAlignment parameter
			{
				processor.Add(CilOpCodes.Ldc_I4, 0);//load false onto the stack
			}

			//Call it
			processor.Add(CilOpCodes.Call, genericInst);

			TypeSignature fieldFullType = arrayDepth switch
			{
				0 => fieldBaseSignature,
				1 => fieldBaseSignature.MakeAssetListType(),
				2 => fieldBaseSignature.MakeAssetListType().MakeAssetListType(),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth)),
			};

			//processor.AddDefaultValue(fieldFullType);

			return fieldFullType;
		}

		private static void ReadByteArrayToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field)
		{
			//Load "this" for field store later
			processor.Add(CilOpCodes.Ldarg_0);
			ReadByteArrayWithoutAligning(node, processor);
			//Store result in field
			processor.Add(CilOpCodes.Stfld, field);
			MaybeAlignBytes(node, processor);
		}

		private static CilLocalVariable ReadByteArrayToLocal(UniversalNode node, CilInstructionCollection processor)
		{
			ReadByteArrayWithoutAligning(node, processor);
			CilLocalVariable local = new CilLocalVariable(SharedState.Instance.Importer.UInt8.MakeSzArrayType());
			processor.Owner.LocalVariables.Add(local);
			processor.Add(CilOpCodes.Stloc, local);
			MaybeAlignBytes(node, processor);
			return local;
		}

		private static void ReadByteArrayWithoutAligning(UniversalNode node, CilInstructionCollection processor)
		{
			//Load reader
			processor.Add(CilOpCodes.Ldarg_1);

			//Call it
			processor.Add(CilOpCodes.Call, readByteArrayMethod!);
		}

		private static void ReadVectorToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth, UnityVersion version)
		{
			UniversalNode listTypeNode = node.SubNodes[0];
			if (listTypeNode.TypeName is "Array")
			{
				ReadArrayToField(listTypeNode, processor, field, arrayDepth, version);
			}
			else
			{
				throw new ArgumentException($"Invalid subnode for {node.TypeName}", nameof(node));
			}

			MaybeAlignBytes(node, processor);
		}

		private static CilLocalVariable ReadVectorToLocal(UniversalNode node, CilInstructionCollection processor, int arrayDepth, UnityVersion version)
		{
			UniversalNode listTypeNode = node.SubNodes[0];
			if (listTypeNode.TypeName is "Array")
			{
				CilLocalVariable result = ReadArrayToLocal(listTypeNode, processor, arrayDepth, version);
				MaybeAlignBytes(node, processor);
				return result;
			}
			else
			{
				throw new ArgumentException($"Invalid subnode for {node.TypeName}", nameof(node));
			}
		}

		private static void ReadArrayToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, int arrayDepth, UnityVersion version)
		{
			UniversalNode listTypeNode = node.SubNodes[1];
			if (SharedState.Instance.SubclassGroups.TryGetValue(listTypeNode.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition fieldType = subclassGroup.GetTypeForVersion(version);
				ReadAssetTypeToField(listTypeNode, processor, field, fieldType, arrayDepth);
			}
			else
			{
				switch (listTypeNode.TypeName)
				{
					case "vector" or "set" or "staticvector":
						ReadVectorToField(listTypeNode, processor, field, arrayDepth + 1, version);
						break;
					case "Array":
						ReadArrayToField(listTypeNode, processor, field, arrayDepth + 1, version);
						break;
					case "map":
						if (arrayDepth > 1)
						{
							throw new("ReadArray does not support dictionary arrays with a depth > 1. Found in {node.Name} (field {field}) of {processor.Body.Method.DeclaringType}");
						}

						ReadDictionaryArrayToField(processor, field, node, version);
						break;
					case "pair":
						if (arrayDepth > 2)
						{
							throw new($"ReadArray does not support Pair arrays with a depth > 2. Found in {node.Name} (field {field}) of {processor.Owner.Owner.DeclaringType}");
						}

						if (arrayDepth == 2)
						{
							ReadPairArrayArrayToField(processor, field, listTypeNode, version);
							break;
						}

						ReadPairArrayToField(processor, field, listTypeNode, version);
						break;
					default:
						ReadPrimitiveTypeToField(listTypeNode, processor, field, arrayDepth);
						break;
				}
			}

			MaybeAlignBytes(node, processor);
		}

		private static CilLocalVariable ReadArrayToLocal(UniversalNode node, CilInstructionCollection processor, int arrayDepth, UnityVersion version)
		{
			UniversalNode listTypeNode = node.SubNodes[1];
			if (SharedState.Instance.SubclassGroups.TryGetValue(listTypeNode.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition fieldType = subclassGroup.GetTypeForVersion(version);
				return ReadAssetTypeToLocal(listTypeNode, processor, fieldType, arrayDepth);
			}
			switch (listTypeNode.TypeName)
			{
				case "vector" or "set" or "staticvector":
					return ReadVectorToLocal(listTypeNode, processor, arrayDepth + 1, version);
				case "Array":
					return ReadArrayToLocal(listTypeNode, processor, arrayDepth + 1, version);
				case "map":
					if (arrayDepth > 1)
					{
						throw new NotSupportedException($"ReadArray does not support dictionary arrays with a depth > 1. Found in {node.Name} of {processor.Owner.Owner.DeclaringType}");
					}

					return ReadDictionaryArrayToLocal(processor, node, version);
				case "pair":
					if (arrayDepth > 2)
					{
						throw new($"ReadArray does not support Pair arrays with a depth > 2. Found in {node.Name} of {processor.Owner.Owner.DeclaringType}");
					}

					if (arrayDepth == 2)
					{
						return ReadPairArrayArrayToLocal(processor, listTypeNode, version);
					}

					return ReadPairArrayToLocal(processor, listTypeNode, version);
				default:
					return ReadPrimitiveTypeToLocal(listTypeNode, processor, arrayDepth);
			}
		}

		private static void ReadPairArrayToField(CilInstructionCollection processor, FieldDefinition field, UniversalNode listTypeNode, UnityVersion version)
		{
			CilLocalVariable arrayLocal = ReadPairArrayToLocal(processor, listTypeNode, version);
			//Now just store field
			processor.Add(CilOpCodes.Ldarg_0); //Load this
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array
			processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static CilLocalVariable ReadPairArrayToLocal(CilInstructionCollection processor, UniversalNode listTypeNode, UnityVersion version)
		{
			//Strategy:
			//Read Count
			//Make array of size count
			//For i = 0 .. count
			//	Read pair, store in array
			//Store array in field

			//Resolve things we'll need
			UniversalNode first = listTypeNode.SubNodes[0];
			UniversalNode second = listTypeNode.SubNodes[1];
			GenericInstanceTypeSignature genericKvp = GenericTypeResolver.ResolvePairType(first, second, version);

			//SzArrayTypeSignature arrayType = genericKvp.MakeSzArrayType();
			GenericInstanceTypeSignature arrayType = genericKvp.MakeAssetListType();

			//Read length of array
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, readInt32Method!); //Call int reader

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			//processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			//processor.Add(CilOpCodes.Newarr, genericKvp.ToTypeDefOrRef()); //Create new array of kvp with given count
			processor.Add(CilOpCodes.Newobj, GetAssetListConstructor(genericKvp));
			CilLocalVariable arrayLocal = new CilLocalVariable(arrayType); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create a label for a dummy instruction to jump back to
			CilInstructionLabel jumpTargetLabel = new CilInstructionLabel();

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			jumpTargetLabel.Instruction = processor.Add(CilOpCodes.Nop); //Create a dummy instruction to jump back to

			//Read element at index i of array
			CilLocalVariable pairLocal = ReadPairToLocal(listTypeNode, processor, version); //Read the pair
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			//processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldloc, pairLocal);
			//processor.Add(CilOpCodes.Stelem, genericKvp.ToTypeDefOrRef()); //Store in array
			//processor.Add(CilOpCodes.Call, GetAssetListSetItemMethod(genericKvp));
			processor.Add(CilOpCodes.Call, GetAssetListAddMethod(genericKvp));

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			return arrayLocal;
		}

		private static void ReadPairArrayArrayToField(CilInstructionCollection processor, FieldDefinition field, UniversalNode listTypeNode, UnityVersion version)
		{
			CilLocalVariable arrayLocal = ReadPairArrayArrayToLocal(processor, listTypeNode, version);
			//Now just store field
			processor.Add(CilOpCodes.Ldarg_0); //Load this
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array
			processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static CilLocalVariable ReadPairArrayArrayToLocal(CilInstructionCollection processor, UniversalNode pairNode, UnityVersion version)
		{
			//Strategy:
			//Read Count
			//Make array of size count
			//For i = 0 .. count
			//	Read array of pairs, store in array of arrays
			//Store array in field

			//Resolve things we'll need
			UniversalNode first = pairNode.SubNodes[0];
			UniversalNode second = pairNode.SubNodes[1];
			GenericInstanceTypeSignature genericKvp = GenericTypeResolver.ResolvePairType(first, second, version);

			//SzArrayTypeSignature arrayType = genericKvp.MakeSzArrayType();
			GenericInstanceTypeSignature arrayType = genericKvp.MakeAssetListType();

			//Read length of array
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, readInt32Method!); //Call int reader

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			//processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			//processor.Add(CilOpCodes.Newarr, arrayType.ToTypeDefOrRef()); //Create new array of arrays of kvps with given count
			processor.Add(CilOpCodes.Newobj, GetAssetListConstructor(arrayType));
			//CilLocalVariable arrayLocal = new CilLocalVariable(arrayType.MakeSzArrayType()); //Create local
			CilLocalVariable arrayLocal = new CilLocalVariable(arrayType.MakeAssetListType()); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read element at index i of array
			CilLocalVariable pairArrayLocal = ReadPairArrayToLocal(processor, pairNode, version); //Read the array of pairs
			
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array of arrays local
			//processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldloc, pairArrayLocal); //Load array of pairs
			//processor.Add(CilOpCodes.Stelem, arrayType.ToTypeDefOrRef()); //Store in array
			//processor.Add(CilOpCodes.Call, GetAssetListSetItemMethod(arrayType));
			processor.Add(CilOpCodes.Call, GetAssetListAddMethod(arrayType));

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			MaybeAlignBytes(pairNode, processor);

			return arrayLocal;
		}

		private static void ReadDictionaryArrayToField(CilInstructionCollection processor, FieldDefinition field, UniversalNode node, UnityVersion version)
		{
			CilLocalVariable arrayLocal = ReadDictionaryArrayToLocal(processor, node, version);
			//Now just store field
			processor.Add(CilOpCodes.Ldarg_0); //Load this
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array
			processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static CilLocalVariable ReadDictionaryArrayToLocal(CilInstructionCollection processor, UniversalNode node, UnityVersion version)
		{
			//you know the drill
			//read count
			//make empty array
			//for i = 0 .. count 
			//  read an entire bloody dictionary
			//set field

			//we need an array type, so let's get that
			UniversalNode dictNode = node.SubNodes[1];
			GenericInstanceTypeSignature dictType = GenericTypeResolver.ResolveDictionaryType(dictNode, version);
			//SzArrayTypeSignature arrayType = dictType.MakeSzArrayType(); //cursed. that is all.
			GenericInstanceTypeSignature arrayType = dictType.MakeAssetListType();

			//Read length of array
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, readInt32Method!); //Call int reader

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty array and local for it
			//processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			//processor.Add(CilOpCodes.Newarr, dictType.ToTypeDefOrRef()); //Create new array of dictionaries with given count
			processor.Add(CilOpCodes.Newobj, GetAssetListConstructor(dictType));
			CilLocalVariable arrayLocal = new CilLocalVariable(arrayType); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read element at index i of array
			CilLocalVariable dictLocal = ReadDictionaryToLocal(dictNode, processor, version);
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			//processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load dict
			//processor.Add(CilOpCodes.Stelem, dictType.ToTypeDefOrRef()); //Store in array
			//processor.Add(CilOpCodes.Call, GetAssetListSetItemMethod(dictType));
			processor.Add(CilOpCodes.Call, GetAssetListAddMethod(dictType));

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			MaybeAlignBytes(node, processor);

			return arrayLocal;
		}

		private static void ReadDictionaryToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, UnityVersion version)
		{
			CilLocalVariable dictLocal = ReadDictionaryToLocal(node, processor, version);
			//Now just store field
			processor.Add(CilOpCodes.Ldarg_0); //Load this

			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load dict

			processor.Add(CilOpCodes.Stfld, field); //Store field
		}

		private static CilLocalVariable ReadDictionaryToLocal(UniversalNode node, CilInstructionCollection processor, UnityVersion version)
		{
			//Strategy:
			//Read Count
			//Make dictionary
			//For i = 0 .. count
			//	Read key, read value, store in dict
			//Store dict in field

			//Resolve things we'll need
			GenericInstanceTypeSignature genericDictType = GenericTypeResolver.ResolveDictionaryType(node, version);
			IMethodDefOrRef genericDictCtor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, genericDictType, 0);
			IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictType, SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>)).Methods.Single(m => m.Name == "Add" && m.Parameters.Count == 2));

			//Read length of array
			processor.Add(CilOpCodes.Ldarg_1); //Load reader
			processor.Add(CilOpCodes.Call, readInt32Method!); //Call int reader

			//Make local and store length in it
			CilLocalVariable countLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			//Create empty dict and local for it
			processor.Add(CilOpCodes.Newobj, genericDictCtor); //Create new dictionary
			CilLocalVariable dictLocal = new CilLocalVariable(genericDictType); //Create local
			processor.Owner.LocalVariables.Add(dictLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, dictLocal); //Store dict in local

			//Make an i
			CilLocalVariable iLocal = new CilLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			CilInstruction unconditionalBranch = processor.Add(CilOpCodes.Br, DummyInstructionLabel);

			//Now we just read key + value, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read ith key-value pair of dict 
			CilLocalVariable local1 = ReadContentToLocal(node.SubNodes[0].SubNodes[1].SubNodes[0], processor, version); //Load first
			CilLocalVariable local2 = ReadContentToLocal(node.SubNodes[0].SubNodes[1].SubNodes[1], processor, version); //Load second
			processor.Add(CilOpCodes.Ldloc, dictLocal); //Load dict local
			processor.Add(CilOpCodes.Ldloc, local1); //Load 1st local
			processor.Add(CilOpCodes.Ldloc, local2); //Load 2nd local
			processor.Add(CilOpCodes.Call, addMethod); //Call Add(TKey, TValue)

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			ICilLabel loopConditionStartLabel = processor.Add(CilOpCodes.Ldloc, iLocal).CreateLabel(); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than
			unconditionalBranch.Operand = loopConditionStartLabel;

			MaybeAlignBytes(node, processor);

			return dictLocal;
		}

		private static void ReadPairToField(UniversalNode node, CilInstructionCollection processor, FieldDefinition field, UnityVersion version)
		{
			//Load this for later usage
			processor.Add(CilOpCodes.Ldarg_0);

			ReadPair(node, processor, version);

			//Store in field if desired
			processor.Add(CilOpCodes.Stfld, field);
		}

		private static CilLocalVariable ReadPairToLocal(UniversalNode node, CilInstructionCollection processor, UnityVersion version)
		{
			TypeSignature pairType = ReadPair(node, processor, version);
			CilLocalVariable local = new CilLocalVariable(pairType);
			processor.Owner.LocalVariables.Add(local);
			processor.Add(CilOpCodes.Stloc, local);
			return local;
		}

		private static TypeSignature ReadPair(UniversalNode node, CilInstructionCollection processor, UnityVersion version)
		{
			//Read one, read two, construct tuple, store field
			//Passing a null field to any of the Read generators causes no field store or this load to be emitted
			//Which is just what we want
			UniversalNode first = node.SubNodes[0];
			UniversalNode second = node.SubNodes[1];

			//Load the left side of the pair
			CilLocalVariable local1 = ReadContentToLocal(first, processor, version);

			//Load the right side of the pair
			CilLocalVariable local2 = ReadContentToLocal(second, processor, version);

			processor.Add(CilOpCodes.Ldloc, local1);
			processor.Add(CilOpCodes.Ldloc, local2);

			GenericInstanceTypeSignature genericKvp = GenericTypeResolver.ResolvePairType(first, second, version);

			IMethodDefOrRef genericCtor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, genericKvp, 2);

			//Call constructor
			processor.Add(CilOpCodes.Newobj, genericCtor);

			return genericKvp;
		}

		private static IMethodDefOrRef GetAssetListSetItemMethod(TypeSignature typeArgument)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == $"set_Item");
			GenericInstanceTypeSignature assetListTypeSignature = typeArgument.MakeAssetListType();
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetListTypeSignature, method);
		}

		private static IMethodDefOrRef GetAssetListAddMethod(TypeSignature typeArgument)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == $"Add");
			GenericInstanceTypeSignature assetListTypeSignature = typeArgument.MakeAssetListType();
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetListTypeSignature, method);
		}

		private static IMethodDefOrRef GetAssetListConstructor(TypeSignature typeArgument)
		{
			GenericInstanceTypeSignature assetListTypeSignature = typeArgument.MakeAssetListType();
			return MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, assetListTypeSignature, 0);
		}

		private static GenericInstanceTypeSignature MakeAssetListType(this TypeSignature typeArgument)
		{
			return SharedState.Instance.Importer.ImportType(typeof(AssetList<>)).MakeGenericInstanceType(typeArgument);
		}
	}
}
