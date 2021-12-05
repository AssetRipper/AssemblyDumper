using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace AssemblyDumper
{
	internal static class ProcessorExtensions
	{
		public static void EmitNotSupportedException(this ILProcessor processor)
		{
			processor.Emit(OpCodes.Newobj, SystemTypeGetter.NotSupportedExceptionConstructor);
			processor.Emit(OpCodes.Throw);
		}

		public static void EmitDefaultValue(this ILProcessor processor, TypeReference targetType)
		{
			if (targetType.FullName == "System.Void")
			{
				//Do nothing
			}
			else if (targetType.IsValueType)
			{
				var variable = new VariableDefinition(targetType);
				processor.Body.Variables.Add(variable);
				processor.Emit(OpCodes.Ldloca, variable);
				processor.Emit(OpCodes.Initobj, targetType);
				processor.Emit(OpCodes.Ldloc, variable);
			}
			else
			{
				processor.Emit(OpCodes.Ldnull);
			}
		}

		public static void EmitLogStatement(this ILProcessor processor, string text)
		{
			Func<MethodDefinition, bool> func = m => m.IsStatic && m.Name == "Info" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "String";
			MethodReference writeMethod = SharedState.Module.ImportCommonMethod("AssetRipper.Core.Logging.Logger", func);
			processor.Emit(OpCodes.Ldstr, text);
			processor.Emit(OpCodes.Call, writeMethod);
		}

		/// <summary>
		/// Remove the last instruction in the processor's collection
		/// </summary>
		/// <param name="processor">The processor to remove the instruction from</param>
		public static void Pop(this ILProcessor processor) => processor.Body.Instructions.RemoveAt(processor.Body.Instructions.Count - 1);
	}
}
