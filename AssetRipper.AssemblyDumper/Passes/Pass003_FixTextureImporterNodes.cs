﻿using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper.Passes
{
	/// <summary>
	/// This pass deliberately modifies the type tree for TextureImporter to reflect its yaml output
	/// </summary>
	internal static class Pass003_FixTextureImporterNodes
	{
		private const string MipMapsTypeName = "TextureImporterMipMapSettings"; //Fabricated
		private const string MipMapsName = "m_MipMaps"; //Fabricated
		private const string MipMapsOriginalName = "mipmaps";
		private const string MipMapsStartFieldName = "m_MipMapMode";
		private const string MipMapsEndFieldName = "m_MipMapFadeDistanceEnd";

		private const string BumpMapTypeName = "TextureImporterBumpMapSettings"; //Fabricated
		private const string BumpMapName = "m_BumpMap"; //Fabricated
		private const string BumpMapOriginalName = "bumpmap";
		private const string BumpMapStartFieldName = "m_ConvertToNormalMap";
		private const string BumpMapEndFieldName = "m_NormalMapFilter";

		private const int TextureImporterTypeId = 1006;

		public static void DoPass()
		{
			VersionedList<UniversalClass> classes = SharedState.Instance.ClassInformation[TextureImporterTypeId];
			foreach (UniversalClass? universalClass in classes.Values)
			{
				if (universalClass is not null)
				{
					DoPassOnClass(universalClass);
				}
			}
		}

		private static void DoPassOnClass(UniversalClass universalClass)
		{
			universalClass.ReleaseRootNode?.DoPassOnRootNode();
			universalClass.EditorRootNode?.DoPassOnRootNode();
		}

		private static void DoPassOnRootNode(this UniversalNode rootNode)
		{
			int mipMapsStartIndex = rootNode.GetSubnodeIndex(MipMapsStartFieldName);
			int mipMapsEndIndex = rootNode.GetSubnodeIndex(MipMapsEndFieldName);
			if (mipMapsStartIndex >= mipMapsEndIndex)
			{
				throw new Exception("MipMaps start later than its end");
			}

			int bumpMapStartIndex = rootNode.GetSubnodeIndex(BumpMapStartFieldName);
			int bumpMapEndIndex = rootNode.GetSubnodeIndex(BumpMapEndFieldName);
			if (bumpMapStartIndex <= mipMapsEndIndex)
			{
				throw new Exception("BumpMap start later than MipMaps end");
			}
			if (bumpMapStartIndex >= bumpMapEndIndex)
			{
				throw new Exception("BumpMap start later than its end");
			}

			UniversalNode mipMapsNode = new();
			mipMapsNode.Name = MipMapsName;
			mipMapsNode.OriginalName = MipMapsOriginalName;
			mipMapsNode.TypeName = MipMapsTypeName;
			mipMapsNode.Version = 1;
			mipMapsNode.MetaFlag = default;

			for (int i = mipMapsStartIndex; i <= mipMapsEndIndex; i++)
			{
				mipMapsNode.SubNodes.Add(rootNode.SubNodes[i]);
			}

			UniversalNode bumpMapNode = new();
			bumpMapNode.Name = BumpMapName;
			bumpMapNode.OriginalName = BumpMapOriginalName;
			bumpMapNode.TypeName = BumpMapTypeName;
			bumpMapNode.Version = 1;
			bumpMapNode.MetaFlag = default;

			for (int i = bumpMapStartIndex; i <= bumpMapEndIndex; i++)
			{
				bumpMapNode.SubNodes.Add(rootNode.SubNodes[i]);
			}

			int count = rootNode.SubNodes.Count - (bumpMapEndIndex - bumpMapStartIndex) - (mipMapsEndIndex - mipMapsStartIndex);
			List<UniversalNode> newSubnodes = new List<UniversalNode>(count);
			for (int i = 0; i < mipMapsStartIndex; i++)
			{
				newSubnodes.Add(rootNode.SubNodes[i]);
			}
			newSubnodes.Add(mipMapsNode);
			for (int i = mipMapsEndIndex + 1; i < bumpMapStartIndex; i++)
			{
				newSubnodes.Add(rootNode.SubNodes[i]);
			}
			newSubnodes.Add(bumpMapNode);
			for (int i = bumpMapEndIndex + 1; i < rootNode.SubNodes.Count; i++)
			{
				newSubnodes.Add(rootNode.SubNodes[i]);
			}

			rootNode.SubNodes = newSubnodes;
		}

		private static int GetSubnodeIndex(this UniversalNode parent, string subnodeName)
		{
			int result = parent.SubNodes.FindIndex(n => n.Name == subnodeName);
			return result >= 0 ? result : throw new Exception($"{subnodeName} not found");
		}
	}
}