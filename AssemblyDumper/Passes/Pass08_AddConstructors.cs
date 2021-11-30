using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass08_AddConstructors
	{
		private readonly static List<string> processed = new List<string>();
		private static TypeReference AssetInfoRef;
		private static TypeReference LayoutInfoRef;

		public static void DoPass()
		{
			Logger.Info("Pass 8: Add Constructors");
			AssetInfoRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.AssetInfo>();
			LayoutInfoRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Layout.LayoutInfo>();
			foreach (var pair in SharedState.ClassDictionary)
			{
				if (processed.Contains(pair.Key))
					continue;

				AddConstructor(pair.Value);
			}
		}

		private static void AddConstructor(UnityClass typeInfo)
		{
			if (Pass04_ExtractDependentNodeTrees.primitives.Contains(typeInfo.Name))
				return;

			if (!string.IsNullOrEmpty(typeInfo.Base) && !processed.Contains(typeInfo.Base))
				AddConstructor(SharedState.ClassDictionary[typeInfo.Base]);

			TypeDefinition type = SharedState.TypeDictionary[typeInfo.Name];
			ConstructorUtils.AddDefaultConstructor(type);
			AddLayoutInfoConstructor(type);
			if(typeInfo.TypeID >= 0)
			{
				AddAssetInfoConstructor(type);
			}
			processed.Add(typeInfo.Name);
		}

		private static MethodDefinition AddAssetInfoConstructor(TypeDefinition typeDefinition)
		{
			return AddSingleParameterConstructor(typeDefinition, AssetInfoRef, "info");
		}

		private static MethodDefinition AddLayoutInfoConstructor(TypeDefinition typeDefinition)
		{
			return AddSingleParameterConstructor(typeDefinition, LayoutInfoRef, "info");
		}

		private static MethodDefinition AddSingleParameterConstructor(TypeDefinition typeDefinition, TypeReference parameterType, string parameterName)
		{
			var module = typeDefinition.Module;
			var constructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				module.ImportReference(typeof(void))
			);

			ParameterDefinition parameter = new ParameterDefinition(parameterName, ParameterAttributes.None, parameterType);
			constructor.Parameters.Add(parameter);

			var processor = constructor.Body.GetILProcessor();

			MethodDefinition baseConstructorDef = typeDefinition.BaseType.Resolve().Methods
				.Where(x =>
				x.IsConstructor &&
				x.Parameters.Count == 1 &&
				x.Parameters[0].ParameterType.Name == parameterType.Name &&
				!x.IsStatic)
				.Single();
			MethodReference baseConstructor = SharedState.Module.ImportReference(baseConstructorDef);

			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldarg, parameter);
			processor.Emit(OpCodes.Call, baseConstructor);

			processor.Emit(OpCodes.Ret);

			typeDefinition.Methods.Add(constructor);
			return constructor;
		}
	}
}