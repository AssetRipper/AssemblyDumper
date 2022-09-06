using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.MetaData;
using AssetRipper.VersionUtilities;

namespace AssetRipper.DocExtraction.DataStructures;

public sealed class DataMemberHistory : HistoryBase
{
	/// <summary>
	/// The full name for the return type of the property
	/// </summary>
	public VersionedList<FullName> TypeFullName { get; set; } = new();

	public override void Initialize(UnityVersion version, DocumentationBase first)
	{
		base.Initialize(version, first);
		DataMemberDocumentation dataMember = (DataMemberDocumentation)first;
		TypeFullName.Add(version, new FullName(dataMember.TypeNamespace, dataMember.TypeName));
	}

	protected override void AddNotNull(UnityVersion version, DocumentationBase next)
	{
		base.AddNotNull(version, next);
		AddIfNotEqual(TypeFullName, version, ((DataMemberDocumentation)next).TypeFullName);
	}
}
