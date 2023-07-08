using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass040_AddEnums
	{
		private static readonly HashSet<string?> namespaceBlacklist = new()
		{
			"UnityEngine.Yoga",
		};
		private static readonly HashSet<string> fullNameBlackList = new()
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
				if (fullNameBlackList.Contains(fullName) || namespaceBlacklist.Contains(enumHistory.Namespace))
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
					enumHistory.GetFields().Order(EnumFieldComparer.Instance),
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
				if (!firsts.Add(enumHistory.Name))
				{
					seconds.TryAdd(enumHistory.Name, 0);
				}
			}
			return seconds;
		}

		private sealed class EnumFieldComparer : IComparer<KeyValuePair<string, long>>
		{
			private EnumFieldComparer() { }

			public static EnumFieldComparer Instance { get; } = new();

			int IComparer<KeyValuePair<string, long>>.Compare(KeyValuePair<string, long> x, KeyValuePair<string, long> y)
			{
				return Compare(x, y);
			}

			/// <summary>
			/// Compare two enum fields
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns>
			/// <paramref name="x"/> &lt; <paramref name="y"/> : -1<br/>
			/// <paramref name="x"/> == <paramref name="y"/> : 0<br/>
			/// <paramref name="x"/> &gt; <paramref name="y"/> : 1<br/>
			/// </returns>
			public static int Compare(KeyValuePair<string, long> x, KeyValuePair<string, long> y)
			{
				if (x.Value != y.Value)
				{
					return x.Value < y.Value ? -1 : 1;
				}
				else
				{
					return x.Key.CompareTo(y.Key);
				}
			}
		}
	}
}
