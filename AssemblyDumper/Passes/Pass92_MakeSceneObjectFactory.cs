using AssemblyDumper.Utils;
using AssetRipper.Core.Classes.OcclusionCullingData;
using AssetRipper.Core.Parser.Asset;

namespace AssemblyDumper.Passes
{
	public static class Pass92_MakeSceneObjectFactory
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes MethodOverrideAttributes = MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual;
		public static TypeDefinition FactoryDefinition { get; private set; }
		public static void DoPass()
		{
			Console.WriteLine("Pass 90: Make Asset Factory");
			FactoryDefinition = CreateFactoryDefinition();
			FactoryDefinition.AddCreateSceneObjectIdentifier();
			FactoryDefinition.AddCreateOcclusionScene();
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			var result = new TypeDefinition(SharedState.RootNamespace, "SceneObjectFactory", SealedClassAttributes, SharedState.Importer.ImportCommonType<SceneObjectFactoryBase>());
			SharedState.Module.TopLevelTypes.Add(result);
			ConstructorUtils.AddDefaultConstructor(result);
			return result;
		}

		private static void AddCreateSceneObjectIdentifier(this TypeDefinition type)
		{
			ITypeDefOrRef returnType = SharedState.Importer.ImportCommonType<ISceneObjectIdentifier>();
			MethodDefinition method = type.AddMethod("CreateSceneObjectIdentifier", MethodOverrideAttributes, returnType);
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.AddNotSupportedException();
		}

		private static void AddCreateOcclusionScene(this TypeDefinition type)
		{
			ITypeDefOrRef returnType = SharedState.Importer.ImportCommonType<IOcclusionScene>();
			MethodDefinition method = type.AddMethod("CreateOcclusionScene", MethodOverrideAttributes, returnType);
			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.AddNotSupportedException();
		}
	}
}
