using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static partial class Pass105_CopyValuesMethods
	{
		private const string CopyValuesName = "CopyValues";
		private const string DeepCloneName = "DeepClone";
		private static readonly Dictionary<TypeSignatureStruct, (IMethodDescriptor, CopyMethodType)> singleTypeDictionary = new();
		private static readonly Dictionary<(TypeSignatureStruct, TypeSignatureStruct), (IMethodDescriptor, CopyMethodType)> doubleTypeDictionary = new();
		private static MethodDefinition? duplicateArrayMethod;
		private static MethodDefinition? duplicateArrayArrayMethod;
		private static ITypeDefOrRef? pptrConverterType;
		private static IMethodDefOrRef? pptrConvertMethod;
		private static IMethodDefOrRef? pptrCopyMethod;
		private static TypeDefinition? helperType;
		private static readonly HashSet<ClassGroupBase> processedGroups = new();

		private static ITypeDefOrRef? accessPairBase;
		private static IMethodDefOrRef? accessPairBaseGetKey;
		private static IMethodDefOrRef? accessPairBaseSetKey;
		private static IMethodDefOrRef? accessPairBaseGetValue;
		private static IMethodDefOrRef? accessPairBaseSetValue;

		private static ITypeDefOrRef? accessListBase;
		private static IMethodDefOrRef? accessListBaseGetCount;
		private static IMethodDefOrRef? accessListBaseSetCapacity;
		private static IMethodDefOrRef? accessListBaseGetItem;
		private static IMethodDefOrRef? accessListBaseAddNew;

		private static ITypeDefOrRef? accessDictionaryBase;
		private static IMethodDefOrRef? accessDictionaryBaseGetCount;
		private static IMethodDefOrRef? accessDictionaryBaseSetCapacity;
		private static IMethodDefOrRef? accessDictionaryBaseGetPair;
		private static IMethodDefOrRef? accessDictionaryBaseAddNew;

		public static void DoPass()
		{
			pptrConverterType = SharedState.Instance.Importer.ImportType<PPtrConverter>();
			pptrConvertMethod = SharedState.Instance.Importer.ImportMethod<PPtrConverter>(m => m.Name == nameof(PPtrConverter.Convert));
			helperType = InjectHelper();

			accessPairBase = SharedState.Instance.Importer.ImportType(typeof(AccessPairBase<,>));
			accessPairBaseGetKey = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"get_{nameof(AccessPairBase<int, int>.Key)}");
			accessPairBaseSetKey = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"set_{nameof(AccessPairBase<int, int>.Key)}");
			accessPairBaseGetValue = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"get_{nameof(AccessPairBase<int, int>.Value)}");
			accessPairBaseSetValue = SharedState.Instance.Importer.ImportMethod(typeof(AccessPairBase<,>), m => m.Name == $"set_{nameof(AccessPairBase<int, int>.Value)}");

			accessListBase = SharedState.Instance.Importer.ImportType(typeof(AccessListBase<>));
			accessListBaseGetCount = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == $"get_{nameof(AccessListBase<int>.Count)}");
			accessListBaseSetCapacity = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == $"set_{nameof(AccessListBase<int>.Capacity)}");
			accessListBaseGetItem = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == "get_Item");
			accessListBaseAddNew = SharedState.Instance.Importer.ImportMethod(typeof(AccessListBase<>), m => m.Name == nameof(AccessListBase<int>.AddNew));

			accessDictionaryBase = SharedState.Instance.Importer.ImportType(typeof(AccessDictionaryBase<,>));
			accessDictionaryBaseGetCount = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == $"get_{nameof(AccessDictionaryBase<int, int>.Count)}");
			accessDictionaryBaseSetCapacity = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == $"set_{nameof(AccessDictionaryBase<int, int>.Capacity)}");
			accessDictionaryBaseGetPair = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == nameof(AccessDictionaryBase<int, int>.GetPair));
			accessDictionaryBaseAddNew = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == nameof(AccessDictionaryBase<int, int>.AddNew));

			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				EnsureGroupProcessed(group);
			}


			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				bool needsConverter = group.Interface.GetMethodByName(CopyValuesName).Parameters.Count == 2;
				{
					MethodDefinition method = group.Interface.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodDeclaration, group.Interface.ToTypeSignature());
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
					}
				}
				foreach (TypeDefinition type in group.Types)
				{
					MethodDefinition copyValuesMethod = type.GetMethodByName(CopyValuesName);
					MethodDefinition method = type.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodImplementation, group.Interface.ToTypeSignature());
					CilInstructionCollection processor = method.GetProcessor();
					processor.Add(CilOpCodes.Newobj, type.GetDefaultConstructor());
					processor.Add(CilOpCodes.Dup);
					processor.Add(CilOpCodes.Ldarg_0);
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
						processor.Add(CilOpCodes.Ldarg_1);
					}
					processor.Add(CilOpCodes.Call, copyValuesMethod);
					processor.Add(CilOpCodes.Ret);
				}
			}


			IMethodDefOrRef objectGetCollection = SharedState.Instance.Importer.ImportMethod<UnityObjectBase>(m => m.Name == $"get_{nameof(UnityObjectBase.Collection)}");
			IMethodDefOrRef interfaceGetCollection = SharedState.Instance.Importer.ImportMethod<IUnityObjectBase>(m => m.Name == $"get_{nameof(IUnityObjectBase.Collection)}");
			IMethodDefOrRef pptrConverterConstructor = ConstructorUtils.ImportConstructor<PPtrConverter>(SharedState.Instance.Importer, 2);
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values)
			{
				if (group.Interface.GetMethodByName(CopyValuesName).Parameters.Count == 2)//Has converter
				{
					{
						MethodDefinition method = group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
						method.AddParameter(group.Interface.ToTypeSignature(), "source");
					}
					foreach (TypeDefinition type in group.Types)
					{
						MethodDefinition originalCopyValuesMethod = type.GetMethodByName(CopyValuesName);
						MethodDefinition method = type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
						method.AddParameter(group.Interface.ToTypeSignature(), "source");
						CilInstructionCollection processor = method.GetProcessor();

						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Callvirt, objectGetCollection);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Callvirt, interfaceGetCollection);
						processor.Add(CilOpCodes.Newobj, pptrConverterConstructor);
						processor.Add(CilOpCodes.Call, originalCopyValuesMethod);

						processor.Add(CilOpCodes.Ret);
					}
				}
			}

			singleTypeDictionary.Clear();
			doubleTypeDictionary.Clear();
			processedGroups.Clear();
		}

		[MemberNotNull(nameof(duplicateArrayMethod))]
		[MemberNotNull(nameof(duplicateArrayArrayMethod))]
		[MemberNotNull(nameof(pptrCopyMethod))]
		private static TypeDefinition InjectHelper()
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(CopyValuesHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			foreach (TypeDefinition type in result.ClonedTopLevelTypes)
			{
				type.Namespace = SharedState.HelpersNamespace;
				SharedState.Instance.Module.TopLevelTypes.Add(type);
			}
			TypeDefinition clonedType = result.ClonedTopLevelTypes.Single();
			duplicateArrayMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.DuplicateArray));
			duplicateArrayArrayMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.DuplicateArrayArray));
			pptrCopyMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.CopyPPtr));
			return clonedType;
		}

		private static void EnsureGroupProcessed(ClassGroupBase group)
		{
			if (!processedGroups.Add(group))
			{
				return;
			}

			if (group.Name.StartsWith("PPtr_", StringComparison.Ordinal))
			{
				{
					MethodDefinition method = group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
					method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					singleTypeDictionary.Add(group.Interface.ToTypeSignature(), (method, CopyMethodType.Callvirt | CopyMethodType.HasConverter));
				}
				foreach (TypeDefinition type in group.Types)
				{
					MethodDefinition method = type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
					method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					CilInstructionCollection processor = method.GetProcessor();
					CilInstructionLabel returnLabel = new();
					CilInstructionLabel isNullLabel = new();
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldnull);
					processor.Add(CilOpCodes.Cgt_Un);
					processor.Add(CilOpCodes.Brfalse, isNullLabel);

					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_2);
					TypeSignature pptrTypeArgument = GetPPtrTypeArgument(type, group.Interface);
					processor.Add(CilOpCodes.Call, pptrCopyMethod!.MakeGenericInstanceMethod(pptrTypeArgument));
					processor.Add(CilOpCodes.Br, returnLabel);

					isNullLabel.Instruction = processor.Add(CilOpCodes.Nop);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Callvirt, type.GetMethodByName(nameof(IUnityAssetBase.Reset)));

					returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					singleTypeDictionary.Add(type.ToTypeSignature(), (method, CopyMethodType.HasConverter));
				}
			}
			else
			{
				bool needsConverter = false;
				bool needsNullCheck = group is SubclassGroup;
				Dictionary<TypeDefinition, MethodDefinition> instanceMethods = new();
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					MethodDefinition method = instance.Type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					CilInstructionCollection processor = method.GetProcessor();
					CilInstructionLabel returnLabel = new();
					CilInstructionLabel isNullLabel = new();
					if (needsNullCheck)
					{
						method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldnull);
						processor.Add(CilOpCodes.Cgt_Un);
						processor.Add(CilOpCodes.Brfalse, isNullLabel);
					}

					foreach (ClassProperty classProperty in instance.Properties)
					{
						if (classProperty.BackingField is not null)
						{
							TypeSignature fieldTypeSignature = classProperty.BackingField.Signature!.FieldType;
							if (fieldTypeSignature is CorLibTypeSignature)
							{
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, classProperty.Base.Definition.GetMethod!);
								processor.Add(CilOpCodes.Stfld, classProperty.BackingField);
							}
							else if (fieldTypeSignature is SzArrayTypeSignature arrayTypeSignature)
							{
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, classProperty.Base.Definition.GetMethod!);
								processor.Add(CilOpCodes.Call, MakeDuplicateArrayMethod(arrayTypeSignature));
								processor.Add(CilOpCodes.Stfld, classProperty.BackingField);
							}
							else
							{
								(IMethodDescriptor fieldCopyMethod, CopyMethodType copyMethodType) = GetOrMakeMethod(
									fieldTypeSignature,
									classProperty.Base.Definition.Signature!.ReturnType);
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Ldfld, classProperty.BackingField);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, classProperty.Base.Definition.GetMethod!);
								if (HasConverter(copyMethodType))
								{
									processor.Add(CilOpCodes.Ldarg_2);
									needsConverter = true;
								}
								processor.Add(GetCallOpCode(copyMethodType), fieldCopyMethod);
							}
						}
					}

					if (needsNullCheck)
					{
						processor.Add(CilOpCodes.Br, returnLabel);

						isNullLabel.Instruction = processor.Add(CilOpCodes.Nop);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Callvirt, instance.Type.GetMethodByName(nameof(IUnityAssetBase.Reset)));
					}

					returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					instanceMethods.Add(instance.Type, method);
				}

				foreach ((TypeDefinition type, MethodDefinition method) in instanceMethods)
				{
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
						singleTypeDictionary.Add(type.ToTypeSignature(), (method, CopyMethodType.HasConverter));
					}
					else
					{
						singleTypeDictionary.Add(type.ToTypeSignature(), (method, CopyMethodType.None));
					}
				}

				{
					MethodDefinition method = group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					if (needsNullCheck)
					{
						method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					}
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
						singleTypeDictionary.Add(group.Interface.ToTypeSignature(), (method, CopyMethodType.Callvirt | CopyMethodType.HasConverter));
					}
					else
					{
						singleTypeDictionary.Add(group.Interface.ToTypeSignature(), (method, CopyMethodType.Callvirt));
					}
				}
			}
		}

		private static (IMethodDescriptor, CopyMethodType) GetOrMakeMethod(TypeSignature targetSignature, TypeSignature sourceSignature)
		{
			if (singleTypeDictionary.TryGetValue(targetSignature, out (IMethodDescriptor, CopyMethodType) pair))
			{
				return pair;
			}
			else if (doubleTypeDictionary.TryGetValue((targetSignature, sourceSignature), out pair))
			{
				return pair;
			}

			switch (targetSignature)
			{
				case TypeDefOrRefSignature typeDefOrRefSignature:
					TypeDefinition type = (TypeDefinition)typeDefOrRefSignature.Type;
					EnsureGroupProcessed(SharedState.Instance.TypesToGroups[type]);
					return singleTypeDictionary[type.ToTypeSignature()];
				case GenericInstanceTypeSignature targetGenericSignature:
					{
						bool needsConverter = false;
						GenericInstanceTypeSignature sourceGenericSignature = (GenericInstanceTypeSignature)sourceSignature;
						MethodDefinition method = helperType!.AddMethod(
							MakeUniqueCopyValuesName(targetSignature, sourceSignature),
							StaticClassCreator.StaticMethodAttributes,
							SharedState.Instance.Importer.Void);
						method.AddParameter(targetSignature, "target");
						method.AddParameter(sourceSignature, "source");
						CilInstructionCollection processor = method.GetProcessor();
						switch (targetGenericSignature.GenericType.Name?.ToString())
						{
							case $"{nameof(AssetDictionary<int, int>)}`2":
								{
									TypeSignature targetKeyTypeSignature = targetGenericSignature.TypeArguments[0];
									TypeSignature targetValueTypeSignature = targetGenericSignature.TypeArguments[1];
									TypeSignature targetPairTypeSignature = accessPairBase!.MakeGenericInstanceType(targetKeyTypeSignature, targetValueTypeSignature);
									TypeSignature sourceKeyTypeSignature = sourceGenericSignature.TypeArguments[0];
									TypeSignature sourceValueTypeSignature = sourceGenericSignature.TypeArguments[1];
									TypeSignature sourcePairTypeSignature = accessPairBase!.MakeGenericInstanceType(sourceKeyTypeSignature, sourceValueTypeSignature);

									CilInstructionLabel returnLabel = new();
									CilInstructionLabel isNullLabel = new();
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Ldnull);
									processor.Add(CilOpCodes.Cgt_Un);
									processor.Add(CilOpCodes.Brfalse, isNullLabel);

									CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Callvirt, MakeDictionaryGetCountMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
									processor.Add(CilOpCodes.Stloc, countLocal);

									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Ldloc, countLocal);
									processor.Add(CilOpCodes.Callvirt, MakeDictionarySetCapacityMethod(targetKeyTypeSignature, targetValueTypeSignature));

									CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
									processor.Add(CilOpCodes.Ldc_I4_0);
									processor.Add(CilOpCodes.Stloc, iLocal);

									CilInstructionLabel conditionLabel = new();
									processor.Add(CilOpCodes.Br, conditionLabel);

									CilInstructionLabel forStartLabel = new();
									forStartLabel.Instruction = processor.Add(CilOpCodes.Nop);

									(IMethodDescriptor copyMethod, CopyMethodType copyMethodType) = GetOrMakeMethod(targetPairTypeSignature, sourcePairTypeSignature);
									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Callvirt, MakeDictionaryAddNewMethod(targetKeyTypeSignature, targetValueTypeSignature));
									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Callvirt, MakeDictionaryGetPairMethod(targetKeyTypeSignature, targetValueTypeSignature));
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Callvirt, MakeDictionaryGetPairMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
									if (HasConverter(copyMethodType))
									{
										processor.Add(CilOpCodes.Ldarg_2);
										needsConverter = true;
									}
									processor.Add(GetCallOpCode(copyMethodType), copyMethod);

									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Ldc_I4_1);
									processor.Add(CilOpCodes.Add);
									processor.Add(CilOpCodes.Stloc, iLocal);

									conditionLabel.Instruction = processor.Add(CilOpCodes.Nop);
									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Ldloc, countLocal);
									processor.Add(CilOpCodes.Clt);
									processor.Add(CilOpCodes.Brtrue, forStartLabel);

									processor.Add(CilOpCodes.Br, returnLabel);

									isNullLabel.Instruction = processor.Add(CilOpCodes.Nop);
									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Ldc_I4_0);
									processor.Add(CilOpCodes.Callvirt, MakeDictionarySetCapacityMethod(targetKeyTypeSignature, targetValueTypeSignature));

									returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
								}
								break;
							case $"{nameof(AssetList<int>)}`1":
								{
									TypeSignature targetElementTypeSignature = targetGenericSignature.TypeArguments[0];
									TypeSignature sourceElementTypeSignature = sourceGenericSignature.TypeArguments[0];

									CilInstructionLabel returnLabel = new();
									CilInstructionLabel isNullLabel = new();
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Ldnull);
									processor.Add(CilOpCodes.Cgt_Un);
									processor.Add(CilOpCodes.Brfalse, isNullLabel);

									CilLocalVariable countLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Callvirt, MakeListGetCountMethod(sourceElementTypeSignature));
									processor.Add(CilOpCodes.Stloc, countLocal);

									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Ldloc, countLocal);
									processor.Add(CilOpCodes.Callvirt, MakeListSetCapacityMethod(targetElementTypeSignature));

									CilLocalVariable iLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
									processor.Add(CilOpCodes.Ldc_I4_0);
									processor.Add(CilOpCodes.Stloc, iLocal);

									CilInstructionLabel conditionLabel = new();
									processor.Add(CilOpCodes.Br, conditionLabel);

									CilInstructionLabel forStartLabel = new();
									forStartLabel.Instruction = processor.Add(CilOpCodes.Nop);

									//Lists can't contain arrays or primitives
									(IMethodDescriptor copyMethod, CopyMethodType copyMethodType) = GetOrMakeMethod(targetElementTypeSignature, sourceElementTypeSignature);

									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Callvirt, MakeListAddNewMethod(targetElementTypeSignature));
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Callvirt, MakeListGetItemMethod(sourceElementTypeSignature));
									if (HasConverter(copyMethodType))
									{
										processor.Add(CilOpCodes.Ldarg_2);
										needsConverter = true;
									}
									processor.Add(GetCallOpCode(copyMethodType), copyMethod);

									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Ldc_I4_1);
									processor.Add(CilOpCodes.Add);
									processor.Add(CilOpCodes.Stloc, iLocal);

									conditionLabel.Instruction = processor.Add(CilOpCodes.Nop);
									processor.Add(CilOpCodes.Ldloc, iLocal);
									processor.Add(CilOpCodes.Ldloc, countLocal);
									processor.Add(CilOpCodes.Clt);
									processor.Add(CilOpCodes.Brtrue, forStartLabel);

									processor.Add(CilOpCodes.Br, returnLabel);

									isNullLabel.Instruction = processor.Add(CilOpCodes.Nop);
									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Ldc_I4_0);
									processor.Add(CilOpCodes.Callvirt, MakeListSetCapacityMethod(targetElementTypeSignature));

									returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
								}
								break;
							case $"{nameof(AssetPair<int, int>)}`2" or $"{nameof(AccessPairBase<int, int>)}`2":
								{
									TypeSignature targetKeyTypeSignature = targetGenericSignature.TypeArguments[0];
									TypeSignature sourceKeyTypeSignature = sourceGenericSignature.TypeArguments[0];
									TypeSignature targetValueTypeSignature = targetGenericSignature.TypeArguments[1];
									TypeSignature sourceValueTypeSignature = sourceGenericSignature.TypeArguments[1];

									CilInstructionLabel returnLabel = new();
									processor.Add(CilOpCodes.Ldarg_1);
									processor.Add(CilOpCodes.Ldnull);
									processor.Add(CilOpCodes.Cgt_Un);
									processor.Add(CilOpCodes.Brfalse, returnLabel);

									if (targetKeyTypeSignature is CorLibTypeSignature)
									{
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetKeyMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										processor.Add(CilOpCodes.Callvirt, MakePairSetKeyMethod(targetKeyTypeSignature, targetValueTypeSignature));
									}
									else if (targetKeyTypeSignature is SzArrayTypeSignature keyArrayTypeSignature)
									{
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetKeyMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										processor.Add(CilOpCodes.Call, MakeDuplicateArrayMethod(keyArrayTypeSignature));
										processor.Add(CilOpCodes.Callvirt, MakePairSetKeyMethod(targetKeyTypeSignature, targetValueTypeSignature));
									}
									else
									{
										(IMethodDescriptor keyCopyMethod, CopyMethodType keyCopyMethodType) = GetOrMakeMethod(targetKeyTypeSignature, sourceKeyTypeSignature);
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Callvirt, MakePairGetKeyMethod(targetKeyTypeSignature, targetValueTypeSignature));
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetKeyMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										if (HasConverter(keyCopyMethodType))
										{
											processor.Add(CilOpCodes.Ldarg_2);
											needsConverter = true;
										}
										processor.Add(GetCallOpCode(keyCopyMethodType), keyCopyMethod);
									}

									if (targetValueTypeSignature is CorLibTypeSignature)
									{
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetValueMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										processor.Add(CilOpCodes.Callvirt, MakePairSetValueMethod(targetKeyTypeSignature, targetValueTypeSignature));
									}
									else if (targetValueTypeSignature is SzArrayTypeSignature valueArrayTypeSignature)
									{
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetValueMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										processor.Add(CilOpCodes.Call, MakeDuplicateArrayMethod(valueArrayTypeSignature));
										processor.Add(CilOpCodes.Callvirt, MakePairSetValueMethod(targetKeyTypeSignature, targetValueTypeSignature));
									}
									else
									{
										(IMethodDescriptor valueCopyMethod, CopyMethodType valueCopyMethodType) = GetOrMakeMethod(targetValueTypeSignature, sourceValueTypeSignature);
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Callvirt, MakePairGetValueMethod(targetKeyTypeSignature, targetValueTypeSignature));
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Callvirt, MakePairGetValueMethod(sourceKeyTypeSignature, sourceValueTypeSignature));
										if (HasConverter(valueCopyMethodType))
										{
											processor.Add(CilOpCodes.Ldarg_2);
											needsConverter = true;
										}
										processor.Add(GetCallOpCode(valueCopyMethodType), valueCopyMethod);
									}
									returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
								}
								break;
							default:
								throw new NotSupportedException();
						}
						(IMethodDescriptor, CopyMethodType) result;
						if (needsConverter)
						{
							method.AddParameter(pptrConverterType!.ToTypeSignature(), "converter");
							result = (method, CopyMethodType.HasConverter);
						}
						else
						{
							result = (method, CopyMethodType.None);
						}
						doubleTypeDictionary.Add((targetSignature, sourceSignature), result);
						return result;
					}
				default:
					throw new NotSupportedException();
			}
		}
		
		private static IMethodDefOrRef MakeDictionaryGetCountMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetCount!);
		}

		private static IMethodDefOrRef MakeDictionarySetCapacityMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseSetCapacity!);
		}

		private static IMethodDefOrRef MakeDictionaryGetPairMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetPair!);
		}

		private static IMethodDefOrRef MakeDictionaryAddNewMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseAddNew!);
		}

		private static IMethodDefOrRef MakeListGetCountMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetCount!);
		}

		private static IMethodDefOrRef MakeListSetCapacityMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseSetCapacity!);
		}

		private static IMethodDefOrRef MakeListGetItemMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetItem!);
		}

		private static IMethodDefOrRef MakeListAddNewMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase!.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseAddNew!);
		}

		private static IMethodDefOrRef MakePairGetKeyMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetKey!);
		}

		private static IMethodDefOrRef MakePairSetKeyMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseSetKey!);
		}

		private static IMethodDefOrRef MakePairGetValueMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetValue!);
		}

		private static IMethodDefOrRef MakePairSetValueMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase!.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseSetValue!);
		}

		private static string MakeUniqueName(TypeSignature type)
		{
			return type switch
			{
				CorLibTypeSignature or TypeDefOrRefSignature => type.Name,
				SzArrayTypeSignature arrayType => $"Array{MakeUniqueName(arrayType.BaseType)}",
				GenericInstanceTypeSignature genericType => $"{genericType.GenericType.Name?.ToString()[..^2]}_{string.Join('_', genericType.TypeArguments.Select(t => MakeUniqueName(t)))}",
				_ => throw new NotSupportedException(),
			}
			?? throw new NullReferenceException();
		}

		private static string MakeUniqueCopyValuesName(TypeSignature target, TypeSignature source)
		{
			return $"{CopyValuesName}__{MakeUniqueName(target)}__{MakeUniqueName(source)}";
		}

		private static IMethodDescriptor MakeDuplicateArrayMethod(SzArrayTypeSignature arrayTypeSignature)
		{
			TypeSignature elementType = arrayTypeSignature.BaseType;
			if (elementType is SzArrayTypeSignature nestedArray)
			{
				Debug.Assert(nestedArray.BaseType is CorLibTypeSignature);
				return duplicateArrayArrayMethod!.MakeGenericInstanceMethod(nestedArray.BaseType);
			}
			else
			{
				Debug.Assert(elementType is CorLibTypeSignature);
				return duplicateArrayMethod!.MakeGenericInstanceMethod(elementType);
			}
		}

		private static TypeSignature GetPPtrTypeArgument(TypeDefinition type, TypeDefinition groupInterface)
		{
			return TryGetPPtrTypeArgument(type)
				?? TryGetPPtrTypeArgument(groupInterface)
				?? throw new Exception("Could not get PPtr type argument.");
		}

		private static TypeSignature? TryGetPPtrTypeArgument(TypeDefinition type)
		{
			foreach (var implem in type.Interfaces)
			{
				if (implem.Interface is TypeSpecification specification
					&& specification.Signature is GenericInstanceTypeSignature genericInstanceTypeSignature
					&& genericInstanceTypeSignature.GenericType.Name == $"{nameof(IPPtr<IUnityObjectBase>)}`1")
				{
					return genericInstanceTypeSignature.TypeArguments[0];
				}
			}

			return null;
		}

		private static bool HasConverter(CopyMethodType copyMethodType)
		{
			return (copyMethodType & CopyMethodType.HasConverter) != 0;
		}

		private static CilOpCode GetCallOpCode(CopyMethodType copyMethodType)
		{
			return (copyMethodType & CopyMethodType.Callvirt) != 0 ? CilOpCodes.Callvirt : CilOpCodes.Call;
		}
	}
}
