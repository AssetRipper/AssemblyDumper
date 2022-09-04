using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static partial class Pass007_ExtractSubclasses
	{
		private readonly struct SubclassCandidate
		{
			public readonly UniversalNode? ReleaseNode;
			public readonly UniversalNode? EditorNode;
			public readonly string Name;
			public readonly UnityVersionRange VersionRange;
			public readonly UniversalNode[] NodesToBeAltered;

			public SubclassCandidate(UniversalNode? releaseNode, UniversalNode? editorNode, Range<UnityVersion> versionRange) : this()
			{
				if (releaseNode is null && editorNode is null)
				{
					throw new Exception("Release and editor can't both be null");
				}

				ReleaseNode = releaseNode;
				EditorNode = editorNode;
				VersionRange = versionRange;
				Name = releaseNode?.TypeName ?? editorNode?.TypeName ?? throw new Exception("All type names were null");
				if(releaseNode is null)
				{
					NodesToBeAltered = new UniversalNode[] { editorNode! };
				}
				else if (editorNode is null)
				{
					NodesToBeAltered = new UniversalNode[] { releaseNode };
				}
				else
				{
					NodesToBeAltered = new UniversalNode[] { releaseNode, editorNode };
				}
			}

			private SubclassCandidate(UniversalNode? releaseNode, UniversalNode? editorNode, string name, Range<UnityVersion> versionRange, UniversalNode[] nodesToBeAltered)
			{
				if (releaseNode is null && editorNode is null)
				{
					throw new Exception("Release and editor can't both be null");
				}

				ReleaseNode = releaseNode;
				EditorNode = editorNode;
				Name = name;
				VersionRange = versionRange;
				NodesToBeAltered = nodesToBeAltered;
			}

			public bool Contains(SubclassCandidate candidate)
			{
				return Name == candidate.Name &&
					VersionRange.Contains(candidate.VersionRange) &&
					(candidate.ReleaseNode is null || UniversalNodeComparer.Equals(ReleaseNode, candidate.ReleaseNode, true)) &&
					(candidate.EditorNode is null || UniversalNodeComparer.Equals(EditorNode, candidate.EditorNode, true));
			}

			public bool CanMerge(SubclassCandidate candidate)
			{
				return Name == candidate.Name &&
					VersionRange.CanUnion(candidate.VersionRange) &&
					UniversalNodeComparer.Equals(ReleaseNode, candidate.ReleaseNode, true) &&
					UniversalNodeComparer.Equals(EditorNode, candidate.EditorNode, true);
			}

			public bool CanMergeRelaxed(SubclassCandidate candidate)
			{
				return Name == candidate.Name &&
					VersionRange.CanUnion(candidate.VersionRange) &&
					(candidate.ReleaseNode is null || ReleaseNode is null || UniversalNodeComparer.Equals(ReleaseNode, candidate.ReleaseNode, true)) &&
					(candidate.EditorNode is null || EditorNode is null || UniversalNodeComparer.Equals(EditorNode, candidate.EditorNode, true));
			}

			public SubclassCandidate Merge(SubclassCandidate candidate)
			{
				UniversalNode[] nodes = new UniversalNode[NodesToBeAltered.Length + candidate.NodesToBeAltered.Length];
				Array.Copy(NodesToBeAltered, nodes, NodesToBeAltered.Length);
				Array.Copy(candidate.NodesToBeAltered, 0, nodes, NodesToBeAltered.Length, candidate.NodesToBeAltered.Length);
				UnityVersionRange range = VersionRange.MakeUnion(candidate.VersionRange);
				return new SubclassCandidate(ReleaseNode ?? candidate.ReleaseNode, EditorNode ?? candidate.EditorNode, Name, range, nodes);
			}
		}

		private static void AddClassesToSharedStateSubclasses(List<SubclassCandidate> unprocessedList)
		{
			List<SubclassCandidate> consolidatedCandidates = ProcessList(unprocessedList);
			if(consolidatedCandidates.Count == 0)
			{
				throw new Exception("Candidate ount can't be zero");
			}
			else if (consolidatedCandidates.Count == 1)
			{
				//Single class, don't change the name
				SubclassCandidate candidate = consolidatedCandidates[0];
				VersionedList<UniversalClass> classList = new();
				SharedState.Instance.SubclassInformation.Add(candidate.Name, classList);
				UniversalClass newClass = new UniversalClass(candidate.ReleaseNode?.ShallowCloneAsRootNode(), candidate.EditorNode?.ShallowCloneAsRootNode());
				classList.Add(candidate.VersionRange.Start, newClass);
				if(candidate.VersionRange.End != UnityVersion.MaxVersion)
				{
					classList.Add(candidate.VersionRange.End, null);
				}
			}
			else if (AnyIntersections(consolidatedCandidates))
			{
				Console.WriteLine($"Using conflict naming for {consolidatedCandidates[0].Name}");
				//Use _2 naming
				for(int i = 0; i < consolidatedCandidates.Count; i++)
				{
					SubclassCandidate candidate = consolidatedCandidates[i];
					string typeName = $"{candidate.Name}_{i}";
					VersionedList<UniversalClass> classList = new();
					SharedState.Instance.SubclassInformation.Add(typeName, classList);
					foreach(UniversalNode node in candidate.NodesToBeAltered)
					{
						node.TypeName = typeName;
					}
					UniversalClass newClass = new UniversalClass(candidate.ReleaseNode?.ShallowCloneAsRootNode(), candidate.EditorNode?.ShallowCloneAsRootNode());
					classList.Add(candidate.VersionRange.Start, newClass);
					if (candidate.VersionRange.End != UnityVersion.MaxVersion)
					{
						classList.Add(candidate.VersionRange.End, null);
					}
					else if (candidate.Name.StartsWith("PPtr"))
					{
						Console.WriteLine($"{candidate.Name} has unresolved conflicts and has no end version");
					}
				}
			}
			else
			{
				//Use _3_4_0f5 naming
				VersionedList<UniversalClass> classList = new();
				SharedState.Instance.SubclassInformation.Add(consolidatedCandidates[0].Name, classList);
				SubclassCandidate[] candidates = consolidatedCandidates.OrderBy(c => c.VersionRange.Start).ToArray();
				for(int i = 0; i < candidates.Length; i++)
				{
					SubclassCandidate candidate = candidates[i];
					UniversalClass newClass = new UniversalClass(candidate.ReleaseNode?.ShallowCloneAsRootNode(), candidate.EditorNode?.ShallowCloneAsRootNode());
					classList.Add(candidate.VersionRange.Start, newClass);
					if(i + 1 < candidates.Length && candidate.VersionRange.End != candidates[i + 1].VersionRange.Start)
					{
						classList.Add(candidate.VersionRange.End, null);
					}
				}
				if (candidates[candidates.Length - 1].VersionRange.End != UnityVersion.MaxVersion)
				{
					classList.Add(candidates[candidates.Length - 1].VersionRange.End, null);
				}
			}
		}

		private static bool AnyIntersections(List<SubclassCandidate> consolidatedCandidates)
		{
			for(int i = 0; i < consolidatedCandidates.Count; i++)
			{
				for(int j = i + 1; j < consolidatedCandidates.Count; j++)
				{
					if (consolidatedCandidates[i].VersionRange.Intersects(consolidatedCandidates[j].VersionRange))
					{
						Console.WriteLine($"{consolidatedCandidates[i].VersionRange} intersects with {consolidatedCandidates[j].VersionRange}");
						return true;
					}
				}
			}
			return false;
		}

		private static List<SubclassCandidate> ProcessList(List<SubclassCandidate> unprocessedList)
		{
			SplitByNullability(unprocessedList, out List<SubclassCandidate> bothList, out List<SubclassCandidate> releaseList, out List<SubclassCandidate> editorList);
			bothList = GetConsolidatedList(bothList);
			MergeIntoBothList(bothList, releaseList);
			MergeIntoBothList(bothList, editorList);
			releaseList = GetConsolidatedList(releaseList);
			editorList = GetConsolidatedList(editorList);
			FinalMergeAttempts(bothList, releaseList, editorList);
			List<SubclassCandidate> unifiedList = new List<SubclassCandidate>(bothList.Count + releaseList.Count + editorList.Count);
			unifiedList.AddRange(bothList);
			unifiedList.AddRange(releaseList);
			unifiedList.AddRange(editorList);
			return unifiedList;
		}

		private static void SplitByNullability(List<SubclassCandidate> inputList, 
			out List<SubclassCandidate> bothList, 
			out List<SubclassCandidate> releaseList,
			out List<SubclassCandidate> editorList)
		{
			bothList = inputList.Where(c => c.EditorNode is not null && c.ReleaseNode is not null).ToList();
			releaseList = inputList.Where(c => c.EditorNode is null && c.ReleaseNode is not null).ToList();
			editorList = inputList.Where(c => c.EditorNode is not null && c.ReleaseNode is null).ToList();
		}

		private static bool TryMergeClasses(List<SubclassCandidate> inputList, out List<SubclassCandidate> outputList)
		{
			outputList = new();
			bool result = false;
			foreach (SubclassCandidate candidate in inputList)
			{
				bool merged = false;
				for(int i = 0; i < outputList.Count; i++)
				{
					if (outputList[i].CanMerge(candidate))
					{
						result = true;
						merged = true;
						outputList[i] = outputList[i].Merge(candidate);
						break;
					}
				}
				if (!merged)
				{
					outputList.Add(candidate);
				}
			}
			return result;
		}

		private static List<SubclassCandidate> GetConsolidatedList(List<SubclassCandidate> inputList)
		{
			List<SubclassCandidate> outputList = inputList;
			bool shouldTryAgain = true;
			while (shouldTryAgain)
			{
				shouldTryAgain = TryMergeClasses(outputList, out List<SubclassCandidate> newOutput);
				outputList = newOutput;
			}
			return outputList;
		}

		private static void MergeIntoBothList(List<SubclassCandidate> bothList, List<SubclassCandidate> singleList)
		{
			List<SubclassCandidate> leftovers = new();
			foreach(SubclassCandidate singleCandidate in singleList)
			{
				bool successful = false;
				for(int i = 0; i < bothList.Count; i++)
				{
					if (bothList[i].Contains(singleCandidate))
					{
						successful = true;
						bothList[i] = bothList[i].Merge(singleCandidate);
						break;
					}
				}
				if (!successful)
				{
					leftovers.Add(singleCandidate);
				}
			}
			singleList.Clear();
			singleList.Capacity = leftovers.Count;
			singleList.AddRange(leftovers);
		}

		private static void FinalMergeAttempts(List<SubclassCandidate> bothList, List<SubclassCandidate> releaseList, List<SubclassCandidate> editorList)
		{
			if(bothList.Count == 0)
			{
				if(releaseList.Count == 1 && editorList.Count == 1)
				{
					SubclassCandidate releaseCandidate = releaseList[0];
					SubclassCandidate editorCandidate = editorList[0];
					if (releaseCandidate.Name == editorCandidate.Name
						&& releaseCandidate.VersionRange.CanUnion(editorCandidate.VersionRange)
						&& AreCompatible(releaseCandidate.ReleaseNode!, editorCandidate.EditorNode!, true))
					{
						SubclassCandidate mergedCandidate = releaseCandidate.Merge(editorCandidate);
						releaseList.Clear();
						editorList.Clear();
						bothList.Add(mergedCandidate);
					}
				}
			}
			else if (bothList.Count == 1)
			{
				if (releaseList.Count == 1 && editorList.Count == 0)
				{
					SubclassCandidate releaseCandidate = releaseList[0];
					SubclassCandidate bothCandidate = bothList[0];
					if (bothCandidate.CanMergeRelaxed(releaseCandidate))
					{
						SubclassCandidate mergedCandidate = bothCandidate.Merge(releaseCandidate);
						releaseList.Clear();
						bothList[0] = mergedCandidate;
					}
				}
				else if (releaseList.Count == 0 && editorList.Count == 1)
				{
					SubclassCandidate editorCandidate = editorList[0];
					SubclassCandidate bothCandidate = bothList[0];
					if (bothCandidate.CanMergeRelaxed(editorCandidate))
					{
						SubclassCandidate mergedCandidate = bothCandidate.Merge(editorCandidate);
						editorList.Clear();
						bothList[0] = mergedCandidate;
					}
				}
			}
		}
	}
}
