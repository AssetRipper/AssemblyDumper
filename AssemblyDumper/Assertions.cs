using System;

namespace AssemblyDumper
{
	public static class Assertions
	{
		public static void AssertEquality(string left, string right)
		{
			if(left != right)
			{
				throw new InvalidOperationException($"{left} was not equal to {right}");
			}
		}
	}
}
