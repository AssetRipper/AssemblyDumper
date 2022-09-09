using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class GeneratedClassInstance
	{
		private readonly Dictionary<PropertyDefinition, string?> propertiesToFields = new();
		private readonly Dictionary<string, PropertyDefinition> fieldsToProperties = new();

		public string Name { get; set; }
		public int ID { get; set; }
		public UniversalClass Class { get; set; }
		public TypeDefinition Type { get; set; }
		public Range<UnityVersion> VersionRange { get; set; }
		public GeneratedClassInstance? Base { get; set; }
		public List<GeneratedClassInstance> Derived { get; } = new();
		public IReadOnlyDictionary<PropertyDefinition, string?> PropertiesToFields => propertiesToFields;
		public IReadOnlyDictionary<string, PropertyDefinition> FieldsToProperties => fieldsToProperties;
		public Dictionary<PropertyDefinition, PropertyDefinition> InterfacePropertiesToInstanceProperties { get; } = new();
		public ComplexTypeHistory? History { get; set; }

		public GeneratedClassInstance(string name, int id, UniversalClass @class, TypeDefinition type, Range<UnityVersion> versionRange)
		{
			Name = name;
			ID = id;
			Class = @class;
			Type = type;
			VersionRange = versionRange;
		}

		public GeneratedClassInstance(string name, int id, UniversalClass @class, TypeDefinition type, UnityVersion startVersion, UnityVersion endVersion)
			: this(name, id, @class, type, new Range<UnityVersion>(startVersion, endVersion))
		{
		}

		public GeneratedClassInstance(UniversalClass @class, TypeDefinition type, UnityVersion startVersion, UnityVersion endVersion)
			: this(@class.Name, @class.TypeID, @class, type, startVersion, endVersion)
		{
		}

		public override bool Equals(object? obj)
		{
			return obj is GeneratedClassInstance instance &&
				   Name == instance.Name &&
				   ID == instance.ID &&
				   EqualityComparer<UniversalClass>.Default.Equals(Class, instance.Class) &&
				   EqualityComparer<TypeDefinition>.Default.Equals(Type, instance.Type) &&
				   VersionRange.Equals(instance.VersionRange) &&
				   EqualityComparer<GeneratedClassInstance?>.Default.Equals(Base, instance.Base) &&
				   EqualityComparer<List<GeneratedClassInstance>>.Default.Equals(Derived, instance.Derived);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, ID, Class, Type, VersionRange, Base, Derived);
		}

		public override string ToString() => $"{Name} {VersionRange.Start} : {VersionRange.End}";

		public int GetSerializedVersion()
		{
			return Class.EditorRootNode is not null
				? Class.EditorRootNode.Version
				: Class.ReleaseRootNode is not null
					? Class.ReleaseRootNode.Version
					: 1;
		}

		public void AddPropertyFieldPair(PropertyDefinition property, string? field)
		{
			propertiesToFields.Add(property, field);
			if (field is not null)
			{
				fieldsToProperties.Add(field, property);
			}
		}

		public void InitializeHistory(HistoryFile historyFile)
		{
			if (ID < 0)
			{
				TryGetSubclass(Name, VersionRange.Start, historyFile, out ComplexTypeHistory? history);
				History = history;
			}
			else
			{
				TryGetClass(Name, VersionRange.Start, historyFile, out ClassHistory? classHistory);
				History = classHistory;
			}
		}

		private static bool TryGetClass(string name, UnityVersion version, HistoryFile historyFile, [NotNullWhen(true)] out ClassHistory? history)
		{
			if (TryGetClassFullName($"UnityEngine.{name}", version, historyFile, out history) && history.InheritsFromUnityEngineObject(version, historyFile))
			{
				return true;
			}
			else if (TryGetClassFullName($"UnityEditor.{name}", version, historyFile, out history) && history.InheritsFromUnityEngineObject(version, historyFile))
			{
				return true;
			}
			else
			{
				foreach ((_, ClassHistory classHistory) in historyFile.Classes)
				{
					if (MatchesNameAndExists(classHistory, name, version) && classHistory.InheritsFromUnityEngineObject(version, historyFile))
					{
						history = classHistory;
						return true;
					}
				}
				history = null;
				return false;
			}
		}

		private static bool TryGetSubclass(string name, UnityVersion version, HistoryFile historyFile, [NotNullWhen(true)] out ComplexTypeHistory? history)
		{
			if (TryGetClassFullName($"UnityEngine.{name}", version, historyFile, out ClassHistory? @class) && !@class.InheritsFromUnityEngineObject(version, historyFile))
			{
				history = @class;
				return true;
			}
			else if (TryGetClassFullName($"UnityEditor.{name}", version, historyFile, out @class) && !@class.InheritsFromUnityEngineObject(version, historyFile))
			{
				history = @class;
				return true;
			}
			if (TryGetStructFullName($"UnityEngine.{name}", version, historyFile, out StructHistory? @struct))
			{
				history = @struct;
				return true;
			}
			else if (TryGetStructFullName($"UnityEditor.{name}", version, historyFile, out @struct))
			{
				history = @struct;
				return true;
			}
			else
			{
				foreach ((_, ClassHistory classHistory) in historyFile.Classes)
				{
					if (MatchesNameAndExists(classHistory, name, version) && !classHistory.InheritsFromUnityEngineObject(version, historyFile))
					{
						history = classHistory;
						return true;
					}
				}
				foreach ((_, StructHistory structHistory) in historyFile.Structs)
				{
					if (MatchesNameAndExists(structHistory, name, version))
					{
						history = structHistory;
						return true;
					}
				}
				history = null;
				return false;
			}
		}

		private static bool MatchesNameAndExists(HistoryBase history, string name, UnityVersion version)
		{
			return (history.Name == name || history.NativeName.GetItemForVersion(version) == name)
				&& history.ExistsOnVersion(version);
		}

		private static bool TryGetClassFullName(string fullName, UnityVersion version, HistoryFile historyFile, [NotNullWhen(true)] out ClassHistory? history)
		{
			if (historyFile.Classes.TryGetValue(fullName, out ClassHistory? potentialHistory) && potentialHistory.ExistsOnVersion(version))
			{
				history = potentialHistory;
				return true;
			}
			else
			{
				history = null;
				return false;
			}
		}

		private static bool TryGetStructFullName(string fullName, UnityVersion version, HistoryFile historyFile, [NotNullWhen(true)] out StructHistory? history)
		{
			if (historyFile.Structs.TryGetValue(fullName, out StructHistory? potentialHistory) && potentialHistory.ExistsOnVersion(version))
			{
				history = potentialHistory;
				return true;
			}
			else
			{
				history = null;
				return false;
			}
		}
	}
}
