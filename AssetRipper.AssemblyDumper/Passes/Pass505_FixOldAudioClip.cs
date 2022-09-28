using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass505_FixOldAudioClip
	{
		public static void DoPass()
		{
			InjectHelper(out TypeDefinition helperType);
			MethodDefinition helperMethod = helperType.Methods.Single(m => m.Name == nameof(AudioClipHelper.ReadOldByteArray));

			foreach (GeneratedClassInstance instance in SharedState.Instance.ClassGroups[83].Instances)
			{
				if (instance.TryGetStreamField(out FieldDefinition? streamField))
				{
					MethodDefinition readReleaseMethod = instance.Type.Methods.Single(m => m.Name == nameof(UnityObjectBase.ReadRelease));
					MethodDefinition readEditorMethod = instance.Type.Methods.Single(m => m.Name == nameof(UnityObjectBase.ReadEditor));
					FieldDefinition dataField = instance.Type.GetFieldByName("m_AudioData");
					FixMethod(readReleaseMethod, helperMethod, dataField, streamField);
					FixMethod(readEditorMethod, helperMethod, dataField, streamField);
				}
			}
		}

		private static void FixMethod(MethodDefinition readMethod, MethodDefinition helperMethod, FieldDefinition dataField, FieldDefinition streamField)
		{
			CilInstructionCollection processor = readMethod.GetProcessor();

			//remove bad instructions
			while (processor.Count > 0)
			{
				int index = processor.Count - 1;
				CilInstruction instruction = processor[index];
				processor.RemoveAt(index);
				if (instruction.OpCode == CilOpCodes.Ldarg_0)
				{
					break;
				}
			}

			processor.Add(CilOpCodes.Ldarg_0);//for the store field

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, streamField);
			processor.Add(CilOpCodes.Call, helperMethod);

			processor.Add(CilOpCodes.Stfld, dataField);

			processor.Add(CilOpCodes.Ret);
		}

		private static void InjectHelper(out TypeDefinition helperType)
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(AudioClipHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			helperType = result.ClonedTopLevelTypes.Single();
			helperType.Namespace = SharedState.HelpersNamespace;
			SharedState.Instance.Module.TopLevelTypes.Add(helperType);
		}

		private static bool TryGetStreamField(this GeneratedClassInstance instance, [NotNullWhen(true)] out FieldDefinition? streamField)
		{
			if (instance.Type.TryGetFieldByName("m_Stream", out streamField))
			{
				CorLibTypeSignature? fieldType = streamField.Signature?.FieldType as CorLibTypeSignature;
				if (fieldType is not null && fieldType.ElementType == ElementType.I4)
				{
					return true;
				}
				else
				{
					streamField = null;
					return false;
				}
			}
			else
			{
				return false;
			}
		}
	}
}
