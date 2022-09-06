using AssetRipper.DocExtraction.Extensions;
using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.MetaData;

public abstract record class TypeDocumentation<TMember> : DocumentationBase where TMember : DocumentationBase, new()
{
	public Dictionary<string, TMember> Members { get; set; } = new();
	public string? Namespace { get; set; }
	[JsonIgnore]
	public FullName FullName => new FullName(Namespace, Name);
	public override string ToString() => FullName.ToString();
	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), Namespace, Members.GetHashCodeByContent());
	}

	public virtual bool Equals(TypeDocumentation<TMember>? other)
	{
		return (object)this == other || (base.Equals(other) && Namespace == other.Namespace && Members.EqualByContent(other.Members));
	}
}
