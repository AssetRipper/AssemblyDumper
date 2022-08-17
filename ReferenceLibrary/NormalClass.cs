namespace ReferenceLibrary
{
	public class NormalClass
	{
		private bool booleanVariable;
		protected bool booleanWithInitializer = true;
		public int IntProperty => booleanVariable ? 1 : 0;

		public void StringEqualityMethod(string test)
		{
			if(test == "true")
			{
				booleanVariable = true;
			}
			else if(test == "false")
			{
				booleanVariable = false;
			}
			else
			{
				throw new ArgumentException(test, nameof(test));
			}
		}

		public static bool StaticStringEqualityMethod(string str1, string str2) => str1 == str2;
	}
}