using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper
{
	internal class InterfaceProperty : PropertyBase
	{
		private readonly List<ClassProperty> implementations = new();
		private DiscontinuousRange<UnityVersion>? presentRange;
		private DiscontinuousRange<UnityVersion>? absentRange;

		public InterfaceProperty(PropertyDefinition definition, ClassGroupBase group) : base(definition)
		{
			Group = group;
		}

		public DiscontinuousRange<UnityVersion> PresentRange
		{
			get
			{
				presentRange ??= CalculateRange(p => p.IsPresent);
				return presentRange.Value;
			}
		}

		public DiscontinuousRange<UnityVersion> AbsentRange
		{
			get
			{
				absentRange ??= CalculateRange(p => p.IsAbsent);
				return absentRange.Value;
			}
		}

		public IReadOnlyList<ClassProperty> Implementations => implementations;

		public ClassGroupBase Group { get; }

		public DiscontinuousRange<UnityVersion> ReleaseOnlyRange => CalculateRange(p => p.IsReleaseOnly);

		public DiscontinuousRange<UnityVersion> EditorOnlyRange => CalculateRange(p => p.IsEditorOnly);

		internal void AddImplementation(ClassProperty implementation)
		{
			implementations.Add(implementation);
			presentRange = null;
			absentRange = null;
		}

		private DiscontinuousRange<UnityVersion> CalculateRange(Func<ClassProperty, bool> predicate)
		{
			return new DiscontinuousRange<UnityVersion>(implementations.Where(predicate).Select(static p => p.Class.VersionRange));
		}
	}
}
