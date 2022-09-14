using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass040_AddEnums
	{
		private static readonly HashSet<string> blackList = new()
		{
			"UnityEditor.PackageManager.LogLevel",
			"UnityEngine.LogOption",
			"UnityEngine.LogType",
		};

		private static readonly Dictionary<string, (TypeDefinition, EnumHistory)> enumDictionary = new();
		internal static IReadOnlyDictionary<string, (TypeDefinition, EnumHistory)> EnumDictionary => enumDictionary;
		public static void DoPass()
		{
			IMethodDefOrRef flagsConstructor = SharedState.Instance.Importer.ImportDefaultConstructor<FlagsAttribute>();
			int count = 0;
			Dictionary<string, int> duplicateNames = GetDuplicateNames(SharedState.Instance.HistoryFile.Enums.Values);
			foreach ((string fullName, EnumHistory enumHistory) in SharedState.Instance.HistoryFile.Enums)
			{
				if (blackList.Contains(fullName))
				{
					continue;
				}
				if (!enumHistory.TryGetMergedElementType(out ElementType elementType))
				{
					Console.WriteLine($"Could not convert {enumHistory.FullName} to IL because it has incompatible underlying types.");
					continue;
				}

				string enumName;
				if (duplicateNames.TryGetValue(enumHistory.Name, out int index))
				{
					enumName = $"{enumHistory.Name}_{index}";
					duplicateNames[enumHistory.Name] = index + 1;
				}
				else
				{
					enumName = enumHistory.Name;
				}

				TypeDefinition type = EnumCreator.CreateFromDictionary(
					SharedState.Instance,
					SharedState.EnumsNamespace,
					enumName,
					GetFields(enumHistory),
					elementType.ToEnumUnderlyingType());

				if (enumHistory.IsFlagsEnum)
				{
					type.AddCustomAttribute(flagsConstructor);
				}

				enumDictionary.Add(fullName, (type, enumHistory));

				count++;
			}
			Console.WriteLine($"\t{count} generated enums.");
		}

		private static Dictionary<string, int> GetDuplicateNames(IEnumerable<EnumHistory> enums)
		{
			HashSet<string> firsts = new();
			Dictionary<string, int> seconds = new();
			foreach (EnumHistory enumHistory in enums)
			{
				if (firsts.Contains(enumHistory.Name))
				{
					seconds.TryAdd(enumHistory.Name, 0);
				}
				else
				{
					firsts.Add(enumHistory.Name);
				}
			}
			return seconds;
		}

		private static IEnumerable<KeyValuePair<string, long>> GetFields(EnumHistory history)
		{
			HashSet<KeyValuePair<string, long>> set = new();
			foreach (EnumMemberHistory member in history.Members.Values)
			{
				if (member.Value.Count == 1)
				{
					set.Add(new KeyValuePair<string, long>(member.Name, member.Value[0].Value));
				}
				else
				{
					foreach (long value in member.Value.Values)
					{
						string fieldName = GetEnumFieldName(member.Name, value);
						set.Add(new KeyValuePair<string, long>(fieldName, value));
					}
				}
			}
			var list = set.ToList();
			list.Sort(CompareEnumFields);
			return list;
		}

		private static string GetEnumFieldName(string name, long value)
		{
			return value < 0 ? $"{name}_N{-value}" : $"{name}_{value}";
		}

		/// <summary>
		/// Compare two enum fields
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>
		/// <paramref name="a"/> &lt; <paramref name="b"/> : -1<br/>
		/// <paramref name="a"/> == <paramref name="b"/> : 0<br/>
		/// <paramref name="a"/> &gt; <paramref name="b"/> : 1<br/>
		/// </returns>
		private static int CompareEnumFields(KeyValuePair<string, long> a, KeyValuePair<string, long> b)
		{
			if (a.Value != b.Value)
			{
				return a.Value < b.Value ? -1 : 1;
			}
			else
			{
				return a.Key.CompareTo(b.Key);
			}
		}
	}
}
