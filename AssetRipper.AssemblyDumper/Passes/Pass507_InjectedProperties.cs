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
		}

		private static void SceneAssetTargetScene()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[1032]; //SceneAsset
			TypeSignature propertyType = SharedState.Instance.Importer.ImportType<AssetCollection>().ToTypeSignature();
			PropertyInjector.InjectFullProperty(group, propertyType, TargetSceneName, true);
		}
	}
}
