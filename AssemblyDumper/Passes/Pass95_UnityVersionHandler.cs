using AssetRipper.Core.Parser.Files;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;
using UnityHandlerBase = AssetRipper.Core.VersionHandling.UnityHandlerBase;

namespace AssemblyDumper.Passes
{
	public static class Pass95_UnityVersionHandler
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes PropertyOverrideAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;

		public static TypeDefinition HandlerDefinition { get; private set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 95: Unity Version Handler");
			TypeReference baseHandler = SharedState.Module.ImportCommonType<AssetRipper.Core.VersionHandling.UnityHandlerBase>();
			HandlerDefinition = new TypeDefinition(SharedState.RootNamespace, "UnityVersionHandler", SealedClassAttributes, baseHandler);
			SharedState.Module.Types.Add(HandlerDefinition);
			HandlerDefinition.AddConstructor();
			HandlerDefinition.AddUnityVersionOverride();
		}

		private static void AddConstructor(this TypeDefinition type)
		{
			var constructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				SystemTypeGetter.Void
			);
			type.Methods.Add(constructor);
			ILProcessor processor = constructor.Body.GetILProcessor();
			processor.EmitBaseConstructorCall();
			processor.EmitAssetFactoryAssignment();
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
		}

		private static void EmitBaseConstructorCall(this ILProcessor processor)
		{
			MethodReference baseConstructor = SharedState.Module.ImportCommonConstructor<UnityHandlerBase>();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseConstructor);
		}

		private static void EmitAssetFactoryAssignment(this ILProcessor processor)
		{
			MethodReference baseAssetFactorySetter = SharedState.Module.ImportCommonMethod<UnityHandlerBase>(m => m.Name == "set_AssetFactory");
			MethodDefinition assetFactoryConstructor = Pass90_MakeAssetFactory.FactoryDefinition.Methods.Single(c => c.IsConstructor && !c.IsStatic);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Newobj, assetFactoryConstructor);
			processor.Emit(OpCodes.Call, baseAssetFactorySetter);
		}

		private static void AddUnityVersionOverride(this TypeDefinition type)
		{
			TypeReference unityVersionRef = SharedState.Module.ImportCommonType<UnityVersion>();
			MethodDefinition getter = new MethodDefinition("get_UnityVersion", PropertyOverrideAttributes, unityVersionRef);
			type.Methods.Add(getter);
			PropertyDefinition property = new PropertyDefinition("UnityVersion", PropertyAttributes.None, unityVersionRef);
			property.GetMethod = getter;
			type.Properties.Add(property);
			ILProcessor processor = getter.Body.GetILProcessor();
			processor.EmitUnityVersion();
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
		}

		private static void EmitUnityVersion(this ILProcessor processor)
		{
			UnityVersion version = GetUnityVersion();
			MethodReference constructor = SharedState.Module.ImportCommonConstructor<UnityVersion>(5);
			processor.Emit(OpCodes.Ldc_I4, version.Major);
			processor.Emit(OpCodes.Ldc_I4, version.Minor);
			processor.Emit(OpCodes.Ldc_I4, version.Build);
			processor.Emit(OpCodes.Ldc_I4, (int)version.Type);
			processor.Emit(OpCodes.Ldc_I4, version.TypeNumber);
			processor.Emit(OpCodes.Newobj, constructor);
		}

		private static UnityVersion GetUnityVersion() => UnityVersion.Parse(SharedState.Version);
	}
}
