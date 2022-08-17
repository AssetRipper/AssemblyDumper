using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyCreationTools.Types
{
	public static class ExceptionCreator
	{
		public static TypeDefinition CreateSimpleException(CachedReferenceImporter importer, string @namespace, string name, string errorMessage)
		{
			ITypeDefOrRef exceptionRef = importer.ImportType<Exception>();
			TypeDefinition type = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Sealed, exceptionRef);
			importer.TargetModule.TopLevelTypes.Add(type);
			IMethodDefOrRef exceptionConstructor = importer.ImportConstructor<Exception>(1);
			MethodDefinition constructor = type.AddEmptyConstructor();
			CilInstructionCollection processor = constructor.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldstr, errorMessage);
			processor.Add(CilOpCodes.Call, exceptionConstructor);
			processor.Add(CilOpCodes.Ret);
			return type;
		}
	}
}
