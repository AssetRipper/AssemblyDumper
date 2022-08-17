using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass556_CreateClassIDTypeEnum
	{
		public static Dictionary<FieldDefinition, ClassGroup> FieldGroupDictionary { get; } = new();
		public static TypeDefinition? ClassIdTypeDefintion { get; private set; }

		public static void DoPass()
		{
			Dictionary<string, int> nameDictionary = CreateDictionary();
			ClassIdTypeDefintion = EnumCreator.CreateFromDictionary(SharedState.Instance, SharedState.RootNamespace, "ClassIDType", nameDictionary);
			
			List<KeyValuePair<string, int>> alphabeticalList = nameDictionary.ToList();
			alphabeticalList.Sort((a,b) => a.Key.CompareTo(b.Key));
			TypeDefinition alphabeticalEnum = EnumCreator.CreateFromDictionary(SharedState.Instance, SharedState.RootNamespace, "ClassIDTypeAlphabetical", alphabeticalList);

			foreach (FieldDefinition field in ClassIdTypeDefintion.Fields)
			{
				if (field.IsStatic)
				{
					int id = nameDictionary[field.Name!];
					FieldGroupDictionary.Add(field, SharedState.Instance.ClassGroups[id]);
				}
			}
			foreach (FieldDefinition field in alphabeticalEnum.Fields)
			{
				if (field.IsStatic)
				{
					int id = nameDictionary[field.Name!];
					FieldGroupDictionary.Add(field, SharedState.Instance.ClassGroups[id]);
				}
			}
		}

		private static Dictionary<string, int> CreateDictionary()
		{
			Dictionary<int, string> rawDictionary = SharedState.Instance.ClassGroups.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value.Name);
			HashSet<string> duplicateNames = GetDuplicates(rawDictionary.Values);
			Dictionary<string, int> result = new Dictionary<string, int>(rawDictionary.Count);
			foreach((int id, string rawName) in rawDictionary)
			{
				if (duplicateNames.Contains(rawName))
				{
					result.Add($"{rawName}_{id}", id);
				}
				else
				{
					result.Add(rawName, id);
				}
			}
			return result;
		}

		private static HashSet<string> GetDuplicates(IEnumerable<string> rawStrings)
		{
			HashSet<string> uniqueStrings = new HashSet<string>();
			HashSet<string> duplicates = new HashSet<string>();
			foreach(string str in rawStrings)
			{
				if (!uniqueStrings.Contains(str))
				{
					uniqueStrings.Add(str);
				}
				else if (!duplicates.Contains(str))
				{
					duplicates.Add(str);
				}
			}
			return duplicates;
		}
	}
}
