using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass08_AddDefaultConstructors
	{
		private readonly static List<string> processed = new List<string>();

		public static void DoPass()
		{
			Logger.Info("Pass 8: Add Default Constructors");
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

			ConstructorUtils.AddDefaultConstructor(SharedState.TypeDictionary[typeInfo.Name]);
			processed.Add(typeInfo.Name);
		}
	}
}