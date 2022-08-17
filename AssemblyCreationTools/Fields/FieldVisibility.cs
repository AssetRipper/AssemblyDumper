namespace AssetRipper.AssemblyCreationTools.Fields
{
	public enum FieldVisibility
	{
		Public,
		Internal,
		Private,
		Protected,
		ProtectedInternal,
		ProtectedPrivate,
	}

	public static class FieldVisibilityExtensions
	{
		public static FieldAttributes ToAttributes(this FieldVisibility visibility)
		{
			return visibility switch
			{
				FieldVisibility.Public => FieldAttributes.Public,
				FieldVisibility.Internal => FieldAttributes.Assembly,
				FieldVisibility.Private => FieldAttributes.Private,
				FieldVisibility.Protected => FieldAttributes.Family,
				FieldVisibility.ProtectedInternal => FieldAttributes.FamilyOrAssembly,
				FieldVisibility.ProtectedPrivate => FieldAttributes.FamilyAndAssembly,
				_ => throw new ArgumentOutOfRangeException(nameof(visibility)),
			};
		}
	}
}
