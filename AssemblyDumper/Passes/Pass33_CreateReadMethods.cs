using System.Linq;
using Mono.Cecil;

namespace AssemblyDumper.Passes
{
	public static class Pass33_CreateReadMethods
	{
		public static void DoPass()
		{
			Logger.Info("Pass 33: Creating empty read methods");
			
			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;
				
				var type = SharedState.TypeDictionary[name];

				var releaseDef = new MethodDefinition("ReadRelease", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				releaseDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));
				
				var debugDef = new MethodDefinition("ReadEditor", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				debugDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));
				
				type.Methods.Add(releaseDef);
				type.Methods.Add(debugDef);
			}
		}
	}
}