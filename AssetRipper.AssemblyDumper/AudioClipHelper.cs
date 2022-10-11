﻿using AssetRipper.Assets;
using AssetRipper.Assets.IO.Reading;
using AssetRipper.IO.Files.Extensions;
using AssetRipper.IO.Files.ResourceFiles;

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
			string resourceFileName = audioClip.Collection.Name + ".resS";
			if (TryFindResourceFile(audioClip, resourceFileName, out ResourceFile resourceFile))
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

		private static bool TryFindResourceFile(UnityObjectBase audioClip, string resourceFileName, out ResourceFile resourceFile)
		{
			resourceFile = audioClip.Collection.Bundle.ResolveResource(resourceFileName);
			return resourceFile is not null;
		}
	}
}

#nullable enable