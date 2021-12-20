using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AssemblyDumper.Utils
{
	public static class InterfaceCreator
	{
		public static TypeDefinition CreateEmptyInterface(AssemblyDefinition assembly, string @namespace, string name)
		{
			var module = assembly.ManifestModule;
			TypeDefinition definition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Interface);
			module.TopLevelTypes.Add(definition);
			return definition;
		}

		public static TypeDefinition CreateEmptyInterface(AssemblyDefinition assembly, string @namespace, string name, ITypeDefOrRef[] interfaces)
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