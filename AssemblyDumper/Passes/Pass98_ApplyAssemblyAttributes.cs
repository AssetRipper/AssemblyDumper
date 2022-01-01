using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Attributes;

namespace AssemblyDumper.Passes
{
	public static class Pass98_ApplyAssemblyAttributes
	{
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 98: Apply Assembly Attributes");
			SharedState.Assembly.AddVersionAttribute();
			SharedState.Assembly.AddVersionHandlerAttribute();
			/*List<KeyValuePair<string, UnityClass>> classList = SharedState.ClassDictionary.ToList();
			classList.Sort(Compare);
			foreach (var pair in classList)
			{
				if (!SystemTypeGetter.primitiveNamesCsharp.Contains(pair.Key))
				{
					//Console.WriteLine(pair.Key);
					SharedState.Assembly.AddAssetTypeAttribute(pair.Key, pair.Value.TypeID, SharedState.TypeDictionary[pair.Key]);
				}
			}
			foreach (var attribute in SharedState.Assembly.CustomAttributes)
			{
				Console.WriteLine(attribute.Signature.FixedArguments[0].Element);
			}*/
		}

		private static void AddVersionAttribute(this AssemblyDefinition _this)
		{
			string versionString = SharedState.Version;
			var registerAssemblyAttributeConstructor = SharedState.Importer.ImportCommonConstructor<RegisterAssemblyAttribute>(1);
			_this.AddCustomAttribute(registerAssemblyAttributeConstructor, SystemTypeGetter.String, versionString);
		}

		private static void AddAssetTypeAttribute(this AssemblyDefinition _this, string typeName, int idNumber, ITypeDefOrRef type)
		{
			var registerAssetTypeAttributeConstructor = SharedState.Importer.ImportCommonConstructor<RegisterAssetTypeAttribute>(3);
			var attrDef = _this.AddCustomAttribute(registerAssetTypeAttributeConstructor);
			attrDef.AddFixedArgument(SystemTypeGetter.String, typeName);
			attrDef.AddFixedArgument(SystemTypeGetter.Int32, idNumber);
			attrDef.AddFixedArgument(SystemTypeGetter.Type.ToTypeSignature(), type.ToTypeSignature());
		}

		private static void AddVersionHandlerAttribute(this AssemblyDefinition _this)
		{
			var registerVersionHandlerAttributeConstructor = SharedState.Importer.ImportCommonConstructor<RegisterVersionHandlerAttribute>(1);
			var attrDef = _this.AddCustomAttribute(registerVersionHandlerAttributeConstructor, SystemTypeGetter.Type.ToTypeSignature(), Pass95_UnityVersionHandler.HandlerDefinition.ToTypeSignature());
		}

		private static int Compare(KeyValuePair<string, UnityClass> left, KeyValuePair<string, UnityClass> right)
		{
			if(left.Value.TypeID != right.Value.TypeID)
			{
				if (left.Value.TypeID == -1)
					return 1;
				if(right.Value.TypeID == -1)
					return -1;
				if (left.Value.TypeID < right.Value.TypeID)
					return -1;
				else
					return 1;
			}
			else
			{
				return left.Key.CompareTo(right.Key);
			}
		}
	}
}