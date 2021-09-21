using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using System.Collections.Generic;

namespace AssemblyDumper.Passes
{
	public static class Pass08_AddDefaultConstructors
	{
		private readonly static List<string> processed = new List<string>();
		/*
		public static void DoPass()
		{
			Logger.Info("Pass 8: Add Default Constructors");
			foreach (var pair in SharedState.Info.Classes)
			{
				if (processed.Contains(pair.Key))
					continue;

				AddConstructor(pair.Value);
			}
		}

		private static void AddConstructor(UnityClass typeInfo)
		{
			if (typeInfo.Base != null && !processed.Contains(typeInfo.Base))
				AddConstructor(typeInfo.BaseType);

			if(typeInfo.Definition != null)
				ConstructorUtils.AddDefaultConstructor(typeInfo.Definition);
			processed.Add(typeInfo.TypeName);
		}*/
	}
}
