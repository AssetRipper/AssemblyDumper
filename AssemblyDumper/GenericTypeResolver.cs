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

			GenericInstanceType genericKvp = ResolvePairType(pairNode);

			return CommonTypeGetter.AssetDictionaryType.MakeGenericInstanceType(genericKvp.GenericArguments[0], genericKvp.GenericArguments[1]);
		}

		public static ArrayType ResolveVectorType(UnityNode vectorNode)
		{
			return ResolveArrayType(vectorNode.SubNodes[0]);
		}

		public static ArrayType ResolveArrayType(UnityNode arrayNode)
		{
			UnityNode contentNode = arrayNode.SubNodes[1];
			TypeReference elementType = ResolveNode(contentNode);
			return SharedState.Module.ImportReference(elementType).MakeArrayType();
		}

		public static GenericInstanceType ResolvePairType(UnityNode pairNode)
		{
			return ResolvePairType(pairNode.SubNodes[0], pairNode.SubNodes[1]);
		}
		public static GenericInstanceType ResolvePairType(UnityNode first, UnityNode second)
		{
			TypeReference firstType = ResolveNode(first);
			TypeReference secondType = ResolveNode(second);

			//Construct a KeyValuePair
			TypeReference kvpType = CommonTypeGetter.NullableKeyValuePair;
			GenericInstanceType genericKvp = kvpType.MakeGenericInstanceType(firstType, secondType);
			return genericKvp;
		}

		public static TypeReference ResolveNode(UnityNode node)
		{
			string typeName = node.TypeName;
			if (typeName == "pair")
			{
				return ResolvePairType(node);
			}
			else if (typeName == "map")
			{
				return ResolveDictionaryType(node);
			}
			else if (typeName is "vector" or "set" or "staticvector")
			{
				return ResolveVectorType(node);
			}
			else if (typeName == "Array")
			{
				return ResolveArrayType(node);
			}
			else
			{
				return SharedState.Module.ImportReference(SharedState.Module.GetPrimitiveType(typeName) ?? SystemTypeGetter.LookupSystemType(typeName) ?? SharedState.TypeDictionary[typeName]);
			}
		}
	}
}
