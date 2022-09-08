#nullable enable

namespace ReferenceLibrary.NullableAdventures
{
	public class GenericClass<T1, T2, T3, T4, T5, T6>
		where T2 : struct
		where T3 : class, new()
		where T4 : class?
		where T5 : class?
		where T6 : class?
	{
	}
}
