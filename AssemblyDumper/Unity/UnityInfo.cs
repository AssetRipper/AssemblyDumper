using System.Collections.Generic;

namespace AssemblyDumper.Unity
{
	public class UnityInfo
	{
		public string Version { get; set; }
		public List<UnityString> Strings { get; set; }
		public List<UnityClass> Classes { get; set; }
	}
}