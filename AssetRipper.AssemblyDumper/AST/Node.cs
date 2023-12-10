using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.AST;

internal abstract class Node
{
	public Node(Node? parent = null)
	{
		Parent = parent;
	}

	public Node? Parent { get; }

	public abstract TypeSignature TypeSignature { get; }
	public virtual IReadOnlyList<Node> Children => [];
	public virtual bool AnyPPtrs => Children.Any(c => c.AnyPPtrs);

	public static Node Create(TypeSignature type, Node parent)
	{
		switch (type)
		{
			case CorLibTypeSignature or SzArrayTypeSignature:
				return new PrimitiveNode(type, parent);
			case TypeDefOrRefSignature typeDefOrRefSignature:
				if (typeDefOrRefSignature.Type is TypeDefinition typeDefinition)
				{
					GeneratedClassInstance instance = SharedState.Instance.TypesToInstances[typeDefinition];
					return instance.Group.IsPPtr
						? new PPtrNode(instance, parent)
						: new TypeNode(instance, parent);
				}
				else
				{
					return new PrimitiveNode(type, parent);//Utf8String
				}
			case GenericInstanceTypeSignature genericInstanceTypeSignature:
				return (genericInstanceTypeSignature.GenericType.Name?.ToString()) switch
				{
					$"{nameof(AssetDictionary<int, int>)}`2" => new DictionaryNode(genericInstanceTypeSignature, parent),
					$"{nameof(AssetList<int>)}`1" => new ArrayNode(genericInstanceTypeSignature, parent),
					$"{nameof(AssetPair<int, int>)}`2" => new PairNode(genericInstanceTypeSignature, parent),
					_ => throw new NotSupportedException(),
				};
			default:
				throw new NotImplementedException();
		}
	}
}
