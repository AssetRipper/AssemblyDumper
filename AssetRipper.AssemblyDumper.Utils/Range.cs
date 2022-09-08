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

		public bool Intersects(Range<T> other, out Range<T> intersection)
		{
			if (Intersects(other))
			{
				intersection = MakeIntersectionInternal(other);
				return true;
			}
			else
			{
				intersection = default;
				return false;
			}
		}

		public bool CanUnion(Range<T> other)
		{
			return Intersects(other) || Start.Equals(other.End) || End.Equals(other.Start);
		}

		public bool CanUnion(Range<T> other, out Range<T> union)
		{
			if (CanUnion(other))
			{
				union = MakeUnionInternal(other);
				return true;
			}
			else
			{
				union = default;
				return false;
			}
		}

		public Range<T> MakeUnion(Range<T> other)
		{
			return CanUnion(other)
				? MakeUnionInternal(other)
				: throw new ArgumentException("These ranges cannot be unioned", nameof(other));
		}

		private Range<T> MakeUnionInternal(Range<T> other)
		{
			return new Range<T>(Minimum(Start, other.Start), Maximum(End, other.End));
		}

		public Range<T> MakeIntersection(Range<T> other)
		{
			return Intersects(other)
				? MakeIntersectionInternal(other)
				: throw new ArgumentException("These ranges do not intersect", nameof(other));
		}

		private Range<T> MakeIntersectionInternal(Range<T> other)
		{
			return new Range<T>(Maximum(Start, other.Start), Minimum(End, other.End));
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
