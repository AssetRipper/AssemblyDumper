﻿namespace AssetRipper.AssemblyDumper.AST;

internal sealed class ValueNode : SingleNode
{
	public ValueNode(TypeSignature typeSignature, Node? parent = null) : base(parent)
	{
		Child = Create(typeSignature, this);
	}

	public override TypeSignature TypeSignature => Child.TypeSignature;
}
