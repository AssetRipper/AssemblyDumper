namespace ReferenceLibrary.Enumerations
{
	public class EnumCasting
	{
		public enum Enum32Bit
		{
			Item1,
			Item2,
			Item3,
			Item4,
		}

		private byte valueField;

		public Enum32Bit ValueProperty
		{
			get => (Enum32Bit)valueField;
			set => valueField = (byte)value;
		}
	}
}
