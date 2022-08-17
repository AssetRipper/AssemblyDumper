using AssetRipper.Tpk;
using AssetRipper.Tpk.TypeTrees;
using System.IO;

namespace AssetRipper.AssemblyDumper
{
	internal static class TpkProcessor
	{
		public static void IntitializeSharedState(string tpkPath)
		{
			TpkTypeTreeBlob blob = ReadTpkFile(tpkPath);
			Dictionary<int, VersionedList<UniversalClass?>> classes = new();
			foreach (TpkClassInformation classInfo in blob.ClassInformation)
			{
				int id = classInfo.ID;

				if (IsUnacceptable(id))
				{
					continue;
				}

				VersionedList<UniversalClass?> classList = new();
				classes.Add(id, classList);
				foreach (var pair in classInfo.Classes)
				{
					if (pair.Value is not null)
					{
						UniversalClass universalClass = UniversalClass.FromTpkUnityClass(pair.Value, id, blob.StringBuffer, blob.NodeBuffer);
						classList.Add(pair.Key, universalClass);
					}
					else
					{
						classList.Add(pair.Key, null);
					}
				}
			}
			UniversalCommonString commonString = UniversalCommonString.FromBlob(blob);
			UnityVersion[] usedVersions = blob.Versions.ToArray();
			SharedState.Initialize(usedVersions, classes, commonString);
		}

		private static TpkTypeTreeBlob ReadTpkFile(string path)
		{
			TpkDataBlob blob = TpkFile.FromFile(path).GetDataBlob();
			return blob is TpkTypeTreeBlob typeTreeBlob
				? typeTreeBlob
				: throw new NotSupportedException($"Blob cannot be type {blob.GetType()}");
		}

		private static bool IsUnacceptable(int typeId)
		{
			return typeId >= 100000 && typeId <= 100011;
		}
	}
}
