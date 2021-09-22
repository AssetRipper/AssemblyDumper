using Mono.Cecil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass98_ApplyAssemblyAttributes
	{
		public static void DoPass()
		{
			Logger.Info("Pass 98: Apply Assembly Attributes");
			SharedState.Assembly.AddVersionAttribute(SharedState.Version);
			foreach(var pair in SharedState.ClassDictionary)
			{
				if (!SystemTypeGetter.primitiveNamesCsharp.Contains(pair.Key))
				{
					SharedState.Assembly.AddAssetTypeAttribute(pair.Key, pair.Value.TypeID, SharedState.TypeDictionary[pair.Key]);
				}
			}
		}

		private static void AddVersionAttribute(this AssemblyDefinition _this, string versionString)
		{
			var attrDef = new CustomAttribute(CommonTypeGetter.RegisterAssemblyAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(CommonTypeGetter.UnityVersionDefinition, versionString));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddAssetTypeAttribute(this AssemblyDefinition _this, string typeName, int idNumber, TypeReference type)
		{
			var attrDef = new CustomAttribute(CommonTypeGetter.RegisterAssetTypeAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.String, typeName));
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.Int32, idNumber));
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(SystemTypeGetter.Type, type));
			_this.CustomAttributes.Add(attrDef);
		}
	}
}
