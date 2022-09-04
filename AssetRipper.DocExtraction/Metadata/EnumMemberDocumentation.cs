namespace AssetRipper.DocExtraction;

public sealed record class EnumMemberDocumentation : DocumentationBase
{
	public long Value { get; set; }
}
