using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass07_ApplyInheritance
	{
		public static void DoPass()
		{
			Logger.Info("Pass 7: Apply Inheritance");


			foreach (var pair in SharedState.ClassDictionary)
			{
				if (Pass04_ExtractDependentNodeTrees.primitives.Contains(pair.Key))
					continue;
				if (string.IsNullOrEmpty(pair.Value.Base))
				{
					if(pair.Key == "Object")
						SharedState.TypeDictionary[pair.Key].BaseType = CommonTypeGetter.UnityObjectBaseDefinition;
					else
						SharedState.TypeDictionary[pair.Key].BaseType = CommonTypeGetter.UnityAssetBaseDefinition;
				}
				else
				{
					SharedState.TypeDictionary[pair.Key].BaseType = SharedState.TypeDictionary[pair.Value.Base];
				}
			}
		}
	}
}
