using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper
{
	internal abstract class ClassGroupBase
	{
		public List<GeneratedClassInstance> Instances { get; } = new();
		public TypeDefinition Interface { get; }
		public List<InterfaceProperty> InterfaceProperties { get; } = new();
		public ComplexTypeHistory? History { get; set; }

		public abstract string Name { get; }
		public abstract string Namespace { get; }
		public abstract int ID { get; }
		public abstract bool UniformlyNamed { get; }

		protected ClassGroupBase(TypeDefinition @interface)
		{
			Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
		}

		public UniversalClass GetClassForVersion(UnityVersion version)
		{
			return GetInstanceForVersion(version).Class;
		}

		public TypeDefinition GetTypeForVersion(UnityVersion version)
		{
			return GetInstanceForVersion(version).Type;
		}

		public GeneratedClassInstance GetInstanceForVersion(UnityVersion version)
		{
			if (Instances.Count == 0)
			{
				throw new Exception("No classes available");
			}
			foreach (GeneratedClassInstance instance in Instances)
			{
				if (instance.VersionRange.Contains(version))
				{
					return instance;
				}
			}
			throw new Exception($"No instance found for {version}");
		}

		public IEnumerable<UniversalClass> Classes => Instances.Select(x => x.Class);

		public IEnumerable<TypeDefinition> Types => Instances.Select(x => x.Type);

		public TypeDefinition GetSingularTypeOrInterface()
		{
			return Instances.Count == 1
				? Instances.First().Type
				: Interface ?? throw new NullReferenceException("Interface was null");
		}

		public override string ToString() => Name;

		public void GetSerializedVersions(out int minimum, out int maximum)
		{
			minimum = 1;
			maximum = 1;
			foreach (GeneratedClassInstance instance in Instances)
			{
				int instanceVersion = instance.GetSerializedVersion();
				if (instanceVersion < minimum)
				{
					minimum = instanceVersion;
				}
				else if (instanceVersion > maximum)
				{
					maximum = instanceVersion;
				}
			}
		}

		public void InitializeHistory(HistoryFile historyFile)
		{
			History = null;

			foreach (GeneratedClassInstance instance in Instances)
			{
				instance.InitializeHistory(historyFile);
			}

			ComplexTypeHistory? firstHistory = Instances[0].History;
			if (firstHistory is not null)
			{
				for (int i = 1; i < Instances.Count; i++)
				{
					ComplexTypeHistory? subsequentHistory = Instances[i].History;
					if (firstHistory != subsequentHistory)
					{
						return;
					}
				}
				History = firstHistory;
			}
		}
	}
}
