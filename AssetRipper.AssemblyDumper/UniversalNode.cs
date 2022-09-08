using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.Tpk.Shared;
using AssetRipper.Tpk.TypeTrees;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class UniversalNode : IEquatable<UniversalNode?>, IDeepCloneable<UniversalNode>
	{
		/// <summary>
		/// The current type name
		/// </summary>
		public string TypeName { get => typeName; set => typeName = value ?? ""; }
		/// <summary>
		/// The original type name as obtained from the tpk file
		/// </summary>
		public string OriginalTypeName
		{
			get => string.IsNullOrEmpty(originalTypeName) ? TypeName : originalTypeName;
			set => originalTypeName = value ?? "";
		}
		public string Name { get => name; set => name = value ?? ""; }
		/// <summary>
		/// The original name as obtained from the tpk file
		/// </summary>
		public string OriginalName
		{
			get => string.IsNullOrEmpty(originalName) ? Name : originalName;
			set => originalName = value ?? "";
		}
		public short Version { get; set; }
		public uint MetaFlag { get; set; }
		public List<UniversalNode> SubNodes { get => subNodes; set => subNodes = value ?? new(); }

		private string originalTypeName = "";
		private string originalName = "";
		private string typeName = "";
		private string name = "";
		private List<UniversalNode> subNodes = new();

		public UniversalNode()
		{
		}

		public bool TryGetSubNodeByName(string nodeName, [NotNullWhen(true)] out UniversalNode? subnode)
		{
			subnode = SubNodes.SingleOrDefault(n => n.Name == nodeName);
			return subnode is not null;
		}

		public UniversalNode? TryGetSubNodeByName(string nodeName)
		{
			return SubNodes.SingleOrDefault(n => n.Name == nodeName);
		}

		public bool TryGetSubNodeByTypeAndName(string nodeTypeName, string nodeName, [NotNullWhen(true)] out UniversalNode? subnode)
		{
			subnode = SubNodes.SingleOrDefault(n => n.Name == nodeName && n.TypeName == nodeTypeName);
			return subnode is not null;
		}

		public UniversalNode GetSubNodeByName(string nodeName)
		{
			return SubNodes.Single(n => n.Name == nodeName);
		}

		public static UniversalNode FromTpkUnityNode(TpkUnityNode tpkNode, TpkStringBuffer stringBuffer, TpkUnityNodeBuffer nodeBuffer)
		{
			UniversalNode result = new UniversalNode();
			result.TypeName = GetFixedTypeName(stringBuffer[tpkNode.TypeName]);
			result.OriginalTypeName = result.TypeName;
			result.Name = stringBuffer[tpkNode.Name];
			result.OriginalName = result.Name;
			result.Version = tpkNode.Version;
			result.MetaFlag = tpkNode.MetaFlag;
			result.SubNodes = tpkNode.SubNodes
				.Select(nodeIndex => FromTpkUnityNode(nodeBuffer[nodeIndex], stringBuffer, nodeBuffer))
				.ToList();
			return result;
		}

		/// <summary>
		/// Only store one name for each primitive integer size.
		/// </summary>
		/// <remarks>
		/// Although this deduplicates, it also prevents these loaded type trees from being used in making new serialized files.
		/// </remarks>
		/// <param name="originalName"></param>
		/// <returns></returns>
		private static string GetFixedTypeName(string originalName)
		{
			return originalName switch
			{
				"short" => "SInt16",
				"int" => "SInt32",
				"long long" => "SInt64",
				"unsigned short" => "UInt16",
				"unsigned int" => "UInt32",
				"unsigned long long" => "UInt64",
				_ => originalName,
			};
		}

		/// <summary>
		/// Deep clones a node and all its subnodes<br/>
		/// Warning: Deep cloning a node with a circular hierarchy will cause an endless loop
		/// </summary>
		/// <returns>The new node</returns>
		public UniversalNode DeepClone()
		{
			UniversalNode clone = new UniversalNode();
			clone.TypeName = TypeName;
			clone.originalTypeName = originalTypeName;
			clone.Name = Name;
			clone.originalName = originalName;
			clone.Version = Version;
			clone.MetaFlag = MetaFlag;
			clone.SubNodes = SubNodes.ConvertAll(x => x.DeepClone());
			return clone;
		}

		/// <summary>
		/// Shallow clones a node but not its subnodes
		/// </summary>
		/// <returns>The new node</returns>
		public UniversalNode ShallowClone()
		{
			UniversalNode clone = new UniversalNode();
			clone.TypeName = TypeName;
			clone.originalTypeName = originalTypeName;
			clone.Name = Name;
			clone.originalName = originalName;
			clone.Version = Version;
			clone.MetaFlag = MetaFlag;
			clone.SubNodes = SubNodes.ToList();
			return clone;
		}

		public UniversalNode DeepCloneAsRootNode()
		{
			UniversalNode clone = DeepClone();
			clone.Name = "Base";
			clone.OriginalName = clone.Name;
			return clone;
		}

		public UniversalNode ShallowCloneAsRootNode()
		{
			UniversalNode clone = ShallowClone();
			clone.Name = "Base";
			clone.OriginalName = clone.Name;
			return clone;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as UniversalNode);
		}

		public bool Equals(UniversalNode? other)
		{
			return other != null &&
				   TypeName == other.TypeName &&
				   OriginalTypeName == other.OriginalTypeName &&
				   Name == other.Name &&
				   OriginalName == other.OriginalName &&
				   Version == other.Version &&
				   MetaFlag == other.MetaFlag &&
				   EqualityComparer<List<UniversalNode>>.Default.Equals(SubNodes, other.SubNodes);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(TypeName, OriginalTypeName, Name, OriginalName, Version, MetaFlag, SubNodes);
		}

		public static bool operator ==(UniversalNode? left, UniversalNode? right)
		{
			return EqualityComparer<UniversalNode>.Default.Equals(left, right);
		}

		public static bool operator !=(UniversalNode? left, UniversalNode? right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return $"{TypeName} {Name}";
		}
	}
}
