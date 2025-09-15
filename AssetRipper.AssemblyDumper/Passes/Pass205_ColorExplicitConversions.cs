using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Numerics;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass205_ColorExplicitConversions
	{
		public static void DoPass()
		{
			AddConversion32(SharedState.Instance.SubclassGroups["ColorRGBA32"]);
			AddConversionF(SharedState.Instance.SubclassGroups["ColorRGBAf"]);
		}

		private static void AddConversion32(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversion32(type);
				AddReverseConversion32(type);
			}
		}

		private static void AddConversion32(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<Color32>();

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, false);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Rgba"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<Color32>(m => m.Name == nameof(Color32.FromRgba)));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversion32(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<Color32>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<Color32>(m => m.Name == $"get_{nameof(Color32.Rgba)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Rgba"));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddConversionF(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversionF(type);
				AddReverseConversionF(type);
			}
		}

		private static void AddConversionF(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<ColorFloat>();

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportConstructor<ColorFloat>(4);

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, true);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_R"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_G"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_B"));

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_A"));

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversionF(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<ColorFloat>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetInstructions();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<ColorFloat>(m => m.Name == $"get_{nameof(ColorFloat.R)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_R"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<ColorFloat>(m => m.Name == $"get_{nameof(ColorFloat.G)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_G"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<ColorFloat>(m => m.Name == $"get_{nameof(ColorFloat.B)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_B"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarga_S, method.Parameters[0]);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<ColorFloat>(m => m.Name == $"get_{nameof(ColorFloat.A)}"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_A"));

			processor.Add(CilOpCodes.Ret);
		}
	}
}
