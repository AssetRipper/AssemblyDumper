using AssetRipper.AssemblyDumper.Documentation;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass999_Documentation
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				InterfaceDocumenter.AddInterfaceDocumentation(group);

				foreach (GeneratedClassInstance instance in group.Instances)
				{
					ClassDocumenter.AddClassDocumentation(instance);
				}
			}

			IdEnumDocumenter.AddIdEnumDocumentation();

			foreach ((TypeDefinition type, EnumHistory history) in Pass040_AddEnums.EnumDictionary.Values)
			{
				EnumTypeDocumenter.AddEnumTypeDocumentation(type, history);
			}

			DocumentationHandler.MakeDocumentationFile();
		}
	}
}
