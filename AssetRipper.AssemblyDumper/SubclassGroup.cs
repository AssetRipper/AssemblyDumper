namespace AssetRipper.AssemblyDumper
{
	internal sealed class SubclassGroup : ClassGroupBase
	{
		public override string Name { get; }

		public override int ID => -1;

		public override string Namespace => SharedState.GetSubclassNamespace(Name);

		public override bool UniformlyNamed => true;

		public override bool IsPPtr => Name.StartsWith("PPtr_", StringComparison.Ordinal);

		public SubclassGroup(string name, TypeDefinition @interface) : base(@interface)
		{
			Name = name;
		}
	}
}
