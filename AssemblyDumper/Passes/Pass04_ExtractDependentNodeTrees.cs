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

		/// <summary>
		/// OriginalTypeName : List(newTypeName,releaseRoot,editorRoot)
		/// </summary>
		private static readonly Dictionary<string, List<(string, UnityNode, UnityNode)>> generatedTypes = new Dictionary<string, List<(string, UnityNode, UnityNode)>>();

		public static void DoPass()
		{
			Logger.Info("Pass 4: Extract Dependent Node Trees");
			AddDependentTypes();
			CreateNewClasses();
			CheckCompatibility();
		}

		private static void CreateNewClasses()
		{
			foreach (var variantList in generatedTypes)
			{
				foreach(var variant in variantList.Value)
				{
					var newClass = new UnityClass(variant.Item2, variant.Item3);
					newClass.Name = variant.Item1;
					newClass.FullName = variant.Item1;
					SharedState.ClassDictionary.Add(variant.Item1, newClass);
				}
			}
		}

		private static void CheckCompatibility()
		{
			foreach (var pair in SharedState.ClassDictionary)
			{
				UnityClass unityClass = pair.Value;
				if(!AreCompatible(unityClass.ReleaseRootNode, unityClass.EditorRootNode, false))
				{
					Logger.Info($"{unityClass.Name} has incompatible release and editor root nodes");
				}
			}
		}

		private static void AddDependentTypes()
		{
			foreach(var pair in SharedState.ClassDictionary)
			{
				AddDependentTypes(pair.Value);
			}
		}

		private static void AddDependentTypes(UnityClass unityClass) => AddDependentTypes(unityClass.ReleaseRootNode, unityClass.EditorRootNode);
		private static void AddDependentTypes(UnityNode releaseNode, UnityNode editorNode)
		{
			List<(UnityNode, UnityNode)> fieldList = GenerateFieldDictionary(releaseNode, editorNode);
			foreach((UnityNode releaseField,UnityNode editorField) in fieldList)
			{
				string typeName = releaseField?.TypeName ?? editorField.TypeName;
				if (primitiveNames.Contains(typeName))
					continue;

				AddDependentTypes(releaseField, editorField);

				if (generatedTypes.TryGetValue(typeName, out var list))
				{
					bool alreadyCovered = false;
					int relevantIndex = -1;
					for(int i = 0; i < list.Count; i++)
					{
						(string elementTypeName, UnityNode releaseTypeNode, UnityNode editorTypeNode) = list[i];
						bool releaseIsEqual = AreEqual(releaseField, releaseTypeNode, true);
						bool editorIsEqual = AreEqual(editorField, editorTypeNode, true);

						bool isUsuable = (releaseIsEqual && editorIsEqual) ||
							(releaseIsEqual && editorField == null) ||
							(editorIsEqual && releaseField == null);

						if (isUsuable)
						{
							alreadyCovered = true;
							relevantIndex = i;
							break;
						}

						bool incomingEditorExistingRelease = releaseField == null && releaseTypeNode != null &&
							editorField != null && editorTypeNode == null &&
							AreCompatible(releaseTypeNode, editorField, true);

						bool existingHasIdenticalReleaseButNoEditor = releaseIsEqual && editorTypeNode == null && editorField != null;

						if (existingHasIdenticalReleaseButNoEditor || incomingEditorExistingRelease)
						{
							var newEditorNode = editorField.DeepClone();
							newEditorNode.Name = "Base";
							newEditorNode.AlternateTypeName = elementTypeName;
							newEditorNode.RecalculateLevel(0); //Needed for type tree method generation

							list[i] = (elementTypeName, releaseTypeNode, newEditorNode);

							alreadyCovered = true;
							relevantIndex = i;
							break;
						}

						bool incomingReleaseExistingEditor = releaseField != null && releaseTypeNode == null &&
							editorField == null && editorTypeNode != null &&
							AreCompatible(releaseField, editorTypeNode, true);

						bool existingHasIdenticalEditorButNoRelease = editorIsEqual && releaseTypeNode == null && releaseField != null;

						if (existingHasIdenticalEditorButNoRelease || incomingReleaseExistingEditor)
						{
							var newReleaseNode = releaseField.DeepClone();
							newReleaseNode.Name = "Base";
							newReleaseNode.AlternateTypeName = elementTypeName;
							newReleaseNode.RecalculateLevel(0); //Needed for type tree method generation

							list[i] = (elementTypeName, newReleaseNode, editorTypeNode);

							alreadyCovered = true;
							relevantIndex = i;
							break;
						}
					}

					if (!alreadyCovered)
					{
						relevantIndex = list.Count;
						string newTypeName = $"{typeName}_{list.Count}";
						var newReleaseNode = releaseField?.DeepClone();
						if (newReleaseNode != null)
						{
							newReleaseNode.Name = "Base";
							newReleaseNode.AlternateTypeName = newTypeName;
							newReleaseNode.RecalculateLevel(0); //Needed for type tree method generation
						}
						var newEditorNode = editorField?.DeepClone();
						if (newEditorNode != null)
						{
							newEditorNode.Name = "Base";
							newEditorNode.AlternateTypeName = newTypeName;
							newEditorNode.RecalculateLevel(0); //Needed for type tree method generation
						}
						list.Add((newTypeName, newReleaseNode, newEditorNode));
					}

					if (relevantIndex != 0)
					{
						if (releaseField != null)
						{
							//Logger.Info($"Release field {releaseField.Name} of type {releaseField.TypeName} renamed to {list[relevantIndex].Item1}");
							releaseField.AlternateTypeName = list[relevantIndex].Item1;
						}
						if (editorField != null)
						{
							//Logger.Info($"Editor field {editorField.Name} of type {editorField.TypeName} renamed to {list[relevantIndex].Item1}");
							editorField.AlternateTypeName = list[relevantIndex].Item1;
						}
					}
				}
				else
				{
					if (!generics.Contains(typeName) && !SharedState.ClassDictionary.ContainsKey(typeName))
					{
						var newReleaseNode = releaseField?.DeepClone();
						if(newReleaseNode != null)
						{
							newReleaseNode.Name = "Base";
							newReleaseNode.RecalculateLevel(0); //Needed for type tree method generation
						}
						var newEditorNode = editorField?.DeepClone();
						if(newEditorNode != null)
						{
							newEditorNode.Name = "Base";
							newEditorNode.RecalculateLevel(0); //Needed for type tree method generation
						}
						var newList = new List<(string,UnityNode,UnityNode)>();
						newList.Add((typeName, newReleaseNode, newEditorNode));
						generatedTypes.Add(typeName, newList);
					}
				}
			}
		}
		
		private static List<(UnityNode,UnityNode)> GenerateFieldDictionary(UnityNode releaseRoot, UnityNode editorRoot)
		{
			if(releaseRoot?.SubNodes == null)
			{
				var func = new Func<UnityNode, (UnityNode, UnityNode)>(node => (null, node));
				return editorRoot?.SubNodes?.Select(func).ToList() ?? new List<(UnityNode, UnityNode)>();
			}
			else if (editorRoot?.SubNodes == null)
			{
				var func = new Func<UnityNode, (UnityNode, UnityNode)>(node => (node, null));
				return releaseRoot?.SubNodes?.Select(func).ToList() ?? new List<(UnityNode, UnityNode)>();
			}
			var result = new Dictionary<string, (UnityNode, UnityNode)>();
			foreach(UnityNode releaseNode in releaseRoot.SubNodes)
			{
				UnityNode editorNode = editorRoot.SubNodes.FirstOrDefault(node => node.Name == releaseNode.Name);
				result.Add(releaseNode.Name, (releaseNode, editorNode));
			}
			foreach(UnityNode editorNode in editorRoot.SubNodes)
			{
				if (!result.ContainsKey(editorNode.Name))
				{
					result.Add(editorNode.Name, (null, editorNode));
				}
			}
			return result.Values.ToList();
		}

		private static void RecalculateLevel(this UnityNode node, int depth)
		{
			node.Level = (byte)depth;
			foreach(var subNode in node.SubNodes)
			{
				RecalculateLevel(subNode, depth + 1);
			}
		}

		private static bool AreEqual(UnityNode left, UnityNode right, bool root)
		{
			if(left == null || right == null)
			{
				return left == null && right == null;
			}
			if(!root && left.Name != right.Name) //The root nodes will not have the same name if one has already been renamed to "Base"
			{
				return false;
			}
			if (left.TypeName != right.TypeName)
			{
				//Logger.Info($"\tInequal because type name {left.TypeName} doesn't match {right.TypeName}");
				return false;
			}
			if (left.SubNodes.Count != right.SubNodes.Count)
			{
				//Logger.Info($"\tInequal because subnode count {left.SubNodes.Count} doesn't match {right.SubNodes.Count}");
				return false;
			}
			for (int i = 0; i < left.SubNodes.Count; i++)
			{
				if(!AreEqual(left.SubNodes[i], right.SubNodes[i], false))
					return false;
			}
			return true;
		}

		private static bool AreCompatible(UnityNode releaseNode, UnityNode editorNode, bool root)
		{
			if (releaseNode == null || editorNode == null)
				return true;
			if(!root && releaseNode.Name != editorNode.Name) //The root nodes will not have the same name if one has already been renamed to "Base"
				return false;
			if(releaseNode.TypeName != editorNode.TypeName)
				return false;
			if (releaseNode.SubNodes == null || editorNode.SubNodes == null)
				return true;
			var releaseFields = releaseNode.SubNodes.ToDictionary(x => x.Name, x => x);
			var editorFields = editorNode.SubNodes.ToDictionary(x => x.Name, x => x);
			foreach(var releasePair in releaseFields)
			{
				if(editorFields.TryGetValue(releasePair.Key, out UnityNode editorField))
				{
					if(!AreCompatible(releasePair.Value, editorField, false))
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}