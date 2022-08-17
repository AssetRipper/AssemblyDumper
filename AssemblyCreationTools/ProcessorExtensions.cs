namespace AssetRipper.AssemblyCreationTools
{
	public static class ProcessorExtensions
	{
		public static void AddDefaultValue(this CilInstructionCollection processor, ITypeDefOrRef targetType)
		{
			processor.AddDefaultValue(targetType.ToTypeSignature());
		}

		public static void AddDefaultValue(this CilInstructionCollection processor, TypeSignature targetType)
		{
			if (targetType.FullName == "System.Void")
			{
				//Do nothing
			}
			else if (targetType.IsValueType)
			{
				CilLocalVariable variable = new CilLocalVariable(targetType);
				processor.Owner.LocalVariables.Add(variable);
				processor.Add(CilOpCodes.Ldloca, variable);
				processor.Add(CilOpCodes.Initobj, targetType.ToTypeDefOrRef());
				processor.Add(CilOpCodes.Ldloc, variable);
			}
			else
			{
				processor.Add(CilOpCodes.Ldnull);
			}
		}

		/// <summary>
		/// Remove the last instruction in the processor's collection
		/// </summary>
		/// <param name="processor">The processor to remove the instruction from</param>
		public static void Pop(this CilInstructionCollection processor) => processor.RemoveAt(processor.Count - 1);
	}
}
