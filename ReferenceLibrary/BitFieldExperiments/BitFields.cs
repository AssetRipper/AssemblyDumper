namespace ReferenceLibrary.BitFieldExperiments
{
	public struct BitFields
	{
		uint bitField1;
		byte bitField2;

		public bool Bit_00_01
		{
			get => BitFieldHelper32.GetSingleBit(bitField1, 0);
			set => bitField1 = BitFieldHelper32.SetSingleBit(bitField1, value, 0);
		}

		public byte Bit_01_07
		{
			get => (byte)BitFieldHelper32.GetBitSelection(bitField1, 1, 7);
			set => bitField1 = BitFieldHelper32.SetBitSelection(bitField1, value, 1, 7);
		}

		public ushort Bit_08_16
		{
			get => (ushort)BitFieldHelper32.GetBitSelection(bitField1, 8, 16);
			set => bitField1 = BitFieldHelper32.SetBitSelection(bitField1, value, 8, 16);
		}

		public bool Bit_32_01
		{
			get => BitFieldHelper8.GetSingleBit(bitField2, 0);
			set => bitField2 = BitFieldHelper8.SetSingleBit(bitField2, value, 0);
		}
	}
}
