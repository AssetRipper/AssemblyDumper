using AssemblyDumper.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyDumper.Passes
{
	public static class Pass02_RenameSubnodes
	{
		private static readonly Regex badCharactersRegex = new Regex(@"[<>\[\]\s&\(\):]", RegexOptions.Compiled);

		public static void DoPass()
		{
			Console.WriteLine("Pass 2: Rename Class Subnodes");
			foreach(UnityClass unityClass in SharedState.ClassDictionary.Values)
			{
				unityClass.EditorRootNode.FixNamesRecursively();
				unityClass.ReleaseRootNode.FixNamesRecursively();
			}
		}

		private static void FixNamesRecursively(this UnityNode node)
		{
			if(node == null)
			{
				return;
			}

			node.OriginalName = node.Name;
			node.Name = GetValidName(node.Name);
			if (!PrimitiveTypes.primitives.Contains(node.TypeName))
			{
				node.OriginalTypeName = node.TypeName;
				node.TypeName = GetValidName(node.TypeName);
			}
			if(node.SubNodes != null)
			{
				foreach(UnityNode subnode in node.SubNodes)
				{
					FixNamesRecursively(subnode);
				}
			}
		}

		public static string GetValidName(string originalName)
		{
			return badCharactersRegex.Replace(originalName, "_");
		}
	}
}
