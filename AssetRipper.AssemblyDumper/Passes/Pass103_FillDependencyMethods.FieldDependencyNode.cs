using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class FieldDependencyNode : SingleDependencyNode
	{
		public FieldDependencyNode(ClassProperty property, DependencyNode? parent = null) : base(parent)
		{
			Debug.Assert(property.BackingField is not null);
			Property = property;
			Child = Create(TypeSignature, this);
		}

		public override string PathContent => Field.Name ?? "";
		public ClassProperty Property { get; }

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			Child.Apply(context, parentContext);
		}
		public FieldDefinition Field => Property.BackingField!;
		public override TypeSignature TypeSignature => Field.Signature!.FieldType;
	}
}
