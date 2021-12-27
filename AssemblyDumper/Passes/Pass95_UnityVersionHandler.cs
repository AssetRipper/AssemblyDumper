using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using AssetRipper.Core.Parser.Files;
using System;
using System.Linq;
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
			HandlerDefinition.AddUnityVersionOverride();
		}

		private static void AddConstructor(this TypeDefinition type)
		{
			var constructor = type.AddMethod(".ctor", ConstructorAttributes, SystemTypeGetter.Void);
			
			CilInstructionCollection processor = constructor.CilMethodBody.Instructions;
			processor.AddBaseConstructorCall();
			processor.AddAssetFactoryAssignment();
			processor.AddImporterFactoryAssignment();
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

		private static void AddImporterFactoryAssignment(this CilInstructionCollection processor)
		{
			IMethodDefOrRef baseAssetImporterFactorySetter = SharedState.Importer.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_ImporterFactory");
			MethodDefinition assetImporterFactoryConstructor = Pass91_MakeImporterFactory.ImporterFactoryDefinition.Methods.Single(c => c.IsConstructor && !c.IsStatic);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Newobj, assetImporterFactoryConstructor);
			processor.Add(CilOpCodes.Call, baseAssetImporterFactorySetter);
		}

		private static void AddUnityVersionOverride(this TypeDefinition type)
		{
			ITypeDefOrRef unityVersionRef = SharedState.Importer.ImportCommonType<UnityVersion>();
			PropertyDefinition property = type.AddGetterProperty("UnityVersion", PropertyOverrideAttributes, unityVersionRef.ToTypeSignature());
			MethodDefinition getter = property.GetMethod;
			CilInstructionCollection processor = getter.CilMethodBody.Instructions;
			processor.AddUnityVersion();
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
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
