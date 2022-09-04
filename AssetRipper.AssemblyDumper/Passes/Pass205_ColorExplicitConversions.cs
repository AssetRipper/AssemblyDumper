using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core.Math.Colors;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass205_ColorExplicitConversions
	{
		public static void DoPass()
		{
			AddConversion32<ColorRGBA32>(SharedState.Instance.SubclassGroups["ColorRGBA32"]);
			AddConversionF<ColorRGBAf>(SharedState.Instance.SubclassGroups["ColorRGBAf"]);
		}

		private static void AddConversion32<T>(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversion32<T>(type);
				AddReverseConversion32<T>(type);
			}
		}

		private static void AddConversion32<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportDefaultConstructor<T>();

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_Rgba"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_Rgba"));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversion32<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_Rgba"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_Rgba"));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddConversionF<T>(SubclassGroup group)
		{
			foreach (TypeDefinition type in group.Types)
			{
				AddConversionF<T>(type);
				AddReverseConversionF<T>(type);
			}
		}

		private static void AddConversionF<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			IMethodDefOrRef constructor = SharedState.Instance.Importer.ImportDefaultConstructor<T>();

			MethodDefinition method = type.AddEmptyConversion(type.ToTypeSignature(), commonType, false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_R"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_R"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_G"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_G"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_B"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_B"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "get_A"));
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "set_A"));

			processor.Add(CilOpCodes.Ret);
		}

		private static void AddReverseConversionF<T>(TypeDefinition type)
		{
			TypeSignature commonType = SharedState.Instance.Importer.ImportTypeSignature<T>();

			MethodDefinition constructor = type.GetDefaultConstructor();

			MethodDefinition method = type.AddEmptyConversion(commonType, type.ToTypeSignature(), false);
			CilInstructionCollection processor = method.GetProcessor();

			processor.Add(CilOpCodes.Newobj, constructor);

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_R"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_R"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_G"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_G"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_B"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_B"));

			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod<T>(m => m.Name == "get_A"));
			processor.Add(CilOpCodes.Call, type.Methods.Single(m => m.Name == "set_A"));

			processor.Add(CilOpCodes.Ret);
		}
	}
}
