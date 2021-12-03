using AssetRipper.Core.Attributes;
using Mono.Cecil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass98_ApplyAssemblyAttributes
	{
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 98: Apply Assembly Attributes");
			SharedState.Assembly.AddVersionAttribute();
			SharedState.Assembly.AddVersionHandlerAttribute();
			foreach (var pair in SharedState.ClassDictionary)
			{
				if (!SystemTypeGetter.primitiveNamesCsharp.Contains(pair.Key))
				{
					SharedState.Assembly.AddAssetTypeAttribute(pair.Key, pair.Value.TypeID, SharedState.TypeDictionary[pair.Key]);
				}
			}
		}

		private static void AddVersionAttribute(this AssemblyDefinition _this)
		{
			string versionString = SharedState.Version;
			var registerAssemblyAttributeConstructor = _this.MainModule.ImportCommonConstructor<RegisterAssemblyAttribute>(1);
			var attrDef = new CustomAttribute(registerAssemblyAttributeConstructor);
			var unityVersionDefinition = _this.MainModule.ImportCommonType<AssetRipper.Core.Parser.Files.UnityVersion>();
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(unityVersionDefinition, versionString));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddAssetTypeAttribute(this AssemblyDefinition _this, string typeName, int idNumber, TypeReference type)
		{
			var registerAssetTypeAttributeConstructor = _this.MainModule.ImportCommonConstructor<RegisterAssetTypeAttribute>(3);
			var attrDef = new CustomAttribute(registerAssetTypeAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.String, typeName));
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.Int32, idNumber));
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.Type, type));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddVersionHandlerAttribute(this AssemblyDefinition _this)
		{
			var registerVersionHandlerAttributeConstructor = _this.MainModule.ImportCommonConstructor<RegisterVersionHandlerAttribute>(1);
			var attrDef = new CustomAttribute(registerVersionHandlerAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.Type, Pass15_UnityVersionHandler.HandlerDefinition));
			_this.CustomAttributes.Add(attrDef);
		}
	}
}