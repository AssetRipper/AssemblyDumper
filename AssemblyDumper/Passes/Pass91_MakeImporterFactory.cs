using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass91_MakeImporterFactory
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes InterfaceOverrideAttributes = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual;

		public static TypeDefinition ImporterFactoryDefinition { get; private set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 91: Make Importer Factory");
			ImporterFactoryDefinition = CreateFactoryDefinition();
			ImporterFactoryDefinition.AddCreateDefaultImporter();
			ImporterFactoryDefinition.AddCreateNativeFormatImporter();
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			var result = new TypeDefinition(SharedState.RootNamespace, "AssetImporterFactory", SealedClassAttributes, SystemTypeGetter.Object);
			SharedState.Module.Types.Add(result);
			result.Interfaces.Add(new InterfaceImplementation(SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.IAssetImporterFactory>()));
			ConstructorUtils.AddDefaultConstructor(result);
			return result;
		}

		private static void AddCreateDefaultImporter(this TypeDefinition factoryDefinition)
		{
			TypeReference idefaultImporter = SharedState.Module.ImportCommonType<AssetRipper.Core.Classes.Meta.Importers.IDefaultImporter>();
			TypeReference layoutInfoType = SharedState.Module.ImportCommonType<AssetRipper.Core.Layout.LayoutInfo>();

			MethodDefinition constructor = SharedState.TypeDictionary["DefaultImporter"].Methods
				.Single(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "LayoutInfo");

			MethodDefinition method = new MethodDefinition("CreateDefaultImporter", InterfaceOverrideAttributes, idefaultImporter);
			var parameter = new ParameterDefinition("layout", ParameterAttributes.None, layoutInfoType);
			method.Parameters.Add(parameter);
			ILProcessor processor = method.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg, parameter);
			processor.Emit(OpCodes.Newobj, constructor);
			processor.Emit(OpCodes.Ret);
			factoryDefinition.Methods.Add(method);
		}

		private static void AddCreateNativeFormatImporter(this TypeDefinition factoryDefinition)
		{
			TypeReference inativeImporter = SharedState.Module.ImportCommonType<AssetRipper.Core.Classes.Meta.Importers.INativeFormatImporter>();
			TypeReference layoutInfoType = SharedState.Module.ImportCommonType<AssetRipper.Core.Layout.LayoutInfo>();

			MethodDefinition constructor = SharedState.TypeDictionary["NativeFormatImporter"].Methods
				.Single(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "LayoutInfo");

			MethodDefinition method = new MethodDefinition("CreateNativeFormatImporter", InterfaceOverrideAttributes, inativeImporter);
			var parameter = new ParameterDefinition("layout", ParameterAttributes.None, layoutInfoType);
			method.Parameters.Add(parameter);
			ILProcessor processor = method.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg, parameter);
			processor.Emit(OpCodes.Newobj, constructor);
			processor.Emit(OpCodes.Ret);
			factoryDefinition.Methods.Add(method);
		}
	}
}
