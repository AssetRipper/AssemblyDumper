using AssetRipper.Assets.Export;
using AssetRipper.Assets.Export.Yaml;
using AssetRipper.Yaml;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
	internal static class HashHelper
	{
		public static int ToSerializedVersion(UnityVersion version)
		{
			//Unity is stupid and didn't change the type trees.
			//To see an example of this, look at Texture3D.
			return version.IsGreaterEqual(5) ? 2 : 1;
		}

		public static YamlNode ExportYaml(byte bytes__0, byte bytes__1, byte bytes__2, byte bytes__3, byte bytes__4, byte bytes__5, byte bytes__6, byte bytes__7, byte bytes__8, byte bytes__9, byte bytes_10, byte bytes_11, byte bytes_12, byte bytes_13, byte bytes_14, byte bytes_15, IExportContainer container)
		{
			int serializedVersion = ToSerializedVersion(container.ExportVersion);
			YamlMappingNode node = new();
			node.AddSerializedVersion(serializedVersion);
			if (serializedVersion > 1)
			{
				string str = ToString(bytes__0, bytes__1, bytes__2, bytes__3, bytes__4, bytes__5, bytes__6, bytes__7, bytes__8, bytes__9, bytes_10, bytes_11, bytes_12, bytes_13, bytes_14, bytes_15);
				node.Add(HashName, str);
			}
			else
			{
				node.Add(Bytes00Name, bytes__0);
				node.Add(Bytes01Name, bytes__1);
				node.Add(Bytes02Name, bytes__2);
				node.Add(Bytes03Name, bytes__3);
				node.Add(Bytes04Name, bytes__4);
				node.Add(Bytes05Name, bytes__5);
				node.Add(Bytes06Name, bytes__6);
				node.Add(Bytes07Name, bytes__7);
				node.Add(Bytes08Name, bytes__8);
				node.Add(Bytes09Name, bytes__9);
				node.Add(Bytes10Name, bytes_10);
				node.Add(Bytes11Name, bytes_11);
				node.Add(Bytes12Name, bytes_12);
				node.Add(Bytes13Name, bytes_13);
				node.Add(Bytes14Name, bytes_14);
				node.Add(Bytes15Name, bytes_15);
			}
			return node;
		}

		public static string ToString(byte bytes__0, byte bytes__1, byte bytes__2, byte bytes__3, byte bytes__4, byte bytes__5, byte bytes__6, byte bytes__7, byte bytes__8, byte bytes__9, byte bytes_10, byte bytes_11, byte bytes_12, byte bytes_13, byte bytes_14, byte bytes_15)
		{
			//Not sure if this depends on Endianess
			//If it does, it might be best to split Hash at Unity 5
			uint Data0 = bytes__0 | (uint)bytes__1 << 8 | (uint)bytes__2 << 16 | (uint)bytes__3 << 24;
			uint Data1 = bytes__4 | (uint)bytes__5 << 8 | (uint)bytes__6 << 16 | (uint)bytes__7 << 24;
			uint Data2 = bytes__8 | (uint)bytes__9 << 8 | (uint)bytes_10 << 16 | (uint)bytes_11 << 24;
			uint Data3 = bytes_12 | (uint)bytes_13 << 8 | (uint)bytes_14 << 16 | (uint)bytes_15 << 24;
			string str = $"{Data0:x8}{Data1:x8}{Data2:x8}{Data3:x8}";
			return str;
		}

		private const string Bytes00Name = "bytes[0]";
		private const string Bytes01Name = "bytes[1]";
		private const string Bytes02Name = "bytes[2]";
		private const string Bytes03Name = "bytes[3]";
		private const string Bytes04Name = "bytes[4]";
		private const string Bytes05Name = "bytes[5]";
		private const string Bytes06Name = "bytes[6]";
		private const string Bytes07Name = "bytes[7]";
		private const string Bytes08Name = "bytes[8]";
		private const string Bytes09Name = "bytes[9]";
		private const string Bytes10Name = "bytes[10]";
		private const string Bytes11Name = "bytes[11]";
		private const string Bytes12Name = "bytes[12]";
		private const string Bytes13Name = "bytes[13]";
		private const string Bytes14Name = "bytes[14]";
		private const string Bytes15Name = "bytes[15]";
		private const string HashName = "Hash";
	}
}
