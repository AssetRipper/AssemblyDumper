using AssetRipper.Core;

namespace AssemblyDumper.Passes
{
	public static class Pass08_ApplyInheritance
	{
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 8: Apply Inheritance");

			ITypeDefOrRef unityObjectBaseDefinition = SharedState.Importer.ImportCommonType<UnityObjectBase>();
			ITypeDefOrRef unityAssetBaseDefinition = SharedState.Importer.ImportCommonType<UnityAssetBase>();

			foreach (var pair in SharedState.ClassDictionary)
			{
				if (PrimitiveTypes.primitives.Contains(pair.Key))
					continue;
				if (string.IsNullOrEmpty(pair.Value.Base))
				{
					if (pair.Key == "Object")
						SharedState.TypeDictionary[pair.Key].BaseType = unityObjectBaseDefinition;
					else
						SharedState.TypeDictionary[pair.Key].BaseType = unityAssetBaseDefinition;
				}
				else
				{
					SharedState.TypeDictionary[pair.Key].BaseType = SharedState.TypeDictionary[pair.Value.Base];
				}
			}
		}
	}
}