using AssemblyDumper.Utils;
using AssetRipper.Core.Classes.OcclusionCullingData;

namespace AssemblyDumper.Passes
{
	public static class Pass43_SceneInterfaces
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 43: Scene Interfaces");
			if(SharedState.TypeDictionary.TryGetValue("SceneObjectIdentifier", out TypeDefinition sceneObjectIdentifier))
			{
				sceneObjectIdentifier.ImplementSceneObjectIdentifier();
			}
			if (SharedState.TypeDictionary.TryGetValue("OcclusionScene", out TypeDefinition occlusionScene))
			{
				occlusionScene.ImplementOcclusionScene();
			}
		}

		private static void ImplementSceneObjectIdentifier(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<ISceneObjectIdentifier>();
			type.ImplementFullProperty(nameof(ISceneObjectIdentifier.TargetObject), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int64, type.GetFieldByName("targetObject"));
			type.ImplementFullProperty(nameof(ISceneObjectIdentifier.TargetPrefab), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int64, type.GetFieldByName("targetPrefab"));
		}

		private static void ImplementOcclusionScene(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<IOcclusionScene>();
			type.ImplementFullProperty(nameof(IOcclusionScene.IndexRenderers), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int32, type.GetFieldByName("indexRenderers"));
			type.ImplementFullProperty(nameof(IOcclusionScene.SizeRenderers), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int32, type.GetFieldByName("sizeRenderers"));
			type.ImplementFullProperty(nameof(IOcclusionScene.IndexPortals), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int32, type.GetFieldByName("indexPortals"));
			type.ImplementFullProperty(nameof(IOcclusionScene.SizePortals), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Int32, type.GetFieldByName("sizePortals"));

			FieldDefinition guidField = type.GetFieldByName("scene");

			//specific to common
			MethodDefinition implicitConversion = guidField.Signature.FieldType.Resolve().Methods.Single(m => m.Name == "op_Implicit");
			//common to specific
			MethodDefinition explicitConversion = guidField.Signature.FieldType.Resolve().Methods.Single(m => m.Name == "op_Explicit");

			ITypeDefOrRef unityGuid = SharedState.Importer.ImportCommonType<AssetRipper.Core.Classes.Misc.UnityGUID>();
			PropertyDefinition property = type.AddFullProperty(nameof(IOcclusionScene.Scene), InterfaceUtils.InterfacePropertyImplementation, unityGuid.ToTypeSignature());
			var getProcessor = property.GetMethod.CilMethodBody.Instructions;
			getProcessor.Add(CilOpCodes.Ldarg_0);
			getProcessor.Add(CilOpCodes.Ldfld, guidField);
			getProcessor.Add(CilOpCodes.Call, implicitConversion);
			getProcessor.Add(CilOpCodes.Ret);
			var setProcessor = property.SetMethod.CilMethodBody.Instructions;
			setProcessor.Add(CilOpCodes.Ldarg_0);
			setProcessor.Add(CilOpCodes.Ldarg_1);
			setProcessor.Add(CilOpCodes.Call, explicitConversion);
			setProcessor.Add(CilOpCodes.Stfld, guidField);
			setProcessor.Add(CilOpCodes.Ret);
		}
	}
}
