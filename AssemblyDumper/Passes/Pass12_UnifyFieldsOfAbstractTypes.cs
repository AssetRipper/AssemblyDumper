using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;

namespace AssemblyDumper.Passes
{
	public static class Pass12_UnifyFieldsOfAbstractTypes
	{
		public static void DoPass()
		{
			Logger.Info("Pass 12: Merging fields of abstract types");
			//We need to get all abstract classes, and we need to do them in order of highest abstraction to lowest.
			//Easy part first - abstract
			var abstractClasses = SharedState.ClassDictionary.Values.Where(c => c.IsAbstract).ToList();

			//Now sort so that most-base classes are first, or more crucially, before any of their subclasses. 
			abstractClasses.Sort((a, b) => a.IsSubclassOf(b) ? 1 : b.IsSubclassOf(a) ? -1 : 0);

			foreach (var abstractClass in abstractClasses)
			{
				// Logger.Info($"\t{abstractClass.Name}");

				var derived = abstractClass.AllNonAbstractDerivedClasses();

				if (derived.Count == 0)
					continue;

				var derivedClass = derived.First();

				//Handle editor node
				abstractClass.EditorRootNode = new()
				{
					Name = "Base",
					TypeName = abstractClass.Name,
					Index = 0,
					Level = 0,
					SubNodes = new(),
					Version = 1,
				};
				for (var editorIdx = 0; editorIdx < derivedClass.EditorRootNode.SubNodes.Count; editorIdx++)
				{
					var subNode = derivedClass.EditorRootNode.SubNodes[editorIdx];

					var mismatching = derived.Where(d => d.EditorRootNode.SubNodes.Count <= editorIdx || d.EditorRootNode?.SubNodes[editorIdx].Name != subNode.Name).ToList();
					if (mismatching.Count >= Math.Ceiling(derived.Count / 10.0))
					{
						//Mismatch in at least one derived class, break out.
						// Logger.Info($"\t\tField {subNode.Name} not present at index {editorIdx} in {mismatching.Count} derived types, e.g. {mismatching.First().Name}");
						break;
					}

					//This field is common to all sub classes. Add it to base.
					// Logger.Info($"\t\tCopying field {subNode.Name} to EDITOR");
					abstractClass.EditorRootNode.SubNodes.Add(subNode.DeapClone());
				}

				//Handle release node
				abstractClass.ReleaseRootNode = new()
				{
					Name = "Base",
					TypeName = abstractClass.Name,
					Index = 0,
					Level = 0,
					SubNodes = new(),
					Version = 1,
				};
				for (var releaseIdx = 0; releaseIdx < derivedClass.ReleaseRootNode.SubNodes.Count; releaseIdx++)
				{
					var subNode = derivedClass.ReleaseRootNode.SubNodes[releaseIdx];

					var mismatching = derived.Where(d => d.ReleaseRootNode.SubNodes.Count <= releaseIdx || d.ReleaseRootNode?.SubNodes[releaseIdx].Name != subNode.Name).ToList();
					if (mismatching.Count >= Math.Ceiling(derived.Count / 10.0))
					{
						//Mismatch in at least one derived class, break out.
						// Logger.Info($"\t\tField {subNode.Name} not present at index {releaseIdx} in {mismatching.Count} derived types, e.g. {mismatching.First().Name}");
						break;
					}

					//This field is common to all sub classes. Add it to base.
					// Logger.Info($"\t\tCopying field {subNode.Name} to RELEASE");
					abstractClass.ReleaseRootNode.SubNodes.Add(subNode.DeapClone());
				}
			}
		}

		private static List<UnityClass> AllNonAbstractDerivedClasses(this UnityClass parent) => parent.Derived
			.Select(c => SharedState.ClassDictionary[c])
			.Where(c => !c.IsAbstract)
			.ToList();

		private static bool IsSubclassOf(this UnityClass potentialSubClass, UnityClass potentialSuperClass)
		{
			var baseTypeName = potentialSubClass.Base;
			while (!string.IsNullOrEmpty(baseTypeName))
			{
				if (baseTypeName == potentialSuperClass.Name)
					return true;

				baseTypeName = SharedState.ClassDictionary[baseTypeName].Base;
			}

			return false;
		}
	}
}