using AssetRipper.Primitives;

namespace AssetRipper.AssemblyDumper;

internal static class TypeSignatureExtensions
{
	public static bool IsUtf8String(this TypeSignature signature)
	{
		return signature is TypeDefOrRefSignature { Name: nameof(Utf8String) } signature2 && signature2.Namespace == typeof(Utf8String).Namespace;
	}
}
