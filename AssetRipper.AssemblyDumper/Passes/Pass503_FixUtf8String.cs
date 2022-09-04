using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core.Classes;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass503_FixUtf8String
	{
		const MethodAttributes PropertyOverrideAttributes =
			MethodAttributes.Public |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.ReuseSlot |
			MethodAttributes.Virtual;

		public static void DoPass()
		{
			TypeDefinition type = SharedState.Instance.SubclassGroups[Pass002_RenameSubnodes.Utf8StringName].Instances.Single().Type;
			type.FixDataProperty();
			type.FixYamlMethod(nameof(Utf8StringBase.ExportYamlRelease));
			type.FixYamlMethod(nameof(Utf8StringBase.ExportYamlEditor));
			type.AddConversionFromString();
		}

		private static void FixDataProperty(this TypeDefinition type)
		{
			if(type.Properties.Any(p => p.Name == nameof(Utf8StringBase.Data)))
			{
				type.Methods.Single(m => m.Name == $"get_{nameof(Utf8StringBase.Data)}").Attributes = PropertyOverrideAttributes;
				type.Methods.Single(m => m.Name == $"set_{nameof(Utf8StringBase.Data)}").Attributes = PropertyOverrideAttributes;
			}
			else
			{
				type.ImplementFullProperty(nameof(Utf8StringBase.Data), PropertyOverrideAttributes, null, type.GetFieldByName("m_Data"));
			}
		}

		private static void FixYamlMethod(this TypeDefinition type, string methodName)
		{
			MethodDefinition method = type.Methods.Single(m => m.Name == methodName);
			IMethodDefOrRef baseMethod = SharedState.Instance.Importer.ImportMethod<Utf8StringBase>(m => m.Name == methodName);
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, baseMethod);
			processor.Add(CilOpCodes.Ret);
		}
		
		private static void AddConversionFromString(this TypeDefinition type)
		{
			MethodDefinition method = type.AddEmptyConversion(SharedState.Instance.Importer.String, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Newobj, type.GetDefaultConstructor());
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<Utf8StringBase>(m => m.Name == $"set_{nameof(Utf8StringBase.String)}"));
			processor.Add(CilOpCodes.Ret);
		}
	}
}
