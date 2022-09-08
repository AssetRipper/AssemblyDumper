using AssetRipper.AssemblyCreationTools.Fields;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass101_FillWriteMethods
	{
		private static bool throwNotSupported = true;

		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					List<FieldDefinition> fields = FieldUtils.GetAllFieldsInTypeAndBase(instance.Type);
					instance.Type.FillEditorWriteMethod(instance.Class, fields);
					instance.Type.FillReleaseWriteMethod(instance.Class, fields);
				}
			}
		}

		private static void FillEditorWriteMethod(this TypeDefinition type, UniversalClass klass, List<FieldDefinition> fields)
		{
			MethodDefinition editorModeReadMethod = type.Methods.First(m => m.Name == "WriteEditor");
			CilMethodBody editorModeBody = editorModeReadMethod.CilMethodBody!;
			CilInstructionCollection editorModeProcessor = editorModeBody.Instructions;

			if (throwNotSupported)
			{
				editorModeProcessor.AddNotSupportedException();
			}
			else
			{
				//Console.WriteLine($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (UniversalNode unityNode in klass.EditorRootNode.SubNodes)
					{
						//AddWriteToProcessor(unityNode, editorModeProcessor, fields);
					}
				}
				editorModeProcessor.Add(CilOpCodes.Ret);
			}
			editorModeProcessor.OptimizeMacros();
		}

		private static void FillReleaseWriteMethod(this TypeDefinition type, UniversalClass klass, List<FieldDefinition> fields)
		{
			MethodDefinition releaseModeReadMethod = type.Methods.First(m => m.Name == "WriteRelease");
			CilMethodBody releaseModeBody = releaseModeReadMethod.CilMethodBody!;
			CilInstructionCollection releaseModeProcessor = releaseModeBody.Instructions;

			if (throwNotSupported)
			{
				releaseModeProcessor.AddNotSupportedException();
			}
			else
			{
				//Console.WriteLine($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode != null)
				{
					foreach (UniversalNode unityNode in klass.ReleaseRootNode.SubNodes)
					{
						//AddWriteToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}
				releaseModeProcessor.Add(CilOpCodes.Ret);
			}
			releaseModeProcessor.OptimizeMacros();
		}

	}
}