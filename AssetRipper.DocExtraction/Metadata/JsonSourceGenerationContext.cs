using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DocumentationFile))]
internal sealed partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
