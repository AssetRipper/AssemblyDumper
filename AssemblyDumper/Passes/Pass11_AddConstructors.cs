using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using System.Collections.Generic;
using System.Linq;
using AssetRipper.Core.Layout;
using AssetRipper.Core.Parser.Asset;

namespace AssemblyDumper.Passes
{
	public static class Pass11_AddConstructors
	{
		private const MethodAttributes PublicInstanceConstructorAttributes = 
			MethodAttributes.Public | 
			MethodAttributes.HideBySig | 
			MethodAttributes.SpecialName | 
			MethodAttributes.RuntimeSpecialName;
		private readonly static List<string> processed = new List<string>();
		private static ITypeDefOrRef AssetInfoRef;
		private static ITypeDefOrRef LayoutInfoRef;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 11: Add Constructors");
			AssetInfoRef = SharedState.Importer.ImportCommonType<AssetInfo>();
			LayoutInfoRef = SharedState.Importer.ImportCommonType<LayoutInfo>();
			foreach (var pair in SharedState.ClassDictionary)
			{
				if (processed.Contains(pair.Key))
					continue;

				AddConstructor(pair.Value);
			}
		}

		private static void AddConstructor(UnityClass typeInfo)
		{
			if (PrimitiveTypes.primitives.Contains(typeInfo.Name))
				return;

			if (!string.IsNullOrEmpty(typeInfo.Base) && !processed.Contains(typeInfo.Base))
				AddConstructor(SharedState.ClassDictionary[typeInfo.Base]);

			TypeDefinition type = SharedState.TypeDictionary[typeInfo.Name];
			ConstructorUtils.AddDefaultConstructor(type);
			type.AddLayoutInfoConstructor();
			if(typeInfo.TypeID >= 0)
			{
				type.AddAssetInfoConstructor();
			}
			processed.Add(typeInfo.Name);
		}

		private static MethodDefinition AddAssetInfoConstructor(this TypeDefinition typeDefinition)
		{
			return AddSingleParameterConstructor(typeDefinition, AssetInfoRef, "info");
		}

		private static MethodDefinition AddLayoutInfoConstructor(this TypeDefinition typeDefinition)
		{
			return AddSingleParameterConstructor(typeDefinition, LayoutInfoRef, "info");
		}

		private static MethodDefinition AddSingleParameterConstructor(this TypeDefinition typeDefinition, ITypeDefOrRef parameterType, string parameterName)
		{
			var constructor = typeDefinition.AddMethod(
				".ctor",
				PublicInstanceConstructorAttributes,
				SystemTypeGetter.Void
			);
			constructor.AddParameter(parameterName, parameterType);
			return constructor;
		}
	}
}