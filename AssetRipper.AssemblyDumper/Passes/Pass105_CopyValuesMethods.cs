using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using AssetRipper.Primitives;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static partial class Pass105_CopyValuesMethods
	{
		private const string CopyValuesName = nameof(IUnityAssetBase.CopyValues);
		private const string DeepCloneName = "DeepClone";
		private static readonly Dictionary<TypeSignatureStruct, (IMethodDescriptor, CopyMethodType)> singleTypeDictionary = new();
		private static readonly Dictionary<(TypeSignatureStruct, TypeSignatureStruct), (IMethodDescriptor, CopyMethodType)> doubleTypeDictionary = new();
		private static readonly HashSet<ClassGroupBase> processedGroups = new();

#nullable disable
		private static TypeSignature pptrCommonType;
		private static IMethodDefOrRef pptrCommonGetFileIDMethod;
		private static IMethodDefOrRef pptrCommonGetPathIDMethod;

		private static ITypeDefOrRef pptrConverterType;
		private static IMethodDefOrRef pptrConverterConvertMethod;

		private static TypeDefinition helperType;
		private static MethodDefinition duplicateArrayMethod;
		private static MethodDefinition duplicateArrayArrayMethod;
		private static IMethodDefOrRef pptrConvertMethod;

		private static ITypeDefOrRef accessPairBase;
		private static IMethodDefOrRef accessPairBaseGetKey;
		private static IMethodDefOrRef accessPairBaseSetKey;
		private static IMethodDefOrRef accessPairBaseGetValue;
		private static IMethodDefOrRef accessPairBaseSetValue;
		
		private static ITypeDefOrRef accessListBase;
		private static IMethodDefOrRef accessListBaseGetCount;
		private static IMethodDefOrRef accessListBaseSetCapacity;
		private static IMethodDefOrRef accessListBaseGetItem;

		private static ITypeDefOrRef accessDictionaryBase;
		private static IMethodDefOrRef accessDictionaryBaseGetCount;
		private static IMethodDefOrRef accessDictionaryBaseSetCapacity;
		private static IMethodDefOrRef accessDictionaryBaseGetPair;

		private static ITypeDefOrRef assetList;
		private static IMethodDefOrRef assetListAdd;
		private static IMethodDefOrRef assetListAddNew;
		private static IMethodDefOrRef assetListClear;

		private static ITypeDefOrRef assetDictionary;
		private static IMethodDefOrRef assetDictionaryAddNew;
		private static IMethodDefOrRef assetDictionaryClear;

		private static ITypeDefOrRef assetPair;
#nullable enable

		public static void DoPass()
		{
			pptrCommonType = SharedState.Instance.Importer.ImportType<PPtr>().ToTypeSignature();
			pptrCommonGetFileIDMethod = SharedState.Instance.Importer.ImportMethod<PPtr>(m => m.Name == $"get_{nameof(PPtr.FileID)}");
			pptrCommonGetPathIDMethod = SharedState.Instance.Importer.ImportMethod<PPtr>(m => m.Name == $"get_{nameof(PPtr.PathID)}");

			pptrConverterType = SharedState.Instance.Importer.ImportType<PPtrConverter>();
			pptrConverterConvertMethod = SharedState.Instance.Importer.ImportMethod<PPtrConverter>(m => m.Name == nameof(PPtrConverter.Convert));
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

			accessDictionaryBase = SharedState.Instance.Importer.ImportType(typeof(AccessDictionaryBase<,>));
			accessDictionaryBaseGetCount = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == $"get_{nameof(AccessDictionaryBase<int, int>.Count)}");
			accessDictionaryBaseSetCapacity = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == $"set_{nameof(AccessDictionaryBase<int, int>.Capacity)}");
			accessDictionaryBaseGetPair = SharedState.Instance.Importer.ImportMethod(typeof(AccessDictionaryBase<,>), m => m.Name == nameof(AccessDictionaryBase<int, int>.GetPair));

			assetList = SharedState.Instance.Importer.ImportType(typeof(AssetList<>));
			assetListAdd = SharedState.Instance.Importer.ImportMethod(typeof(AssetList<>), m => m.Name == nameof(AssetList<int>.Add));
			assetListAddNew = SharedState.Instance.Importer.ImportMethod(typeof(AssetList<>), m => m.Name == nameof(AssetList<int>.AddNew));
			assetListClear = SharedState.Instance.Importer.ImportMethod(typeof(AssetList<>), m => m.Name == nameof(AssetList<int>.Clear));

			assetDictionary = SharedState.Instance.Importer.ImportType(typeof(AssetDictionary<,>));
			assetDictionaryAddNew = SharedState.Instance.Importer.ImportMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.AddNew));
			assetDictionaryClear = SharedState.Instance.Importer.ImportMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.Clear));

			assetPair = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>));

			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				EnsureGroupProcessed(group);
			}


			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				bool needsConverter = GetPrimaryCopyValuesMethod(group.Interface).Parameters.Count == 2;
				{
					MethodDefinition method = group.Interface.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodDeclaration, group.Interface.ToTypeSignature());
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
					}
				}
				foreach (TypeDefinition type in group.Types)
				{
					MethodDefinition copyValuesMethod = GetPrimaryCopyValuesMethod(type);
					MethodDefinition method = type.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodImplementation, group.Interface.ToTypeSignature());
					CilInstructionCollection processor = method.GetProcessor();
					processor.Add(CilOpCodes.Newobj, type.GetDefaultConstructor());
					processor.Add(CilOpCodes.Dup);
					processor.Add(CilOpCodes.Ldarg_0);
					if (needsConverter)
					{
						method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
						processor.Add(CilOpCodes.Ldarg_1);
					}
					processor.Add(CilOpCodes.Call, copyValuesMethod);
					processor.Add(CilOpCodes.Ret);
				}
			}


			IMethodDefOrRef pptrConverterConstructor = SharedState.Instance.Importer.ImportMethod<PPtrConverter>(m =>
			{
				return m.IsConstructor && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.Name == nameof(IUnityObjectBase);
			});
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values)
			{
				if (GetPrimaryCopyValuesMethod(group.Interface).Parameters.Count == 2)//Has converter
				{
					{
						MethodDefinition method = group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
						method.AddParameter(group.Interface.ToTypeSignature(), "source");
					}
					foreach (TypeDefinition type in group.Types)
					{
						MethodDefinition originalCopyValuesMethod = GetPrimaryCopyValuesMethod(type);
						MethodDefinition method = type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
						method.AddParameter(group.Interface.ToTypeSignature(), "source");
						CilInstructionCollection processor = method.GetProcessor();

						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Newobj, pptrConverterConstructor);
						processor.Add(CilOpCodes.Call, originalCopyValuesMethod);

						processor.Add(CilOpCodes.Ret);
					}
				}
			}

			TypeSignature unityAssetBaseInterfaceRef = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetBase>();
			Dictionary<TypeDefinition, MethodDefinition> overridenMethods = new();
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (TypeDefinition type in group.Types)
				{
					MethodDefinition copyValuesMethod = type.AddMethod(
						nameof(UnityAssetBase.CopyValues),
						Pass060_CreateEmptyMethods.OverrideMethodAttributes,
						SharedState.Instance.Importer.Void);
					copyValuesMethod.AddParameter(unityAssetBaseInterfaceRef, "source");
					copyValuesMethod.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
					copyValuesMethod.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					overridenMethods.Add(type, copyValuesMethod);
				}
			}
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					MethodDefinition primaryMethod = GetPrimaryCopyValuesMethod(instance.Type);
					MethodDefinition thisMethod = overridenMethods[instance.Type];
					MethodDefinition? baseMethod = instance.Base is null ? null : overridenMethods[instance.Base.Type];
					CilInstructionCollection processor = thisMethod.GetProcessor();

					if (group is SubclassGroup)//Optimization for subclasses since 2 null checks is unnecessary
					{
						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Isinst, group.Interface);
						if (primaryMethod.Parameters.Count == 2)
						{
							processor.Add(CilOpCodes.Ldarg_2);//Converter is needed
						}
						processor.Add(CilOpCodes.Callvirt, primaryMethod);
						processor.Add(CilOpCodes.Ret);
					}
					else
					{
						CilInstructionLabel returnLabel = new();
						CilInstructionLabel isNullLabel = new();
						CilLocalVariable castedArgumentLocal = processor.AddLocalVariable(group.Interface.ToTypeSignature());

						processor.Add(CilOpCodes.Ldarg_1);
						processor.Add(CilOpCodes.Isinst, group.Interface);
						processor.Add(CilOpCodes.Stloc, castedArgumentLocal);

						processor.Add(CilOpCodes.Ldloc, castedArgumentLocal);
						processor.Add(CilOpCodes.Ldnull);
						processor.Add(CilOpCodes.Cgt_Un);
						processor.Add(CilOpCodes.Brfalse, isNullLabel);

						processor.Add(CilOpCodes.Ldarg_0);
						processor.Add(CilOpCodes.Ldloc, castedArgumentLocal);
						if (primaryMethod.Parameters.Count == 2)
						{
							processor.Add(CilOpCodes.Ldarg_2);//Converter is needed
						}
						processor.Add(CilOpCodes.Callvirt, primaryMethod);
						processor.Add(CilOpCodes.Br, returnLabel);

						isNullLabel.Instruction = processor.Add(CilOpCodes.Nop);

						if (baseMethod is null)//Object
						{
							processor.Add(CilOpCodes.Ldarg_0);
							processor.Add(CilOpCodes.Callvirt, instance.Type.GetMethodByName(nameof(IUnityAssetBase.Reset)));
						}
						else
						{
							processor.Add(CilOpCodes.Ldarg_0);
							processor.Add(CilOpCodes.Ldarg_1);
							processor.Add(CilOpCodes.Ldarg_2);
							processor.Add(CilOpCodes.Call, baseMethod);
						}

						returnLabel.Instruction = processor.Add(CilOpCodes.Ret);
					}
				}
			}

			singleTypeDictionary.Clear();
			doubleTypeDictionary.Clear();
			processedGroups.Clear();
			overridenMethods.Clear();
		}

		[MemberNotNull(nameof(duplicateArrayMethod))]
		[MemberNotNull(nameof(duplicateArrayArrayMethod))]
		[MemberNotNull(nameof(pptrConvertMethod))]
		private static TypeDefinition InjectHelper()
		{
			TypeDefinition clonedType = SharedState.Instance.InjectHelperType(typeof(CopyValuesHelper));
			duplicateArrayMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.DuplicateArray));
			duplicateArrayArrayMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.DuplicateArrayArray));
			pptrConvertMethod = clonedType.GetMethodByName(nameof(CopyValuesHelper.ConvertPPtr));
			return clonedType;
		}

		private static void EnsureGroupProcessed(ClassGroupBase group)
		{
			if (!processedGroups.Add(group))
			{
				return;
			}

			if (group is SubclassGroup { IsPPtr: true })
			{
				{
					MethodDefinition method = group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
					method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					singleTypeDictionary.Add(group.Interface.ToTypeSignature(), (method, CopyMethodType.Callvirt | CopyMethodType.HasConverter));
				}
				foreach (TypeDefinition type in group.Types)
				{
					MethodDefinition method = type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
					method.AddParameter(group.Interface.ToTypeSignature(), "source");
					method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
					method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					CilInstructionCollection processor = method.GetProcessor();
					CilInstructionLabel returnLabel = new();
					CilInstructionLabel isNullLabel = new();
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldnull);
					processor.Add(CilOpCodes.Cgt_Un);
					processor.Add(CilOpCodes.Brfalse, isNullLabel);

					//Convert PPtr
					CilLocalVariable convertedPPtr = processor.AddLocalVariable(pptrCommonType);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldarg_2);
					processor.Add(CilOpCodes.Call, pptrConvertMethod.MakeGenericInstanceMethod(GetPPtrTypeArgument(type, group.Interface)));
					processor.Add(CilOpCodes.Stloc, convertedPPtr);

					//Store FileID
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldloca, convertedPPtr);
					processor.Add(CilOpCodes.Call, pptrCommonGetFileIDMethod);
					processor.Add(CilOpCodes.Stfld, type.GetFieldByName("m_FileID_"));

					//Store PathID
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldloca, convertedPPtr);
					processor.Add(CilOpCodes.Call, pptrCommonGetPathIDMethod);
					FieldDefinition pathIDField = type.GetFieldByName("m_PathID_");
					if (pathIDField.Signature!.FieldType is CorLibTypeSignature { ElementType: ElementType.I4 })
					{
						processor.Add(CilOpCodes.Conv_Ovf_I4);//Convert I8 to I4
					}
					processor.Add(CilOpCodes.Stfld, pathIDField);

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
							if (fieldTypeSignature is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
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
						method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
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
						method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
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
						MethodDefinition method = helperType.AddMethod(
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
									//Argument 0 (target) is AssetDictionary`2. Argument 1 (source) is AccessDictionaryBase`2.

									TypeSignature targetKeyTypeSignature = targetGenericSignature.TypeArguments[0];
									TypeSignature targetValueTypeSignature = targetGenericSignature.TypeArguments[1];
									TypeSignature targetPairTypeSignature = assetPair.MakeGenericInstanceType(targetKeyTypeSignature, targetValueTypeSignature);
									TypeSignature sourceKeyTypeSignature = sourceGenericSignature.TypeArguments[0];
									TypeSignature sourceValueTypeSignature = sourceGenericSignature.TypeArguments[1];
									TypeSignature sourcePairTypeSignature = accessPairBase.MakeGenericInstanceType(sourceKeyTypeSignature, sourceValueTypeSignature);

									CilInstructionLabel returnLabel = new();
									CilInstructionLabel isNullLabel = new();

									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Callvirt, MakeAssetDictionaryClearMethod(targetKeyTypeSignature, targetValueTypeSignature));

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
									processor.Add(CilOpCodes.Callvirt, MakeAssetDictionaryAddNewMethod(targetKeyTypeSignature, targetValueTypeSignature));
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
									//Argument 0 (target) is AssetList`1. Argument 1 (source) is AccessListBase`1.

									TypeSignature targetElementTypeSignature = targetGenericSignature.TypeArguments[0];
									TypeSignature sourceElementTypeSignature = sourceGenericSignature.TypeArguments[0];

									CilInstructionLabel returnLabel = new();
									CilInstructionLabel isNullLabel = new();

									processor.Add(CilOpCodes.Ldarg_0);
									processor.Add(CilOpCodes.Callvirt, MakeAssetListClearMethod(targetElementTypeSignature));

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

									if (targetElementTypeSignature is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
									{
										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Ldloc, iLocal);
										processor.Add(CilOpCodes.Callvirt, MakeListGetItemMethod(sourceElementTypeSignature));
										processor.Add(CilOpCodes.Callvirt, MakeAssetListAddMethod(targetElementTypeSignature));
									}
									else if (targetElementTypeSignature is SzArrayTypeSignature keyArrayTypeSignature)
									{
										throw new NotSupportedException();
									}
									else
									{
										(IMethodDescriptor copyMethod, CopyMethodType copyMethodType) = GetOrMakeMethod(targetElementTypeSignature, sourceElementTypeSignature);

										processor.Add(CilOpCodes.Ldarg_0);
										processor.Add(CilOpCodes.Callvirt, MakeAssetListAddNewMethod(targetElementTypeSignature));
										processor.Add(CilOpCodes.Ldarg_1);
										processor.Add(CilOpCodes.Ldloc, iLocal);
										processor.Add(CilOpCodes.Callvirt, MakeListGetItemMethod(sourceElementTypeSignature));
										if (HasConverter(copyMethodType))
										{
											processor.Add(CilOpCodes.Ldarg_2);
											needsConverter = true;
										}
										processor.Add(GetCallOpCode(copyMethodType), copyMethod);
									}

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

									if (targetKeyTypeSignature is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
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

									if (targetValueTypeSignature is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
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
							method.AddParameter(pptrConverterType.ToTypeSignature(), "converter");
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

		private static MethodDefinition GetPrimaryCopyValuesMethod(this TypeDefinition type)
		{
			return (MethodDefinition)singleTypeDictionary[type.ToTypeSignature()].Item1;
		}
		
		private static IMethodDefOrRef MakeDictionaryGetCountMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetCount);
		}

		private static IMethodDefOrRef MakeDictionarySetCapacityMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseSetCapacity);
		}

		private static IMethodDefOrRef MakeDictionaryGetPairMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessDictionaryBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessDictionaryBaseGetPair);
		}

		private static IMethodDefOrRef MakeAssetDictionaryAddNewMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				assetDictionary.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				assetDictionaryAddNew);
		}

		private static IMethodDefOrRef MakeAssetDictionaryClearMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				assetDictionary.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				assetDictionaryClear);
		}

		private static IMethodDefOrRef MakeListGetCountMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetCount);
		}

		private static IMethodDefOrRef MakeListSetCapacityMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseSetCapacity);
		}

		private static IMethodDefOrRef MakeListGetItemMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessListBase.MakeGenericInstanceType(elementTypeSignature),
				accessListBaseGetItem);
		}

		private static IMethodDefOrRef MakeAssetListAddNewMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				assetList.MakeGenericInstanceType(elementTypeSignature),
				assetListAddNew);
		}

		private static IMethodDefOrRef MakeAssetListAddMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				assetList.MakeGenericInstanceType(elementTypeSignature),
				assetListAdd);
		}

		private static IMethodDefOrRef MakeAssetListClearMethod(TypeSignature elementTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				assetList.MakeGenericInstanceType(elementTypeSignature),
				assetListClear);
		}

		private static IMethodDefOrRef MakePairGetKeyMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetKey);
		}

		private static IMethodDefOrRef MakePairSetKeyMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseSetKey);
		}

		private static IMethodDefOrRef MakePairGetValueMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseGetValue);
		}

		private static IMethodDefOrRef MakePairSetValueMethod(TypeSignature keyTypeSignature, TypeSignature valueTypeSignature)
		{
			return MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				accessPairBase.MakeGenericInstanceType(keyTypeSignature, valueTypeSignature),
				accessPairBaseSetValue);
		}

		private static string MakeUniqueCopyValuesName(TypeSignature target, TypeSignature source)
		{
			return $"{CopyValuesName}__{UniqueNameFactory.MakeUniqueName(target)}__{UniqueNameFactory.MakeUniqueName(source)}";
		}

		private static IMethodDescriptor MakeDuplicateArrayMethod(SzArrayTypeSignature arrayTypeSignature)
		{
			TypeSignature elementType = arrayTypeSignature.BaseType;
			if (elementType is SzArrayTypeSignature nestedArray)
			{
				Debug.Assert(nestedArray.BaseType is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) });
				return duplicateArrayArrayMethod.MakeGenericInstanceMethod(nestedArray.BaseType);
			}
			else
			{
				Debug.Assert(elementType is CorLibTypeSignature or TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) });
				return duplicateArrayMethod.MakeGenericInstanceMethod(elementType);
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
