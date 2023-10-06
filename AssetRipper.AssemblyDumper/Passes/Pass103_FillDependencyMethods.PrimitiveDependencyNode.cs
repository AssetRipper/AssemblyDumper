namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class PrimitiveDependencyNode : DependencyNode
	{
		public PrimitiveDependencyNode(TypeSignature typeSignature, DependencyNode parent) : base(parent)
		{
			TypeSignature = typeSignature;
		}

		public override string PathContent => "";

		public override TypeSignature TypeSignature { get; }

		public override bool AnyPPtrs => false;

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			throw new NotSupportedException();
		}
	}
}
