using AssemblyDumper.Unity;
using System;
using System.Linq;
using System.Text.RegularExpressions;

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
				unityClass.CorrectInheritedTypeNames();
				unityClass.EditorRootNode.FixNamesRecursively();
				unityClass.ReleaseRootNode.FixNamesRecursively();
			}
		}

		/// <summary>
		/// Corrects the root nodes of classes to have the correct Type Name.<br/>
		/// For example, Behaviour uses Component as its type name in the root nodes
		/// </summary>
		/// <param name="unityClass"></param>
		private static void CorrectInheritedTypeNames(this UnityClass unityClass)
		{
			if(unityClass.EditorRootNode != null && unityClass.EditorRootNode.TypeName != unityClass.Name)
			{
				//Console.WriteLine($"Correcting editor type name from {unityClass.EditorRootNode.TypeName} to {unityClass.Name}");
				unityClass.EditorRootNode.TypeName = unityClass.Name;
			}
			if (unityClass.ReleaseRootNode != null && unityClass.ReleaseRootNode.TypeName != unityClass.Name)
			{
				//Console.WriteLine($"Correcting release type name from {unityClass.ReleaseRootNode.TypeName} to {unityClass.Name}");
				unityClass.ReleaseRootNode.TypeName = unityClass.Name;
			}
		}

		/// <summary>
		/// Uses a regex to replace invalid characters with an underscore, ie data[0] to data_0_
		/// </summary>
		/// <param name="node"></param>
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
