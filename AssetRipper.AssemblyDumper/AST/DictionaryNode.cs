using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.AST;

internal sealed class DictionaryNode : SingleNode<PairNode>
{
	public DictionaryNode(GenericInstanceTypeSignature typeSignature, Node? parent = null) : base(parent)
	{
		TypeSignature = typeSignature;
		GenericInstanceTypeSignature pairType = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>)).MakeGenericInstanceType(
			typeSignature.TypeArguments[0],
			typeSignature.TypeArguments[1]);
		Child = new PairNode(pairType, this);
	}

	public override GenericInstanceTypeSignature TypeSignature { get; }

	public TypeSignature KeyTypeSignature => Child.Key.TypeSignature;

	public TypeSignature ValueTypeSignature => Child.Value.TypeSignature;
}
