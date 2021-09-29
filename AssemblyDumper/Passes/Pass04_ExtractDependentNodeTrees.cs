using AssemblyDumper.Unity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass04_ExtractDependentNodeTrees
	{
		public readonly static string[] primitives =
		{
			"Array",
			"bool",
			"char",
			"double",
			"float",
			"int",
			"list",
			"long long", //long in C#
			"map",
			"pair",
			"set",
			"short",
			"SInt16",
			"SInt32",
			"SInt64",
			"SInt8",
			"staticvector",
			"string",
			"TypelessData", //byte[]
			"UInt16",
			"UInt32",
			"UInt64",
			"UInt8",
			"unsigned int",
			"unsigned long long",
			"unsigned short",
			"Type*", //int32
			"vector",
			"void"
		};

		private readonly static string[] primitiveNames =
		{
			"bool",
			"char",
			"double",
			"float",
			"int",
			"long long", //long in C#
			"short",
			"SInt16",
			"SInt32",
			"SInt64",
			"SInt8",
			"string",
			"TypelessData", //byte[]
			"UInt16",
			"UInt32",
			"UInt64",
			"UInt8",
			"unsigned int",
			"unsigned long long",
			"unsigned short",
			"Type*", //int32
			"void"
		};

		public readonly static string[] generics =
		{
			"Array",
			"first",
			"list",
			"map",
			"pair",
			"second",
			"set",
			"staticvector",
			"vector"
		};

		public static void DoPass()
		{
			Logger.Info("Pass 4: Extract Dependent Node Trees");
			AddDependentTypes();
			CreateNewClasses();
		}

		private static void CreateNewClasses()
		{
			foreach (var pair in releaseRootNodes)
			{
				if (editorRootNodes.TryGetValue(pair.Key, out UnityNode editorNode))
				{
					var newClass = new UnityClass(pair.Value, editorNode);
					SharedState.ClassDictionary.Add(pair.Key, newClass);
				}
				else
				{
					var newClass = new UnityClass(pair.Value, null);
					SharedState.ClassDictionary.Add(pair.Key, newClass);
				}
			}

			foreach (var pair in editorRootNodes)
			{
				if (!releaseRootNodes.ContainsKey(pair.Key))
				{
					var newClass = new UnityClass(null, pair.Value);
					SharedState.ClassDictionary.Add(pair.Key, newClass);
				}
			}
		}

		private static void AddDependentTypes()
		{
			Dictionary<string, UnityClass> dict = SharedState.ClassDictionary;
			string[] types = dict.Keys.ToArray();
			foreach (string typeName in types)
			{
				AddDependentTypes(dict[typeName].EditorRootNode, editorRootNodes);
				AddDependentTypes(dict[typeName].ReleaseRootNode, releaseRootNodes);
			}
		}

		private static void AddDependentTypes(UnityNode tree, Dictionary<string, UnityNode> dict)
		{
			if (tree?.SubNodes == null)
				return;

			foreach (var subnode in tree.SubNodes)
			{
				string typeName = subnode.TypeName;
				if (!primitiveNames.Contains(typeName) && !SharedState.ClassDictionary.ContainsKey(typeName) && !dict.ContainsKey(typeName))
				{
					if (!generics.Contains(typeName))
					{
						var newNode = subnode.DeapClone();
						newNode.Name = "Base";
						dict.Add(typeName, newNode);
					}

					AddDependentTypes(subnode, dict);
				}
			}
		}

		private static Dictionary<string, UnityNode> releaseRootNodes = new Dictionary<string, UnityNode>();
		private static Dictionary<string, UnityNode> editorRootNodes = new Dictionary<string, UnityNode>();
	}
}