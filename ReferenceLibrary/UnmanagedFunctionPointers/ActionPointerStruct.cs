using System.Runtime.InteropServices;

namespace ReferenceLibrary.UnmanagedFunctionPointers
{
	public unsafe struct ActionPointerStruct
	{
		public IntPtr address;

		public ActionPointerStruct(IntPtr address)
		{
			this.address = address;
		}

		public static implicit operator delegate* unmanaged[Cdecl]<void>(ActionPointerStruct ptrStruct)
		{
			return (delegate* unmanaged[Cdecl]<void>)ptrStruct.address;
		}

		public static implicit operator ActionPointerStruct(delegate* unmanaged[Cdecl]<void> del)
		{
			return new ActionPointerStruct((IntPtr)del);
		}

		public static explicit operator Action(ActionPointerStruct ptrStruct)
		{
			return Marshal.GetDelegateForFunctionPointer<Action>(ptrStruct.address);
		}

		public static explicit operator IntPtr(ActionPointerStruct ptrStruct)
		{
			return ptrStruct.address;
		}

		public static explicit operator ActionPointerStruct(IntPtr ptr)
		{
			return new ActionPointerStruct(ptr);
		}

		public void Invoke()
		{
			((Action)this).Invoke();
		}
	}
}
