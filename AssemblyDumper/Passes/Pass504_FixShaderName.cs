using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.Core;
using AssetRipper.Core.Classes;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass504_FixShaderName
	{
		public static void DoPass()
		{
			int id = SharedState.Instance.NameToTypeID["Shader"].Single();
			ClassGroup group = SharedState.Instance.ClassGroups[id];
			foreach(TypeDefinition type in group.Types)
			{
				type.FixShaderTypeDefinition();
			}
		}

		private static void FixShaderTypeDefinition(this TypeDefinition type)
		{
			FieldDefinition? nameField = type.TryGetFieldByName("m_Name");
			FieldDefinition? parsedFormField = type.TryGetFieldByName("m_ParsedForm");
			if (nameField is null || parsedFormField is null)
			{
				return;
			}

			TypeDefinition? serializedShaderDefinition = parsedFormField.Signature?.FieldType.ToTypeDefOrRef() as TypeDefinition;
			if (serializedShaderDefinition is null)
			{
				throw new NullReferenceException($"{nameof(serializedShaderDefinition)} is null");
			}

			FieldDefinition? parsedFormNameField = serializedShaderDefinition.TryGetFieldByName("m_Name");
			if (parsedFormNameField is null)
			{
				throw new NullReferenceException($"{nameof(parsedFormNameField)} is null");
			}

			IMethodDefOrRef copyContentMethod = SharedState.Instance.Importer.ImportMethod<Utf8StringBase>(m => m.Name == nameof(Utf8StringBase.CopyIfNullOrEmpty));

			type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadRelease))
				.AddCopyString(copyContentMethod, nameField, parsedFormField, parsedFormNameField);
			type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadEditor))
				.AddCopyString(copyContentMethod, nameField, parsedFormField, parsedFormNameField);
		}

		private static void AddCopyString(
			this MethodDefinition method, 
			IMethodDefOrRef copyContentMethod,
			FieldDefinition nameField,
			FieldDefinition parsedFormField,
			FieldDefinition parsedFormNameField)
		{
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop();//Remove the return
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, nameField);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, parsedFormField);
			processor.Add(CilOpCodes.Ldfld, parsedFormNameField);
			processor.Add(CilOpCodes.Call, copyContentMethod);
			processor.Add(CilOpCodes.Pop);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
