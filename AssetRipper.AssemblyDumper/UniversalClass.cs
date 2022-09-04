using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.Tpk.Shared;
using AssetRipper.Tpk.TypeTrees;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class UniversalClass : IDeepCloneable<UniversalClass>
	{
		private string name = "";

		/// <summary>
		/// The name of the class not including the namespace
		/// </summary>
		public string Name { get => name; set => name = value ?? ""; }
		/// <summary>
		/// The unique number used to identify the class. Negative value indicates that the class doesn't have a type id
		/// </summary>
		public int TypeID { get; set; }
		/// <summary>
		/// The name of the base class if it exists. Namespace not included
		/// </summary>
		public string? BaseString { get; set; }
		/// <summary>
		/// The base class if it exists
		/// </summary>
		public UniversalClass? BaseClass { get; set; }
		/// <summary>
		/// The names of the classes that directly derive from this. Namespaces not included
		/// </summary>
		public List<UniversalClass> DerivedClasses { get; } = new();
		/// <summary>
		/// The count of all classes that descend from this class<br/>
		/// It includes this class, so the count is always positive<br/>
		/// This gets generated in <see cref="Passes.Pass011_ApplyInheritance"/>
		/// </summary>
		public uint DescendantCount { get; internal set; } = 1;
		/// <summary>
		/// Is the class abstract?
		/// </summary>
		public bool IsAbstract { get; set; }
		/// <summary>
		/// Is the class sealed?
		/// </summary>
		public bool IsSealed => DerivedClasses.Count == 0 && !IsAbstract;
		/// <summary>
		/// Does the class only appear in the editor?
		/// </summary>
		public bool IsEditorOnly { get; set; }
		/// <summary>
		/// Does the class only appear in game files?
		/// </summary>
		public bool IsReleaseOnly { get; set; }
		/// <summary>
		/// Is the class stripped?
		/// </summary>
		public bool IsStripped { get; set; }
		public UniversalNode? EditorRootNode { get; set; }
		public UniversalNode? ReleaseRootNode { get; set; }

		private UniversalClass() { }

		/// <summary>
		/// The constructor used to make dependent class definitions
		/// </summary>
		public UniversalClass(UniversalNode? releaseRootNode, UniversalNode? editorRootNode)
		{
			if (releaseRootNode == null && editorRootNode == null)
			{
				throw new ArgumentException("Both root nodes cannot be null");
			}

			ReleaseRootNode = releaseRootNode;
			EditorRootNode = editorRootNode;
			UniversalNode? mainRootNode = releaseRootNode ?? editorRootNode;

			Name = mainRootNode!.TypeName;
			TypeID = -1;
			IsAbstract = false;
			IsEditorOnly = releaseRootNode == null;
			IsReleaseOnly = editorRootNode == null;
			IsStripped = false;
		}

		public static UniversalClass FromTpkUnityClass(TpkUnityClass tpkClass, int typeId, TpkStringBuffer stringBuffer, TpkUnityNodeBuffer nodeBuffer)
		{
			UniversalClass result = new UniversalClass();
			result.Name = stringBuffer[tpkClass.Name];
			result.TypeID = typeId;
			result.BaseString = stringBuffer[tpkClass.Base];
			result.IsAbstract = tpkClass.Flags.IsAbstract();
			result.IsEditorOnly = tpkClass.Flags.IsEditorOnly();
			result.IsReleaseOnly = tpkClass.Flags.IsReleaseOnly();
			result.IsStripped = tpkClass.Flags.IsStripped();
			if (tpkClass.Flags.HasEditorRootNode())
			{
				result.EditorRootNode = UniversalNode.FromTpkUnityNode(nodeBuffer[tpkClass.EditorRootNode], stringBuffer, nodeBuffer);
			}
			if (tpkClass.Flags.HasReleaseRootNode())
			{
				result.ReleaseRootNode = UniversalNode.FromTpkUnityNode(nodeBuffer[tpkClass.ReleaseRootNode], stringBuffer, nodeBuffer);
			}
			return result;
		}

		/// <summary>
		/// Gets the original name of the type and asserts compatible naming
		/// </summary>
		/// <param name="originalTypeName">The original name of the type before any changes were applied</param>
		/// <returns>True if the original name is different from the current name</returns>
		public string GetOriginalTypeName()
		{
			if (ReleaseRootNode == null && EditorRootNode == null)
			{
				return Name;
			}
			else if (ReleaseRootNode == null)
			{
				Assertions.AssertEquality(Name, EditorRootNode!.TypeName);
				return EditorRootNode.OriginalTypeName;
			}
			else if (EditorRootNode == null)
			{
				Assertions.AssertEquality(Name, ReleaseRootNode.TypeName);
				return ReleaseRootNode.OriginalTypeName;
			}
			else
			{
				Assertions.AssertEquality(Name, ReleaseRootNode.TypeName);
				Assertions.AssertEquality(Name, EditorRootNode.TypeName);
				Assertions.AssertEquality(ReleaseRootNode.OriginalTypeName, EditorRootNode.OriginalTypeName);
				return ReleaseRootNode.OriginalTypeName;
			}
		}

		public UniversalClass DeepClone()
		{
			UniversalClass newClass = new();
			newClass.Name = Name;
			newClass.TypeID = TypeID;
			newClass.BaseString = BaseString;
			newClass.BaseClass = BaseClass;
			newClass.DerivedClasses.Capacity = DerivedClasses.Count;
			newClass.DerivedClasses.AddRange(DerivedClasses);
			newClass.DescendantCount = DescendantCount;
			newClass.IsAbstract = IsAbstract;
			newClass.IsEditorOnly = IsEditorOnly;
			newClass.IsReleaseOnly = IsReleaseOnly;
			newClass.IsStripped = IsStripped;
			newClass.EditorRootNode = EditorRootNode?.DeepClone();
			newClass.ReleaseRootNode = ReleaseRootNode?.DeepClone();
			return newClass;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
