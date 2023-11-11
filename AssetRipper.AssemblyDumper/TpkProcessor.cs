using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.Tpk;
using AssetRipper.Tpk.TypeTrees;

namespace AssetRipper.AssemblyDumper
{
	internal static class TpkProcessor
	{
		private static UnityVersion MinimumVersion { get; } = new UnityVersion(3, 5, 0);

		public static void IntitializeSharedState(string tpkPath)
		{
			TpkTypeTreeBlob blob = ReadTpkFile(tpkPath);
			Console.WriteLine($"\tCreation time: {blob.CreationTime.ToLocalTime()}");
			Dictionary<UnityVersion, UnityVersion> versionRedirectDictionary = MakeVersionRedirectDictionary(blob.Versions);
			Dictionary<int, VersionedList<UniversalClass>> classes = new();
			foreach (TpkClassInformation classInfo in blob.ClassInformation)
			{
				int id = classInfo.ID;

				if (IsUnacceptable(id) || HasNoDataAfterMinimumVersion(classInfo))
				{
					continue;
				}

				VersionedList<UniversalClass> classList = new();
				classes.Add(id, classList);
				for (int i = 0; i < classInfo.Classes.Count; i++)
				{
					KeyValuePair<UnityVersion, TpkUnityClass?> pair = classInfo.Classes[i];
					UnityVersion version = versionRedirectDictionary[pair.Key];
					if (version == MinimumVersion && i < classInfo.Classes.Count - 1 && versionRedirectDictionary[classInfo.Classes[i + 1].Key] == MinimumVersion)
					{
						//Skip. This TpkUnityClass conflicts with the next one because they're both redirected to the minimum version.
					}
					else if (pair.Value is not null)
					{
						UniversalClass universalClass = UniversalClass.FromTpkUnityClass(pair.Value, id, blob.StringBuffer, blob.NodeBuffer);
						classList.Add(version, universalClass);
					}
					else if (classList.Count is not 0)
					{
						classList.Add(version, null);
					}
				}
			}
			UniversalCommonString commonString = UniversalCommonString.FromBlob(blob);
			UnityVersion[] usedVersions = blob.Versions.Where(v => v >= MinimumVersion).ToArray();
			SharedState.Initialize(usedVersions, classes, commonString);

			static bool IsUnacceptable(int typeId) => typeId >= 100000 && typeId <= 100011;

			static bool HasNoDataAfterMinimumVersion(TpkClassInformation info)
			{
				KeyValuePair<UnityVersion, TpkUnityClass?> lastPair = info.Classes[info.Classes.Count - 1];
				return lastPair.Key < MinimumVersion && lastPair.Value is null;
			}
		}

		private static TpkTypeTreeBlob ReadTpkFile(string path)
		{
			TpkDataBlob blob = TpkFile.FromFile(path).GetDataBlob();
			return blob is TpkTypeTreeBlob typeTreeBlob
				? typeTreeBlob
				: throw new NotSupportedException($"Blob cannot be type {blob.GetType()}");
		}

		private static Dictionary<UnityVersion, UnityVersion> MakeVersionRedirectDictionary(List<UnityVersion> list)
		{
			Dictionary<UnityVersion, UnityVersion> dict = new();

			UnityVersion first = list.First(v => v >= MinimumVersion);
			dict.Add(first, first.StripType());

			int firstIndex = list.IndexOf(first);
			for (int i = 0; i < firstIndex; i++)
			{
				dict.Add(list[i], MinimumVersion);
			}
			for (int i = firstIndex + 1; i < list.Count; i++)
			{
				UnityVersion previous = list[i - 1];
				UnityVersion current = list[i];
				if (current.Major != previous.Major)
				{
					dict.Add(current, current.StripMinor());
				}
				else if (current.Minor != previous.Minor)
				{
					dict.Add(current, current.StripBuild());
				}
				else if (current.Build != previous.Build)
				{
					dict.Add(current, current.StripType());
				}
				else if (current.Type != previous.Type)
				{
					dict.Add(current, current.StripTypeNumber());
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
