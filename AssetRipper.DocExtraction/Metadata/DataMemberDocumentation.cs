using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.MetaData;

public sealed record class DataMemberDocumentation : DocumentationBase
{
	/// <summary>
	/// The namespace for the return type of the property
	/// </summary>
	public string? TypeNamespace { get; set; }
	/// <summary>
	/// The name for the return type of the property
	/// </summary>
	public string TypeName { get; set; } = "";
	/// <summary>
	/// The full name for the return type of the property
	/// </summary>
	[JsonIgnore]
	public string TypeFullName => string.IsNullOrEmpty(TypeNamespace) ? TypeName : $"{TypeNamespace}.{TypeName}";
}
