using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.AssemblyDumper.Documentation;

internal static partial class InterfaceDocumenter
{
	private static void AddInterfacePropertyDocumentation(ClassGroupBase group)
	{
		foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
		{
			DiscontinuousRange<UnityVersion> releaseOnlyRange = interfaceProperty.ReleaseOnlyRange;
			if (!releaseOnlyRange.IsEmpty())
			{
				if (releaseOnlyRange == interfaceProperty.PresentRange)
				{
					DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, "Release Only");
				}
				else
				{
					DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, $"Sometimes Release Only: {releaseOnlyRange.GetString(interfaceProperty.Group.MinimumVersion)}");
				}
			}

			DiscontinuousRange<UnityVersion> editorOnlyRange = interfaceProperty.EditorOnlyRange;
			if (!editorOnlyRange.IsEmpty())
			{
				if (editorOnlyRange == interfaceProperty.PresentRange)
				{
					DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, "Editor Only");
				}
				else
				{
					DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, $"Sometimes Editor Only: {editorOnlyRange.GetString(interfaceProperty.Group.MinimumVersion)}");
				}
			}

			if (interfaceProperty.HasMethod is not null)
			{
				string versionString = interfaceProperty.PresentRange.GetString(group.MinimumVersion);
				DocumentationHandler.AddMethodDefinitionLine(interfaceProperty.HasMethod, versionString);
				DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, versionString);
			}
			else
			{
				DocumentationHandler.AddPropertyDefinitionLine(interfaceProperty, interfaceProperty.Definition.IsValueType() ? "Not absent" : "Not null");
			}
		}
	}
}
