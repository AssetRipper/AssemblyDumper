using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace ReferenceLibrary.NullableAdventures
{
	public static class NullableExamples
	{
		public static void DoSomething(IBaseType obj)
		{
			if (obj.Has_S())
			{
				Console.WriteLine(obj.S[0]);
			}
			//Console.WriteLine(obj.S[0]);
		}
		[return: NotNull]
		public static string? LotsOfParameters(string s1, string? s2, string s3, string? s4, string s5, string? s6) => s1;
		public static string? GetNull() => null;
		public static string GetEmpty() => "";
		public static byte[]? NotNullProperty
		{
			[return: NotNull]
			get => backerField;
			set
			{
				backerField = value ?? Array.Empty<byte>();
			}
		}
		private static byte[] backerField = Array.Empty<byte>();
	}

	public interface IBaseType
	{
		[MemberNotNullWhen(true, "S")]
		bool Has_S();
		string? S { get; }
		string X { get; set; }
	}

	public class HasS : IBaseType
	{
		//[MemberNotNullWhen(true, "S")]
		public bool Has_S() => true;
		public string S
		{
			get => _S;
		}
		private string _S = "";
		public string X
		{
			get => "";
			set { }
		}
	}

	public class DoesNotHaveS : IBaseType
	{
		//[MemberNotNullWhen(true, "S")]
		public bool Has_S() => false;
		public string? S
		{
			get => null;
		}
		public string X
		{
			get => "";
			set { }
		}
	}

	public class ParameterSequenceTest
	{
		public string? InstanceMaybeNull(string s1, string? s2) => s1;

		public static string? StaticMaybeNull(string s1, string? s2) => s1;

		[return: NotNull]
		public string? InstanceNotNull(string s1, string? s2) => s1;

		[return: NotNull]
		public static string? StaticNotNull(string s1, string? s2) => s1;
	}
}
