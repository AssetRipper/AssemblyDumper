using AssetRipper.Core.IO;

namespace AssetRipper.AssemblyDumper
{
	internal static class GenericTypeResolver
	{
		public static GenericInstanceTypeSignature ResolveDictionaryType(UniversalNode node, UnityVersion version)
		{
			UniversalNode pairNode = node.SubNodes![0] //Array
				.SubNodes![1]; //Pair

			GenericInstanceTypeSignature genericKvp = ResolvePairType(pairNode, version);
			
			return SharedState.Instance.Importer.ImportType(typeof(AssetDictionary<,>)).MakeGenericInstanceType(genericKvp.TypeArguments[0], genericKvp.TypeArguments[1]);
		}

		public static TypeSignature ResolveVectorType(UniversalNode vectorNode, UnityVersion version)
		{
			return ResolveArrayType(vectorNode.SubNodes![0], version);
		}

		public static TypeSignature ResolveArrayType(UniversalNode arrayNode, UnityVersion version)
		{
			UniversalNode contentNode = arrayNode.SubNodes![1];
			TypeSignature elementType = ResolveNode(contentNode, version);
			
			if(elementType is SzArrayTypeSignature or CorLibTypeSignature)
			{
				return elementType.MakeSzArrayType();
			}

			return SharedState.Instance.Importer.ImportType(typeof(AssetList<>)).MakeGenericInstanceType(elementType);
		}

		public static SzArrayTypeSignature MakeAndImportArrayType(this ITypeDefOrRef type)
		{
			return MakeAndImportArrayType(type.ToTypeSignature());
		}

		public static SzArrayTypeSignature MakeAndImportArrayType(this TypeSignature typeSignature)
		{
			return new SzArrayTypeSignature(SharedState.Instance.Importer.UnderlyingImporter.ImportTypeSignature(typeSignature));
		}

		public static GenericInstanceTypeSignature ResolvePairType(UniversalNode pairNode, UnityVersion version)
		{
			return ResolvePairType(pairNode.SubNodes![0], pairNode.SubNodes[1], version);
		}
		public static GenericInstanceTypeSignature ResolvePairType(UniversalNode first, UniversalNode second, UnityVersion version)
		{
			TypeSignature firstType = ResolveNode(first, version);
			TypeSignature secondType = ResolveNode(second, version);

			if (firstType is SzArrayTypeSignature || secondType is SzArrayTypeSignature)
			{
				throw new Exception("Arrays not supported in pairs/dictionaries");
			}

			//Construct a KeyValuePair
			ITypeDefOrRef kvpType = SharedState.Instance.Importer.ImportType(typeof(NullableKeyValuePair<,>));
			GenericInstanceTypeSignature genericKvp = kvpType.MakeGenericInstanceType(firstType, secondType);
			return genericKvp;
		}

		public static TypeSignature ResolveNode(UniversalNode node, UnityVersion version)
		{
			string typeName = node.TypeName!;
			return typeName switch
			{
				"pair" => ResolvePairType(node, version),
				"map" => ResolveDictionaryType(node, version),
				"vector" or "set" or "staticvector" => ResolveVectorType(node, version),
				"TypelessData" => SharedState.Instance.Importer.UInt8.MakeSzArrayType(),
				"Array" => ResolveArrayType(node, version),
				_ => SharedState.Instance.Importer.GetCppPrimitiveTypeSignature(typeName)
						?? SharedState.Instance.SubclassGroups[typeName].GetTypeForVersion(version).ToTypeSignature(),
			};
		}
	}
}
