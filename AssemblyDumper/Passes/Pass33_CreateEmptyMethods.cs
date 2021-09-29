using Mono.Cecil;

namespace AssemblyDumper.Passes
{
	public static class Pass33_CreateEmptyMethods
	{
		public static void DoPass()
		{
			Logger.Info("Pass 33: Creating empty methods on generated types");

			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var releaseReadDef = new MethodDefinition("ReadRelease", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				releaseReadDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));

				var editorReadDef = new MethodDefinition("ReadEditor", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				editorReadDef.Parameters.Add(new("reader", ParameterAttributes.None, CommonTypeGetter.AssetReaderDefinition));

				var releaseWriteDef = new MethodDefinition("WriteRelease", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				releaseWriteDef.Parameters.Add(new("writer", ParameterAttributes.None, CommonTypeGetter.AssetWriterDefinition));

				var editorWriteDef = new MethodDefinition("WriteEditor", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig, SystemTypeGetter.Void);
				editorWriteDef.Parameters.Add(new("writer", ParameterAttributes.None, CommonTypeGetter.AssetWriterDefinition));

				type.Methods.Add(releaseReadDef);
				type.Methods.Add(editorReadDef);
				type.Methods.Add(releaseWriteDef);
				type.Methods.Add(editorWriteDef);
			}
		}
	}
}