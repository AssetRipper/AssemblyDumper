using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core.Classes.Misc;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass206_BoneWeights4ExplicitConversions
	{
		public static void DoPass()
		{
			AddConversion<BoneWeights4>(SharedState.Instance.SubclassGroups["BoneWeights4"]);
		}

		private static void AddConversion<T>(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversion<T>(type);
				AddReverseConversion<T>(type);
			}
		}

		private static void AddConversion<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportDefaultConstructor<T>();

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_0_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_BoneIndex_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_1_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_BoneIndex_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_2_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_BoneIndex_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_3_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_BoneIndex_3_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_0_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_Weight_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_1_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_Weight_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_2_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_Weight_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_3_"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_Weight_3_"));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversion<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_BoneIndex_0_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_BoneIndex_1_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_BoneIndex_2_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_BoneIndex_3_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_3_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_Weight_0_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_Weight_1_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_Weight_2_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_Weight_3_"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_3_"));

			processor.Add(CilOpCodes.Ret);
		}
	}
}
