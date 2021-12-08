using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AssemblyDumper.Unity
{
	public class UnityNode
	{
		/// <summary>
		/// The unique type name used in the <see cref = "SharedState"/> dictionaries
		/// </summary>
		public string TypeName { get; set; }
		/// <summary>
		/// The original type name as obtained from the json file
		/// </summary>
		[JsonIgnore]
		public string OriginalTypeName 
		{
			get => string.IsNullOrEmpty(m_OriginalTypeName) ? TypeName : m_OriginalTypeName;
			set => m_OriginalTypeName = value;
		}
		public string Name { get; set; }
		/// <summary>
		/// The original name as obtained from the json file
		/// </summary>
		[JsonIgnore]
		public string OriginalName
		{
			get => string.IsNullOrEmpty(m_OriginalName) ? Name : m_OriginalName;
			set => m_OriginalName = value;
		}
		public byte Level { get; set; }
		public int ByteSize { get; set; }
		public int Index { get; set; }
		public short Version { get; set; }
		public byte TypeFlags { get; set; }
		public int MetaFlag { get; set; }
		public List<UnityNode> SubNodes { get; set; }

		private string m_OriginalTypeName;
		private string m_OriginalName;

		/// <summary>
		/// Deep clones a node and all its subnodes<br/>
		/// Warning: Deep cloning a node with a circular hierarchy will cause an endless loop
		/// </summary>
		/// <returns>The new node</returns>
		public UnityNode DeepClone()
		{
			var cloned = new UnityNode();
			cloned.TypeName = CloneString(TypeName);
			cloned.m_OriginalTypeName = CloneString(m_OriginalTypeName);
			cloned.Name = CloneString(Name);
			cloned.m_OriginalName = CloneString(m_OriginalName);
			cloned.Level = Level;
			cloned.ByteSize = ByteSize;
			cloned.Index = Index;
			cloned.Version = Version;
			cloned.TypeFlags = TypeFlags;
			cloned.MetaFlag = MetaFlag;
			cloned.SubNodes = SubNodes.ConvertAll(x => x.DeepClone());
			return cloned;
		}

		private static string CloneString(string @string)
		{
			return @string == null ? null : new string(@string);
		}
	}
}