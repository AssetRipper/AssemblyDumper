using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class NullableExtensions
	{
		internal static void AddNullableAttributesForMaybeNull(this PropertyDefinition property)
		{
			TypeSignature typeSignature = property.Signature!.ReturnType;
			if (typeSignature is SzArrayTypeSignature or GenericInstanceTypeSignature)
			{
				property.AddNullableAttribute(GetNullableByteArray(typeSignature));
			}
			else
			{
				property.AddNullableAttribute(NullableAnnotation.MaybeNull);
			}
			property.GetMethod!.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
			property.SetMethod?.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
		}

		internal static void AddNullableAttributesForMaybeNull(this FieldDefinition field)
		{
			TypeSignature typeSignature = field.Signature!.FieldType;
			if (typeSignature is SzArrayTypeSignature or GenericInstanceTypeSignature)
			{
				field.AddNullableAttribute(GetNullableByteArray(typeSignature));
			}
			else
			{
				field.AddNullableAttribute(NullableAnnotation.MaybeNull);
			}
		}

		internal static CustomAttribute AddNullableAttribute(this IHasCustomAttribute hasCustomAttribute, NullableAnnotation annotation)
		{
			return hasCustomAttribute.AddCustomAttribute(SharedState.Instance.NullableAttributeConstructorByte, SharedState.Instance.Importer.UInt8, (byte)annotation);
		}

		internal static CustomAttribute AddNullableAttribute(this IHasCustomAttribute hasCustomAttribute, byte[] annotationArray)
		{
			return hasCustomAttribute.AddCustomAttribute(SharedState.Instance.NullableAttributeConstructorByteArray, SharedState.Instance.Importer.UInt8.MakeSzArrayType(), annotationArray);
		}

		internal static CustomAttribute AddNullableContextAttribute(this IHasCustomAttribute hasCustomAttribute, NullableAnnotation annotation)
		{
			return hasCustomAttribute.AddCustomAttribute(SharedState.Instance.NullableContextAttributeConstructor, SharedState.Instance.Importer.UInt8, (byte)annotation);
		}

		internal static CustomAttribute AddNotNullAttribute(this MethodDefinition method)
		{
			IMethodDefOrRef attributeConstructor = SharedState.Instance.Importer.ImportDefaultConstructor<NotNullAttribute>();
			return method
				.GetOrAddReturnTypeParameterDefinition()
				.AddCustomAttribute(attributeConstructor);
		}

		private static byte[] GetNullableByteArray(TypeSignature type)
		{
			List<byte> result = new();
			AddNullableIndicatorBytes(type, result);
			result[0] = 2;
			return result.ToArray();
		}

		private static void AddNullableIndicatorBytes(TypeSignature type, List<byte> byteList)
		{
			byteList.Add(type.IsValueType ? (byte)0 : (byte)1);
			if (type is SzArrayTypeSignature arrayType)
			{
				AddNullableIndicatorBytes(arrayType.BaseType, byteList);
			}
			else if (type is GenericInstanceTypeSignature genericInstanceTypeSignature)
			{
				foreach (TypeSignature typeArgument in genericInstanceTypeSignature.TypeArguments)
				{
					AddNullableIndicatorBytes(typeArgument, byteList);
				}
			}
		}
	}
}
