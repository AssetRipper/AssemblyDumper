namespace AssetRipper.AssemblyDumper.AST;

internal sealed class PairNode : Node
{
	public PairNode(GenericInstanceTypeSignature typeSignature, Node? parent = null) : base(parent)
	{
		TypeSignature = typeSignature;
		Key = new(typeSignature.TypeArguments[0], this);
		Value = new(typeSignature.TypeArguments[1], this);
	}

	public KeyNode Key { get; }
	public ValueNode Value { get; }

	public override IReadOnlyList<Node> Children => [Key, Value];

	public override bool AnyPPtrs => Key.AnyPPtrs || Value.AnyPPtrs;

	public override GenericInstanceTypeSignature TypeSignature { get; }
}
