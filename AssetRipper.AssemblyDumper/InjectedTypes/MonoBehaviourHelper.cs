using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets.Traversal;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
	internal static class MonoBehaviourHelper
	{
		private const string FieldName = "m_Structure";

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

		public static IEnumerable<(string, PPtr)> MaybeAppendStructureDependencies(IEnumerable<(string, PPtr)> dependencies, IUnityAssetBase structure)
		{
			return structure == null ? dependencies : dependencies.Concat(structure.FetchDependencies());
		}

		public static void ResetStructure(IUnityAssetBase structure)
		{
			structure?.Reset();
		}
	}
}
