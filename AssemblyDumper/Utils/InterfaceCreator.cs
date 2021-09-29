using Mono.Cecil;

namespace AssemblyDumper.Utils
{
	public static class InterfaceCreator
	{
		public static TypeDefinition CreateEmptyInterface(AssemblyDefinition assembly, string @namespace, string name)
		{
			var module = assembly.MainModule;
			TypeDefinition definition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Interface);
			module.Types.Add(definition);
			return definition;
		}

		public static TypeDefinition CreateEmptyInterface(AssemblyDefinition assembly, string @namespace, string name, TypeReference[] interfaces)
		{
			var emptyInterface = CreateEmptyInterface(assembly, @namespace, name);
			foreach (var implementedInterface in interfaces)
			{
				emptyInterface.Interfaces.Add(new InterfaceImplementation(implementedInterface));
			}

			return emptyInterface;
		}
	}
}