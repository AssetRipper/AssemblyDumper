namespace ReferenceLibrary.BitFieldExperiments
{
	public static class BitFieldHelper64
	{
		public static ulong GetBitMask(int start, int size)
		{
			return RemoveLeftBits(RemoveRightBits(ulong.MaxValue, start), 64 - size - start);
		}

		public static ulong RemoveLeftBits(ulong value, int num)
		{
			return value << num >> num;
		}

		public static ulong RemoveRightBits(ulong value, int num)
		{
			return value >> num << num;
		}

		public static ulong GetBitSelection(ulong bitFieldValue, int start, int size)
		{
			return bitFieldValue >> start << (64 - size) >> (64 - size);
		}

		public static bool GetSingleBit(ulong bitFieldValue, int start)
		{
			return GetBitSelection(bitFieldValue, start, 1) != 0;
		}

		public static ulong SetBitSelection(ulong bitFieldOldValue, ulong assignedValue, int start, int size)
		{
			ulong bitMask = GetBitMask(start, size);
			bitFieldOldValue &= ~bitMask;
			return bitFieldOldValue | ((assignedValue << start) & bitMask);
		}

		public static ulong SetSingleBit(ulong bitFieldOldValue, bool assignedValue, int start)
		{
			return SetBitSelection(bitFieldOldValue, assignedValue ? 1UL : 0, start, 1);
		}
	}

	internal static class BitFieldHelper32
	{
		public static uint GetBitMask(int start, int size)
		{
			return RemoveLeftBits(RemoveRightBits(uint.MaxValue, start), 32 - size - start);
		}

		public static uint RemoveLeftBits(uint value, int num)
		{
			return value << num >> num;
		}

		public static uint RemoveRightBits(uint value, int num)
		{
			return value >> num << num;
		}

		public static uint GetBitSelection(uint bitFieldValue, int start, int size)
		{
			return bitFieldValue >> start << (32 - size) >> (32 - size);
		}

		public static bool GetSingleBit(uint bitFieldValue, int start)
		{
			return GetBitSelection(bitFieldValue, start, 1) != 0;
		}

		public static uint SetBitSelection(uint bitFieldOldValue, uint assignedValue, int start, int size)
		{
			uint bitMask = GetBitMask(start, size);
			bitFieldOldValue &= ~bitMask;
			return bitFieldOldValue | ((assignedValue << start) & bitMask);
		}

		public static uint SetSingleBit(uint bitFieldOldValue, bool assignedValue, int start)
		{
			return SetBitSelection(bitFieldOldValue, assignedValue ? 1U : 0, start, 1);
		}
	}

	internal static class BitFieldHelper16
	{
		public static ushort GetBitSelection(ushort bitFieldValue, int start, int size)
		{
			return unchecked((ushort)BitFieldHelper32.GetBitSelection(bitFieldValue, start, size));
		}

		public static bool GetSingleBit(ushort bitFieldValue, int start)
		{
			return GetBitSelection(bitFieldValue, start, 1) != 0;
		}

		public static ushort SetBitSelection(ushort bitFieldOldValue, ushort assignedValue, int start, int size)
		{
			return unchecked((ushort)BitFieldHelper32.SetBitSelection(bitFieldOldValue, assignedValue, start, size));
		}

		public static ushort SetSingleBit(ushort bitFieldOldValue, bool assignedValue, int start)
		{
			return unchecked((ushort)BitFieldHelper32.SetBitSelection(bitFieldOldValue, assignedValue ? 1U : 0, start, 1));
		}
	}

	internal static class BitFieldHelper8
	{
		public static byte GetBitSelection(byte bitFieldValue, int start, int size)
		{
			return unchecked((byte)BitFieldHelper32.GetBitSelection(bitFieldValue, start, size));
		}

		public static bool GetSingleBit(byte bitFieldValue, int start)
		{
			return GetBitSelection(bitFieldValue, start, 1) != 0;
		}

		public static byte SetBitSelection(byte bitFieldOldValue, byte assignedValue, int start, int size)
		{
			return unchecked((byte)BitFieldHelper32.SetBitSelection(bitFieldOldValue, assignedValue, start, size));
		}

		public static byte SetSingleBit(byte bitFieldOldValue, bool assignedValue, int start)
		{
			return unchecked((byte)BitFieldHelper32.SetBitSelection(bitFieldOldValue, assignedValue ? 1U : 0, start, 1));
		}
	}
}
