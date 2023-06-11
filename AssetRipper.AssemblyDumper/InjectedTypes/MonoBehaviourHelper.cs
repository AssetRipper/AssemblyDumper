using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.Export.Yaml;
using AssetRipper.Yaml;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
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
}
