namespace AssetRipper.AssemblyDumper.AST;

internal abstract class SingleNode : Node
{
	public SingleNode(Node? parent = null) : base(parent)
	{
	}

	public Node Child { get; set; } = null!;

	public sealed override IReadOnlyList<Node> Children => [Child];

	public sealed override bool AnyPPtrs => Child.AnyPPtrs;
}
