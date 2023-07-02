using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass001_MergeMovedGroups
{
	public static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> Changes = new Dictionary<int, IReadOnlyList<int>>
	{
		{ 258, new int[] { 197 } },//LightProbes
		{ 319, new int[] { 1011 } },//AvatarMask
		{ 329, new int[] { 327 } },//VideoClip
	};

	public static void DoPass()
	{
		foreach ((int mainID, IReadOnlyList<int> idList) in Changes)
		{
			VersionedList<UniversalClass> versionedList = SharedState.Instance.ClassInformation[mainID];
			foreach (int id in idList)
			{
				VersionedList<UniversalClass> otherVersionedList = SharedState.Instance.ClassInformation[id];
				versionedList = VersionedList.Merge(versionedList, otherVersionedList);
				SharedState.Instance.ClassInformation.Remove(id);
			}
			foreach (UniversalClass? universalClass in versionedList.Values)
			{
				if (universalClass is not null)
				{
					universalClass.TypeID = mainID;
				}
			}
			SharedState.Instance.ClassInformation[mainID] = versionedList;
		}
	}
}
