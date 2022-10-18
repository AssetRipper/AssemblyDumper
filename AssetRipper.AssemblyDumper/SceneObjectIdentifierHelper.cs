using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.Metadata;

#nullable disable

namespace AssetRipper.AssemblyDumper
{
	internal static class SceneObjectIdentifierHelper
	{
		public static long GetExportId(IUnityObjectBase targetAsset, long previousValue, IExportContainer container)
		{
			if (targetAsset is null)
			{
				return previousValue;
			}
			else
			{
				MetaPtr metaPtr = container.CreateExportPointer(targetAsset);
				return metaPtr.FileID;
			}
		}
	}
}

#nullable enable