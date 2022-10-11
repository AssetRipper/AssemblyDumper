using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper
{
	internal static class CreateClassIdExample
	{
		private static SomeInterface AbstractClassAlone(AssetInfo assetInfo, UnityVersion version)
		{
			throw new Exception();
		}

		private static SomeInterface OneClass(AssetInfo assetInfo, UnityVersion version)
		{
			return new ClassVersion1(assetInfo);
		}

		private static SomeInterface TwoClasses(AssetInfo assetInfo, UnityVersion version)
		{
			if (version.IsLess(3, 4, 0, UnityVersionType.Final, 5))
			{
				return new ClassVersion1(assetInfo);
			}
			else
			{
				return new ClassVersion2(assetInfo);
			}
		}

		private static SomeInterface ThreeClasses(AssetInfo assetInfo, UnityVersion version)
		{
			if (version.IsLess(3, 4, 0, UnityVersionType.Final, 5))
			{
				return new ClassVersion1(assetInfo);
			}
			else if (version.IsLess(5, 0, 0, UnityVersionType.Alpha, 1))
			{
				return new ClassVersion2(assetInfo);
			}
			else
			{
				return new ClassVersion3(assetInfo);
			}
		}

		private static SomeInterface ThreeClassesButOneAbstract(AssetInfo assetInfo, UnityVersion version)
		{
			if (version.IsLess(3, 4, 0, UnityVersionType.Final, 5))
			{
				return new ClassVersion1(assetInfo);
			}
			else if (version.IsLess(5, 0, 0, UnityVersionType.Alpha, 1))
			{
				throw new Exception();
			}
			else
			{
				return new ClassVersion3(assetInfo);
			}
		}

		private interface SomeInterface { }
		private sealed class ClassVersion1 : SomeInterface
		{
			public ClassVersion1(AssetInfo assetInfo) { }
		}
		private sealed class ClassVersion2 : SomeInterface
		{
			public ClassVersion2(AssetInfo assetInfo) { }
		}
		private sealed class ClassVersion3 : SomeInterface
		{
			public ClassVersion3(AssetInfo assetInfo) { }
		}
		private sealed class ClassVersion4 : SomeInterface
		{
			public ClassVersion4(AssetInfo assetInfo) { }
		}
		private sealed class ClassVersion5 : SomeInterface
		{
			public ClassVersion5(AssetInfo assetInfo) { }
		}
	}
}
