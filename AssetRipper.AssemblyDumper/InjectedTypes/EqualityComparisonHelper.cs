using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Generics;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes;

internal static class EqualityComparisonHelper
{
	public static bool? GetNull() => null;

	public static bool? GetTrue() => true;

	public static bool? GetFalse() => false;

	public static bool IsNull(bool? value) => value == null;

	public static bool IsNotNull(bool? value) => value != null;

	public static bool IsTrue(bool? value) => value == true;

	public static bool IsFalse(bool? value) => value == false;

	public static bool ByteArrayEquals(byte[] x, byte[] y)
	{
		return x.AsSpan().SequenceEqual(y);
	}

	// eg string, int, bool, Vector3f
	public static bool EquatableEquals<T>(T x, T y) where T : IEquatable<T>
	{
		return x.Equals(y);
	}

	// eg AssetList<float>
	public static bool UnmanagedListEquals<T>(AssetList<T> x, AssetList<T> y) where T : unmanaged
	{
		return x.GetSpan().SequenceEqual(y.GetSpan());
	}

	// eg AssetList<Utf8String> or AssetList<Vector3f>
	public static bool EquatableListEquals<T>(AssetList<T> x, AssetList<T> y) where T : IEquatable<T>, new()
	{
		return x.SequenceEqual(y);
	}

	// eg AssetList<ChildAnimatorState>
	public static bool? AssetListEquals<T>(AssetList<T> x, AssetList<T> y, AssetEqualityComparer comparer) where T : IUnityAssetBase, new()
	{
		if (x.Count != y.Count)
		{
			return false;
		}

		bool? result = true;
		for (int i = 0; i < x.Count; i++)
		{
			switch (x[i].AddToEqualityComparer(y[i], comparer))
			{
				case false:
					return false;
				case null:
					result = null;
					break;
			}
		}
		return result;
	}

	public static bool EquatablePairEquals<TKey, TValue>(AssetPair<TKey, TValue> x, AssetPair<TKey, TValue> y) where TKey : IEquatable<TKey>, new() where TValue : IEquatable<TValue>, new()
	{
		return x.Key.Equals(y.Key) && x.Value.Equals(y.Value);
	}

	public static bool EquatableDictionaryEquals<TKey, TValue>(AssetDictionary<TKey, TValue> x, AssetDictionary<TKey, TValue> y) where TKey : IEquatable<TKey>, new() where TValue : IEquatable<TValue>, new()
	{
		if (x.Count != y.Count)
		{
			return false;
		}

		for (int i = 0; i < x.Count; i++)
		{
			if (!x.GetPair(i).Equals(y.GetPair(i)))
			{
				return false;
			}
		}
		return true;
	}
}

#nullable enable