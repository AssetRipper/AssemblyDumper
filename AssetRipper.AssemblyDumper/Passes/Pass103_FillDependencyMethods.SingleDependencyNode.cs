namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private abstract class SingleDependencyNode : DependencyNode
	{
		public SingleDependencyNode(DependencyNode? parent = null) : base(parent)
		{
		}

		public DependencyNode Child { get; set; } = null!;

		public sealed override IEnumerable<DependencyNode> Children
		{
			get
			{
				yield return Child;
			}
		}

		public sealed override bool AnyPPtrs => Child.AnyPPtrs;
	}
}
