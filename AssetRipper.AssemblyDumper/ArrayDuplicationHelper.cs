#nullable disable

namespace AssetRipper.AssemblyDumper
{
	internal static class ArrayDuplicationHelper
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
	}
}

#nullable enable
