namespace ReferenceLibrary
{
	public static class StaticClass
	{
		public const string ConstString = nameof(StaticClass);
		public const int ConstInt = 42;
		public const bool ConstBool = true;
		public static string StaticString = ConstString;
		public static int StaticInt = ConstInt;
		public static bool StaticBool = ConstBool;
		private static readonly byte[] readOnlyDataLength4 = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		private static readonly byte[] readOnlyDataLength5 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
		private static readonly byte[] readOnlyDataLength8 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 , 0x08 };
		private static readonly byte[] readOnlyDataLength9 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 , 0x08, 0x09 };

		public static readonly HashSet<KeyValuePair<int, long>> HashSetExample = new HashSet<KeyValuePair<int, long>>()
		{
			new KeyValuePair<int, long>(ConstInt, ConstInt),
			new KeyValuePair<int, long>(32, 87),
			new KeyValuePair<int, long>(43, 83),
			new KeyValuePair<int, long>(42, 82),
			new KeyValuePair<int, long>(41, 81),
		};
	}
}
