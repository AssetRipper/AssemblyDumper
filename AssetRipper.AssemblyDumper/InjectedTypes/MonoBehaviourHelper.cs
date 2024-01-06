using AssetRipper.Assets;
using AssetRipper.Assets.Export;
using AssetRipper.Assets.Export.Yaml;
using AssetRipper.Assets.Traversal;
using AssetRipper.Yaml;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
	internal static class MonoBehaviourHelper
	{
		private const string FieldName = "m_Structure";

		public static void MaybeExportYamlForStructure(IUnityAssetBase structure, YamlMappingNode node, IExportContainer container)
		{
			if (structure != null)
			{
				YamlMappingNode structureNode = (YamlMappingNode)structure.ExportYaml(container);
				node.Append(structureNode);
			}
		}

		public static void MaybeWalkStructureEditor(IUnityObjectBase asset, IUnityAssetBase structure, AssetWalker walker)
		{
			if (structure != null)
			{
				walker.DivideAsset(asset);
				if (walker.EnterField(asset, FieldName))
				{
					structure.WalkEditor(walker);
					walker.ExitField(asset, FieldName);
				}
			}
		}

		public static void MaybeWalkStructureRelease(IUnityObjectBase asset, IUnityAssetBase structure, AssetWalker walker)
		{
			if (structure != null)
			{
				walker.DivideAsset(asset);
				if (walker.EnterField(asset, FieldName))
				{
					structure.WalkRelease(walker);
					walker.ExitField(asset, FieldName);
				}
			}
		}

		public static void MaybeWalkStructureStandard(IUnityObjectBase asset, IUnityAssetBase structure, AssetWalker walker)
		{
			if (structure != null)
			{
				walker.DivideAsset(asset);
				if (walker.EnterField(asset, FieldName))
				{
					structure.WalkStandard(walker);
					walker.ExitField(asset, FieldName);
				}
			}
		}
	}
}
