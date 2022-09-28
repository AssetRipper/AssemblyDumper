using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core;
using AssetRipper.Core.IO;
using AssetRipper.Core.IO.Asset;
using AssetRipper.IO.Endian;
using System.Diagnostics;
using System.IO;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass101_FillWriteMethods
	{
		private static IMethodDefOrRef? alignStreamMethod;
		private static IMethodDescriptor? writeInt32Method;
		private static ITypeDefOrRef? assetWriterReference;

		private static ITypeDefOrRef? assetDictionaryReference;
		private static TypeDefinition? assetDictionaryDefinition;
		private static ITypeDefOrRef? assetListReference;
		private static TypeDefinition? assetListDefinition;
		private static ITypeDefOrRef? keyValuePairReference;
		private static TypeDefinition? keyValuePairDefinition;

		private static MethodDefinition? writeAssetDefinition;
		private static MethodDefinition? writeAssetAlignDefinition;

		private const string WriteRelease = nameof(UnityAssetBase.WriteRelease);
		private const string WriteEditor = nameof(UnityAssetBase.WriteEditor);
		private static string WriteMethod => emittingRelease ? WriteRelease : WriteEditor;

		private static readonly bool throwNotSupported = false;
		private static readonly Dictionary<string, IMethodDescriptor> methodDictionary = new();
		private static readonly SignatureComparer signatureComparer = new()
		{
			AcceptNewerAssemblyVersionNumbers = true,
			IgnoreAssemblyVersionNumbers = true
		};
		private static bool emittingRelease;

		private static void Initialize()
		{
			alignStreamMethod = SharedState.Instance.Importer.ImportMethod<EndianWriter>(m => m.Name == nameof(EndianWriter.AlignStream));
			writeInt32Method = ImportPrimitiveWriteMethod(ElementType.I4);
			assetWriterReference = SharedState.Instance.Importer.ImportType<AssetWriter>();

			assetDictionaryReference = SharedState.Instance.Importer.ImportType(typeof(AssetDictionary<,>));
			assetListReference = SharedState.Instance.Importer.ImportType(typeof(AssetList<>));
			keyValuePairReference = SharedState.Instance.Importer.ImportType(typeof(NullableKeyValuePair<,>));

			assetDictionaryDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>));
			assetListDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetList<>));
			keyValuePairDefinition = SharedState.Instance.Importer.LookupType(typeof(NullableKeyValuePair<,>));
		}

		public static void DoPass()
		{
			Initialize();

			emittingRelease = true;
			methodDictionary.Clear();
			writeAssetDefinition = MakeGenericListMethod(false);
			writeAssetAlignDefinition = MakeGenericListMethod(true);
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.FillReleaseWriteMethod(instance.Class, instance.VersionRange.Start);
				}
			}
			CreateHelperClassForWriteMethods();

			emittingRelease = false;
			methodDictionary.Clear();
			writeAssetDefinition = MakeGenericListMethod(false);
			writeAssetAlignDefinition = MakeGenericListMethod(true);
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.FillEditorWriteMethod(instance.Class, instance.VersionRange.Start);
				}
			}
			CreateHelperClassForWriteMethods();

			methodDictionary.Clear();
		}

		private static void CreateHelperClassForWriteMethods()
		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.HelpersNamespace, $"{WriteMethod}Methods");
			type.IsPublic = false;
			type.Methods.Add(writeAssetDefinition!);
			type.Methods.Add(writeAssetAlignDefinition!);
			foreach ((string _, IMethodDescriptor method) in methodDictionary.OrderBy(pair => pair.Key))
			{
				if (method is MethodDefinition methodDefinition && methodDefinition.DeclaringType is null)
				{
					type.Methods.Add(methodDefinition);
				}
			}
			Console.WriteLine($"\t{type.Methods.Count} {WriteMethod} helper methods");
		}

		private static void FillEditorWriteMethod(this TypeDefinition type, UniversalClass klass, UnityVersion version)
		{
			MethodDefinition method = type.Methods.First(m => m.Name == WriteEditor);
			CilInstructionCollection processor = method.GetProcessor();

			if (throwNotSupported)
			{
				processor.AddNotSupportedException();
			}
			else
			{
				if (klass.EditorRootNode != null)
				{
					foreach (UniversalNode unityNode in klass.EditorRootNode.SubNodes)
					{
						FieldDefinition field = type.GetFieldByName(unityNode.Name, true);
						IMethodDescriptor fieldWriteMethod = GetOrMakeMethod(unityNode, field.Signature!.FieldType, version);
						processor.Add(CilOpCodes.Ldarg_0);//this
						processor.Add(CilOpCodes.Ldfld, field);
						processor.Add(CilOpCodes.Ldarg_1);//writer
						processor.AddCall(fieldWriteMethod);
					}
				}
				processor.Add(CilOpCodes.Ret);
			}
			processor.OptimizeMacros();
		}

		private static void FillReleaseWriteMethod(this TypeDefinition type, UniversalClass klass, UnityVersion version)
		{
			MethodDefinition method = type.Methods.First(m => m.Name == WriteRelease);
			CilInstructionCollection processor = method.GetProcessor();

			if (throwNotSupported)
			{
				processor.AddNotSupportedException();
			}
			else
			{
				if (klass.ReleaseRootNode != null)
				{
					foreach (UniversalNode unityNode in klass.ReleaseRootNode.SubNodes)
					{
						FieldDefinition field = type.GetFieldByName(unityNode.Name, true);
						IMethodDescriptor fieldWriteMethod = GetOrMakeMethod(unityNode, field.Signature!.FieldType, version);
						processor.Add(CilOpCodes.Ldarg_0);//this
						processor.Add(CilOpCodes.Ldfld, field);
						processor.Add(CilOpCodes.Ldarg_1);//writer
						processor.AddCall(fieldWriteMethod);
					}
				}
				processor.Add(CilOpCodes.Ret);
			}
			processor.OptimizeMacros();
		}

		private static IMethodDescriptor GetOrMakeMethod(UniversalNode node, TypeSignature type, UnityVersion version)
		{
			string uniqueName = GetName(node, version);
			if (methodDictionary.TryGetValue(uniqueName, out IMethodDescriptor? method))
			{
				return method;
			}

			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition typeDefinition = subclassGroup.GetTypeForVersion(version);
				Debug.Assert(signatureComparer.Equals(typeDefinition.ToTypeSignature(), type));
				method = typeDefinition.GetMethodByName(WriteMethod);
				methodDictionary.Add(uniqueName, method);
				return method;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode elementTypeNode = arrayNode.SubNodes[1];
						bool align = node.AlignBytes || arrayNode.AlignBytes;
						if (type is GenericInstanceTypeSignature genericSignature)
						{
							Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetList<int>)}`1");
							Debug.Assert(genericSignature.TypeArguments.Count == 1);
							method = MakeListMethod(uniqueName, elementTypeNode, genericSignature.TypeArguments[0], version, align);
						}
						else
						{
							SzArrayTypeSignature arrayType = (SzArrayTypeSignature)type;
							TypeSignature elementType = arrayType.BaseType;
							method = MakeArrayMethod(uniqueName, elementTypeNode, elementType, version, align);
						}
					}
					break;
				case "map":
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode pairNode = arrayNode.SubNodes[1];
						UniversalNode firstTypeNode = pairNode.SubNodes[0];
						UniversalNode secondTypeNode = pairNode.SubNodes[1];
						bool align = node.AlignBytes || arrayNode.AlignBytes;
						GenericInstanceTypeSignature genericSignature = (GenericInstanceTypeSignature)type;
						Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetDictionary<int, int>)}`2");
						Debug.Assert(genericSignature.TypeArguments.Count == 2);
						method = MakeDictionaryMethod(uniqueName, firstTypeNode, genericSignature.TypeArguments[0], secondTypeNode, genericSignature.TypeArguments[1], version, align);
					}
					break;
				case "pair":
					{
						UniversalNode firstTypeNode = node.SubNodes[0];
						UniversalNode secondTypeNode = node.SubNodes[1];
						bool align = node.AlignBytes;
						GenericInstanceTypeSignature genericSignature = (GenericInstanceTypeSignature)type;
						Debug.Assert(genericSignature.GenericType.Name == $"{nameof(NullableKeyValuePair<int, int>)}`2");
						Debug.Assert(genericSignature.TypeArguments.Count == 2);
						method = MakePairMethod(uniqueName, firstTypeNode, genericSignature.TypeArguments[0], secondTypeNode, genericSignature.TypeArguments[1], version, align);
					}
					break;
				case "TypelessData": //byte array
					{
						method = MakeTypelessDataMethod(uniqueName, node.AlignBytes);
					}
					break;
				case "Array":
					{
						UniversalNode elementTypeNode = node.SubNodes[1];
						bool align = node.AlignBytes;
						if (type is GenericInstanceTypeSignature genericSignature)
						{
							Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetList<int>)}`1");
							Debug.Assert(genericSignature.TypeArguments.Count == 1);
							method = MakeListMethod(uniqueName, elementTypeNode, genericSignature.TypeArguments[0], version, align);
						}
						else
						{
							SzArrayTypeSignature arrayType = (SzArrayTypeSignature)type;
							TypeSignature elementType = arrayType.BaseType;
							method = MakeArrayMethod(uniqueName, elementTypeNode, elementType, version, align);
						}
					}
					break;
				default:
					method = MakePrimitiveMethod(uniqueName, node, node.AlignBytes);
					break;
			}

			if (method is null)
			{
				throw new InvalidOperationException();
			}

			methodDictionary.Add(uniqueName, method);
			return method;
		}

		private static IMethodDescriptor? MakeTypelessDataMethod(string uniqueName, bool align)
		{
			IMethodDefOrRef writeBytesMethod = SharedState.Instance.Importer.ImportMethod<BinaryWriter>(m =>
			{
				return m.Name == nameof(BinaryWriter.Write)
					&& m.Parameters.Count == 1
					&& m.Parameters[0].ParameterType is SzArrayTypeSignature arrayTypeSignature
					&& arrayTypeSignature.BaseType is CorLibTypeSignature corLibTypeSignature
					&& corLibTypeSignature.ElementType == ElementType.U1;
			});
			MethodDefinition method = NewWriteMethod(uniqueName, SharedState.Instance.Importer.UInt8.MakeSzArrayType());
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldlen);
			processor.AddCall(writeInt32Method!);

			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(writeBytesMethod);
			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor? MakePrimitiveMethod(string uniqueName, UniversalNode node, bool align)
		{
			IMethodDescriptor primitiveMethod = GetPrimitiveMethod(node);
			MethodDefinition method = NewWriteMethod(uniqueName, primitiveMethod.Signature!.ParameterTypes[0]);
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(primitiveMethod);
			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor? MakePairMethod(string uniqueName, UniversalNode firstTypeNode, TypeSignature typeSignature1, UniversalNode secondTypeNode, TypeSignature typeSignature2, UnityVersion version, bool align)
		{
			IMethodDescriptor firstWriteMethod = GetOrMakeMethod(firstTypeNode, typeSignature1, version);
			IMethodDescriptor secondWriteMethod = GetOrMakeMethod(secondTypeNode, typeSignature2, version);

			GenericInstanceTypeSignature genericPairType = keyValuePairReference!.MakeGenericInstanceType(typeSignature1, typeSignature2);

			IMethodDefOrRef keyMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericPairType,
				keyValuePairDefinition!.Methods.Single(m => m.Name == $"get_{nameof(NullableKeyValuePair<int, int>.Key)}"));

			IMethodDefOrRef valueMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericPairType,
				keyValuePairDefinition!.Methods.Single(m => m.Name == $"get_{nameof(NullableKeyValuePair<int, int>.Value)}"));

			MethodDefinition method = NewWriteMethod(uniqueName, genericPairType);
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(keyMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(firstWriteMethod);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(valueMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(secondWriteMethod);
			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor? MakeArrayMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version, bool align)
		{
			IMethodDescriptor elementWriteMethod = GetOrMakeMethod(elementTypeNode, elementType, version);

			SzArrayTypeSignature arrayTypeSignature = elementType.MakeSzArrayType();

			MethodDefinition method = NewWriteMethod(uniqueName, arrayTypeSignature);
			CilInstructionCollection processor = method.GetProcessor();

			//Read length of array
			processor.Add(CilOpCodes.Ldarg_0); //Load array
			processor.Add(CilOpCodes.Ldlen); //Get length

			//Make local and store length in it
			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.AddCall(writeInt32Method!);//Write the count

			//Make an i
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			CilInstructionLabel loopConditionStartLabel = new(); //Needed later

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.AddLoadElement(elementType);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(elementWriteMethod);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor MakeListMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version, bool align)
		{
			if (elementType is TypeDefOrRefSignature typeDefOrRefSignature && typeDefOrRefSignature.Type is TypeDefinition)
			{
				return align
					? writeAssetAlignDefinition!.MakeGenericInstanceMethod(elementType)
					: writeAssetDefinition!.MakeGenericInstanceMethod(elementType);
			}
			else
			{
				IMethodDescriptor elementWriteMethod = GetOrMakeMethod(elementTypeNode, elementType, version);
				return MakeListMethod(uniqueName, elementType, elementWriteMethod, align);
			}
		}

		private static MethodDefinition MakeGenericListMethod(bool align)
		{
			string uniqueName = align ? "ArrayAlign_Asset" : "Array_Asset";
			GenericParameterSignature elementType = new GenericParameterSignature(SharedState.Instance.Module, GenericParameterType.Method, 0);
			IMethodDefOrRef writeMethod = SharedState.Instance.Importer.ImportMethod<UnityAssetBase>(m => m.Name == WriteMethod);
			MethodDefinition method = MakeListMethod(uniqueName, elementType, writeMethod, align);

			GenericParameter genericParameter = new GenericParameter("T");
			genericParameter.Constraints.Add(new GenericParameterConstraint(SharedState.Instance.Importer.ImportType<UnityAssetBase>()));
			method.GenericParameters.Add(genericParameter);
			method.Signature!.GenericParameterCount = 1;

			return method;
		}

		private static MethodDefinition MakeListMethod(string uniqueName, TypeSignature elementType, IMethodDescriptor elementWriteMethod, bool align)
		{
			GenericInstanceTypeSignature genericListType = assetListReference!.MakeGenericInstanceType(elementType);

			IMethodDefOrRef countMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericListType,
				assetListDefinition!.Methods.Single(m => m.Name == $"get_{nameof(AssetList<int>.Count)}"));

			IMethodDefOrRef getElementMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericListType,
				assetListDefinition!.Methods.Single(m => m.Name == "get_Item"));

			MethodDefinition method = NewWriteMethod(uniqueName, genericListType);
			CilInstructionCollection processor = method.GetProcessor();

			//Load Count
			processor.Add(CilOpCodes.Ldarg_0); //Load list
			processor.AddCall(countMethod); //Call get Count

			//Make local and store length in it
			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.AddCall(writeInt32Method!);//Write the count

			//Make an i
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			CilInstructionLabel loopConditionStartLabel = new(); //Needed later

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.AddCall(getElementMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(elementWriteMethod);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor? MakeDictionaryMethod(string uniqueName, UniversalNode firstTypeNode, TypeSignature typeSignature1, UniversalNode secondTypeNode, TypeSignature typeSignature2, UnityVersion version, bool align)
		{
			IMethodDescriptor firstWriteMethod = GetOrMakeMethod(firstTypeNode, typeSignature1, version);
			IMethodDescriptor secondWriteMethod = GetOrMakeMethod(secondTypeNode, typeSignature2, version);

			GenericInstanceTypeSignature genericPairType = keyValuePairReference!.MakeGenericInstanceType(typeSignature1, typeSignature2);

			IMethodDefOrRef keyMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericPairType,
				keyValuePairDefinition!.Methods.Single(m => m.Name == $"get_{nameof(NullableKeyValuePair<int, int>.Key)}"));

			IMethodDefOrRef valueMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericPairType,
				keyValuePairDefinition!.Methods.Single(m => m.Name == $"get_{nameof(NullableKeyValuePair<int, int>.Value)}"));

			GenericInstanceTypeSignature genericDictionaryType = assetDictionaryReference!.MakeGenericInstanceType(typeSignature1, typeSignature2);

			IMethodDefOrRef countMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericDictionaryType,
				assetDictionaryDefinition!.Methods.Single(m => m.Name == $"get_{nameof(AssetDictionary<int, int>.Count)}"));

			IMethodDefOrRef getPairMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericDictionaryType,
				assetDictionaryDefinition!.Methods.Single(m => m.Name == nameof(AssetDictionary<int, int>.GetPair)));

			MethodDefinition method = NewWriteMethod(uniqueName, genericDictionaryType);
			CilInstructionCollection processor = method.GetProcessor();

			//Load Count
			processor.Add(CilOpCodes.Ldarg_0); //Load dictionary
			processor.AddCall(countMethod); //Call get Count

			//Make local and store length in it
			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Stloc, countLocal); //Store count in it

			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.AddCall(writeInt32Method!);//Write the count

			//Make an i
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32); //Create local
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			CilInstructionLabel loopConditionStartLabel = new(); //Needed later

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTarget = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			CilLocalVariable pairLocal = processor.AddLocalVariable(genericPairType);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.AddCall(getPairMethod);
			processor.Add(CilOpCodes.Stloc, pairLocal);

			processor.Add(CilOpCodes.Ldloc, pairLocal);
			processor.AddCall(keyMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(firstWriteMethod);
			processor.Add(CilOpCodes.Ldloc, pairLocal);
			processor.AddCall(valueMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(secondWriteMethod);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTarget); //Jump back up if less than

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod!);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static string GetName(UniversalNode node, UnityVersion version)
		{
			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition fieldType = subclassGroup.GetTypeForVersion(version);
				return fieldType.Name ?? throw new NullReferenceException();
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode listTypeNode = arrayNode.SubNodes[1];
						string listName = GetName(listTypeNode, version);
						return node.AlignBytes || arrayNode.AlignBytes ? $"ArrayAlign_{listName}" : $"Array_{listName}";
					}
				case "map":
					{
						UniversalNode arrayNode = node.SubNodes[0];
						UniversalNode pairNode = arrayNode.SubNodes[1];
						UniversalNode firstTypeNode = pairNode.SubNodes[0];
						UniversalNode secondTypeNode = pairNode.SubNodes[1];
						string firstTypeName = GetName(firstTypeNode, version);
						string secondTypeName = GetName(secondTypeNode, version);
						return node.AlignBytes || arrayNode.AlignBytes
							? $"MapAlign_{firstTypeName}_{secondTypeName}"
							: $"Map_{firstTypeName}_{secondTypeName}";
					}
				case "pair":
					{
						UniversalNode firstTypeNode = node.SubNodes[0];
						UniversalNode secondTypeNode = node.SubNodes[1];
						string firstTypeName = GetName(firstTypeNode, version);
						string secondTypeName = GetName(secondTypeNode, version);
						return node.AlignBytes ? $"PairAlign_{firstTypeName}_{secondTypeName}" : $"Pair_{firstTypeName}_{secondTypeName}";
					}
				case "TypelessData": //byte array
					{
						return node.AlignBytes ? "TypelessDataAlign" : "TypelessData";
					}
				case "Array":
					{
						UniversalNode listTypeNode = node.SubNodes[1];
						string listName = GetName(listTypeNode, version);
						return node.AlignBytes ? $"ArrayAlign_{listName}" : $"Array_{listName}";
					}
				default:
					return GetPrimitiveName(node);
			}
		}

		private static string GetPrimitiveName(UniversalNode node)
		{
			return node.TypeName switch
			{
				"bool" => "Boolean",
				"char" => "Character",
				"SInt8" => "SInt8",
				"UInt8" => "UInt8",
				"short" or "SInt16" => "SInt16",
				"ushort" or "UInt16" or "unsigned short" => "UInt16",
				"int" or "SInt32" or "Type*" => "SInt32",
				"uint" or "UInt32" or "unsigned int" => "UInt32",
				"SInt64" or "long long" => "SInt64",
				"UInt64" or "FileSize" or "unsigned long long" => "UInt64",
				"float" => "Single",
				"double" => "Double",
				_ => throw new NotSupportedException(node.TypeName),
			};
		}

		private static IMethodDescriptor GetPrimitiveMethod(UniversalNode node)
		{
			return node.TypeName switch
			{
				"bool" => ImportPrimitiveWriteMethod(ElementType.Boolean),
				"char" => ImportPrimitiveWriteMethod(ElementType.Char),
				"SInt8" => ImportPrimitiveWriteMethod(ElementType.I1),
				"UInt8" => ImportPrimitiveWriteMethod(ElementType.U1),
				"short" or "SInt16" => ImportPrimitiveWriteMethod(ElementType.I2),
				"ushort" or "UInt16" or "unsigned short" => ImportPrimitiveWriteMethod(ElementType.U2),
				"int" or "SInt32" or "Type*" => writeInt32Method!,
				"uint" or "UInt32" or "unsigned int" => ImportPrimitiveWriteMethod(ElementType.U4),
				"SInt64" or "long long" => ImportPrimitiveWriteMethod(ElementType.I8),
				"UInt64" or "FileSize" or "unsigned long long" => ImportPrimitiveWriteMethod(ElementType.U8),
				"float" => ImportPrimitiveWriteMethod(ElementType.R4),
				"double" => ImportPrimitiveWriteMethod(ElementType.R8),
				_ => throw new NotSupportedException(node.TypeName),
			};
		}

		private static IMethodDescriptor ImportPrimitiveWriteMethod(ElementType elementType)
		{
			return SharedState.Instance.Importer.ImportMethod<BinaryWriter>(m =>
			{
				return m.Name == nameof(BinaryWriter.Write)
					&& m.Parameters.Count == 1
					&& m.Parameters[0].ParameterType is CorLibTypeSignature corLibTypeSignature
					&& corLibTypeSignature.ElementType == elementType;
			});
		}

		private static MethodDefinition NewWriteMethod(string uniqueName, TypeSignature parameter)
		{
			MethodSignature methodSignature = MethodSignature.CreateStatic(SharedState.Instance.Importer.Void);
			MethodDefinition method = new MethodDefinition($"{WriteMethod}_{uniqueName}", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, methodSignature);
			method.CilMethodBody = new CilMethodBody(method);
			method.AddParameter(parameter, "value");
			method.AddParameter(assetWriterReference!.ToTypeSignature(), "writer");
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