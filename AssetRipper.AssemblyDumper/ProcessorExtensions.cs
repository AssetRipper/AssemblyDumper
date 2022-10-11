using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper
{
	internal static class ProcessorExtensions
	{
		public static void AddNotSupportedException(this CilInstructionCollection processor)
		{
			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportDefaultConstructor<NotSupportedException>();
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Throw);
		}

		/*public static void AddLogStatement(this CilInstructionCollection processor, string text)
		{
			Func<MethodDefinition, bool> func = m => m.IsStatic && m.Name == nameof(Logger.Info) && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "String";
			IMethodDefOrRef writeMethod = SharedState.Instance.Importer.ImportMethod(typeof(Logger), func);
			processor.Add(CilOpCodes.Ldstr, text);
			processor.Add(CilOpCodes.Call, writeMethod);
		}*/
	}
}
