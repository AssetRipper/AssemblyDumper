using AssetRipper.Assets.Export.Dependencies;
using AssetRipper.Assets.Metadata;
using DependencyEnumerable = System.Collections.Generic.IEnumerable<System.ValueTuple<AssetRipper.Assets.Export.Dependencies.FieldName, AssetRipper.Assets.Metadata.PPtr<AssetRipper.Assets.IUnityObjectBase>>>;

#nullable disable

namespace AssetRipper.AssemblyDumper.InjectedTypes
{
	internal static class FetchDependenciesHelper
	{
		public static DependencyEnumerable FromSingle(FieldName fieldName, IPPtr pptr)
		{
			yield return (fieldName, pptr.ToStruct());
		}

		public static DependencyEnumerable AppendPPtr(this DependencyEnumerable items, FieldName fieldName, IPPtr pptr)
		{
			return items.Append((fieldName, pptr.ToStruct()));
		}
	}
}

#nullable enable