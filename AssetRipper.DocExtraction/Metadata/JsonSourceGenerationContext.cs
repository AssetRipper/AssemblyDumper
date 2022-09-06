using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.MetaData;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DocumentationFile))]
internal sealed partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
