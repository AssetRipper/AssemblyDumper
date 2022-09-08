using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core;

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
			TypeSignature classIdTypeSignature = SharedState.Instance.Importer.ImportTypeSignature<ClassIDType>();
			foreach ((int id, ClassGroup group) in SharedState.Instance.ClassGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.Type.AddClassIdOverride(id, classIdTypeSignature);
					instance.Type.AddClassNameOverride(instance.Name);
				}
			}
		}

		private static void AddClassIdOverride(this TypeDefinition type, int id, TypeSignature classIdTypeSignature)
		{
			PropertyDefinition property = type.AddGetterProperty(nameof(UnityObjectBase.ClassID), PropertyOverrideAttributes, classIdTypeSignature);
			CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldc_I4, id);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddClassNameOverride(this TypeDefinition type, string className)
		{
			PropertyDefinition property = type.AddGetterProperty(nameof(UnityObjectBase.AssetClassName), PropertyOverrideAttributes, SharedState.Instance.Importer.String);
			CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldstr, className);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
