using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper;

internal static class EnumHistoryExtensions
{
	public static IEnumerable<KeyValuePair<string, long>> GetFields(this EnumHistory history)
	{
		foreach (EnumMemberHistory member in history.Members.Values)
		{
			if (member.TryGetUniqueValue(out long value, out IEnumerable<KeyValuePair<string, long>>? pairs))
			{
				yield return new KeyValuePair<string, long>(member.Name, value);
			}
			else
			{
				foreach (KeyValuePair<string, long> pair in pairs)
				{
					yield return pair;
				}
			}
		}
	}
}
