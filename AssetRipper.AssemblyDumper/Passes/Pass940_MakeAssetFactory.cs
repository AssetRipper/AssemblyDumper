﻿using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;
using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass940_MakeAssetFactory
	{
		private const MethodAttributes CreateAssetAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;
#nullable disable
		private static TypeSignature iunityObjectBase;
		private static TypeSignature assetInfoType;
		private static TypeSignature assetCollectionType;
		private static TypeSignature unityVersionType;
		private static TypeDefinition abstractClassException;
		private static MethodDefinition abstractClassExceptionConstructor;
		private static IMethodDefOrRef unityVersionIsGreaterEqualMethod;
		private static IMethodDefOrRef assetInfoConstructor;
		private static IMethodDefOrRef assetInfoCollectionGetMethod;
		private static IMethodDefOrRef assetCollectionVersionGetMethod;
#nullable enable
		private static readonly HashSet<int> importerClassIDs = new();

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
				m.Name == nameof(UnityVersion.GreaterThanOrEquals) && m.Parameters.Count == 5);
			assetInfoConstructor = SharedState.Instance.Importer.ImportMethod<AssetInfo>(method =>
				method.Name == ".ctor"
				&& method.Parameters.Count == 3);
			assetInfoCollectionGetMethod = SharedState.Instance.Importer.ImportMethod<AssetInfo>(method =>
				method.Name == $"get_{nameof(AssetInfo.Collection)}");
			assetCollectionVersionGetMethod = SharedState.Instance.Importer.ImportMethod<AssetCollection>(method =>
				method.Name == $"get_{nameof(AssetCollection.Version)}");

			TypeDefinition factoryDefinition = CreateFactoryDefinition();
			List<GeneratedMethodInformation> assetInfoMethods = AddAllClassCreationMethods();
			MethodDefinition creationMethod = factoryDefinition.AddAssetInfoCreationMethod("Create", assetInfoMethods);
			AddMethodWithoutVersionParameter(creationMethod);

			// CreateSerialized
			{
				// On Unity 5 and later, we want to return null for ClassID 194 (NavMesh).
				// It was made obsolete and contains no data, so it's safe to ignore.
				// This is done in CreateSerialized so that version changing can still use the Create method.
				// See pass 1 for more information.
				MethodDefinition method = factoryDefinition.AddMethod("CreateSerialized", CreateAssetAttributes, iunityObjectBase);
				Parameter assetInfoParameter = method.AddParameter(assetInfoType, "info");
				Parameter versionParameter = method.AddParameter(unityVersionType, "version");

				CilInstructionCollection processor = method.GetProcessor();

				CilLocalVariable switchCondition = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
				processor.Add(CilOpCodes.Ldarga, assetInfoParameter);
				IMethodDefOrRef propertyRef = SharedState.Instance.Importer.ImportMethod<AssetInfo>(m => m.Name == $"get_{nameof(AssetInfo.ClassID)}");
				processor.Add(CilOpCodes.Call, propertyRef);
				processor.Add(CilOpCodes.Stloc, switchCondition);

				CilInstructionLabel defaultLabel = new();
				CilInstructionLabel returnLabel = new();

				processor.Add(CilOpCodes.Ldloc, switchCondition);
				processor.Add(CilOpCodes.Ldc_I4, 194);
				processor.Add(CilOpCodes.Ceq);
				processor.Add(CilOpCodes.Brfalse, defaultLabel);

				processor.AddIsGreaterOrEqualToVersion(versionParameter, new UnityVersion(5));
				processor.Add(CilOpCodes.Brfalse, defaultLabel);

				processor.Add(CilOpCodes.Ldnull);
				processor.Add(CilOpCodes.Br, returnLabel);

				defaultLabel.Instruction = processor.Add(CilOpCodes.Nop);
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.Add(CilOpCodes.Call, creationMethod);

				returnLabel.Instruction = processor.Add(CilOpCodes.Ret);

				AddMethodWithoutVersionParameter(method);
			}
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			return StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "AssetFactory");
		}

		private static MethodDefinition AddAssetInfoCreationMethod(this TypeDefinition factoryDefinition, string methodName, List<GeneratedMethodInformation> constructors)
		{
			MethodDefinition method = factoryDefinition.AddMethod(methodName, CreateAssetAttributes, iunityObjectBase);
			Parameter assetInfoParameter = method.AddParameter(assetInfoType, "info");
			Parameter versionParameter = method.AddParameter(unityVersionType, "version");
			method.GetProcessor().EmitIdSwitchStatement(assetInfoParameter, versionParameter, constructors);
			return method;
		}

		private static void EmitIdSwitchStatement(this CilInstructionCollection processor, Parameter assetInfoParameter, Parameter versionParameter, List<GeneratedMethodInformation> constructors)
		{
			int count = constructors.Count;

			CilLocalVariable switchCondition = processor.AddLocalVariable(SharedState.Instance.Importer.Int32);
			processor.Add(CilOpCodes.Ldarga, assetInfoParameter);
			IMethodDefOrRef propertyRef = SharedState.Instance.Importer.ImportMethod<AssetInfo>(m => m.Name == $"get_{nameof(AssetInfo.ClassID)}");
			processor.Add(CilOpCodes.Call, propertyRef);
			processor.Add(CilOpCodes.Stloc, switchCondition);

			CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count).Select(i => new CilInstructionLabel()).ToArray();
			CilInstructionLabel defaultNop = new CilInstructionLabel();
			for (int i = 0; i < count; i++)
			{
				processor.Add(CilOpCodes.Ldloc, switchCondition);
				processor.Add(CilOpCodes.Ldc_I4, constructors[i].ClassID);
				processor.Add(CilOpCodes.Beq, nopInstructions[i]);
			}
			processor.Add(CilOpCodes.Br, defaultNop);
			for (int i = 0; i < count; i++)
			{
				nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);
				GeneratedMethodInformation tuple = constructors[i];
				if (tuple.AssetInfoMethod is null)
				{
					processor.AddThrowAbstractClassException();
				}
				else
				{
					processor.Add(CilOpCodes.Ldarg, assetInfoParameter);

					if (tuple.HasVersionParameter)
					{
						processor.Add(CilOpCodes.Ldarg, versionParameter);
					}

					processor.Add(CilOpCodes.Call, tuple.AssetInfoMethod);
					processor.Add(CilOpCodes.Ret);
				}
			}
			defaultNop.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldnull);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddMethodWithoutVersionParameter(MethodDefinition mainMethod)
		{
			//The main method has two parameters: UnityVersion and AssetInfo.
			//This generates a second method with one parameter: AssetInfo.
			//UnityVersion is pulled from AssetInfo.Collection.Version.
			MethodDefinition method = mainMethod.DeclaringType!.AddMethod(mainMethod.Name, mainMethod.Attributes, mainMethod.Signature!.ReturnType);
			Parameter parameter = method.AddParameter(assetInfoType, "info");
			CilInstructionCollection processor = method.GetProcessor();

			//Load Parameter 1
			processor.Add(CilOpCodes.Ldarg_0);

			//Load Parameter 2
			processor.Add(CilOpCodes.Ldarga, parameter);
			processor.Add(CilOpCodes.Call, assetInfoCollectionGetMethod);
			processor.Add(CilOpCodes.Call, assetCollectionVersionGetMethod);

			//Call main method and return
			processor.Add(CilOpCodes.Call, mainMethod);
			processor.Add(CilOpCodes.Ret);
		}

		private static List<GeneratedMethodInformation> AddAllClassCreationMethods()
		{
			List<GeneratedMethodInformation> assetInfoMethods = new();
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values.OrderBy(g => g.ID))
			{
				group.AddMethodsForGroup(
					out MethodDefinition? assetInfoMethod,
					out bool usesVersion);
				if (usesVersion && assetInfoMethod is not null)
				{
					AddMethodWithoutVersionParameter(assetInfoMethod);
				}
				assetInfoMethods.Add((group.ID, assetInfoMethod, usesVersion));
				if (Pass001_MergeMovedGroups.Changes.TryGetValue(group.ID, out IReadOnlyList<int>? aliasIDs))
				{
					foreach (int aliasID in aliasIDs)
					{
						assetInfoMethods.Add((aliasID, assetInfoMethod, usesVersion));
					}
				}
			}
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				group.AddMethodsForGroup(out _, out _);
			}
			assetInfoMethods.Sort((a, b) => a.ClassID.CompareTo(b.ClassID));
			return assetInfoMethods;
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
				TypeDefinition factoryClass = group.GetOrCreateMainClass();
				MethodDefinition generatedMethod = ImplementSingleCreationMethod(group, factoryClass);
				if (group is SubclassGroup)
				{
					assetInfoMethod = null;
				}
				else
				{
					assetInfoMethod = generatedMethod;
					MaybeImplementImporterCreationMethod(group, assetInfoMethod, usesVersion, factoryClass);
				}
			}
			else
			{
				usesVersion = true;
				TypeDefinition factoryClass = group.GetOrCreateMainClass();
				MethodDefinition generatedMethod = ImplementNormalCreationMethod(group, factoryClass);
				if (group is SubclassGroup)
				{
					assetInfoMethod = null;
				}
				else
				{
					assetInfoMethod = generatedMethod;
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
			MethodDefinition method = factoryClass.AddMethod("Create", CreateAssetAttributes, group.GetSingularTypeOrInterface().ToTypeSignature());
			CilInstructionCollection processor = method.GetProcessor();
			Parameter? assetInfoParameter = group is SubclassGroup ? null : method.AddParameter(assetInfoType, "info");
			processor.AddReturnNewConstructedObject(group.Instances[0].Type, assetInfoParameter);
			return method;
		}

		private static MethodDefinition ImplementNormalCreationMethod(ClassGroupBase group, TypeDefinition factoryClass)
		{
			MethodDefinition method = factoryClass.AddMethod("Create", CreateAssetAttributes, group.GetSingularTypeOrInterface().ToTypeSignature());
			Parameter? assetInfoParameter = group is SubclassGroup ? null : method.AddParameter(assetInfoType, "info");
			Parameter versionParameter = method.AddParameter(unityVersionType, "version");
			CilInstructionCollection processor = method.GetProcessor();
			processor.FillNormalCreationMethod(group, versionParameter, assetInfoParameter);
			return method;
		}

		private static MethodDefinition ImplementImporterCreationMethod(ClassGroupBase group, TypeDefinition factoryClass, MethodDefinition assetInfoMethod, bool hasVersion)
		{
			MethodDefinition method = factoryClass.AddMethod("Create",
				CreateAssetAttributes,
				group.GetSingularTypeOrInterface().ToTypeSignature());

			DocumentationHandler.AddMethodDefinitionLine(method, $"This is a special factory method for creating a temporary {SeeXmlTagGenerator.MakeCRef(group.Interface)} with no PathID.");

			CilInstructionCollection processor = method.GetProcessor();

			method.AddParameter(assetCollectionType, "collection");
			processor.Add(CilOpCodes.Ldarg_0);

			processor.Add(CilOpCodes.Ldc_I4_0);//PathID
			processor.Add(CilOpCodes.Conv_I8);
			processor.Add(CilOpCodes.Ldc_I4, group.ID);//ClassID
			processor.Add(CilOpCodes.Newobj, assetInfoConstructor);

			if (hasVersion)
			{
				method.AddParameter(unityVersionType, "version");
				processor.Add(CilOpCodes.Ldarg_1);
			}

			processor.Add(CilOpCodes.Call, assetInfoMethod);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static bool IsAbstractGroup(this ClassGroupBase group)
		{
			return group.Types.All(t => t.IsAbstract);
		}

		private static void AddThrowExceptionOrReturnNewObject(this CilInstructionCollection processor, TypeDefinition objectType, Parameter? assetInfoParameter)
		{
			if (objectType.IsAbstract)
			{
				processor.AddThrowAbstractClassException();
			}
			else
			{
				processor.AddReturnNewConstructedObject(objectType, assetInfoParameter);
			}
		}

		private static void AddThrowAbstractClassException(this CilInstructionCollection processor)
		{
			processor.Add(CilOpCodes.Newobj, abstractClassExceptionConstructor);
			processor.Add(CilOpCodes.Throw);
		}

		private static void AddReturnNewConstructedObject(this CilInstructionCollection processor, TypeDefinition objectType, Parameter? assetInfoParameter)
		{
			if (assetInfoParameter is not null)
			{
				processor.Add(CilOpCodes.Ldarg, assetInfoParameter);
				processor.Add(CilOpCodes.Newobj, objectType.GetAssetInfoConstructor());
			}
			else
			{
				processor.Add(CilOpCodes.Newobj, objectType.GetDefaultConstructor());
			}
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddIsGreaterOrEqualToVersion(this CilInstructionCollection processor, Parameter versionParameter, UnityVersion versionToCompareWith)
		{
			processor.Add(CilOpCodes.Ldarga, versionParameter);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Major);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Minor);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Build);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.Type);
			processor.Add(CilOpCodes.Ldc_I4, (int)versionToCompareWith.TypeNumber);
			processor.Add(CilOpCodes.Call, unityVersionIsGreaterEqualMethod);
		}

		private static void FillNormalCreationMethod(this CilInstructionCollection processor, ClassGroupBase group, Parameter versionParameter, Parameter? assetInfoParameter)
		{
			for (int i = group.Instances.Count - 1; i > 0; i--)
			{
				CilInstructionLabel label = new();
				UnityVersion startVersion = group.Instances[i].VersionRange.Start;
				processor.AddIsGreaterOrEqualToVersion(versionParameter, startVersion);
				processor.Add(CilOpCodes.Brfalse, label);
				processor.AddThrowExceptionOrReturnNewObject(group.Instances[i].Type, assetInfoParameter);
				label.Instruction = processor.Add(CilOpCodes.Nop);
			}
			processor.AddThrowExceptionOrReturnNewObject(group.Instances[0].Type, assetInfoParameter);
			processor.OptimizeMacros();
		}

		private static MethodDefinition GetAssetInfoConstructor(this TypeDefinition typeDefinition)
		{
			return typeDefinition.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == nameof(AssetInfo)).Single();
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

		private readonly record struct GeneratedMethodInformation(int ClassID, MethodDefinition? AssetInfoMethod, bool HasVersionParameter)
		{
			public static implicit operator GeneratedMethodInformation((int, MethodDefinition?, bool) tuple)
			{
				return new GeneratedMethodInformation(tuple.Item1, tuple.Item2, tuple.Item3);
			}
		}
	}
}
