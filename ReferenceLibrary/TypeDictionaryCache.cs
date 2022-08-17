namespace ReferenceLibrary
{
	public static class TypeDictionaryCache
	{
		public readonly static Dictionary<Type, int> cache = new()
		{
			{ typeof(StaticClass), 0 },
			{ typeof(PointerTests), 1 },
			{ typeof(KeywordClass), 1 },
		};
	}
}
