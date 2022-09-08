using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass350_AddEnums
	{
		internal static HistoryFile historyFile = new();
		internal readonly static Dictionary<TypeDefinition, EnumHistory> enumDictionary = new();
		public static void DoPass()
		{
			historyFile = HistoryFile.FromFile("consolidated.json");
			IMethodDefOrRef flagsConstructor = SharedState.Instance.Importer.ImportDefaultConstructor<FlagsAttribute>();
			int count = 0;
			Dictionary<string, int> duplicateNames = GetDuplicateNames(historyFile.Enums.Values);
			foreach (EnumHistory enumHistory in historyFile.Enums.Values)
			{
				if (enumHistory.ElementType.Count != 1)
				{
					Console.WriteLine($"Could not convert {enumHistory.FullName} to IL because it has multiple underlying types.");
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

				EnumUnderlyingType enumType = enumHistory.ElementType[0].Value.ToEnumUnderlyingType();

				TypeDefinition type = EnumCreator.CreateFromDictionary(
					SharedState.Instance,
					SharedState.EnumsNamespace,
					enumName,
					GetFields(enumHistory),
					enumType);

				if (enumHistory.IsFlagsEnum)
				{
					type.AddCustomAttribute(flagsConstructor);
				}

				enumDictionary.Add(type, enumHistory);

				count++;
			}
			Console.WriteLine($"{count} enums created.");
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

		private static HashSet<KeyValuePair<string, long>> GetFields(EnumHistory history)
		{
			HashSet<KeyValuePair<string, long>> result = new();
			foreach (EnumMemberHistory member in history.Members.Values)
			{
				if (member.Value.Count == 1)
				{
					result.Add(new KeyValuePair<string, long>(member.Name, member.Value[0].Value));
				}
				else
				{
					foreach (long value in member.Value.Values)
					{
						string fieldName = GetEnumFieldName(member.Name, value);
						result.Add(new KeyValuePair<string, long>(fieldName, value));
					}
				}
			}
			return result;
		}

		private static string GetEnumFieldName(string name, long value)
		{
			return value < 0 ? $"{name}_N{-value}" : $"{name}_{value}";
		}
	}
}
