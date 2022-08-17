namespace ReferenceLibrary
{
	public static class UnmanagedConstraint
	{
		private struct BlittableStruct
		{
			byte value;
		}

		private struct NonblittableStruct
		{
			string value;
		}

		public static void MethodWithUnmanagedTypeConstraint<T>() where T : unmanaged
		{
		}

		public static void Test()
		{
			MethodWithUnmanagedTypeConstraint<BlittableStruct>();
		}
	}
}
