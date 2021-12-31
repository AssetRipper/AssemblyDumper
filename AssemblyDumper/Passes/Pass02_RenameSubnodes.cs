using AssemblyDumper.Unity;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyDumper.Passes
{
	public static class Pass02_RenameSubnodes
	{
		private static readonly Regex badCharactersRegex = new Regex(@"[<>\[\]\s&\(\):]", RegexOptions.Compiled);
		private const string OffsetPtrName = "OffsetPtr";
		private const string KeyframeName = "Keyframe";
		private const string AnimationCurveName = "AnimationCurve";
		private const string ColorRGBAName = "ColorRGBA";
		private const string PackedBitVectorName = "PackedBitVector";

		public static void DoPass()
		{
			Console.WriteLine("Pass 2: Rename Class Subnodes");
			foreach(UnityClass unityClass in SharedState.ClassDictionary.Values)
			{
				unityClass.CorrectInheritedTypeNames();
				unityClass.EditorRootNode.FixNamesRecursively();
				unityClass.ReleaseRootNode.FixNamesRecursively();
				unityClass.EditorRootNode.DoSecondaryRenamingRecursively();
				unityClass.ReleaseRootNode.DoSecondaryRenamingRecursively();
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

		/// <summary>
		/// Replace unusable characters in a string with the underscore
		/// </summary>
		/// <param name="originalName"></param>
		/// <returns></returns>
		private static string GetValidName(string originalName)
		{
			if (string.IsNullOrWhiteSpace(originalName))
			{
				throw new ArgumentException("Nodes cannot have a null or whitespace type name", nameof(originalName));
			}
			string result = badCharactersRegex.Replace(originalName, "_");
			if (char.IsDigit(result[0]))
			{
				result = "_" + result;
			}
			return result;
		}

		private static void DoSecondaryRenamingRecursively(this UnityNode node)
		{
			if (node == null)
			{
				return;
			}

			if (node.SubNodes != null)
			{
				foreach (UnityNode subnode in node.SubNodes)
				{
					subnode.DoSecondaryRenamingRecursively();
				}
			}

			node.DoSecondaryRenaming();
		}

		private static void DoSecondaryRenaming(this UnityNode node)
		{
			if (node.IsOffsetPtr(out string offsetPtrElement))
			{
				node.TypeName = $"{OffsetPtrName}_{offsetPtrElement}";
			}
			else if (node.IsKeyframe(out string keyframeElement))
			{
				node.TypeName = $"{KeyframeName}_{keyframeElement}";
			}
			else if (node.IsAnimationCurve(out string animationCurveElement))
			{
				node.TypeName = $"{AnimationCurveName}_{animationCurveElement}";
			}
			else if (node.IsColorRGBA(out string newColorName))
			{
				node.TypeName = newColorName;
			}
			else if (node.IsPackedBitVector(out string newBitVectorName))
			{
				node.TypeName = newBitVectorName;
			}
			else if (node.IsEditorScene())
			{
				node.TypeName = "EditorScene";
			}
		}

		private static bool IsOffsetPtr(this UnityNode node, out string elementType)
		{
			var subnodes = node.SubNodes;
			if (node.TypeName == OffsetPtrName && subnodes != null && subnodes.Count == 1 && subnodes[0].Name == "data")
			{
				elementType = subnodes[0].TypeName;
				return true;
			}

			elementType = null;
			return false;
		}

		private static bool IsKeyframe(this UnityNode node, out string elementType)
		{
			var subnodes = node.SubNodes;
			if (node.TypeName == KeyframeName && subnodes != null && subnodes.Any(n => n.Name == "value"))
			{
				elementType = subnodes.Single(n => n.Name == "value").TypeName;
				return true;
			}

			elementType = null;
			return false;
		}

		private static bool IsAnimationCurve(this UnityNode node, out string elementType)
		{
			elementType = null;

			if(node.TypeName != AnimationCurveName)
				return false;
			
			var subnodes = node.SubNodes;
			if(subnodes == null)
				return false;
			
			UnityNode curveNode = subnodes.SingleOrDefault(subnode => subnode.Name == "m_Curve");
			if (curveNode == null || curveNode.TypeName != "vector")
				return false;
			UnityNode keyframeNode = curveNode.SubNodes[0].SubNodes[1];
			
			if(!keyframeNode.TypeName.StartsWith($"{KeyframeName}_"))
				return false;
			
			elementType = keyframeNode.TypeName.Substring(KeyframeName.Length + 1);
			return true;
		}

		private static bool IsColorRGBA(this UnityNode node, out string newName)
		{
			newName = null;

			if (node.TypeName != ColorRGBAName)
				return false;

			var subnodes = node.SubNodes;
			if (subnodes == null)
				return false;

			if (subnodes.Count == 4 && subnodes.All(n => n.TypeName == "float"))
			{
				newName = $"{ColorRGBAName}f";
				return true;
			}

			if (subnodes.Count == 1 && subnodes[0].Name == "rgba")
			{
				newName = $"{ColorRGBAName}32";
				return true;
			}

			return false;
		}

		private static bool IsPackedBitVector(this UnityNode node, out string newName)
		{
			newName = null;

			if (node.TypeName != PackedBitVectorName)
				return false;

			var subnodes = node.SubNodes;
			if (subnodes == null)
				return false;

			//The packed bit vectors are constant throughout all the unity versions and identifiable by their number of fields
			if (subnodes.Count == 5)
			{
				newName = $"{PackedBitVectorName}_float";
				return true;
			}
			if (subnodes.Count == 3)
			{
				newName = $"{PackedBitVectorName}_int";
				return true;
			}
			if (subnodes.Count == 2)
			{
				newName = $"{PackedBitVectorName}_Quaternionf";
				return true;
			}

			return false;
		}

		private static bool IsEditorScene(this UnityNode node)
		{
			var subnodes = node.SubNodes;
			if (node.TypeName == "Scene" && subnodes != null && subnodes.Any(n => n.Name == "enabled") && subnodes.Any(n => n.Name == "path"))
			{
				return true;
			}
			return false;
		}
	}
}
