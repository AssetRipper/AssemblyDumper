using AssetRipper.Core;
using AssetRipper.Core.Extensions;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.Parser.Files.ResourceFiles;

#nullable disable

namespace AssetRipper.AssemblyDumper
{
	internal static class AudioClipHelper
	{
		internal static byte[] ReadOldByteArray(this UnityObjectBase audioClip, AssetReader reader, int m_Stream)
		{
			return m_Stream == 2 //AudioClipLoadType.Streaming
				? ReadStreamedByteArray(audioClip, reader)
				: ReadAlignedByteArray(reader);
		}

		private static byte[] ReadAlignedByteArray(AssetReader reader)
		{
			byte[] result = reader.ReadByteArray(allowAlignment: false);
			reader.AlignStream();
			return result;
		}

		private static byte[] ReadStreamedByteArray(UnityObjectBase audioClip, AssetReader reader)
		{
			uint size = reader.ReadUInt32();
			uint offset = reader.ReadUInt32();
			string resourceFileName = audioClip.SerializedFile.Name + ".resS";
			if (TryFindResourceFile(audioClip, resourceFileName, out IResourceFile resourceFile))
			{
				byte[] result = new byte[size];
				resourceFile.Stream.Position = offset;
				resourceFile.Stream.ReadBuffer(result, 0, result.Length);
				return result;
			}
			else
			{
				return Array.Empty<byte>();
			}
		}

		private static bool TryFindResourceFile(UnityObjectBase audioClip, string resourceFileName, out IResourceFile resourceFile)
		{
			resourceFile = audioClip.SerializedFile.Collection.FindResourceFile(resourceFileName);
			return resourceFile != null;
		}
	}
}

#nullable enable