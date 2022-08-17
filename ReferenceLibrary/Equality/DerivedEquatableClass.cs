using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferenceLibrary.Equality
{
	public class DerivedEquatableClass : EquatableClass, IEquatable<DerivedEquatableClass>
	{
		public double d;

		public override bool Equals(object obj)
		{
			return Equals(obj as DerivedEquatableClass);
		}

		public bool Equals(DerivedEquatableClass other)
		{
			return other is not null &&
				   base.Equals(other) &&
				   d == other.d;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(base.GetHashCode(), d);
		}

		public static bool operator ==(DerivedEquatableClass left, DerivedEquatableClass right)
		{
			return EqualityComparer<DerivedEquatableClass>.Default.Equals(left, right);
		}

		public static bool operator !=(DerivedEquatableClass left, DerivedEquatableClass right)
		{
			return !(left == right);
		}
	}
}
