using System.Runtime.InteropServices;

namespace ReferenceLibrary.UnmanagedFunctionPointers
{
	public unsafe struct FunctionPointerStruct
	{
		public IntPtr address;

		public FunctionPointerStruct(IntPtr address)
		{
			this.address = address;
		}

		public static implicit operator delegate* unmanaged[Cdecl]<uint, int, short>(FunctionPointerStruct ptrStruct)
		{
			return (delegate* unmanaged[Cdecl]<uint, int, short>)ptrStruct.address;
		}

		public static implicit operator FunctionPointerStruct(delegate* unmanaged[Cdecl]<uint, int, short> del)
		{
			return new FunctionPointerStruct((IntPtr)del);
		}

		public static explicit operator Func<uint, int, ushort>(FunctionPointerStruct ptrStruct)
		{
			return Marshal.GetDelegateForFunctionPointer<Func<uint, int, ushort>>(ptrStruct.address);
		}

		public static explicit operator IntPtr(FunctionPointerStruct ptrStruct)
		{
			return ptrStruct.address;
		}

		public static explicit operator FunctionPointerStruct(IntPtr ptr)
		{
			return new FunctionPointerStruct(ptr);
		}

		public ushort Invoke(uint param1, int param2)
		{
			return ((Func<uint, int, ushort>)this).Invoke(param1, param2);
		}
	}
}
