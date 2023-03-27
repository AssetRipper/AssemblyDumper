﻿using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.Generics;
using AssetRipper.IO.Endian;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass100_FillReadMethods
	{
#nullable disable
		private static IMethodDefOrRef alignStreamMethod;
		private static IMethodDefOrRef readInt32Method;
		/// <summary>
		/// TypeSignature for <see langword="ref"/> <see cref="EndianSpanReader"/>
		/// </summary>
		private static TypeSignature endianSpanReaderReference;

		private static ITypeDefOrRef assetDictionaryReference;
		private static TypeDefinition assetDictionaryDefinition;
		private static ITypeDefOrRef assetListReference;
		private static TypeDefinition assetListDefinition;
		private static ITypeDefOrRef keyValuePairReference;
		private static TypeDefinition keyValuePairDefinition;

		private static MethodDefinition readAssetAlignDefinition;
		private static MethodDefinition readAssetListDefinition;
		private static MethodDefinition readAssetListAlignDefinition;
		private static MethodDefinition readAssetDictionaryDefinition;
		private static MethodDefinition readAssetDictionaryAlignDefinition;
#nullable enable

		private static readonly Dictionary<ElementType, IMethodDefOrRef> primitiveReadMethods = new();

		private static string ReadMethod => emittingRelease ? ReadRelease : ReadEditor;
		private const string ReadRelease = nameof(UnityAssetBase.ReadRelease);
		private const string ReadEditor = nameof(UnityAssetBase.ReadEditor);
		private const int MaxArraySize = 1024;

		private static readonly Dictionary<string, IMethodDescriptor> methodDictionary = new();
		private static readonly SignatureComparer signatureComparer = new(SignatureComparisonFlags.VersionAgnostic);
		private static bool emittingRelease;

		public static void DoPass()
		{
			methodDictionary.Clear();
			Initialize();

			emittingRelease = true;
			readAssetAlignDefinition = MakeGenericAssetAlignMethod();
			readAssetListDefinition = MakeGenericListMethod(false);
			readAssetListAlignDefinition = MakeGenericListMethod(true);
			readAssetDictionaryDefinition = MakeGenericDictionaryMethod(false);
			readAssetDictionaryAlignDefinition = MakeGenericDictionaryMethod(true);
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.FillReleaseWriteMethod(instance.Class, instance.VersionRange.Start);
				}
			}
			CreateHelperClassForWriteMethods();
			methodDictionary.Clear();

			emittingRelease = false;
			readAssetAlignDefinition = MakeGenericAssetAlignMethod();
			readAssetListDefinition = MakeGenericListMethod(false);
			readAssetListAlignDefinition = MakeGenericListMethod(true);
			readAssetDictionaryDefinition = MakeGenericDictionaryMethod(false);
			readAssetDictionaryAlignDefinition = MakeGenericDictionaryMethod(true);
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

		private static void Initialize()
		{
			primitiveReadMethods.Add(ElementType.Boolean, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadBoolean)));
			primitiveReadMethods.Add(ElementType.Char, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadChar)));
			primitiveReadMethods.Add(ElementType.I1, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadSByte)));
			primitiveReadMethods.Add(ElementType.U1, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadByte)));
			primitiveReadMethods.Add(ElementType.I2, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadInt16)));
			primitiveReadMethods.Add(ElementType.U2, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadUInt16)));
			primitiveReadMethods.Add(ElementType.I4, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadInt32)));
			primitiveReadMethods.Add(ElementType.U4, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadUInt32)));
			primitiveReadMethods.Add(ElementType.I8, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadInt64)));
			primitiveReadMethods.Add(ElementType.U8, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadUInt64)));
			primitiveReadMethods.Add(ElementType.R4, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadSingle)));
			primitiveReadMethods.Add(ElementType.R8, SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.ReadDouble)));

			readInt32Method = primitiveReadMethods[ElementType.I4];

			alignStreamMethod = SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader), m => m.Name == nameof(EndianSpanReader.Align));
			endianSpanReaderReference = SharedState.Instance.Importer.ImportTypeSignature(typeof(EndianSpanReader)).MakeByReferenceType();

			assetDictionaryReference = SharedState.Instance.Importer.ImportType(typeof(AssetDictionary<,>));
			assetListReference = SharedState.Instance.Importer.ImportType(typeof(AssetList<>));
			keyValuePairReference = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>));

			assetDictionaryDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetDictionary<,>));
			assetListDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetList<>));
			keyValuePairDefinition = SharedState.Instance.Importer.LookupType(typeof(AssetPair<,>));
		}

		private static void CreateHelperClassForWriteMethods()
		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.HelpersNamespace, $"{ReadMethod}Methods");
			type.IsPublic = false;
			type.Methods.Add(readAssetAlignDefinition);
			type.Methods.Add(readAssetListDefinition);
			type.Methods.Add(readAssetListAlignDefinition);
			type.Methods.Add(readAssetDictionaryDefinition);
			type.Methods.Add(readAssetDictionaryAlignDefinition);
			foreach ((string _, IMethodDescriptor method) in methodDictionary.OrderBy(pair => pair.Key))
			{
				if (method is MethodDefinition methodDefinition && methodDefinition.DeclaringType is null)
				{
					type.Methods.Add(methodDefinition);
				}
			}
			Console.WriteLine($"\t{type.Methods.Count} {ReadMethod} helper methods");
		}

		private static void FillEditorWriteMethod(this TypeDefinition type, UniversalClass klass, UnityVersion version)
		{
			type.FillMethod(ReadEditor, klass.EditorRootNode, version);
		}

		private static void FillReleaseWriteMethod(this TypeDefinition type, UniversalClass klass, UnityVersion version)
		{
			type.FillMethod(ReadRelease, klass.ReleaseRootNode, version);
		}

		private static void FillMethod(this TypeDefinition type, string methodName, UniversalNode? rootNode, UnityVersion version)
		{
			MethodDefinition method = type.Methods.First(m => m.Name == methodName);
			CilInstructionCollection processor = method.GetProcessor();

			if (rootNode is not null)
			{
				foreach (UniversalNode unityNode in rootNode.SubNodes)
				{
					FieldDefinition field = type.GetFieldByName(unityNode.Name, true);
					IMethodDescriptor fieldReadMethod = GetOrMakeMethod(unityNode, field.Signature!.FieldType, version);
					if (field.Signature.FieldType.IsArrayOrPrimitive())
					{
						processor.Add(CilOpCodes.Ldarg_0);//this
						processor.Add(CilOpCodes.Ldarg_1);//reader
						processor.AddCall(fieldReadMethod);
						processor.Add(CilOpCodes.Stfld, field);
					}
					else
					{
						processor.Add(CilOpCodes.Ldarg_0);//this
						processor.Add(CilOpCodes.Ldfld, field);
						processor.Add(CilOpCodes.Ldarg_1);//reader
						processor.AddCall(fieldReadMethod);
					}
				}
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static IMethodDescriptor GetOrMakeMethod(UniversalNode node, TypeSignature type, UnityVersion version)
		{
			string uniqueName = UniqueNameFactory.GetReadWriteName(node, version);
			if (methodDictionary.TryGetValue(uniqueName, out IMethodDescriptor? method))
			{
				return method;
			}

			if (SharedState.Instance.SubclassGroups.TryGetValue(node.TypeName, out SubclassGroup? subclassGroup))
			{
				TypeDefinition typeDefinition = subclassGroup.GetTypeForVersion(version);
				Debug.Assert(signatureComparer.Equals(typeDefinition.ToTypeSignature(), type));
				MethodDefinition typeReadMethod = typeDefinition.GetMethodByName(ReadMethod);
				method = node.AlignBytes ? readAssetAlignDefinition.MakeGenericInstanceMethod(type) : typeReadMethod;
				methodDictionary.Add(uniqueName, method);
				return method;
			}

			switch (node.NodeType)
			{
				case NodeType.Vector:
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
				case NodeType.Map:
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
				case NodeType.Pair:
					{
						UniversalNode firstTypeNode = node.SubNodes[0];
						UniversalNode secondTypeNode = node.SubNodes[1];
						bool align = node.AlignBytes;
						GenericInstanceTypeSignature genericSignature = (GenericInstanceTypeSignature)type;
						Debug.Assert(genericSignature.GenericType.Name == $"{nameof(AssetPair<int, int>)}`2");
						Debug.Assert(genericSignature.TypeArguments.Count == 2);
						method = MakePairMethod(uniqueName, firstTypeNode, genericSignature.TypeArguments[0], secondTypeNode, genericSignature.TypeArguments[1], version, align);
					}
					break;
				case NodeType.TypelessData: //byte array
					{
						method = MakeTypelessDataMethod(uniqueName, node.AlignBytes);
					}
					break;
				case NodeType.Array:
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

			methodDictionary.Add(uniqueName, method);
			return method;
		}

		private static MethodDefinition MakeGenericAssetAlignMethod()
		{
			string uniqueName = "AssetAlign";
			GenericParameterSignature elementType = new GenericParameterSignature(SharedState.Instance.Module, GenericParameterType.Method, 0);
			IMethodDefOrRef readMethod = SharedState.Instance.Importer.ImportMethod<UnityAssetBase>(m => m.Name == ReadMethod && m.Parameters[0].ParameterType is ByReferenceTypeSignature);
			MethodDefinition method = NewMethod(uniqueName, elementType);

			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Callvirt, readMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddCall(alignStreamMethod);
			processor.Add(CilOpCodes.Ret);

			GenericParameter genericParameter = new GenericParameter("T");
			genericParameter.Constraints.Add(new GenericParameterConstraint(SharedState.Instance.Importer.ImportType<UnityAssetBase>()));
			method.GenericParameters.Add(genericParameter);
			method.Signature!.GenericParameterCount = 1;

			return method;
		}

		private static IMethodDescriptor MakeDictionaryMethod(string uniqueName, UniversalNode keyTypeNode, TypeSignature keySignature, UniversalNode valueTypeNode, TypeSignature valueSignature, UnityVersion version, bool align)
		{
			if (keySignature.IsTypeDefinition() && valueSignature.IsTypeDefinition())
			{
				return align
					? readAssetDictionaryAlignDefinition.MakeGenericInstanceMethod(keySignature, valueSignature)
					: readAssetDictionaryDefinition.MakeGenericInstanceMethod(keySignature, valueSignature);
			}
			else
			{
				IMethodDescriptor firstWriteMethod = GetOrMakeMethod(keyTypeNode, keySignature, version);
				IMethodDescriptor secondWriteMethod = GetOrMakeMethod(valueTypeNode, valueSignature, version);
				return MakeDictionaryMethod(uniqueName, firstWriteMethod, keySignature, secondWriteMethod, valueSignature, align);
			}
		}

		private static MethodDefinition MakeGenericDictionaryMethod(bool align)
		{
			string uniqueName = align ? "MapAlign_Asset_Asset" : "Map_Asset_Asset";
			GenericParameterSignature keyType = new GenericParameterSignature(SharedState.Instance.Module, GenericParameterType.Method, 0);
			GenericParameterSignature valueType = new GenericParameterSignature(SharedState.Instance.Module, GenericParameterType.Method, 1);
			IMethodDefOrRef readMethod = SharedState.Instance.Importer.ImportMethod<UnityAssetBase>(m => m.Name == ReadMethod && m.Parameters[0].ParameterType is ByReferenceTypeSignature);
			MethodDefinition method = MakeDictionaryMethod(uniqueName, readMethod, keyType, readMethod, valueType, align);

			GenericParameter keyParameter = new GenericParameter("TKey", GenericParameterAttributes.DefaultConstructorConstraint);
			keyParameter.Constraints.Add(new GenericParameterConstraint(SharedState.Instance.Importer.ImportType<UnityAssetBase>()));
			method.GenericParameters.Add(keyParameter);
			GenericParameter valueParameter = new GenericParameter("TValue", GenericParameterAttributes.DefaultConstructorConstraint);
			valueParameter.Constraints.Add(new GenericParameterConstraint(SharedState.Instance.Importer.ImportType<UnityAssetBase>()));
			method.GenericParameters.Add(valueParameter);
			method.Signature!.GenericParameterCount = 2;

			return method;
		}

		private static MethodDefinition MakeDictionaryMethod(string uniqueName, IMethodDescriptor keyReadMethod, TypeSignature keySignature, IMethodDescriptor valueReadMethod, TypeSignature valueSignature, bool align)
		{
			GenericInstanceTypeSignature genericDictionaryType = assetDictionaryReference.MakeGenericInstanceType(keySignature, valueSignature);

			IMethodDefOrRef getPairMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				genericDictionaryType,
				assetDictionaryDefinition.Methods.Single(m => m.Name == nameof(AssetDictionary<int, int>.GetPair)));

			MethodDefinition addMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.Add) && m.Parameters.Count == 2);
			IMethodDefOrRef addMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictionaryType, addMethodDefinition);
			MethodDefinition clearMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.Clear));
			IMethodDefOrRef clearMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericDictionaryType, clearMethodDefinition);

			MethodDefinition method = NewMethod(uniqueName, genericDictionaryType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable keyLocal = processor.AddLocalVariable(keySignature);
			CilLocalVariable valueLocal = processor.AddLocalVariable(valueSignature);

			CilInstructionLabel loopConditionStartList = new();

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, clearMethodReference);

			//Read count
			processor.Add(CilOpCodes.Ldarg_1);//reader
			processor.AddCall(readInt32Method);
			processor.Add(CilOpCodes.Stloc, countLocal);

			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartList);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetList = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read key
			if (keySignature.IsArrayOrPrimitive())
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(keyReadMethod);
				processor.Add(CilOpCodes.Stloc, keyLocal);
			}
			else if (keySignature is GenericParameterSignature)
			{
				IMethodDefOrRef createInstanceMethod = SharedState.Instance.Importer.ImportMethod(typeof(Activator), m => m.Name == nameof(Activator.CreateInstance) && m.Parameters.Count == 0 && m.GenericParameters.Count == 1);
				processor.Add(CilOpCodes.Call, createInstanceMethod.MakeGenericInstanceMethod(keySignature));
				processor.Add(CilOpCodes.Stloc, keyLocal);
				processor.Add(CilOpCodes.Ldloc, keyLocal);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(keyReadMethod);
			}
			else
			{
				processor.Add(CilOpCodes.Newobj, keySignature.GetDefaultConstructor());
				processor.Add(CilOpCodes.Stloc, keyLocal);
				processor.Add(CilOpCodes.Ldloc, keyLocal);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(keyReadMethod);
			}

			//Read value
			if (valueSignature.IsArrayOrPrimitive())
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(valueReadMethod);
				processor.Add(CilOpCodes.Stloc, valueLocal);
			}
			else if (valueSignature is GenericParameterSignature)
			{
				IMethodDefOrRef createInstanceMethod = SharedState.Instance.Importer.ImportMethod(typeof(Activator), m => m.Name == nameof(Activator.CreateInstance) && m.Parameters.Count == 0 && m.GenericParameters.Count == 1);
				processor.Add(CilOpCodes.Call, createInstanceMethod.MakeGenericInstanceMethod(valueSignature));
				processor.Add(CilOpCodes.Stloc, valueLocal);
				processor.Add(CilOpCodes.Ldloc, valueLocal);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(valueReadMethod);
			}
			else
			{
				processor.Add(CilOpCodes.Newobj, valueSignature.GetDefaultConstructor());
				processor.Add(CilOpCodes.Stloc, valueLocal);
				processor.Add(CilOpCodes.Ldloc, valueLocal);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(valueReadMethod);
			}

			//Add to dictionary
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, keyLocal);
			processor.Add(CilOpCodes.Ldloc, valueLocal);
			processor.AddCall(addMethodReference);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartList.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetList); //Jump back up if less than

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod);
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static IMethodDescriptor MakePairMethod(string uniqueName, UniversalNode keyTypeNode, TypeSignature keySignature, UniversalNode valueTypeNode, TypeSignature valueSignature, UnityVersion version, bool align)
		{
			IMethodDescriptor keyReadMethod = GetOrMakeMethod(keyTypeNode, keySignature, version);
			IMethodDescriptor valueReadMethod = GetOrMakeMethod(valueTypeNode, valueSignature, version);

			GenericInstanceTypeSignature genericPairType = keyValuePairReference.MakeGenericInstanceType(keySignature, valueSignature);

			MethodDefinition method = NewMethod(uniqueName, genericPairType);
			CilInstructionCollection processor = method.GetProcessor();

			if (keySignature.IsArrayOrPrimitive())
			{
				IMethodDefOrRef setKeyMethod = MethodUtils.MakeMethodOnGenericType(
					SharedState.Instance.Importer,
					genericPairType,
					keyValuePairDefinition.Methods.Single(m => m.Name == $"set_{nameof(AssetPair<int, int>.Key)}"));

				processor.Add(CilOpCodes.Ldarg_0);//pair
				processor.Add(CilOpCodes.Ldarg_1);//reader
				processor.AddCall(keyReadMethod);
				processor.AddCall(setKeyMethod);
			}
			else
			{
				IMethodDefOrRef getKeyMethod = MethodUtils.MakeMethodOnGenericType(
					SharedState.Instance.Importer,
					genericPairType,
					keyValuePairDefinition.Methods.Single(m => m.Name == $"get_{nameof(AssetPair<int, int>.Key)}"));

				processor.Add(CilOpCodes.Ldarg_0);//pair
				processor.AddCall(getKeyMethod);
				processor.Add(CilOpCodes.Ldarg_1);//reader
				processor.AddCall(keyReadMethod);
			}

			if (valueSignature.IsArrayOrPrimitive())
			{
				IMethodDefOrRef setValueMethod = MethodUtils.MakeMethodOnGenericType(
					SharedState.Instance.Importer,
					genericPairType,
					keyValuePairDefinition.Methods.Single(m => m.Name == $"set_{nameof(AssetPair<int, int>.Value)}"));

				processor.Add(CilOpCodes.Ldarg_0);//pair
				processor.Add(CilOpCodes.Ldarg_1);//reader
				processor.AddCall(valueReadMethod);
				processor.AddCall(setValueMethod);
			}
			else
			{
				IMethodDefOrRef getValueMethod = MethodUtils.MakeMethodOnGenericType(
					SharedState.Instance.Importer,
					genericPairType,
					keyValuePairDefinition.Methods.Single(m => m.Name == $"get_{nameof(AssetPair<int, int>.Value)}"));

				processor.Add(CilOpCodes.Ldarg_0);//pair
				processor.AddCall(getValueMethod);
				processor.Add(CilOpCodes.Ldarg_1);//reader
				processor.AddCall(valueReadMethod);
			}

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);//reader
				processor.AddCall(alignStreamMethod);
			}
			processor.Add(CilOpCodes.Ret);
			return method;
		}

		private static IMethodDescriptor MakeTypelessDataMethod(string uniqueName, bool align)
		{
			CorLibTypeSignature elementType = SharedState.Instance.Importer.UInt8;
			SzArrayTypeSignature arrayType = elementType.MakeSzArrayType();
			MethodDefinition method = NewMethod(uniqueName, arrayType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable arrayLocal = processor.AddLocalVariable(arrayType);

			//Read count
			processor.Add(CilOpCodes.Ldarg_0);//reader
			processor.AddCall(readInt32Method);
			processor.Add(CilOpCodes.Stloc, countLocal);

			IMethodDefOrRef readBytesMethod = SharedState.Instance.Importer.ImportMethod(typeof(EndianSpanReader),
				m => m.Name == nameof(EndianSpanReader.ReadBytesExact) && m.Signature!.ReturnType is SzArrayTypeSignature);

			processor.Add(CilOpCodes.Ldarg_0);//reader
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.AddCall(readBytesMethod);
			processor.Add(CilOpCodes.Stloc, arrayLocal);

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.AddCall(alignStreamMethod);
			}
			processor.Add(CilOpCodes.Ldloc, arrayLocal);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static IMethodDescriptor MakeListMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version, bool align)
		{
			if (elementType.IsTypeDefinition())
			{
				return align
					? readAssetListAlignDefinition.MakeGenericInstanceMethod(elementType)
					: readAssetListDefinition.MakeGenericInstanceMethod(elementType);
			}
			else
			{
				IMethodDescriptor elementReadMethod = GetOrMakeMethod(elementTypeNode, elementType, version);
				return MakeListMethod(uniqueName, elementType, elementReadMethod, align);
			}
		}

		private static MethodDefinition MakeGenericListMethod(bool align)
		{
			string uniqueName = align ? "ArrayAlign_Asset" : "Array_Asset";
			GenericParameterSignature elementType = new GenericParameterSignature(SharedState.Instance.Module, GenericParameterType.Method, 0);
			IMethodDefOrRef readMethod = SharedState.Instance.Importer.ImportMethod<UnityAssetBase>(m => m.Name == ReadMethod && m.Parameters[0].ParameterType is ByReferenceTypeSignature);
			MethodDefinition method = MakeListMethod(uniqueName, elementType, readMethod, align);

			GenericParameter genericParameter = new GenericParameter("T", GenericParameterAttributes.DefaultConstructorConstraint);
			genericParameter.Constraints.Add(new GenericParameterConstraint(SharedState.Instance.Importer.ImportType<UnityAssetBase>()));
			method.GenericParameters.Add(genericParameter);
			method.Signature!.GenericParameterCount = 1;

			return method;
		}

		private static MethodDefinition MakeListMethod(string uniqueName, TypeSignature elementType, IMethodDescriptor elementReadMethod, bool align)
		{
			GenericInstanceTypeSignature genericListType = assetListReference.MakeGenericInstanceType(elementType);

			MethodDefinition addMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == nameof(AssetList<int>.Add));
			IMethodDefOrRef addMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericListType, addMethodDefinition);
			MethodDefinition clearMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(AssetList<>), m => m.Name == nameof(AssetList<int>.Clear));
			IMethodDefOrRef clearMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, genericListType, clearMethodDefinition);

			MethodDefinition method = NewMethod(uniqueName, genericListType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);

			CilInstructionLabel loopConditionStartList = new();

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, clearMethodReference);

			//Read count
			processor.Add(CilOpCodes.Ldarg_1);//reader
			processor.AddCall(readInt32Method);
			processor.Add(CilOpCodes.Stloc, countLocal);

			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartList);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetList = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read and add to list
			processor.Add(CilOpCodes.Ldarg_0);
			if (elementType.IsArrayOrPrimitive())
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(elementReadMethod);
			}
			else if (elementType is GenericParameterSignature)
			{
				IMethodDefOrRef createInstanceMethod = SharedState.Instance.Importer.ImportMethod(typeof(Activator), m => m.Name == nameof(Activator.CreateInstance) && m.Parameters.Count == 0 && m.GenericParameters.Count == 1);
				processor.Add(CilOpCodes.Call, createInstanceMethod.MakeGenericInstanceMethod(elementType));
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(elementReadMethod);
			}
			else
			{
				processor.Add(CilOpCodes.Newobj, elementType.GetDefaultConstructor());
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(elementReadMethod);
			}
			processor.AddCall(addMethodReference);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartList.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetList); //Jump back up if less than

			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_1);
				processor.AddCall(alignStreamMethod);
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();

			return method;
		}

		private static IMethodDescriptor MakeArrayMethod(string uniqueName, UniversalNode elementTypeNode, TypeSignature elementType, UnityVersion version, bool align)
		{
			if (elementType is CorLibTypeSignature corLibTypeSignature && corLibTypeSignature.ElementType == ElementType.U1)
			{
				return MakeTypelessDataMethod(uniqueName, align);
			}

			IMethodDescriptor elementReadMethod = GetOrMakeMethod(elementTypeNode, elementType, version);

			SzArrayTypeSignature arrayType = elementType.MakeSzArrayType();
			GenericInstanceTypeSignature listType = SharedState.Instance.Importer.ImportType(typeof(List<>)).MakeGenericInstanceType(elementType);
			MethodDefinition method = NewMethod(uniqueName, arrayType);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			CilLocalVariable arrayLocal = processor.AddLocalVariable(arrayType);
			CilLocalVariable listLocal = processor.AddLocalVariable(listType);

			//Read count
			processor.Add(CilOpCodes.Ldarg_0);//reader
			processor.AddCall(readInt32Method);
			processor.Add(CilOpCodes.Stloc, countLocal);

			CilInstructionLabel readAsListInstruction = new();
			CilInstructionLabel loopConditionStartArray = new();
			CilInstructionLabel loopConditionStartList = new();
			CilInstructionLabel returnInstruction = new();

			//Check size of count
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.Add(CilOpCodes.Ldc_I4, MaxArraySize);
			processor.Add(CilOpCodes.Bgt, readAsListInstruction);

			//Read into array
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.Add(CilOpCodes.Newarr, elementType.ToTypeDefOrRef());
			processor.Add(CilOpCodes.Stloc, arrayLocal);

			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartArray);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetArray = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read and add to array
			processor.Add(CilOpCodes.Ldloc, arrayLocal);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(elementReadMethod);
			processor.AddStoreElement(elementType);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartArray.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetArray); //Jump back up if less than

			processor.Add(CilOpCodes.Br, returnInstruction);//Jump to return statement

			//Read into list (because we don't trust large counts)

			MethodDefinition listConstructorDefinition = SharedState.Instance.Importer.LookupMethod(typeof(List<>), m =>
			{
				return m.IsConstructor
					&& m.Parameters.Count == 1
					&& m.Parameters[0].ParameterType is CorLibTypeSignature corLibTypeSignature
					&& corLibTypeSignature.ElementType == ElementType.I4;
			});
			IMethodDefOrRef listConstructorReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, listType, listConstructorDefinition);
			MethodDefinition addMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(List<>), m => m.Name == nameof(List<int>.Add));
			IMethodDefOrRef addMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, listType, addMethodDefinition);
			MethodDefinition toArrayMethodDefinition = SharedState.Instance.Importer.LookupMethod(typeof(List<>), m => m.Name == nameof(List<int>.ToArray));
			IMethodDefOrRef toArrayMethodReference = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, listType, toArrayMethodDefinition);

			readAsListInstruction.Instruction = processor.Add(CilOpCodes.Ldc_I4, MaxArraySize);
			processor.Add(CilOpCodes.Newobj, listConstructorReference);
			processor.Add(CilOpCodes.Stloc, listLocal);

			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			processor.Add(CilOpCodes.Br, loopConditionStartList);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			ICilLabel jumpTargetList = processor.Add(CilOpCodes.Nop).CreateLabel(); //Create a dummy instruction to jump back to

			//Read byte and add to list
			processor.Add(CilOpCodes.Ldloc, listLocal);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddCall(elementReadMethod);
			processor.AddCall(addMethodReference);

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartList.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetList); //Jump back up if less than

			processor.Add(CilOpCodes.Ldloc, listLocal);
			processor.AddCall(toArrayMethodReference);
			processor.Add(CilOpCodes.Stloc, arrayLocal);

			returnInstruction.Instruction = processor.Add(CilOpCodes.Nop);
			if (align)
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.AddCall(alignStreamMethod);
			}
			processor.Add(CilOpCodes.Ldloc, arrayLocal);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static IMethodDescriptor MakePrimitiveMethod(string uniqueName, UniversalNode node, bool align)
		{
			IMethodDescriptor primitiveMethod = GetPrimitiveMethod(node);
			if (align)
			{
				MethodDefinition method = NewMethod(uniqueName, primitiveMethod.Signature!.ReturnType);
				CilInstructionCollection processor = method.GetProcessor();
				processor.Add(CilOpCodes.Ldarg_0);
				processor.AddCall(primitiveMethod);
				if (align)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.AddCall(alignStreamMethod);
				}
				processor.Add(CilOpCodes.Ret);
				return method;
			}
			else
			{
				return primitiveMethod;
			}
		}

		/// <summary>
		/// Array and primitive read methods have the Func&lt;AssetReader, T&gt; signature.<br/>
		/// Others have the Action&lt;T, AssetReader&gt; signature.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static bool IsArrayOrPrimitive(this TypeSignature type) => type is SzArrayTypeSignature or CorLibTypeSignature;

		private static bool IsTypeDefinition(this TypeSignature type)
		{
			return type is TypeDefOrRefSignature defOrRefSignature && defOrRefSignature.Type is TypeDefinition;
		}

		private static IMethodDescriptor GetPrimitiveMethod(UniversalNode node)
		{
			return node.NodeType switch
			{
				NodeType.Boolean => primitiveReadMethods[ElementType.Boolean],
				NodeType.Character => primitiveReadMethods[ElementType.Char],
				NodeType.Int8 => primitiveReadMethods[ElementType.I1],
				NodeType.UInt8 => primitiveReadMethods[ElementType.U1],
				NodeType.Int16 => primitiveReadMethods[ElementType.I2],
				NodeType.UInt16 => primitiveReadMethods[ElementType.U2],
				NodeType.Int32 => primitiveReadMethods[ElementType.I4],
				NodeType.UInt32 => primitiveReadMethods[ElementType.U4],
				NodeType.Int64 => primitiveReadMethods[ElementType.I8],
				NodeType.UInt64 => primitiveReadMethods[ElementType.U8],
				NodeType.Single => primitiveReadMethods[ElementType.R4],
				NodeType.Double => primitiveReadMethods[ElementType.R8],
				_ => throw new NotSupportedException(node.TypeName),
			};
		}

		private static MethodDefinition NewMethod(string uniqueName, TypeSignature parameter)
		{
			if (parameter.IsArrayOrPrimitive())
			{
				MethodSignature methodSignature = MethodSignature.CreateStatic(parameter);
				MethodDefinition method = new MethodDefinition($"{ReadMethod}_{uniqueName}", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, methodSignature);
				method.CilMethodBody = new CilMethodBody(method);
				method.AddParameter(endianSpanReaderReference, "reader");
				method.AddExtensionAttribute(SharedState.Instance.Importer);
				return method;
			}
			else
			{
				MethodSignature methodSignature = MethodSignature.CreateStatic(SharedState.Instance.Importer.Void);
				MethodDefinition method = new MethodDefinition($"{ReadMethod}_{uniqueName}", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, methodSignature);
				method.CilMethodBody = new CilMethodBody(method);
				method.AddParameter(parameter, "value");
				method.AddParameter(endianSpanReaderReference, "reader");
				method.AddExtensionAttribute(SharedState.Instance.Importer);
				return method;
			}
		}

		private static CilInstruction AddCall(this CilInstructionCollection processor, IMethodDescriptor method)
		{
			return method is MethodDefinition definition && definition.IsStatic
				? processor.Add(CilOpCodes.Call, method)
				: processor.Add(CilOpCodes.Callvirt, method);
		}

		private static IMethodDefOrRef GetDefaultConstructor(this TypeSignature type)
		{
			return type switch
			{
				TypeDefOrRefSignature typeDefOrRefSignature => typeDefOrRefSignature.Type is TypeDefinition typeDefinition
											? typeDefinition.GetDefaultConstructor()
											: throw new InvalidOperationException(),
				GenericInstanceTypeSignature genericInstanceTypeSignature => MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, genericInstanceTypeSignature, 0),
				_ => throw new NotSupportedException(),
			};
		}
	}
}
