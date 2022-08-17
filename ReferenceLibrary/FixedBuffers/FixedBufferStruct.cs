namespace ReferenceLibrary.FixedBuffers
{
	public struct FixedBufferStruct
	{
		public byte element0;
		public byte element1;
		public byte element2;
		public byte element3;

		public byte this[int index]
		{
			get
			{
				return index switch
				{
					0 => element0,
					1 => element1,
					2 => element2,
					3 => element3,
					_ => throw new ArgumentOutOfRangeException(),
				};
			}
			set
			{
				switch (index)
				{
					case 0:
						element0 = value;
						return;
					case 1:
						element1 = value;
						return;
					case 2:
						element2 = value;
						return;
					case 3:
						element3 = value;
						return;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}
