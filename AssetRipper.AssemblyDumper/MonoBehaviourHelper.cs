using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Yaml;

#nullable disable

namespace AssetRipper.AssemblyDumper
{
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
	internal static class MonoBehaviourHelper
	{
		public static void MaybeExportYamlForStructure(IUnityAssetBase structure, YamlMappingNode node, IExportContainer container)
		{
			if (structure != null)
			{
				YamlMappingNode structureNode = (YamlMappingNode)structure.ExportYaml(container);
				node.Append(structureNode);
			}
		}
	}
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
}
