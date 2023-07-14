using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Files;
using AssetRipper.Yaml;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes;

internal static class PPtrHelper
{
	public static YamlNode ExportYaml<T>(IPPtr<T> pptr, IExportContainer container, int classID) where T : IUnityObjectBase
	{
		if (pptr.PathID == 0)
		{
			return MetaPtr.NullPtr.ExportYaml();
		}

		if (pptr.TryGetAsset(container.File, out T asset))
		{
			MetaPtr exPointer = container.CreateExportPointer(asset);
			return exPointer.ExportYaml();
		}
		else
		{
			AssetType assetType = container.ToExportType(typeof(T));
			MetaPtr pointer = MetaPtr.CreateMissingReference(classID, assetType);
			return pointer.ExportYaml();
		}
	}
	public static PPtr ForceCreatePPtr(AssetCollection collection, IUnityObjectBase asset)
	{
		return collection.ForceCreatePPtr(asset);
	}
	public static bool TryGetAsset<T>(AssetCollection collection, int fileID, long pathID, [NotNullWhen(true)] out T asset)
	{
		if (pathID == 0)
		{
			asset = default;
			return false;
		}
		IUnityObjectBase @object = collection.TryGetAsset(fileID, pathID);
		switch (@object)
		{
			case null:
				asset = default;
				return false;
			case T t:
				asset = t;
				return true;
			case NullObject:
				asset = default;
				return false;
			default:
				throw new Exception($"Object's type {@object.GetType().Name} isn't assignable from {typeof(T).Name}");
		}
	}
}

#nullable enable