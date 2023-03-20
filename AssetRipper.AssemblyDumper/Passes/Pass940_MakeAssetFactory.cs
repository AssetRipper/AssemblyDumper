using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using System.Linq;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass940_MakeAssetFactory
	{
#nullable disable
		private static TypeSignature iunityObjectBase;
		private static TypeSignature assetInfoType;
		private static TypeSignature assetCollectionType;
		private static TypeSignature unityVersionType;
		private static TypeDefinition abstractClassException;
		private static MethodDefinition abstractClassExceptionConstructor;
		private static IMethodDefOrRef unityVersionIsGreaterEqualMethod;
		private static IMethodDefOrRef assetInfoConstructor;
#nullable enable
		private static HashSet<int> importerClassIDs = new();

		public static void DoPass()
		{
			FindImporterGroups();

			iunityObjectBase = SharedState.Instance.Importer.ImportTypeSignature<IUnityObjectBase>();
			assetInfoType = SharedState.Instance.Importer.ImportTypeSignature<AssetInfo>();
			assetCollectionType = SharedState.Instance.Importer.ImportTypeSignature<AssetCollection>();
			unityVersionType = SharedState.Instance.Importer.ImportTypeSignature<UnityVersion>();

			abstractClassException = ExceptionCreator.CreateSimpleException(
				SharedState.Instance.Importer,
				SharedState.ExceptionsNamespace,
				"AbstractClassException",
				"Abstract class could not be created");
			abstractClassExceptionConstructor = abstractClassException.GetDefaultConstructor();

			unityVersionIsGreaterEqualMethod = SharedState.Instance.Importer.ImportMethod<UnityVersion>(m =>
				m.Name == nameof(UnityVersion.IsGreaterEqual) && m.Parameters.Count == 5);
			assetInfoConstructor = SharedState.Instance.Importer.ImportMethod<AssetInfo>(method =>
				method.Name == ".ctor"
				&& method.Parameters.Count == 3);

			TypeDefinition factoryDefinition = CreateFactoryDefinition();
			AddAllClassCreationMethods(out List<(int, MethodDefinition?, bool)> assetInfoMethods);
			factoryDefinition.AddAssetInfoCreationMethod(assetInfoMethods);
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			return StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "AssetFactory");
		}

		private static MethodDefinition AddAssetInfoCreationMethod(this TypeDefinition factoryDefinition, List<(int, MethodDefinition?, bool)> constructors)
		{
			MethodDefinition method = factoryDefinition.AddMethod("CreateAsset", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, iunityObjectBase);
			method.AddParameter(unityVersionType, "version");
			method.AddParameter(assetInfoType, "info");
			method.GetProcessor().EmitIdSwitchStatement(constructors);
			return method;
		}

		private static void EmitIdSwitchStatement(this CilInstructionCollection processor, List<(int, MethodDefinition?, bool)> constructors)
		{
			int count = constructors.Count;

			CilLocalVariable switchCondition = new CilLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Owner.LocalVariables.Add(switchCondition);
			processor.Add(CilOpCodes.Ldarga, processor.Owner.Owner.Parameters[1]);
			IMethodDefOrRef propertyRef = SharedState.Instance.Importer.ImportMethod<AssetInfo>(m => m.Name == $"get_{nameof(AssetInfo.ClassID)}");
			processor.Add(CilOpCodes.Call, propertyRef);
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
				(int, MethodDefinition?, bool) tuple = constructors[i];
				if (tuple.Item2 is null)
				{
					processor.AddThrowAbstractClassException();
				}
				else
				{
					if (tuple.Item3)//uses version
					{
						processor.Add(CilOpCodes.Ldarg_0);
					}

					processor.Add(CilOpCodes.Ldarg_1);

					processor.Add(CilOpCodes.Call, tuple.Item2);
					processor.Add(CilOpCodes.Ret);
				}
			}
			defaultNop.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldnull);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddAllClassCreationMethods(out List<(int, MethodDefinition?, bool)> assetInfoMethods)
		{
			assetInfoMethods = new();
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values.OrderBy(g => g.ID))
			{
				group.AddMethodsForGroup(
					out MethodDefinition? assetInfoMethod,
					out bool usesVersion);
				assetInfoMethods.Add((group.ID, assetInfoMethod, usesVersion));
			}
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				group.AddMethodsForGroup(out _, out _);
			}
		}

		private static void AddMethodsForGroup(
			this ClassGroupBase group,
			out MethodDefinition? assetInfoMethod,
			out bool usesVersion)
		{
			if (group.IsAbstractGroup())
			{
				usesVersion = false;
				assetInfoMethod = null;
			}
			else if (group.Instances.Count == 1)
			{
				usesVersion = false;
				TypeDefinition factoryClass = group.MakeFactoryClass();
				if (group.ID < 0)
				{
					assetInfoMethod = null;
				}
				else
				{
					assetInfoMethod = ImplementSingleCreationMethod(group, factoryClass);
					MaybeImplementImporterCreationMethod(group, assetInfoMethod, usesVersion, factoryClass);
				}
			}
			else
			{
				usesVersion = true;
				TypeDefinition factoryClass = group.MakeFactoryClass();
				if (group.ID < 0)
				{
					assetInfoMethod = null;
				}
				else
				{
					assetInfoMethod = ImplementNormalCreationMethod(group, factoryClass);
					MaybeImplementImporterCreationMethod(group, assetInfoMethod, usesVersion, factoryClass);
				}
			}

			static void MaybeImplementImporterCreationMethod(ClassGroupBase group, MethodDefinition assetInfoMethod, bool usesVersion, TypeDefinition factoryClass)
			{
				if (importerClassIDs.Contains(group.ID))
				{
					ImplementImporterCreationMethod(group, factoryClass, assetInfoMethod, usesVersion);
				}
			}
		}

		private static MethodDefinition ImplementSingleCreationMethod(ClassGroupBase group, TypeDefinition factoryClass)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			CilInstructionCollection processor = method.GetProcessor();
			method.AddParameter(assetInfoType, "info");
			processor.AddReturnNewConstructedObject(group.Instances[0].Type, false);
			return method;
		}

		private static MethodDefinition ImplementNormalCreationMethod(ClassGroupBase group, TypeDefinition factoryClass)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			method.AddParameter(unityVersionType, "version");
			CilInstructionCollection processor = method.GetProcessor();
			method.AddParameter(assetInfoType, "info");
			processor.FillNormalCreationMethod(group);
			return method;
		}

		private static MethodDefinition ImplementImporterCreationMethod(ClassGroupBase group, TypeDefinition factoryClass, MethodDefinition assetInfoMethod, bool hasVersion)
		{
			MethodDefinition method = factoryClass.AddMethod("CreateAsset",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				group.GetSingularTypeOrInterface().ToTypeSignature());
			CilInstructionCollection processor = method.GetProcessor();

			if (hasVersion)
			{
				method.AddParameter(unityVersionType, "version");
				method.AddParameter(assetCollectionType, "collection");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldarg_1);
			}
			else
			{
				method.AddParameter(assetCollectionType, "collection");
				processor.Add(CilOpCodes.Ldarg_0);
			}

			processor.Add(CilOpCodes.Ldc_I4_0);//PathID
			processor.Add(CilOpCodes.Conv_I8);
			processor.Add(CilOpCodes.Ldc_I4, group.ID);//ClassID
			processor.Add(CilOpCodes.Newobj, assetInfoConstructor);

			processor.Add(CilOpCodes.Call, assetInfoMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static bool IsAbstractGroup(this ClassGroupBase group)
		{
			return group.Types.All(t => t.IsAbstract);
		}

		private static void AddThrowExceptionOrReturnNewObject(this CilInstructionCollection processor, TypeDefinition objectType)
		{
			if (objectType.IsAbstract)
			{
				processor.AddThrowAbstractClassException();
			}
			else
			{
				processor.AddReturnNewConstructedObject(objectType, true);
			}
		}

		private static void AddThrowAbstractClassException(this CilInstructionCollection processor)
		{
			processor.Add(CilOpCodes.Newobj, abstractClassExceptionConstructor);
			processor.Add(CilOpCodes.Throw);
		}

		private static void AddReturnNewConstructedObject(this CilInstructionCollection processor, TypeDefinition objectType, bool hasVersion)
		{
			if (hasVersion)
			{
				processor.Add(CilOpCodes.Ldarg_1);//version is always the first argument
			}
			else
			{
				processor.Add(CilOpCodes.Ldarg_0);
			}
			processor.Add(CilOpCodes.Newobj, objectType.GetAssetInfoConstructor());
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddIsGreaterOrEqualToVersion(this CilInstructionCollection processor, UnityVersion versionToCompareWith)
		{
			Parameter versionParameter = processor.Owner.Owner.Parameters[0];
			processor.Add(CilOpCodes.Ldarga, versionParameter);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Major);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Minor);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Build);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Type);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.TypeNumber);
			processor.Add(CilOpCodes.Call, unityVersionIsGreaterEqualMethod);
		}

		private static void FillNormalCreationMethod(this CilInstructionCollection processor, ClassGroupBase group)
		{
			for (int i = group.Instances.Count - 1; i > 0; i--)
			{
				CilInstructionLabel label = new();
				UnityVersion startVersion = group.Instances[i].VersionRange.Start;
				processor.AddIsGreaterOrEqualToVersion(startVersion);
				processor.Add(CilOpCodes.Brfalse, label);
				processor.AddThrowExceptionOrReturnNewObject(group.Instances[i].Type);
				label.Instruction = processor.Add(CilOpCodes.Nop);
			}
			processor.AddThrowExceptionOrReturnNewObject(group.Instances[0].Type);
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

		private static void FindImporterGroups()
		{
			foreach ((int id, ClassGroup group) in SharedState.Instance.ClassGroups)
			{
				if (group.Instances.Any(i => i.InheritsFromAssetImporter()))
				{
					importerClassIDs.Add(id);
				}
			}
		}
	}
}
