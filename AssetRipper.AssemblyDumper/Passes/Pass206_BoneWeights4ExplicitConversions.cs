using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Numerics;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass206_BoneWeights4ExplicitConversions
	{
		public static void DoPass()
		{
			AddConversion(SharedState.Instance.SubclassGroups["BoneWeights4"]);
		}

		private static void AddConversion(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversion(type);
				AddReverseConversion(type);
			}
		}

		private static void AddConversion(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<BoneWeight4>();

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportConstructor<BoneWeight4>(8);

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, true);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_0_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_1_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_2_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Weight_3_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_0_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_1_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_2_"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_BoneIndex_3_"));

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversion(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<BoneWeight4>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Index0)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Index1)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Index2)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Index3)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_BoneIndex_3_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Weight0)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_0_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Weight1)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_1_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Weight2)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_2_"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<BoneWeight4>(m => m.Name == $"get_{nameof(BoneWeight4.Weight3)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Weight_3_"));

			processor.Add(CilOpCodes.Ret);
		}
	}
}
