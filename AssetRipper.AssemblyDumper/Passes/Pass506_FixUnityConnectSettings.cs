using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core;
using AssetRipper.Core.IO;
using AssetRipper.Core.IO.Asset;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass506_FixUnityConnectSettings
	{
		/// <summary>
		/// On some platforms, the UnityConnectSettings asset doesn't contain any settings. As this is platform specific, it is not reflected in the type trees.
		/// </summary>
		public static void DoPass()
		{
			foreach (GeneratedClassInstance instance in SharedState.Instance.ClassGroups[310].Instances)
			{
				FixMethod(instance.Type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadRelease)));
				FixMethod(instance.Type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadEditor)));
			}
		}

		private static void FixMethod(MethodDefinition method)
		{
			CilInstructionCollection processor = method.GetProcessor();
			InsertInstructions(processor, FindInsertionPoint(processor));
		}

		private static int FindInsertionPoint(CilInstructionCollection processor)
		{
			for (int i = 0; i < processor.Count; i++)
			{
				CilInstruction instruction = processor[i];
				if (instruction.OpCode == CilOpCodes.Stfld 
					&& instruction.Operand is FieldDefinition field 
					&& (field.Name == "m_UnityPurchasingSettings" || field.Name == "m_CrashReportingSettings"))
				{
					//ldarg.0
					//ldarg.1
					//call
					//stfld
	
					return i - 3;
				}
			}
			throw new Exception("Could not determine the insertion point");
		}

		private static void InsertInstructions(CilInstructionCollection processor, int insertionPoint)
		{
			ICilLabel returnLabel = processor[processor.Count - 1].CreateLabel();
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Brtrue, returnLabel));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ceq));

			IMethodDefOrRef getAdjustableStream = SharedState.Instance.Importer.ImportMethod<AssetReader>(m => m.Name == $"get_{nameof(AssetReader.AdjustableStream)}");
			IMethodDefOrRef getLength = SharedState.Instance.Importer.ImportMethod<AdjustableStream>(m => m.Name == $"get_{nameof(AdjustableStream.Length)}");
			IMethodDefOrRef getPosition = SharedState.Instance.Importer.ImportMethod<AdjustableStream>(m => m.Name == $"get_{nameof(AdjustableStream.Position)}");

			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getLength));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getAdjustableStream));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ldarg_1));

			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getPosition));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getAdjustableStream));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ldarg_1));

			processor.OptimizeMacros();
		}
	}
}
