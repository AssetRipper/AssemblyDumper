using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Unity
{
	public class UnityNode
	{
		public string TypeName { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		public string AlternateTypeName { get; set; }
		public string Name { get; set; }
		public byte Level { get; set; }
		public int ByteSize { get; set; }
		public int Index { get; set; }
		public short Version { get; set; }
		public byte TypeFlags { get; set; }
		public int MetaFlag { get; set; }
		public List<UnityNode> SubNodes { get; set; }

		public string GetRelevantTypeName() => string.IsNullOrEmpty(AlternateTypeName) ? TypeName : AlternateTypeName;

		/// <summary>
		/// Deep clones a node and all its subnodes<br/>
		/// Warning: Deep cloning a node with a circular hierarchy will cause an endless loop
		/// </summary>
		/// <returns>The new node</returns>
		public UnityNode DeepClone()
		{
			var cloned = new UnityNode();
			cloned.TypeName = new string(TypeName ?? "");
			cloned.AlternateTypeName = new string(AlternateTypeName ?? "");
			cloned.Name = new string(Name ?? "");
			cloned.Level = Level;
			cloned.ByteSize = ByteSize;
			cloned.Index = Index;
			cloned.Version = Version;
			cloned.TypeFlags = TypeFlags;
			cloned.MetaFlag = MetaFlag;
			cloned.SubNodes = SubNodes.ConvertAll(x => x.DeepClone());
			return cloned;
		}
	}
}