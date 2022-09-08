using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static partial class InterfaceDocumenter
	{
		public static void AddInterfaceDocumentation(ClassGroupBase group)
		{
			if (TryGetHistoryForGroup(group, Passes.Pass350_AddEnums.historyFile, out ComplexTypeHistory? history))
			{
				AddDocumentationFromHistory(group, history, Passes.Pass350_AddEnums.historyFile);
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

		private static bool TryGetHistoryForGroup(ClassGroupBase group, HistoryFile historyFile, [NotNullWhen(true)] out ComplexTypeHistory? history)
		{
			if (!group.Instances[0].TryGetHistory(historyFile, out ComplexTypeHistory? firstHistory))
			{
				history = null;
				return false;
			}
			for (int i = 1; i < group.Instances.Count; i++)
			{
				GeneratedClassInstance instance = group.Instances[i];
				if (!instance.TryGetHistory(historyFile, out ComplexTypeHistory? subsequentHistory) || firstHistory != subsequentHistory)
				{
					history = null;
					return false;
				}
			}
			history = firstHistory;
			return true;
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
