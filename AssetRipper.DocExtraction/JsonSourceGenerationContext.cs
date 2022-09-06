using AssetRipper.DocExtraction.MetaData;
using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DocumentationFile))]
[JsonSerializable(typeof(FullName))]
public sealed partial class JsonSourceGenerationContext : JsonSerializerContext
{
}