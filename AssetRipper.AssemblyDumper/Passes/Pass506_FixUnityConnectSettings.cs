using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets;
using System.IO;

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
				if (instruction.OpCode == CilOpCodes.Ldfld
					&& instruction.Operand is FieldDefinition field
					&& (field.Name == "m_UnityPurchasingSettings" || field.Name == "m_CrashReportingSettings"))
				{
					//ldarg.0  asset
					//ldfld    field
					//ldarg.1  reader
					//callvirt ReadRelease

					return i - 1;
				}
			}
			throw new Exception("Could not determine the insertion point");
		}

		private static void InsertInstructions(CilInstructionCollection processor, int insertionPoint)
		{
			ICilLabel returnLabel = processor[processor.Count - 1].CreateLabel();
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Brtrue, returnLabel));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ceq));

			IMethodDefOrRef getStream = SharedState.Instance.Importer.ImportMethod<BinaryReader>(m => m.Name == $"get_{nameof(BinaryReader.BaseStream)}");
			IMethodDefOrRef getLength = SharedState.Instance.Importer.ImportMethod<Stream>(m => m.Name == $"get_{nameof(Stream.Length)}");
			IMethodDefOrRef getPosition = SharedState.Instance.Importer.ImportMethod<Stream>(m => m.Name == $"get_{nameof(Stream.Position)}");

			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getLength));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getStream));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ldarg_1));

			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getPosition));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Callvirt, getStream));
			processor.Insert(insertionPoint, new CilInstruction(CilOpCodes.Ldarg_1));

			processor.OptimizeMacros();
		}
	}
}
