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
	}
}
