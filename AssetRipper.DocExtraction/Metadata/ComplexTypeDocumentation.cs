using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.DocExtraction;

public abstract record class ComplexTypeDocumentation : TypeDocumentation
{
	public Dictionary<string, DataMemberDocumentation> DataMembers { get; set; } = new();

	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), DataMembers.GetHashCodeByContent());
	}

	public virtual bool Equals(ComplexTypeDocumentation? other)
	{
		return (object)this == other || (base.Equals(other) && DataMembers.EqualByContent(other.DataMembers));
	}
}