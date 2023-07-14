#nullable disable

using AssetRipper;
using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
	internal static class CopyValuesHelper
	{
		public static T[] DuplicateArray<T>(T[] array)
		{
			if (array is null || array.Length == 0)
			{
				return Array.Empty<T>();
			}
			else
			{
				T[] copy = new T[array.Length];
				Array.Copy(array, copy, array.Length);
				return copy;
			}
		}
		public static T[][] DuplicateArrayArray<T>(T[][] array)
		{
			if (array is null || array.Length == 0)
			{
				return Array.Empty<T[]>();
			}
			else
			{
				T[][] copy = new T[array.Length][];
				for (int i = 0; i < array.Length; i++)
				{
					copy[i] = DuplicateArray(array[i]);
				}
				return copy;
			}
		}
		public static void CopyPPtr<T>(IPPtr<T> targetPPtr, IPPtr sourcePPtr, PPtrConverter converter) where T : IUnityObjectBase
		{
			targetPPtr.CopyValues(converter.Convert<T>(new PPtr(sourcePPtr.FileID, sourcePPtr.PathID)));
		}
	}
}

#nullable enable
