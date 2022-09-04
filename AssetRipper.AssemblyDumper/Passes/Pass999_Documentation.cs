using AssetRipper.AssemblyDumper.Documentation;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass999_Documentation
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				InterfaceTypeDocumenter.AddInterfaceTypeDocumentation(group);
				InterfacePropertyDocumenter.AddInterfacePropertyDocumentation(group);

				foreach (GeneratedClassInstance instance in group.Instances)
				{
					ClassTypeDocumenter.AddClassTypeDocumentation(instance);
					ClassPropertyDocumenter.AddClassPropertyDocumentation(instance);
				}
			}

			IdEnumDocumenter.AddIdEnumDocumentation();

			DocumentationHandler.MakeDocumentationFile();
		}
	}
}
