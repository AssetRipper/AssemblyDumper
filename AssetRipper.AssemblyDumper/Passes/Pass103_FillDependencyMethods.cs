using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;
using AssetRipper.Assets.Export.Dependencies;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using AssetRipper.Primitives;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass103_FillDependencyMethods
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static GenericInstanceTypeSignature unityObjectBasePPtrRef;
		private static MethodSpecification emptyArrayMethod;
		private static GenericInstanceTypeSignature returnType;
		private static TypeSignature fieldNameRef;
		private static IMethodDefOrRef fieldNameConstructor;
		private static TypeDefinition helperType;
		private static MethodDefinition fromSingleMethod;
		private static MethodDefinition appendPPtrMethod;

		private static ITypeDefOrRef accessPairBase;
		private static IMethodDefOrRef accessPairBaseGetKey;
		private static IMethodDefOrRef accessPairBaseGetValue;

		private static ITypeDefOrRef accessListBase;
		private static IMethodDefOrRef accessListBaseGetCount;
		private static IMethodDefOrRef accessListBaseGetItem;

		private static ITypeDefOrRef accessDictionaryBase;
		private static IMethodDefOrRef accessDictionaryBaseGetCount;
		private static IMethodDefOrRef accessDictionaryBaseGetPair;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private readonly static HashSet<ClassGroupBase> processedGroups = new();
		private readonly static HashSet<TypeSignatureStruct> nonDependentTypes = new();
		private readonly static Dictionary<TypeSignatureStruct, MethodDefinition> methodDictionary = new();

		private static void InitializeStaticFields()
		{
			helperType = InjectHelper();

			ITypeDefOrRef commonPPtrTypeRef = SharedState.Instance.Importer.ImportType(typeof(PPtr<>));
			ITypeDefOrRef unityObjectBaseInterfaceRef = SharedState.Instance.Importer.ImportType<IUnityObjectBase>();
			unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef.ToTypeSignature());

			ITypeDefOrRef ienumerableRef = SharedState.Instance.Importer.ImportType(typeof(IEnumerable<>));
			ITypeDefOrRef valueTupleRef = SharedState.Instance.Importer.ImportType(typeof(ValueTuple<,>));
			fieldNameRef = SharedState.Instance.Importer.ImportType<FieldName>().ToTypeSignature();
			GenericInstanceTypeSignature tupleGenericInstance = valueTupleRef.MakeGenericInstanceType(fieldNameRef, unityObjectBasePPtrRef);
			returnType = ienumerableRef.MakeGenericInstanceType(tupleGenericInstance);

			fieldNameConstructor = ConstructorUtils.ImportConstructor<FieldName>(SharedState.Instance.Importer, 2);

			IMethodDefOrRef emptyArray = SharedState.Instance.Importer.ImportMethod(typeof(Enumerable), method => method.Name == nameof(Enumerable.Empty));
			emptyArrayMethod = MethodUtils.MakeGenericInstanceMethod(SharedState.Instance.Importer, emptyArray, tupleGenericInstance);

			accessPairBase = SharedState.Instance.Importer.ImportType(typeof(AccessPairBase<,>));
			accessPairBaseGetKey = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"get_{nameof(AccessPairBase<int, int>.Key)}");
			accessPairBaseGetValue = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"get_{nameof(AccessPairBase<int, int>.Value)}");

			accessListBase = SharedState.Instance.Importer.ImportType(typeof(AccessListBase<>));
			accessListBaseGetCount = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == $"get_{nameof(AccessListBase<int>.Count)}");
			accessListBaseGetItem = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == "get_Item");

			accessDictionaryBase = SharedState.Instance.Importer.ImportType(typeof(AccessDictionaryBase<,>));
			accessDictionaryBaseGetCount = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == $"get_{nameof(AccessDictionaryBase<int, int>.Count)}");
			accessDictionaryBaseGetPair = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == nameof(AccessDictionaryBase<int, int>.GetPair));
		}

		[MemberNotNull(nameof(fromSingleMethod))]
		[MemberNotNull(nameof(appendPPtrMethod))]
		private static TypeDefinition InjectHelper()
		{
			TypeDefinition clonedType = SharedState.Instance.InjectHelperType(typeof(FetchDependenciesHelper));
			fromSingleMethod = clonedType.GetMethodByName(nameof(FetchDependenciesHelper.FromSingle));
			appendPPtrMethod = clonedType.GetMethodByName(nameof(FetchDependenciesHelper.AppendPPtr));
			return clonedType;
		}

		public static void DoPass()
		{
			InitializeStaticFields();

			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				EnsureGroupProcessed(group);
			}
		}

		private static void EnsureGroupProcessed(ClassGroupBase group)
		{
			if (!processedGroups.Add(group))
			{
				return;
			}

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod(nameof(UnityAssetBase.FetchDependencies), Pass099_CreateEmptyMethods.OverrideMethodAttributes, returnType);
				method.AddParameter(fieldNameRef, "parent", out ParameterDefinition parameterDefinition);
				parameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
				CilInstructionCollection processor = method.GetProcessor();
				if (group.IsPPtr)
				{
					processor.Add(CilOpCodes.Ldstr, "PPtr");
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Newobj, fieldNameConstructor);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Call, fromSingleMethod);
				}
				else
				{
					processor.Add(CilOpCodes.Call, emptyArrayMethod);
					if (GetOrMakeMethod(instance.Type.ToTypeSignature(), out MethodDefinition? appendMethod))
					{
						//enumerable is already loaded
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Call, appendMethod);
					}
				}
				processor.Add(CilOpCodes.Ret);
			}
		}

		private static bool GetOrMakeMethod(TypeSignature typeSignature, [NotNullWhen(true)] out MethodDefinition? method)
		{
			if (nonDependentTypes.Contains(typeSignature))
			{
				method = null;
				return false;
			}
			else if (methodDictionary.TryGetValue(typeSignature, out method))
			{
				return true;
			}
			else if (typeSignature is CorLibTypeSignature or SzArrayTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
			{
				nonDependentTypes.Add(typeSignature);
				method = null;
				return false;
			}
			else if (typeSignature is TypeDefOrRefSignature typeDefOrRefSignature)
			{
				TypeDefinition type = (TypeDefinition)typeDefOrRefSignature.Type;

				if (type.Name!.ToString().StartsWith("PPtr_", StringComparison.Ordinal))
				{
					method = appendPPtrMethod;
					return true;
				}

				GeneratedClassInstance instance = SharedState.Instance.TypesToGroups[type].Instances.Single(i => i.Type == type);

				List<(string, FieldDefinition, MethodDefinition)> usableFields = new();

				foreach (ClassProperty classProperty in instance.Properties)
				{
					if (classProperty.BackingField is not null
						&& GetOrMakeMethod(classProperty.BackingField.Signature!.FieldType, out MethodDefinition? appendMethod))
					{
						string originalName = classProperty.OriginalFieldName ?? throw new NullReferenceException();
						usableFields.Add((originalName, classProperty.BackingField, appendMethod));
					}
				}

				if (usableFields.Count == 0)
				{
					nonDependentTypes.Add(typeSignature);
					method = null;
					return false;
				}

				method = helperType.AddMethod($"Append_{UniqueNameFactory.MakeUniqueName(typeDefOrRefSignature)}", StaticClassCreator.StaticMethodAttributes, returnType);
				method.AddParameter(returnType, "enumerable");
				method.AddParameter(fieldNameRef, "parent", out ParameterDefinition parameterDefinition);
				parameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
				method.AddParameter(typeDefOrRefSignature, "item");
				method.AddExtensionAttribute(SharedState.Instance.Importer);

				CilInstructionCollection processor = method.GetProcessor();
				processor.Add(CilOpCodes.Ldarg_0);

				foreach ((string originalName, FieldDefinition field, MethodDefinition appendMethod) in usableFields)
				{
					//enumerable is already loaded
					processor.Add(CilOpCodes.Ldstr, originalName);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Newobj, fieldNameConstructor);
					processor.Add(CilOpCodes.Ldarg_2);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Call, appendMethod);
				}

				processor.Add(CilOpCodes.Ret);

				methodDictionary.Add(typeDefOrRefSignature, method);
				return true;
			}

			GenericInstanceTypeSignature genericType = (GenericInstanceTypeSignature)typeSignature;
			MethodDefinition?[] typeArgumentMethods = new MethodDefinition?[genericType.TypeArguments.Count];
			bool anyTypeArgumentsHavePPtrs = false;
			for (int i = 0; i < typeArgumentMethods.Length; i++)
			{
				if (GetOrMakeMethod(genericType.TypeArguments[i], out MethodDefinition? typeArgumentMethod))
				{
					anyTypeArgumentsHavePPtrs = true;
					typeArgumentMethods[i] = typeArgumentMethod;
				}
			}

			if (!anyTypeArgumentsHavePPtrs)
			{
				nonDependentTypes.Add(genericType);
				method = null;
				return false;
			}
			else
			{
				method = helperType.AddMethod($"Append_{UniqueNameFactory.MakeUniqueName(genericType)}", StaticClassCreator.StaticMethodAttributes, returnType);
				method.AddParameter(returnType, "enumerable");
				method.AddParameter(fieldNameRef, "fieldName");
				method.AddParameter(genericType, "item");
				method.AddExtensionAttribute(SharedState.Instance.Importer);

				CilInstructionCollection processor = method.GetProcessor();

				switch (genericType.GenericType.Name?.ToString())
				{
					case $"{nameof(AssetPair<int, int>)}`2" or $"{nameof(AccessPairBase<int, int>)}`2":
						processor.FillProcessorForPair(genericType);
						break;
					case $"{nameof(AssetDictionary<int, int>)}`2":
						processor.FillProcessorForDictionary(genericType);
						break;
					case $"{nameof(AssetList<int>)}`1":
						processor.FillProcessorForList(genericType);
						break;
					default:
						throw new NotSupportedException();
				}

				methodDictionary.Add(genericType, method);
				return true;
			}
		}

		private static void FillProcessorForList(this CilInstructionCollection processor, GenericInstanceTypeSignature listType)
		{
			TypeSignature elementType = listType.TypeArguments[0];

			if (!GetOrMakeMethod(elementType, out MethodDefinition? appendMethod))
			{
				throw new InvalidOperationException();
			}

			CilLocalVariable enumerableLocal = processor.AddLocalVariable(returnType);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Stloc, enumerableLocal);

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Callvirt, MakeListGetCountMethod(elementType));
			processor.Add(CilOpCodes.Stloc, countLocal);

			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Add(CilOpCodes.Ldc_I4_0);
			processor.Add(CilOpCodes.Stloc, iLocal);

			CilInstructionLabel conditionLabel = new();
			processor.Add(CilOpCodes.Br, conditionLabel);

			CilInstructionLabel forStartLabel = new();
			forStartLabel.Instruction = processor.Add(CilOpCodes.Nop);

			processor.Add(CilOpCodes.Ldloc, enumerableLocal);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Callvirt, MakeListGetItemMethod(elementType));
			processor.Add(CilOpCodes.Call, appendMethod);
			processor.Add(CilOpCodes.Stloc, enumerableLocal);

			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Ldc_I4_1);
			processor.Add(CilOpCodes.Add);
			processor.Add(CilOpCodes.Stloc, iLocal);

			conditionLabel.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.Add(CilOpCodes.Clt);
			processor.Add(CilOpCodes.Brtrue, forStartLabel);

			processor.Add(CilOpCodes.Ldloc, enumerableLocal);

			processor.Add(CilOpCodes.Ret);
		}

		private static void FillProcessorForPair(this CilInstructionCollection processor, GenericInstanceTypeSignature pairType)
		{
			TypeSignature keyType = pairType.TypeArguments[0];
			TypeSignature valueType = pairType.TypeArguments[1];

			processor.Add(CilOpCodes.Ldarg_0);

			if (GetOrMakeMethod(keyType, out MethodDefinition? keyAppendMethod))
			{
				//enumerable is already loaded
				processor.Add(CilOpCodes.Ldstr, "Key");
				processor.Add(CilOpCodes.Ldarg_1);
				processor.Add(CilOpCodes.Newobj, fieldNameConstructor);
				processor.Add(CilOpCodes.Ldarg_2);
				processor.Add(CilOpCodes.Callvirt, MakePairGetKeyMethod(keyType, valueType));
				processor.Add(CilOpCodes.Call, keyAppendMethod);
			}

			if (GetOrMakeMethod(valueType, out MethodDefinition? valueAppendMethod))
			{
				//enumerable is already loaded
				processor.Add(CilOpCodes.Ldstr, "Value");
				processor.Add(CilOpCodes.Ldarg_1);
				processor.Add(CilOpCodes.Newobj, fieldNameConstructor);
				processor.Add(CilOpCodes.Ldarg_2);
				processor.Add(CilOpCodes.Callvirt, MakePairGetValueMethod(keyType, valueType));
				processor.Add(CilOpCodes.Call, valueAppendMethod);
			}

			processor.Add(CilOpCodes.Ret);
		}

		private static void FillProcessorForDictionary(this CilInstructionCollection processor, GenericInstanceTypeSignature dictionaryType)
		{
			TypeSignature keyType = dictionaryType.TypeArguments[0];
			TypeSignature valueType = dictionaryType.TypeArguments[1];
			TypeSignature pairType = accessPairBase.MakeGenericInstanceType(keyType, valueType);

			if (!GetOrMakeMethod(pairType, out MethodDefinition? appendMethod))
			{
				throw new InvalidOperationException();
			}

			CilLocalVariable enumerableLocal = processor.AddLocalVariable(returnType);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Stloc, enumerableLocal);

			CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Callvirt, MakeDictionaryGetCountMethod(keyType, valueType));
			processor.Add(CilOpCodes.Stloc, countLocal);

			CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Add(CilOpCodes.Ldc_I4_0);
			processor.Add(CilOpCodes.Stloc, iLocal);

			CilInstructionLabel conditionLabel = new();
			processor.Add(CilOpCodes.Br, conditionLabel);

			CilInstructionLabel forStartLabel = new();
			forStartLabel.Instruction = processor.Add(CilOpCodes.Nop);

			processor.Add(CilOpCodes.Ldloc, enumerableLocal);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Callvirt, MakeDictionaryGetPairMethod(keyType, valueType));
			processor.Add(CilOpCodes.Call, appendMethod);
			processor.Add(CilOpCodes.Stloc, enumerableLocal);

			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Ldc_I4_1);
			processor.Add(CilOpCodes.Add);
			processor.Add(CilOpCodes.Stloc, iLocal);

			conditionLabel.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldloc, iLocal);
			processor.Add(CilOpCodes.Ldloc, countLocal);
			processor.Add(CilOpCodes.Clt);
			processor.Add(CilOpCodes.Brtrue, forStartLabel);

			processor.Add(CilOpCodes.Ldloc, enumerableLocal);

			processor.Add(CilOpCodes.Ret);
		}

		private static IMethodDefOrRef MakeDictionaryGetCountMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetCount!);
		}

		private static IMethodDefOrRef MakeDictionaryGetPairMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetPair!);
		}

		private static IMethodDefOrRef MakeListGetCountMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetCount!);
		}

		private static IMethodDefOrRef MakeListGetItemMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetItem!);
		}

		private static IMethodDefOrRef MakePairGetKeyMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetKey!);
		}

		private static IMethodDefOrRef MakePairGetValueMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetValue!);
		}
	}
}
