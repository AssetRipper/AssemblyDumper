using Mono.Cecil;

namespace AssemblyDumper.Utils
{
	public static class StructCreator
	{
		public static TypeDefinition CreateTest(AssemblyDefinition assembly)
		{
			var module = assembly.MainModule;
			var testStructDef = new TypeDefinition(SharedState.ExamplesNamespace, "TestStruct",
				TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
				module.ImportSystemType("System.ValueType"));

			assembly.MainModule.Types.Add(testStructDef);
			FieldUtils.AddIntField(testStructDef, "testInt");
			FieldUtils.AddByteField(testStructDef, "testByte");
			FieldUtils.AddByteArrayField(testStructDef, "testByteArray");
			return null;
		}
	}
}