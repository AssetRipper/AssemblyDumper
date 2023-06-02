namespace ReferenceLibrary
{
	internal static class IntegerConversions
	{
		private static byte u1;
		private static sbyte i1;
		private static ushort u2;
		private static short i2;
		private static uint u4;
		private static int i4;
		private static ulong u8;
		private static long i8;

		public static int ByteBacker
		{
			get => u1;
			set => u1 = (byte)value;
		}
		public static int SByteBacker
		{
			get => i1;
			set => i1 = (sbyte)value;
		}
		public static byte IntBacker
		{
			get => (byte)i4;
			set => i4 = value;
		}
		public static sbyte IntBacker2
		{
			get => (sbyte)i4;
			set => i4 = value;
		}
		public static ulong LongBacker
		{
			get => (ulong)i8;
			set => i8 = (long)value;
		}
		public static long ULongBacker
		{
			get => (long)u8;
			set => u8 = (ulong)value;
		}

		public static uint UncheckedCast(int value) => unchecked((uint)value);
		public static int UncheckedCast(uint value) => unchecked((int)value);
		public static uint CheckedCast(int value) => (uint)value;
		public static int CheckedCast(uint value) => (int)value;
	}
}
