namespace AssetRipper.DocExtraction.MetaData;

public sealed record class ClassDocumentation : ComplexTypeDocumentation
{
	public string? BaseNamespace { get; set; }
	public string? BaseName { get; set; }
}
