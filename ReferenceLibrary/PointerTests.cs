namespace ReferenceLibrary
{
	public static class PointerTests
	{
		public static unsafe void Test()
		{
			IntPtr ptr = IntPtr.Zero;
			void* ptr2 = (void*)ptr;
			void** ptr3 = (void**)ptr2;
			TestStruct* ptr4 = (TestStruct*)ptr3;
			Console.WriteLine(ptr4->whatever.ToString());
		}

		internal struct TestStruct
		{
			public byte whatever;
		}
	}
}
