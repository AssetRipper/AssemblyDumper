using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass940_MakeAssetFactory
	{
		private static TypeSignature? iunityObjectBase;
		private static TypeSignature? assetInfoType;
		private static TypeSignature? unityVersionType;
		private static TypeDefinition? abstractClassException;
		private static MethodDefinition? abstractClassExceptionConstructor;
		private static IMethodDefOrRef? unityVersionIsLessMethod;
		private static IMethodDefOrRef? makeDummyAssetInfo;

		public static void DoPass()
		{
			iunityObjectBase = SharedState.Instance.Importer.ImportTypeSignature<IUnityObjectBase>();
			assetInfoType = SharedState.Instance.Importer.ImportTypeSignature<AssetInfo>();
			unityVersionType = SharedState.Instance.Importer.ImportTypeSignature<UnityVersion>();

			abstractClassException = ExceptionCreator.CreateSimpleException(
				SharedState.Instance.Importer,
				SharedState.ExceptionsNamespace,
				"AbstractClassException",
				"Abstract class could not be created");
			abstractClassExceptionConstructor = abstractClassException.GetDefaultConstructor();

			unityVersionIsLessMethod = SharedState.Instance.Importer.ImportMethod<UnityVersion>(m =>
				m.Name == nameof(UnityVersion.IsLess) && m.Parameters.Count == 5);
			makeDummyAssetInfo = SharedState.Instance.Importer.ImportMethod<AssetInfo>(method =>
				method.Name == nameof(AssetInfo.MakeDummyAssetInfo)
				&& method.Parameters.Count == 1);

			TypeDefinition factoryDefinition = CreateFactoryDefinition();
			AddAllClassCreationMethods(
				out List<(int, MethodDefinition?, bool)> defaultMethods,
				out List<(int, MethodDefinition?, bool)> assetInfoMethods);
			factoryDefinition.AddDefaultCreationMethod(defaultMethods);
			factoryDefinition.AddAssetInfoCreationMethod(assetInfoMethods);
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			return StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "AssetFactory");
		}

		private static MethodDefinition AddAssetInfoCreationMethod(this TypeDefinition factoryDefinition, List<(int, MethodDefinition?, bool)> constructors)
		{
			MethodDefinition method = factoryDefinition.AddMethod("CreateAsset", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, iunityObjectBase!);
			method.AddParameter(unityVersionType!, "version");
			method.AddParameter(assetInfoType!, "info");
			method.GetProcessor().EmitIdSwitchStatement(constructors, true, true);
			return method;
		}

		private static MethodDefinition AddDefaultCreationMethod(this TypeDefinition factoryDefinition, List<(int, MethodDefinition?, bool)> constructors)
		{
			MethodDefinition method = factoryDefinition.AddMethod("CreateAsset", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, iunityObjectBase!);
			method.AddParameter(unityVersionType!, "version");
			method.AddParameter(SharedState.Instance.Importer.Int32, "id");
			method.GetProcessor().EmitIdSwitchStatement(constructors, false, false);
			return method;
		}

		private static void EmitIdSwitchStatement(this CilInstructionCollection processor, List<(int, MethodDefinition?, bool)> constructors, bool usesInfo, bool isAssetInfo)
		{
			int count = constructors.Count;

			CilLocalVariable? switchCondition = new CilLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Owner.LocalVariables.Add(switchCondition);
			{
				if (usesInfo)
				{
					if (isAssetInfo)
					{
						processor.Add(CilOpCodes.Ldarga, processor.Owner.Owner.Parameters[1]);
						IMethodDefOrRef propertyRef = SharedState.Instance.Importer.ImportMethod<AssetInfo>(m => m.Name == $"get_{nameof(AssetInfo.ClassID)}");
						processor.Add(CilOpCodes.Call, propertyRef);
					}
					else
					{
						processor.Add(CilOpCodes.Ldarg_2);
					}
				}
				else
				{
					processor.Add(CilOpCodes.Ldarg_1);
				}
			}
			processor.Add(CilOpCodes.Stloc, switchCondition);

			CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count).Select(i => new CilInstructionLabel()).ToArray();
			CilInstructionLabel defaultNop = new CilInstructionLabel();
			for (int i = 0; i < count; i++)
			{
				processor.Add(CilOpCodes.Ldloc, switchCondition);
				processor.Add(CilOpCodes.Ldc_I4, constructors[i].Item1);
				processor.Add(CilOpCodes.Beq, nopInstructions[i]);
			}
			processor.Add(CilOpCodes.Br, defaultNop);
			for (int i = 0; i < count; i++)
			{
				nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);
				if (constructors[i].Item2 is null)
				{
					processor.AddThrowAbstractClassException();
				}
				else
				{
					if (constructors[i].Item3)//uses version
					{
						processor.Add(CilOpCodes.Ldarg_0);
					}

					if (usesInfo)
					{
						processor.Add(CilOpCodes.Ldarg_1);
					}

					processor.Add(CilOpCodes.Call, constructors[i].Item2!);
					processor.Add(CilOpCodes.Ret);
				}
			}
			defaultNop.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldnull);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddAllClassCreationMethods(
			out List<(int, MethodDefinition?, bool)> defaultMethods,
			out List<(int, MethodDefinition?, bool)> assetInfoMethods)
		{
			defaultMethods = new();
			assetInfoMethods = new();
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values.OrderBy(g => g.ID))
			{
				group.AddMethodsForGroup(
					out MethodDefinition? defaultMethod,
					out MethodDefinition? assetInfoMethod,
					out bool usesVersion);
				defaultMethods.Add((group.ID, defaultMethod, usesVersion));
				assetInfoMethods.Add((group.ID, assetInfoMethod, usesVersion));
			}
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				group.AddMethodsForGroup(out _, out _, out _);
			}
		}

		private static void AddMethodsForGroup(
			this ClassGroupBase group,
			out MethodDefinition? defaultMethod,
			out MethodDefinition? assetInfoMethod,
			out bool usesVersion)
		{
			assetInfoMethod = null;

			if (group.IsAbstractGroup())
			{
				usesVersion = false;
				defaultMethod = null;
			}
			else if (group.Instances.Count == 1)
			{
				usesVersion = false;
				TypeDefinition factoryClass = group.MakeFactoryClass();
				defaultMethod = ImplementSingleCreationMethod(group, factoryClass, false);
				if (group.ID >= 0)
				{
					assetInfoMethod = ImplementSingleCreationMethod(group, factoryClass, true);
				}
			}
			else
			{
				usesVersion = true;
				TypeDefinition factoryClass = group.MakeFactoryClass();
				if (group.ID >= 0)
				{
					assetInfoMethod = ImplementNormalCreationMethod(group, factoryClass, true);
					defaultMethod = ImplementNormalCreationMethod(group, factoryClass, assetInfoMethod);
				}
				else
				{
					defaultMethod = ImplementNormalCreationMethod(group, factoryClass, false);
				}
			}
		}

		private static MethodDefinition ImplementSingleCreationMethod(ClassGroupBase group, TypeDefinition factoryClass, bool hasInfo)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			CilInstructionCollection processor = method.GetProcessor();
			if (hasInfo)
			{
				method.AddParameter(assetInfoType!, "info");
			}
			processor.AddReturnNewConstructedObject(group.Instances[0].Type, hasInfo, false);
			return method;
		}

		private static MethodDefinition ImplementNormalCreationMethod(ClassGroupBase group, TypeDefinition factoryClass, bool hasInfo)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			method.AddParameter(unityVersionType!, "version");
			CilInstructionCollection processor = method.GetProcessor();
			if (hasInfo)
			{
				method.AddParameter(assetInfoType!, "info");
			}
			processor.FillNormalCreationMethod(group, hasInfo);
			return method;
		}

		private static MethodDefinition ImplementNormalCreationMethod(ClassGroupBase group, TypeDefinition factoryClass, MethodDefinition assetInfoMethod)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			method.AddParameter(unityVersionType!, "version");
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldc_I4, group.ID);
			processor.Add(CilOpCodes.Call, makeDummyAssetInfo!);
			processor.Add(CilOpCodes.Call, assetInfoMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static bool IsAbstractGroup(this ClassGroupBase group)
		{
			return group.Types.All(t => t.IsAbstract);
		}

		private static void AddThrowExceptionOrReturnNewObject(this CilInstructionCollection processor, TypeDefinition objectType, bool hasInfo, bool hasVersion)
		{
			if (objectType.IsAbstract)
			{
				processor.AddThrowAbstractClassException();
			}
			else
			{
				processor.AddReturnNewConstructedObject(objectType, hasInfo, hasVersion);
			}
		}

		private static void AddThrowAbstractClassException(this CilInstructionCollection processor)
		{
			processor.Add(CilOpCodes.Newobj, abstractClassExceptionConstructor!);
			processor.Add(CilOpCodes.Throw);
		}

		private static void AddReturnNewConstructedObject(this CilInstructionCollection processor, TypeDefinition objectType, bool hasInfo, bool hasVersion)
		{
			MethodDefinition constructor;
			if (hasInfo)
			{
				constructor = objectType.GetAssetInfoConstructor();
				if (hasVersion)
				{
					processor.Add(CilOpCodes.Ldarg_1);//version is always the first argument
				}
				else
				{
					processor.Add(CilOpCodes.Ldarg_0);
				}
			}
			else
			{
				constructor = objectType.GetDefaultConstructor();
			}
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddIsLessThanVersion(this CilInstructionCollection processor, UnityVersion versionToCompareWith)
		{
			Parameter versionParameter = processor.Owner.Owner.Parameters[0];
			processor.Add(CilOpCodes.Ldarga, versionParameter);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Major);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Minor);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Build);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Type);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.TypeNumber);
			processor.Add(CilOpCodes.Call, unityVersionIsLessMethod!);
		}

		private static void FillNormalCreationMethod(this CilInstructionCollection processor, ClassGroupBase group, bool hasInfo)
		{
			int count = group.Instances.Count;

			CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count - 1).Select(i => new CilInstructionLabel()).ToArray();

			for (int i = 0; i < count - 1; i++)
			{
				UnityVersion endVersion = group.Instances[i + 1].VersionRange.Start;
				processor.AddIsLessThanVersion(endVersion);
				processor.Add(CilOpCodes.Brfalse, nopInstructions[i]);
				processor.AddThrowExceptionOrReturnNewObject(group.Instances[i].Type, hasInfo, true);
				nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);
			}
			processor.AddThrowExceptionOrReturnNewObject(group.Instances[count - 1].Type, hasInfo, true);
			processor.OptimizeMacros();
		}

		private static MethodDefinition GetAssetInfoConstructor(this TypeDefinition typeDefinition)
		{
			return typeDefinition.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == nameof(AssetInfo)).Single();
		}

		private static TypeDefinition MakeFactoryClass(this ClassGroupBase group)
		{
			return StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, group.Namespace, $"{group.Name}Factory");
		}
	}
}
