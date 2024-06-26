﻿using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass504_FixShaderName
	{
		public static void DoPass()
		{
			int id = SharedState.Instance.NameToTypeID["Shader"].Single();
			ClassGroup group = SharedState.Instance.ClassGroups[id];
			foreach (TypeDefinition type in group.Types)
			{
				type.FixShaderTypeDefinition();
			}
		}

		private static void FixShaderTypeDefinition(this TypeDefinition type)
		{
			FieldDefinition nameField = type.GetFieldByName("m_Name", true);
			FieldDefinition? parsedFormField = type.TryGetFieldByName("m_ParsedForm");
			if (parsedFormField is null)
			{
				return;
			}

			if (parsedFormField.Signature?.FieldType.ToTypeDefOrRef() is not TypeDefinition serializedShaderDefinition)
			{
				throw new NullReferenceException($"{nameof(serializedShaderDefinition)} is null");
			}

			FieldDefinition? parsedFormNameField = serializedShaderDefinition.TryGetFieldByName("m_Name");
			if (parsedFormNameField is null)
			{
				throw new NullReferenceException($"{nameof(parsedFormNameField)} is null");
			}

			type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadRelease))
				.AddCopyString(nameField, parsedFormField, parsedFormNameField);
			type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadEditor))
				.AddCopyString(nameField, parsedFormField, parsedFormNameField);
		}

		private static void AddCopyString(
			this MethodDefinition method,
			FieldDefinition nameField,
			FieldDefinition parsedFormField,
			FieldDefinition parsedFormNameField)
		{
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop();//Remove the return
			processor.Add(CilOpCodes.Ldarg_0);

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, parsedFormField);
			processor.Add(CilOpCodes.Ldfld, parsedFormNameField);

			processor.Add(CilOpCodes.Stfld, nameField);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
