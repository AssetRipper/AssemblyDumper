using System.Diagnostics;

namespace AssetRipper.AssemblyDumper
{
	internal static class UnityVersionExtensions
	{
		public static UnityVersion StripType(this UnityVersion version)
		{
			return new UnityVersion(version.Major, version.Minor, version.Build);
		}

		public static string ToCleanString(this UnityVersion version, char separator)
		{
			if (version.Type == UnityVersionType.Alpha && version.TypeNumber == 0)
			{
				return $"{version.Major}{separator}{version.Minor}{separator}{version.Build}";
			}
			else if (separator == '_')
			{
				return $"{version.Major}_{version.Minor}_{version.Build}_{version.Type.ToCharacter()}{version.TypeNumber}";
			}
			else
			{
				Debug.Assert(separator == '.');
				return $"{version.Major}.{version.Minor}.{version.Build}{version.Type.ToCharacter()}{version.TypeNumber}";
			}
		}
	}
}
