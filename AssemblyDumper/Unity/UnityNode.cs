using System.Collections.Generic;

namespace AssemblyDumper.Unity
{
	public class UnityNode
	{
		public string TypeName { get; set; }
		public string Name { get; set; }
		public byte Level { get; set; }
		public int ByteSize { get; set; }
		public int Index { get; set; }
		public short Version { get; set; }
		public byte TypeFlags { get; set; }
		public int MetaFlag { get; set; }
		public List<UnityNode> SubNodes { get; set; }
	}
}
