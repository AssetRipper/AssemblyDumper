using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass401_EqualityOperators
	{
		public static void DoPass()
		{
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.Name == Pass002_RenameSubnodes.Utf8StringName)
				{
					continue;
				}
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.AddDefaultEqualityOperators(
						SharedState.Instance.Importer,
						out MethodDefinition equalityMethod,
						out MethodDefinition inequalityMethod);

					equalityMethod.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
					inequalityMethod.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
				}
			}
		}
	}
}
