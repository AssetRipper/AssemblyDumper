using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass110_ClassNameAndIdOverrides
	{
		const MethodAttributes PropertyOverrideAttributes =
			MethodAttributes.Public |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.ReuseSlot |
			MethodAttributes.Virtual;

		public static void DoPass()
		{
			foreach ((int id, ClassGroup group) in SharedState.Instance.ClassGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.AddClassNameOverride(instance.Class.OriginalName);
				}
			}
		}

		private static void AddClassNameOverride(this TypeDefinition type, string className)
		{
			PropertyDefinition property = type.AddGetterProperty(nameof(UnityObjectBase.ClassName), PropertyOverrideAttributes, SharedState.Instance.Importer.String);
			CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldstr, className);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
