using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass11_AddConstructors
	{
		private readonly static List<string> processed = new List<string>();
		private static TypeReference AssetInfoRef;
		private static TypeReference LayoutInfoRef;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 11: Add Constructors");
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
			var constructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				SystemTypeGetter.Void
			);

			ParameterDefinition parameter = new ParameterDefinition(parameterName, ParameterAttributes.None, parameterType);
			constructor.Parameters.Add(parameter);

			typeDefinition.Methods.Add(constructor);
			return constructor;
		}
	}
}