using System.Collections.Generic;

namespace AssemblyDumper.Passes
{
	public static class Pass07_ApplyInheritance
	{/*
		public static void DoPass()
		{
			Logger.Info("Pass 7: Apply Inheritance");
			foreach (var pair in SharedState.TypeInformationDictionary)
			{
				var runtimeType = pair.Value.RuntimeType;
				if (runtimeType != null && runtimeType.Base != null)
				{
					try
					{
						var baseType = SharedState.TypeInformationDictionary[runtimeType.Base.Name];
						pair.Value.BaseType = baseType;
						baseType.Derived.Add(pair.Value);
					}
					catch(KeyNotFoundException)
					{
						Logger.Info($"{runtimeType.Base.Name} was not present in the dictionary");
					}
				}
			}

			foreach (var pair in SharedState.TypeInformationDictionary)
			{
				if (pair.Value.BaseType != null)
				{
					pair.Value.Definition.BaseType = pair.Value.BaseType.Definition;
				}
			}
		}*/
	}
}
