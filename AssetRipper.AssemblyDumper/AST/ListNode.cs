namespace AssetRipper.AssemblyDumper.AST;

internal sealed class ListNode : SingleNode<Node>
{
	public ListNode(GenericInstanceTypeSignature typeSignature, Node? parent = null) : base(parent)
	{
		TypeSignature = typeSignature;
		Child = Create(typeSignature.TypeArguments[0], this);
	}

	public override GenericInstanceTypeSignature TypeSignature { get; }
}
