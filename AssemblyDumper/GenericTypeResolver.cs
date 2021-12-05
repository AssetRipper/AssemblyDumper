using AssemblyDumper.Unity;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper
{
	public static class GenericTypeResolver
	{
		public static GenericInstanceType ResolveDictionaryType(UnityNode node)
		{
			UnityNode pairNode = node.SubNodes[0] //Array
				.SubNodes[1]; //Pair

			UnityNode first = pairNode.SubNodes[0];
			UnityNode second = pairNode.SubNodes[1];
			GenericInstanceType genericKvp = ResolvePairType(first, second);

			return CommonTypeGetter.AssetDictionaryType.MakeGenericInstanceType(genericKvp.GenericArguments[0], genericKvp.GenericArguments[1]);
		}

		public static ArrayType ResolveVectorType(UnityNode node)
		{
			UnityNode contentNode = node.SubNodes[0].SubNodes[1];
			string typeName = contentNode.TypeName;
			TypeReference elementType = SharedState.Module.GetPrimitiveType(typeName) ?? SystemTypeGetter.LookupSystemType(typeName) ?? SharedState.TypeDictionary[typeName];
			return SharedState.Module.ImportReference(elementType).MakeArrayType();
		}

		public static GenericInstanceType ResolvePairType(UnityNode first, UnityNode second)
		{
			string firstName = first.TypeName;
			string secondName = second.TypeName;

			TypeReference firstType;
			TypeReference secondType;
			if (firstName == "pair")
				firstType = ResolvePairType(first.SubNodes[0], first.SubNodes[1]);
			else if (firstName == "map")
				firstType = ResolveDictionaryType(first);
			else if (firstName is "vector" or "set" or "staticvector")
				firstType = ResolveVectorType(first);
			else
				firstType = SharedState.Module.ImportReference(SharedState.Module.GetPrimitiveType(firstName) ?? SystemTypeGetter.LookupSystemType(firstName) ?? SharedState.TypeDictionary[firstName]);

			if (secondName == "pair")
				secondType = ResolvePairType(second.SubNodes[0], second.SubNodes[1]);
			else if (secondName == "map")
				secondType = ResolveDictionaryType(second);
			else if (secondName is "vector" or "set" or "staticvector")
				secondType = ResolveVectorType(second);
			else
				secondType = SharedState.Module.ImportReference(SharedState.Module.GetPrimitiveType(secondName) ?? SystemTypeGetter.LookupSystemType(secondName) ?? SharedState.TypeDictionary[secondName]);

			//Construct a KeyValuePair
			TypeReference kvpType = SystemTypeGetter.KeyValuePair;
			GenericInstanceType genericKvp = kvpType.MakeGenericInstanceType(firstType, secondType);
			return genericKvp;
		}
	}
}
