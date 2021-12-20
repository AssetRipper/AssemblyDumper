using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using System;

namespace AssemblyDumper
{
	internal static class ProcessorExtensions
	{
		public static void AddNotSupportedException(this CilInstructionCollection processor)
		{
			processor.Add(CilOpCodes.Newobj, SystemTypeGetter.NotSupportedExceptionConstructor);
			processor.Add(CilOpCodes.Throw);
		}

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
				var variable = new CilLocalVariable(targetType);
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

		public static void AddLogStatement(this CilInstructionCollection processor, string text)
		{
			Func<MethodDefinition, bool> func = m => m.IsStatic && m.Name == "Info" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "String";
			IMethodDefOrRef writeMethod = SharedState.Module.ImportCommonMethod("AssetRipper.Core.Logging.Logger", func);
			processor.Add(CilOpCodes.Ldstr, text);
			processor.Add(CilOpCodes.Call, writeMethod);
		}

		/// <summary>
		/// Remove the last instruction in the processor's collection
		/// </summary>
		/// <param name="processor">The processor to remove the instruction from</param>
		public static void Pop(this CilInstructionCollection processor) => processor.RemoveAt(processor.Count - 1);
	}
}
