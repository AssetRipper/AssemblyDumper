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
			"long long",//long in C#
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
		public static void DoPass()
		{
			Logger.Info("Pass 4: Extract Dependent Node Trees");
		}
		/*
		private static void AddDependentTypes(Dictionary<string, NodeTree> dict)
		{
			string[] types = dict.Keys.ToArray();
			foreach (string typeName in types)
			{
				AddDependentTypes(dict[typeName], dict);
			}
		}

		private static void AddDependentTypes(NodeTree tree, Dictionary<string, NodeTree> dict)
		{
			foreach(var subnode in tree.Root.SubNodes)
			{
				string typeName = subnode.TypeName;
				if (!primitives.Contains(typeName) && !dict.ContainsKey(typeName))
				{
					var newTree = new NodeTree(subnode);
					dict.Add(typeName, newTree);
					AddDependentTypes(newTree, dict);
				}
			}
		}*/
	}
}
