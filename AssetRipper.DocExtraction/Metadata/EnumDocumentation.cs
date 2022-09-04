using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AssetRipper.DocExtraction;

public sealed record class EnumDocumentation : TypeDocumentation
{
	public ElementType ElementType { get; set; }
	public bool IsFlagsEnum { get; set; }
	public Dictionary<string, EnumMemberDocumentation> Members { get; set; } = new();
}
