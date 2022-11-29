using AssetRipper.AssemblyDumper.Utils;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass008_DivideAmbiguousPPtr
	{
		public static void DoPass()
		{
			foreach ((string name, VersionedList<UniversalClass> list) in SharedState.Instance.SubclassInformation)
			{
				if (!name.StartsWith("PPtr_", StringComparison.Ordinal))
				{
					continue;
				}

				string parameterTypeName = name.Substring(5);
				if (!MapsToMultipleIds(parameterTypeName))
				{
					continue;
				}

				List<List<UnityVersionRange>> rangeListList = MakeRangeListList(parameterTypeName);
				HashSet<UnityVersion> divisions = ExtractDivisions(rangeListList);

				foreach (UnityVersion division in divisions)
				{
					list.Divide(division);
				}
			}
		}

		private static HashSet<UnityVersion> ExtractDivisions(List<List<UnityVersionRange>> rangeListList)
		{
			HashSet<UnityVersion> divisions = new();
			foreach (List<UnityVersionRange> rangeList in rangeListList)
			{
				foreach (List<UnityVersionRange> otherRangeList in rangeListList)
				{
					if (rangeList == otherRangeList)
					{
						continue;
					}

					foreach (UnityVersionRange range in rangeList)
					{
						foreach (UnityVersionRange otherRange in otherRangeList)
						{
							if (range.CanUnion(otherRange))
							{
								divisions.Add(UnityVersion.Max(range.Start, otherRange.Start));
								divisions.Add(UnityVersion.Min(range.End, otherRange.End));
							}
						}
					}
				}
			}

			return divisions;
		}

		private static List<List<UnityVersionRange>> MakeRangeListList(string parameterTypeName)
		{
			List<List<UnityVersionRange>> rangeListList = new();
			IEnumerable<VersionedList<UniversalClass>> classes = SharedState.Instance.NameToTypeID[parameterTypeName]
				.Select(id => SharedState.Instance.ClassInformation[id]);
			foreach (VersionedList<UniversalClass> versionedList in classes)
			{
				List<UnityVersionRange> rangeList = new();
				bool first = true;
				UnityVersionRange cumulativeRange = default;
				for (int i = 0; i < versionedList.Count; i++)
				{
					(UnityVersion start, UniversalClass? universalClass) = versionedList[i];
					if (universalClass is not null && universalClass.Name == parameterTypeName)
					{
						UnityVersionRange currentRange = i < versionedList.Count - 1
							? new UnityVersionRange(start, versionedList[i + 1].Key)
							: new UnityVersionRange(start, UnityVersion.MaxVersion);

						if (first)
						{
							cumulativeRange = currentRange;
							first = false;
						}
						else if (cumulativeRange.CanUnion(currentRange))
						{
							cumulativeRange = cumulativeRange.MakeUnion(currentRange);
						}
						else
						{
							rangeList.Add(cumulativeRange);
							cumulativeRange = currentRange;
						}
					}
				}
				if (cumulativeRange != default)
				{
					rangeList.Add(cumulativeRange);
					rangeListList.Add(rangeList);
				}
			}

			Debug.Assert(rangeListList.Count > 1);

			return rangeListList;
		}

		private static bool MapsToMultipleIds(string name)
		{
			return SharedState.Instance.NameToTypeID.TryGetValue(name, out HashSet<int>? set) && set.Count > 1;
		}
	}
}
