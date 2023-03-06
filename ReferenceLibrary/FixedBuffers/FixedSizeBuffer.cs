namespace ReferenceLibrary.FixedBuffers
{
	public unsafe struct FixedSizeBuffer
	{
		public fixed byte byteBuffer[128];
		public fixed ushort ushortBuffer[128];

		public void Test()
		{
			byteBuffer[2] = 3;
			byteBuffer[130] = 3;
		}
	}
}
