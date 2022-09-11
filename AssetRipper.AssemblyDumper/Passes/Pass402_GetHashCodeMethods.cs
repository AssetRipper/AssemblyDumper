using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass402_GetHashCodeMethods
	{
		public static void DoPass()
		{
			ITypeDefOrRef hashCodeType = SharedState.Instance.Importer.ImportType<HashCode>();
			IMethodDefOrRef addMethod = SharedState.Instance.Importer.ImportMethod<HashCode>(
				m => m.Name == nameof(HashCode.Add) && m.Parameters.Count == 1 && m.Signature!.GenericParameterCount == 1);
			IMethodDefOrRef toHashCodeMethod = SharedState.Instance.Importer.ImportMethod<HashCode>(m => m.Name == nameof(HashCode.ToHashCode));
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.Name == Pass002_RenameSubnodes.Utf8StringName)
				{
					continue;
				}
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					instance.AddGetHashCodeMethod(hashCodeType, addMethod, toHashCodeMethod);
				}
			}
		}

		private static void AddGetHashCodeMethod(this GeneratedClassInstance instance, ITypeDefOrRef hashCodeType, IMethodDefOrRef addMethod, IMethodDefOrRef toHashCodeMethod)
		{
			TypeDefinition type = instance.Type;
			MethodDefinition method = type.AddMethod(
				nameof(object.GetHashCode),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				SharedState.Instance.Importer.Int32);
			CilInstructionCollection processor = method.GetProcessor();

			CilLocalVariable variable = new CilLocalVariable(hashCodeType.ToTypeSignature());
			processor.Owner.LocalVariables.Add(variable);
			processor.Add(CilOpCodes.Ldloca, variable);
			processor.Add(CilOpCodes.Initobj, hashCodeType);

			foreach (PropertyDefinition property in instance.Properties.Select(c => c.Definition))
			{
				processor.Add(CilOpCodes.Ldloca, variable);
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, property.GetMethod!);
				processor.Add(CilOpCodes.Call, addMethod.MakeGenericInstanceMethod(property.Signature!.ReturnType));
			}
			processor.Add(CilOpCodes.Ldloca, variable);
			processor.Add(CilOpCodes.Call, toHashCodeMethod);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();
		}
	}
}
