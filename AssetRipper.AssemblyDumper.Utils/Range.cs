namespace AssetRipper.AssemblyDumper.Utils
{
	public readonly struct Range<T> : IEquatable<Range<T>> where T : struct, IEquatable<T>, IComparable<T>
	{
		/// <summary>
		/// Represent the inclusive start of the Range.
		/// </summary>
		public T Start { get; }

		/// <summary>
		/// Represent the exclusive end of the Range.
		/// </summary>
		public T End { get; }

		public Range(T start, T end)
		{
			if (start.CompareTo(end) >= 0)
			{
				throw new ArgumentException($"{nameof(start)} {start} must be less than {nameof(end)} {end}");
			}

			Start = start;
			End = end;
		}

		public bool Contains(T value)
		{
			return Start.CompareTo(value) <= 0 && End.CompareTo(value) > 0;
		}

		public bool Contains(Range<T> range)
		{
			return Start.CompareTo(range.Start) <= 0 && End.CompareTo(range.End) >= 0;
		}

		public bool Intersects(Range<T> other)
		{
			return Contains(other.Start) || other.Contains(Start);
		}

		public bool CanUnion(Range<T> other)
		{
			return Intersects(other) || Start.Equals(other.End) || End.Equals(other.Start);
		}

		public Range<T> MakeUnion(Range<T> other)
		{
			return CanUnion(other)
				? new Range<T>(Minimum(Start, other.Start), Maximum(End, other.End))
				: throw new ArgumentException("These ranges cannot be unioned", nameof(other));
		}

		public Range<T> MakeIntersection(Range<T> other)
		{
			return Intersects(other)
				? new Range<T>(Maximum(Start, other.Start), Minimum(End, other.End))
				: throw new ArgumentException("These ranges do not intersect", nameof(other));
		}

		private static T Minimum(T left, T right)
		{
			return left.CompareTo(right) < 0 ? left : right;
		}

		private static T Maximum(T left, T right)
		{
			return left.CompareTo(right) > 0 ? left : right;
		}

		public override bool Equals(object? obj)
		{
			return obj is Range<T> range && Equals(range);
		}

		public bool Equals(Range<T> other)
		{
			return Start.Equals(other.Start) && End.Equals(other.End);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Start, End);
		}

		public static bool operator ==(Range<T> left, Range<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Range<T> left, Range<T> right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return $"{Start} : {End}";
		}
	}
}
