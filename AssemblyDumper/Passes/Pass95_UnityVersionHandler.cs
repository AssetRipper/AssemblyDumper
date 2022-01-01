using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files;
using UnityHandlerBase = AssetRipper.Core.VersionHandling.UnityHandlerBase;

namespace AssemblyDumper.Passes
{
	public static class Pass95_UnityVersionHandler
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes ConstructorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName;
		const MethodAttributes PropertyOverrideAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;

		public static TypeDefinition HandlerDefinition { get; private set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 95: Unity Version Handler");
			ITypeDefOrRef baseHandler = SharedState.Importer.ImportCommonType<UnityHandlerBase>();
			HandlerDefinition = new TypeDefinition(SharedState.RootNamespace, "UnityVersionHandler", SealedClassAttributes, baseHandler);
			SharedState.Module.TopLevelTypes.Add(HandlerDefinition);
			HandlerDefinition.AddConstructor();
		}

		private static void AddConstructor(this TypeDefinition type)
		{
			var constructor = type.AddMethod(".ctor", ConstructorAttributes, SystemTypeGetter.Void);
			
			CilInstructionCollection processor = constructor.CilMethodBody.Instructions;
			processor.AddBaseConstructorCall();
			processor.AddAssetFactoryAssignment();
			processor.AddImporterFactoryAssignment();
			processor.AddSceneObjectFactoryAssignment();
			processor.AddUnityVersionAssignment();
			processor.AddCommonStringAssignment();
			processor.AddClassIDTypeEnumAssignment();
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddBaseConstructorCall(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseConstructor = SharedState.Importer.ImportCommonConstructor<UnityHandlerBase>();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, baseConstructor);
		}

		private static void AddAssetFactoryAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseAssetFactorySetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_AssetFactory");
			MethodDefinition assetFactoryConstructor = Pass90_MakeAssetFactory.FactoryDefinition.Methods.Single(c => c.IsConstructor && !c.IsStatic);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Newobj, assetFactoryConstructor);
			processor.Add(CilOpCodes.Call, baseAssetFactorySetter);
		}

		private static void AddSceneObjectFactoryAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef setter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_SceneObjectFactory");
			MethodDefinition constructor = Pass92_MakeSceneObjectFactory.FactoryDefinition.Methods.Single(c => c.IsConstructor && !c.IsStatic);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Call, setter);
		}

		private static void AddImporterFactoryAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseAssetImporterFactorySetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_ImporterFactory");
			MethodDefinition assetImporterFactoryConstructor = Pass91_MakeImporterFactory.ImporterFactoryDefinition.Methods.Single(c => c.IsConstructor && !c.IsStatic);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Newobj, assetImporterFactoryConstructor);
			processor.Add(CilOpCodes.Call, baseAssetImporterFactorySetter);
		}

		private static void AddCommonStringAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseCommonStringSetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_CommonStringDictionary");
			FieldDefinition field = Pass01_CreateBasicTypes.CommonStringTypeDefinition.GetFieldByName("dictionary");
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldsfld, field);
			processor.Add(CilOpCodes.Call, baseCommonStringSetter);
		}

		private static void AddClassIDTypeEnumAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseClassIdSetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_ClassIDTypeEnum");
			IMethodDefOrRef getTypeFromHandle = SharedState.Importer.ImportSystemMethod<Type>(m => m.Name == "GetTypeFromHandle");
			TypeDefinition type = Pass01_CreateBasicTypes.ClassIDTypeDefinition;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldtoken, type);
			processor.Add(CilOpCodes.Call, getTypeFromHandle);
			processor.Add(CilOpCodes.Call, baseClassIdSetter);
		}

		private static void AddUnityVersionAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseUnityVersionSetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_UnityVersion");
			ITypeDefOrRef unityVersionRef = SharedState.Importer.ImportCommonType<UnityVersion>();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.AddUnityVersion();
			processor.Add(CilOpCodes.Call, baseUnityVersionSetter);
		}

		private static void AddUnityVersion(this CilInstructionCollection processor)
		{
			UnityVersion version = GetUnityVersion();
			IMethodDefOrRef constructor = SharedState.Importer.ImportCommonConstructor<UnityVersion>(5);
			processor.Add(CilOpCodes.Ldc_I4, version.Major);
			processor.Add(CilOpCodes.Ldc_I4, version.Minor);
			processor.Add(CilOpCodes.Ldc_I4, version.Build);
			processor.Add(CilOpCodes.Ldc_I4, (int)version.Type);
			processor.Add(CilOpCodes.Ldc_I4, version.TypeNumber);
			processor.Add(CilOpCodes.Newobj, constructor);
		}

		private static UnityVersion GetUnityVersion() => UnityVersion.Parse(SharedState.Version);
	}
}
