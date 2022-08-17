using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferenceLibrary
{
	public static class DummyMethods
	{
		public static void NoParametersNoReturn() { }

		public static int NoParametersValueTypeReturn()
		{
			return default;
		}

		public static string NoParametersReferenceTypeReturn()
		{
			return default;
		}

		public static void ReferenceTypeParameterNoReturn(string parameter) { }

		public static void ValueTypeParameterNoReturn(int parameter) { }

		public static void RefValueTypeParameterNoReturn(ref int parameter) { }

		public static void InReferenceTypeParameterNoReturn(in string parameter) { }

		public static void InValueTypeParameterNoReturn(in int parameter) { }

		public static void OutReferenceTypeParameterNoReturn(out string parameter)
		{
			parameter = default;
		}

		public static void OutBoolParameterNoReturn(out bool parameter)
		{
			parameter = default;
		}

		public static void OutByteParameterNoReturn(out byte parameter)
		{
			parameter = default;
		}

		public static void OutUIntParameterNoReturn(out uint parameter)
		{
			parameter = default;
		}

		public static void OutIntParameterNoReturn(out int parameter)
		{
			parameter = default;
		}

		public static void OutLongParameterNoReturn(out long parameter)
		{
			parameter = default;
		}

		public static void OutStructParameterNoReturn(out HashCode parameter)
		{
			parameter = default;
		}
	}
}
