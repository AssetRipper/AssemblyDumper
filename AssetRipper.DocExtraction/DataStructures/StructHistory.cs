using AssetRipper.DocExtraction.MetaData;
using AssetRipper.VersionUtilities;

namespace AssetRipper.DocExtraction.DataStructures;

public sealed class StructHistory : ComplexTypeHistory
{
	public static StructHistory From(UnityVersion version, StructDocumentation @struct)
	{
		StructHistory? history = new();
		history.Initialize(version, @struct);
		return history;
	}
}
