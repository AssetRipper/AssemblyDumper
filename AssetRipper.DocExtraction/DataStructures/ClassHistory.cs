using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.MetaData;
using AssetRipper.VersionUtilities;

namespace AssetRipper.DocExtraction.DataStructures;

public sealed class ClassHistory : ComplexTypeHistory
{
	/// <summary>
	/// The full name for the base type of the class
	/// </summary>
	public VersionedList<FullName> BaseFullName { get; set; } = new();

	public override void Initialize(UnityVersion version, DocumentationBase first)
	{
		base.Initialize(version, first);
		ClassDocumentation @class = (ClassDocumentation)first;
		BaseFullName.Add(version, new FullName(@class.BaseNamespace, @class.BaseName));
	}

	protected override void AddNotNull(UnityVersion version, DocumentationBase next)
	{
		base.AddNotNull(version, next);
		AddIfNotEqual(BaseFullName, version, ((ClassDocumentation)next).BaseFullName);
	}

	public static ClassHistory From(UnityVersion version, ClassDocumentation @class)
	{
		ClassHistory? classHistory = new();
		classHistory.Initialize(version, @class);
		return classHistory;
	}
}