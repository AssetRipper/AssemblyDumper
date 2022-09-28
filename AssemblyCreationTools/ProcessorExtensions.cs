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

		public static CilLocalVariable AddLocalVariable(this CilInstructionCollection processor, TypeSignature variableType)
		{
			CilLocalVariable variable = new CilLocalVariable(variableType); //Create local
			processor.Owner.LocalVariables.Add(variable); //Add to method
			return variable;
		}

		public static CilInstruction AddLoadElement(this CilInstructionCollection processor, TypeSignature elementType)
		{
			return elementType is CorLibTypeSignature corLibTypeSignature
				? corLibTypeSignature.ElementType switch
				{
					ElementType.Boolean => processor.Add(CilOpCodes.Ldelem_U1),
					ElementType.I1 => processor.Add(CilOpCodes.Ldelem_I1),
					ElementType.U1 => processor.Add(CilOpCodes.Ldelem_U1),
					ElementType.I2 => processor.Add(CilOpCodes.Ldelem_I2),
					ElementType.U2 => processor.Add(CilOpCodes.Ldelem_U2),
					ElementType.I4 => processor.Add(CilOpCodes.Ldelem_I4),
					ElementType.U4 => processor.Add(CilOpCodes.Ldelem_U4),
					ElementType.I8 => processor.Add(CilOpCodes.Ldelem_I8),
					ElementType.U8 => processor.Add(CilOpCodes.Ldelem_I8),
					ElementType.R4 => processor.Add(CilOpCodes.Ldelem_R4),
					ElementType.R8 => processor.Add(CilOpCodes.Ldelem_R8),
					_ => processor.Add(CilOpCodes.Ldelem_Ref),
				}
				: processor.Add(CilOpCodes.Ldelem_Ref);
		}
	}
}
