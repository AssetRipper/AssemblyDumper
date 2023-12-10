namespace AssetRipper.AssemblyDumper.AST;

internal sealed class ArrayNode : SingleNode
{
	public ArrayNode(GenericInstanceTypeSignature typeSignature, Node? parent = null) : base(parent)
	{
		TypeSignature = typeSignature;
		Child = Create(typeSignature.TypeArguments[0], this);
	}

	public override GenericInstanceTypeSignature TypeSignature { get; }
}
