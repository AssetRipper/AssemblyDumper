using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper
{
	internal class InterfaceProperty : PropertyBase
	{
		private readonly List<ClassProperty> implementations = new();

		public InterfaceProperty(PropertyDefinition definition, ClassGroupBase group) : base(definition)
		{
			Group = group;
			PresentRange = DiscontinuousRange<UnityVersion>.Empty;
			AbsentRange = DiscontinuousRange<UnityVersion>.Empty;
		}

		public DiscontinuousRange<UnityVersion> PresentRange { get; private set; }
		public DiscontinuousRange<UnityVersion> AbsentRange { get; private set; }
		public IReadOnlyList<ClassProperty> Implementations => implementations;
		public ClassGroupBase Group { get; }
		public bool IsAnyImplemplementationAbsent => Implementations.Any(p => p.IsAbsent);

		internal void AddImplementation(ClassProperty implementation)
		{
			implementations.Add(implementation);
		}

		public void RecalculateRanges()
		{
			PresentRange = new DiscontinuousRange<UnityVersion>(implementations.Where(p => p.IsPresent).Select(p => p.Class.VersionRange));
			AbsentRange = new DiscontinuousRange<UnityVersion>(implementations.Where(p => p.IsAbsent).Select(p => p.Class.VersionRange));
		}
	}
}
