namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class ValueDependencyNode : SingleDependencyNode
	{
		public ValueDependencyNode(TypeSignature typeSignature, DependencyNode? parent = null) : base(parent)
		{
			Child = Create(typeSignature, this);
		}

		public override string PathContent => ".Value";

		public override TypeSignature TypeSignature => Child.TypeSignature;

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			Child.Apply(context, parentContext);
		}
	}
}
