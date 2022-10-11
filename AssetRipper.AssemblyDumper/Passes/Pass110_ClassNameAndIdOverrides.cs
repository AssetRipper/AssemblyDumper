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
					//instance.Type.AddClassIdOverride(id);
					instance.Type.AddClassNameOverride(instance.Name);
				}
			}
		}

		private static void AddClassIdOverride(this TypeDefinition type, int id)
		{
			PropertyDefinition property = type.AddGetterProperty(nameof(UnityObjectBase.ClassID), PropertyOverrideAttributes, SharedState.Instance.Importer.Int32);
			CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldc_I4, id);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
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
