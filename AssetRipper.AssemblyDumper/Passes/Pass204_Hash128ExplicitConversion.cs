using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass204_Hash128ExplicitConversion
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
		public static void DoPass()
		{
			foreach (TypeDefinition type in SharedState.Instance.SubclassGroups["Hash128"].Types)
			{
				type.AddConversion();
			}
		}

		private static void AddConversion(this TypeDefinition type)
		{
			ITypeDefOrRef returnType = SharedState.Instance.Importer.ImportType<AssetRipper.Core.Classes.Misc.Hash128>();
			MethodDefinition method = type.AddMethod("op_Explicit", ConversionAttributes, returnType.ToTypeSignature());
			method.AddParameter(type.ToTypeSignature(), "value");
			method.CilMethodBody!.InitializeLocals = true;

			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			SzArrayTypeSignature arrayType = SharedState.Instance.Importer.UInt8.MakeSzArrayType();

			processor.Add(CilOpCodes.Ldc_I4, 16);
			processor.Add(CilOpCodes.Newarr, SharedState.Instance.Importer.UInt8.ToTypeDefOrRef());

			CilLocalVariable array = new CilLocalVariable(arrayType);
			processor.Owner.LocalVariables.Add(array);
			processor.Add(CilOpCodes.Stloc, array);

			for (int i = 0; i < 16; i++)
			{
				FieldDefinition field = type.GetFieldByName($"m_Bytes_{i}_");
				processor.Add(CilOpCodes.Ldloc, array);
				processor.Add(CilOpCodes.Ldc_I4, i);
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
				processor.Add(CilOpCodes.Stelem, SharedState.Instance.Importer.UInt8.ToTypeDefOrRef());
			}

			processor.Add(CilOpCodes.Ldloc, array);

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportMethod<AssetRipper.Core.Classes.Misc.Hash128>(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType is SzArrayTypeSignature);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}
	}
}
