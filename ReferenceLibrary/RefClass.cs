using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReferenceLibrary
{
	public class RefClassInt
	{
		public RefClassInt(int normalVar, in int inVar, ref int refVar, out int outVar)
		{
			outVar = default;
		}

		public RefClassInt() : this(default, default, ref Unsafe.NullRef<int>(), out int outVar2)
		{
		}

		public unsafe RefClassInt([In][Out] int normalVar) : this(normalVar, in *(int*)null, ref *(int*)null, out *(int*)null)
		{
		}

		public static ref int RefReturn()
		{
			return ref Unsafe.NullRef<int>();
		}

		public static int NormalReturn() => default;
	}
	public class RefClassFloat
	{
		public RefClassFloat(float normalVar, in float inVar, ref float refVar, out float outVar)
		{
			outVar = default;
		}

		public RefClassFloat() : this(default, default, ref Unsafe.NullRef<float>(), out float outVar2)
		{
		}

		public static ref float RefReturn()
		{
			return ref Unsafe.NullRef<float>();
		}

		public static float NormalReturn() => default;
	}
	public class RefClassString
	{
		public RefClassString(string normalVar, in string inVar, ref string refVar, out string outVar)
		{
			outVar = default;
		}

		public RefClassString() : this(default, default, ref Unsafe.NullRef<string>(), out string outVar2)
		{
		}

		public static ref string RefReturn()
		{
			return ref Unsafe.NullRef<string>();
		}

		public static string NormalReturn() => default;
	}
	public class RefClassGeneric<T>
	{
		public RefClassGeneric(T normalVar, in T inVar, ref T refVar, out T outVar)
		{
			outVar = default;
		}

		public RefClassGeneric() : this(default, default, ref Unsafe.NullRef<T>(), out T outVar2)
		{
		}

		public static ref T RefReturn()
		{
			return ref Unsafe.NullRef<T>();
		}

		public static T NormalReturn() => default;
	}
	public class RefClassGenericValueType<T> where T : struct
	{
		public RefClassGenericValueType(T normalVar, in T inVar, ref T refVar, out T outVar)
		{
			outVar = default;
		}

		public RefClassGenericValueType() : this(default, default, ref Unsafe.NullRef<T>(), out T outVar2)
		{
		}

		public static ref T RefReturn()
		{
			return ref Unsafe.NullRef<T>();
		}

		public static T NormalReturn() => default;
	}
	public class RefClassGenericReferenceType<T> where T : class
	{
		public RefClassGenericReferenceType(T normalVar, in T inVar, ref T refVar, out T outVar)
		{
			outVar = default;
		}

		public RefClassGenericReferenceType() : this(default, default, ref Unsafe.NullRef<T>(), out T outVar2)
		{
		}

		public static ref T RefReturn()
		{
			return ref Unsafe.NullRef<T>();
		}

		public static T NormalReturn() => default;
	}
}