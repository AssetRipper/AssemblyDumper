using System.Collections.Generic;

namespace AssemblyDumper.Unity
{
	public class UnityClass
	{
		public string Name { get; set; }
		public string Namespace { get; set; }
		public string FullName { get; set; }
		public string Module { get; set; }
		public int TypeID { get; set; }
		public string Base { get; set; }
		public List<string> Derived { get; set; }
		public uint DescendantCount { get; set; }
		public int Size { get; set; }
		public uint TypeIndex { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsSealed { get; set; }
		public bool IsEditorOnly { get; set; }
		public bool IsStripped { get; set; }
		public UnityNode EditorRootNode { get; set; }
		public UnityNode ReleaseRootNode { get; set; }
	}
}
