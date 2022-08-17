namespace ReferenceLibrary.Equality
{
	public class EquatableClass : IEquatable<EquatableClass>
	{
		public int i;
		public string s;
		public List<float> f;
		public bool b;
		public int t;
		public int u;
		public int v;
		public int w;
		public int x;
		public int y;
		public int z;

		public override bool Equals(object obj)
		{
			return Equals(obj as EquatableClass);
		}

		public bool Equals(EquatableClass other)
		{
			return other is not null &&
				   i == other.i &&
				   s == other.s &&
				   EqualityComparer<List<float>>.Default.Equals(f, other.f) &&
				   b == other.b &&
				   t == other.t &&
				   u == other.u &&
				   v == other.v &&
				   w == other.w &&
				   x == other.x &&
				   y == other.y &&
				   z == other.z;
		}

		public override int GetHashCode()
		{
			HashCode hash = new HashCode();
			hash.Add(i);
			hash.Add(s);
			hash.Add(f);
			hash.Add(b);
			hash.Add(t);
			hash.Add(u);
			hash.Add(v);
			hash.Add(w);
			hash.Add(x);
			hash.Add(y);
			hash.Add(z);
			return hash.ToHashCode();
		}

		public static bool operator ==(EquatableClass left, EquatableClass right)
		{
			return EqualityComparer<EquatableClass>.Default.Equals(left, right);
		}

		public static bool operator !=(EquatableClass left, EquatableClass right)
		{
			return !(left == right);
		}
	}
}
