using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.Tpk;
using AssetRipper.Tpk.TypeTrees;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper
{
	internal static class TpkProcessor
	{
		public static void IntitializeSharedState(string tpkPath)
		{
			TpkTypeTreeBlob blob = ReadTpkFile(tpkPath);
			Dictionary<UnityVersion, UnityVersion> versionRedirectDictionary = MakeVersionRedirectDictionary(blob.Versions);
			Dictionary<int, VersionedList<UniversalClass>> classes = new();
			foreach (TpkClassInformation classInfo in blob.ClassInformation)
			{
				int id = classInfo.ID;

				if (IsUnacceptable(id))
				{
					continue;
				}

				VersionedList<UniversalClass> classList = new();
				classes.Add(id, classList);
				foreach (var pair in classInfo.Classes)
				{
					UnityVersion version = versionRedirectDictionary[pair.Key];
					if (pair.Value is not null)
					{
						UniversalClass universalClass = UniversalClass.FromTpkUnityClass(pair.Value, id, blob.StringBuffer, blob.NodeBuffer);
						classList.Add(version, universalClass);
					}
					else
					{
						classList.Add(version, null);
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

		private static Dictionary<UnityVersion, UnityVersion> MakeVersionRedirectDictionary(List<UnityVersion> list)
		{
			Dictionary<UnityVersion, UnityVersion> dict = new();
			Debug.Assert(list.Count > 0);
			{
				UnityVersion first = list[0];
				dict.Add(first, first.StripType());
			}
			for (int i = 1; i < list.Count; i++)
			{
				UnityVersion previous = list[i - 1];
				UnityVersion current = list[i];
				if (current.Build != previous.Build || current.Minor != previous.Minor || current.Major != previous.Major)
				{
					dict.Add(current, current.StripType());
				}
				else
				{
					dict.Add(current, current);
				}
			}
			return dict;
		}
	}
}
