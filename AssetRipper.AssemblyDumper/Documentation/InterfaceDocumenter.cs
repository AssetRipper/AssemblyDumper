using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static partial class InterfaceDocumenter
	{
		public static void AddInterfaceDocumentation(ClassGroupBase group)
		{
			if (group.History is not null)
			{
				AddDocumentationFromHistory(group, group.History, SharedState.Instance.HistoryFile);
			}
			AddInterfaceTypeDocumentation(group);
			AddInterfacePropertyDocumentation(group);
		}

		private static void AddDocumentationFromHistory(ClassGroupBase group, ComplexTypeHistory history, HistoryFile historyFile)
		{
			VersionedListDocumenter.AddSet(group.Interface, history.NativeName, "Native Name: ");
			VersionedListDocumenter.AddSet(group.Interface, history.DocumentationString, "Summary: ");
			VersionedListDocumenter.AddList(group.Interface, history.ObsoleteMessage, "Obsolete Message: ");

			Dictionary<PropertyDefinition, string> fieldsToProperties = GetFieldsToInterfaceProperties(group);
			foreach ((_, DataMemberHistory memberHistory) in history.GetAllMembers(historyFile))
			{
				string memberName = Passes.Pass002_RenameSubnodes.GetValidFieldName(memberHistory.Name);
				foreach ((PropertyDefinition property, string fieldName) in fieldsToProperties)
				{
					if (memberName == fieldName)
					{
						VersionedListDocumenter.AddSet(property, memberHistory.NativeName, "Native Name: ");
						VersionedListDocumenter.AddList(property, memberHistory.TypeFullName, "Managed Type: ");
						VersionedListDocumenter.AddSet(property, memberHistory.DocumentationString, "Summary: ");
						VersionedListDocumenter.AddList(property, memberHistory.ObsoleteMessage, "Obsolete Message: ");
					}
				}
			}
		}

		private static Dictionary<PropertyDefinition, string> GetFieldsToInterfaceProperties(ClassGroupBase group)
		{
			Dictionary<PropertyDefinition, string> result = new();

			foreach (PropertyDefinition interfaceProperty in group.InterfaceProperties)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					string? fieldName = instance.PropertiesToFields[instance.InterfacePropertiesToInstanceProperties[interfaceProperty]];
					if (fieldName is not null)
					{
						result.Add(interfaceProperty, fieldName);
						break;
					}
				}
			}

			return result;
		}
	}
}
