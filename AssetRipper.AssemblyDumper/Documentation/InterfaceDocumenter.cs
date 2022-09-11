using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static partial class InterfaceDocumenter
	{
		public static void AddInterfaceDocumentation(ClassGroupBase group)
		{
			AddDocumentationFromHistory(group);
			AddInterfaceTypeDocumentation(group);
			AddInterfacePropertyDocumentation(group);
		}

		private static void AddDocumentationFromHistory(ClassGroupBase group)
		{
			if (group.History is not null)
			{
				VersionedListDocumenter.AddSet(group.Interface, group.History.NativeName, "Native Name: ");
				VersionedListDocumenter.AddSet(group.Interface, group.History.DocumentationString, "Summary: ");
				VersionedListDocumenter.AddList(group.Interface, group.History.ObsoleteMessage, "Obsolete Message: ");
			}

			foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
			{
				if (interfaceProperty.History is not null)
				{
					VersionedListDocumenter.AddSet(interfaceProperty.Definition, interfaceProperty.History.NativeName, "Native Name: ");
					VersionedListDocumenter.AddList(interfaceProperty.Definition, interfaceProperty.History.TypeFullName, "Managed Type: ");
					VersionedListDocumenter.AddSet(interfaceProperty.Definition, interfaceProperty.History.DocumentationString, "Summary: ");
					VersionedListDocumenter.AddList(interfaceProperty.Definition, interfaceProperty.History.ObsoleteMessage, "Obsolete Message: ");
				}
			}
		}
	}
}
