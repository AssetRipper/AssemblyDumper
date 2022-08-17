using System.Runtime.InteropServices;

namespace ReferenceLibrary.UnmanagedFunctionPointers
{
	public delegate void ExampleDelegate();

	public class DelegateExamples
	{
		public static unsafe void Test(delegate* unmanaged[Cdecl]<uint, int, short> functionPointer)
		{
			UseFunctionPointer(functionPointer);
		}

		public static void UseFunctionPointer(FunctionPointerStruct ptr)
		{
			Console.WriteLine(ptr.address);
		}

		public unsafe static void InvokeAction()
		{
			((ActionPointerStruct)(delegate* unmanaged[Cdecl]<void>)(&TestPrint)).Invoke();
		}

		[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static void TestPrint()
		{
			Console.WriteLine("Test!!!!!!!!!!!!");
		}
	}
}
