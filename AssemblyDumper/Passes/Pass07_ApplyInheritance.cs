using AssetRipper.Core;
using Mono.Cecil;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass07_ApplyInheritance
	{
		public static void DoPass()
		{
			Logger.Info("Pass 7: Apply Inheritance");

			TypeReference unityObjectBaseDefinition = SharedState.Module.ImportCommonType<UnityObjectBase>();
			TypeReference unityAssetBaseDefinition = SharedState.Module.ImportCommonType<UnityAssetBase>();

			foreach (var pair in SharedState.ClassDictionary)
			{
				if (Pass04_ExtractDependentNodeTrees.primitives.Contains(pair.Key))
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