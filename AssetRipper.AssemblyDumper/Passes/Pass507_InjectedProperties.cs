using AssetRipper.Assets.Collections;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass507_InjectedProperties
	{
		public const string TargetSceneName = "TargetScene";
		public const string TerrainDataName = "TerrainData";

		public static void DoPass()
		{
			SceneAssetTargetScene();
			TextureTerrainData();
		}

		private static void SceneAssetTargetScene()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[1032]; //SceneAsset
			TypeSignature propertyType = SharedState.Instance.Importer.ImportType<AssetCollection>().ToTypeSignature();
			PropertyInjector.InjectFullProperty(group, propertyType, TargetSceneName, true);
		}

		private static void TextureTerrainData()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[28]; //Texture2D
			TypeSignature propertyType = SharedState.Instance.ClassGroups[156].Interface.ToTypeSignature();//ITerrainData
			PropertyInjector.InjectFullProperty(group, propertyType, TerrainDataName, true);
		}
	}
}
