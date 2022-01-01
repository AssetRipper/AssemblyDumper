namespace AssemblyDumper.Utils
{
	public static class InterfaceUtils
	{
		public const MethodAttributes InterfacePropertyImplementation =
			InterfaceMethodImplementation |
			MethodAttributes.SpecialName;
		public const MethodAttributes InterfaceMethodImplementation =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;

		public static void AddInterfaceImplementation<T>(this TypeDefinition type)
		{
			type.Interfaces.Add(new InterfaceImplementation(SharedState.Importer.ImportCommonType<T>()));
		}
	}
}
