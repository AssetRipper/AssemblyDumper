using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.MetaData;

public abstract record class TypeDocumentation : DocumentationBase
{
	public string? Namespace { get; set; }
	[JsonIgnore]
	public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
	public override string ToString()
	{
		return FullName;
	}
}
