using System.Text;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class StringBuilderExtensions
	{
		public static void AppendLineAndThreeTabs(this StringBuilder sb) => sb.Append("\n\t\t\t");
		public static void AppendBreakTag(this StringBuilder sb) => sb.Append("<br />");
		public static void AppendUnityVersionRange(this StringBuilder sb, Range<UnityVersion> range)
		{
			sb.Append(range.Start == UnityVersion.MinVersion || range.Start == SharedState.Instance.MinVersion ? "Min" : range.Start);
			sb.Append(" to ");
			sb.Append(range.End == UnityVersion.MaxVersion ? "Max" : range.End);
		}
		public static void AppendUnityVersionRanges(this StringBuilder sb, List<UnityVersionRange> ranges)
		{
			sb.AppendUnityVersionRange(ranges[0]);
			for (int i = 1; i < ranges.Count; i++)
			{
				sb.Append(", ");
				sb.AppendUnityVersionRange(ranges[i]);
			}
		}
	}
}
