using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass054_AssignPropertyHistories
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					if (instance.History is null)
					{
						continue;
					}

					IReadOnlyDictionary<string, DataMemberHistory> members = instance.History.GetAllMembers(instance.VersionRange.Start, SharedState.Instance.HistoryFile);
					foreach (ClassProperty classProperty in instance.Properties)
					{
						SetHistory(classProperty, members);
					}
				}
				foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
				{
					interfaceProperty.History = interfaceProperty.DetermineHistoryFromImplementations();
				}
			}
		}

		private static void SetHistory(ClassProperty classProperty, IReadOnlyDictionary<string, DataMemberHistory> dictionary)
		{
			if (classProperty.OriginalFieldName is not null && dictionary.TryGetValue(classProperty.OriginalFieldName, out DataMemberHistory? history))
			{
			}
			else if (classProperty.BackingField is not null)
			{
				if (dictionary.TryGetValue(classProperty.BackingField.Name!, out history))
				{
				}
				else
				{
					history = dictionary.FirstOrDefault(pair =>
					{
						string fieldName = Pass002_RenameSubnodes.GetValidFieldName(pair.Value.Name);
						return fieldName == classProperty.BackingField.Name;
	
					}).Value;
				}
			}
			else
			{
				history = null;
			}
			classProperty.History = history;
		}

		private static DataMemberHistory? DetermineHistoryFromImplementations(this InterfaceProperty interfaceProperty)
		{
			DataMemberHistory? history = null;
			foreach (ClassProperty classProperty in interfaceProperty.Implementations)
			{
				if (history is null)
				{
					history = classProperty.History;
				}
				else if (classProperty.History is null)
				{
				}
				else if (history != classProperty.History)
				{
					return null;
				}
			}
			return history;
		}
	}
}
