namespace AssetRipper.AssemblyDumper
{
	internal class ClassProperty : PropertyBase
	{
		public ClassProperty(PropertyDefinition definition, FieldDefinition? backingField, InterfaceProperty @base, GeneratedClassInstance @class) : base(definition)
		{
			BackingField = backingField;
			if (!IsInjected && backingField?.Name is not null)
			{
				UniversalNode node = @class.Class.ReleaseRootNode?.TryGetSubNodeByName(backingField.Name)
					?? @class.Class.EditorRootNode?.TryGetSubNodeByName(backingField.Name)
					?? throw new Exception($"Failed to find node: {@class.Name}.{backingField.Name} on {@class.VersionRange}");
				OriginalFieldName = node.OriginalName;
			}
			Class = @class;
			Base = @base;
			Base.AddImplementation(this);
		}

		public string? OriginalFieldName { get; }

		public FieldDefinition? BackingField { get; }

		public InterfaceProperty Base { get; }

		public GeneratedClassInstance Class { get; }

		[MemberNotNullWhen(false, nameof(BackingField))]
		public bool IsAbsent => BackingField is null;

		[MemberNotNullWhen(true, nameof(BackingField))]
		public bool IsPresent => !IsAbsent;

		[MemberNotNullWhen(true, nameof(BackingField))]
		public bool HasBackingFieldInDeclaringType
		{
			get
			{
				return BackingField is not null && BackingField.DeclaringType == Class.Type;
			}
		}
	}
}
