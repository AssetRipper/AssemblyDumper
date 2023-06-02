using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass555_CreateCommonString
	{
		public static void DoPass()
		{
			ThrowIfStringCountIsWrong();
			TypeDefinition newTypeDef = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "CommonString");

			GenericInstanceTypeSignature readOnlyUintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(IReadOnlyDictionary<,>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
			GenericInstanceTypeSignature uintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
			IMethodDefOrRef dictionaryConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, uintStringDictionary, 0);
			IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, uintStringDictionary, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "Add"));

			const string propertyName = "Dictionary";
			FieldDefinition field = newTypeDef.AddField(readOnlyUintStringDictionary, $"<{propertyName}>k__BackingField", true, FieldVisibility.Private);
			field.Attributes |= FieldAttributes.InitOnly;
			field.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			MethodDefinition staticConstructor = newTypeDef.AddEmptyConstructor(true);
			CilInstructionCollection processor = staticConstructor.GetProcessor();
			processor.Add(CilOpCodes.Newobj, dictionaryConstructor);
			foreach ((uint index, string str) in SharedState.Instance.CommonString.Strings)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldc_I4, (int)index);
				processor.Add(CilOpCodes.Ldstr, str);
				processor.Add(CilOpCodes.Call, addMethod);
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			newTypeDef.ImplementGetterProperty(
					propertyName,
					MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName,
					readOnlyUintStringDictionary,
					field)
				.GetMethod!.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);
		}

		private static void ThrowIfStringCountIsWrong()
		{
			int count = SharedState.Instance.CommonString.Strings.Count;
			if (count != 109)
			{
				throw new Exception($"The size of Common String has changed! {count}");
			}
		}
	}
}
