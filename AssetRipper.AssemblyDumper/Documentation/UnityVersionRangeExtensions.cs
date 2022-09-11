using System.Text;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class UnityVersionRangeExtensions
	{
		public static IReadOnlyList<UnityVersionRange> GetUnionedRanges(this IEnumerable<UnityVersionRange> ranges)
		{
			List<UnityVersionRange> unionedRanges = new();
			foreach (UnityVersionRange range in ranges)
			{
				if (unionedRanges.Count > 0 && unionedRanges[unionedRanges.Count - 1].CanUnion(range))
				{
					unionedRanges[unionedRanges.Count - 1] = unionedRanges[unionedRanges.Count - 1].MakeUnion(range);
				}
				else
				{
					unionedRanges.Add(range);
				}
			}
			return unionedRanges;
		}

		public static string GetString(this IReadOnlyList<UnityVersionRange> ranges)
		{
			StringBuilder sb = new();
			sb.AppendUnityVersionRanges(ranges);
			return sb.ToString();
		}
	}
}
