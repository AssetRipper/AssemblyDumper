namespace AssetRipper.AssemblyDumper.AST;

internal sealed class PrimitiveNode : Node
{
	public PrimitiveNode(TypeSignature typeSignature, Node parent) : base(parent)
	{
		TypeSignature = typeSignature;
	}

	public override TypeSignature TypeSignature { get; }

	public override bool AnyPPtrs => false;
}
