using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass508_LazySceneObjectIdentifier
	{
		public const string TargetObjectName = "TargetObjectReference";
		public const string TargetPrefabName = "TargetPrefabReference";
		public static void DoPass()
		{
			TypeSignature unityObjectBase = SharedState.Instance.Importer.ImportType<IUnityObjectBase>().ToTypeSignature();

			MethodDefinition helperMethod = SharedState.Instance.InjectHelperType(typeof(SceneObjectIdentifierHelper)).Methods.Single();
			SubclassGroup group = SharedState.Instance.SubclassGroups["SceneObjectIdentifier"];
			PropertyInjector.InjectFullProperty(group, unityObjectBase, TargetObjectName, true);
			PropertyInjector.InjectFullProperty(group, unityObjectBase, TargetPrefabName, true);
			foreach (TypeDefinition type in group.Types)
			{
				FixMethod(type, type.GetMethodByName(nameof(UnityAssetBase.ExportYamlEditor)), helperMethod);
				FixMethod(type, type.GetMethodByName(nameof(UnityAssetBase.ExportYamlRelease)), helperMethod);
			}
		}

		private static void FixMethod(TypeDefinition type, MethodDefinition method, MethodDefinition helperMethod)
		{
			FieldDefinition objectField = type.GetFieldByName("m_TargetObject");
			FieldDefinition prefabField = type.GetFieldByName("m_TargetPrefab");

			CilInstructionCollection processor = method.GetProcessor();
			CilLocalVariable objectLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int64);
			CilLocalVariable prefabLocal = processor.AddLocalVariable(SharedState.Instance.Importer.Int64);

			List<CilInstruction> beginningInstructions = new()
			{
				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldfld, objectField),
				new CilInstruction(CilOpCodes.Stloc, objectLocal),

				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldfld, prefabField),
				new CilInstruction(CilOpCodes.Stloc, prefabLocal),


				new CilInstruction(CilOpCodes.Ldarg_0),

				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Call, type.Properties.Single(p => p.Name == TargetObjectName).GetMethod),
				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldfld, objectField),
				new CilInstruction(CilOpCodes.Ldarg_1),
				new CilInstruction(CilOpCodes.Call, helperMethod),

				new CilInstruction(CilOpCodes.Stfld, objectField),


				new CilInstruction(CilOpCodes.Ldarg_0),

				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Call, type.Properties.Single(p => p.Name == TargetPrefabName).GetMethod),
				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldfld, prefabField),
				new CilInstruction(CilOpCodes.Ldarg_1),
				new CilInstruction(CilOpCodes.Call, helperMethod),

				new CilInstruction(CilOpCodes.Stfld, prefabField),
			};

			List<CilInstruction> endingInstructions = new()
			{
				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldloc, objectLocal),
				new CilInstruction(CilOpCodes.Stfld, objectField),

				new CilInstruction(CilOpCodes.Ldarg_0),
				new CilInstruction(CilOpCodes.Ldloc, prefabLocal),
				new CilInstruction(CilOpCodes.Stfld, prefabField),
			};

			processor.InsertRange(0, beginningInstructions);
			processor.InsertRange(processor.Count - 1, endingInstructions);
		}
	}
}
