namespace AssetRipper.AssemblyDumper
{
	internal sealed class GeneratedClassInstance
	{
		public string Name { get; set; }
		public int ID { get; set; }
		public UniversalClass Class { get; set; }
		public TypeDefinition Type { get; set; }
		public Range<UnityVersion> VersionRange { get; set; }
		public GeneratedClassInstance? Base { get; set; }
		public List<GeneratedClassInstance> Derived { get; } = new();
		public Dictionary<PropertyDefinition, string?> PropertiesToFields { get; } = new();
		public Dictionary<PropertyDefinition, PropertyDefinition> InterfacePropertiesToInstanceProperties { get; } = new();

		public GeneratedClassInstance(string name, int id, UniversalClass @class, TypeDefinition type, Range<UnityVersion> versionRange)
		{
			Name = name;
			ID = id;
			Class = @class;
			Type = type;
			VersionRange = versionRange;
		}

		public GeneratedClassInstance(string name, int id, UniversalClass @class, TypeDefinition type, UnityVersion startVersion, UnityVersion endVersion)
			: this(name,id, @class, type, new Range<UnityVersion>(startVersion, endVersion))
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
	}
}
