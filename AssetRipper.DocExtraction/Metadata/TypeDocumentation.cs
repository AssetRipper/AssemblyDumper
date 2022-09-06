using AssetRipper.DocExtraction.Extensions;
using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.MetaData;

public abstract record class TypeDocumentation<TMember> : DocumentationBase where TMember : DocumentationBase, new()
{
	public string? Namespace { get; set; }
	[JsonIgnore]
	public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
	public Dictionary<string, TMember> Members { get; set; } = new();
	public override string ToString() => FullName;
	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), Namespace, Members.GetHashCodeByContent());
	}

	public virtual bool Equals(TypeDocumentation<TMember>? other)
	{
		return (object)this == other || (base.Equals(other) && Namespace == other.Namespace && Members.EqualByContent(other.Members));
	}
}
