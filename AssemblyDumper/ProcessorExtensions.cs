using Mono.Cecil.Cil;

namespace AssemblyDumper
{
	internal static class ProcessorExtensions
	{
		public static void EmitNotSupportedException(this ILProcessor processor)
		{
			processor.Emit(OpCodes.Newobj, SystemTypeGetter.NotSupportedExceptionConstructor);
			processor.Emit(OpCodes.Throw);
		}

		/// <summary>
		/// Remove the last instruction in the processor's collection
		/// </summary>
		/// <param name="processor">The processor to remove the instruction from</param>
		public static void Pop(this ILProcessor processor) => processor.Body.Instructions.RemoveAt(processor.Body.Instructions.Count - 1);
	}
}
