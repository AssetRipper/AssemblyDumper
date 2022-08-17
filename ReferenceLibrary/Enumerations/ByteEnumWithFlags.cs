namespace ReferenceLibrary.Enumerations
{
	[Flags]
	public enum ByteEnumWithFlags : byte
	{
		None = 0,
		Bit0 = 1,
		Bit1 = 2,
		Bit2 = 4,
		Bit3 = 8,
		Bit4 = 16,
		Bit5 = 32,
		Bit6 = 64,
		Bit7 = 128,
	}
}
