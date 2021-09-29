using System;
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

		/// <summary>
		/// The constructor used in json deserialization
		/// </summary>
		public UnityClass() { }

		/// <summary>
		/// The constructor used to make dependent class definitions
		/// </summary>
		public UnityClass(UnityNode releaseRootNode, UnityNode editorRootNode)
		{
			if (releaseRootNode == null && editorRootNode == null)
				throw new ArgumentException("Both root nodes cannot be negative");

			ReleaseRootNode = releaseRootNode;
			EditorRootNode = editorRootNode;
			var mainRootNode = releaseRootNode ?? editorRootNode;

			Name = mainRootNode.TypeName;
			FullName = Name;
			TypeID = -1;
			Derived = new List<string>();
			DescendantCount = 0;
			Size = mainRootNode.ByteSize;
			IsAbstract = false;
			IsSealed = true;
			IsEditorOnly = false;
			IsStripped = false;
		}
	}
}