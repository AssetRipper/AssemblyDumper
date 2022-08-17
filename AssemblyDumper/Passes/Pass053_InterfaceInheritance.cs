using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass053_InterfaceInheritance
	{
		public static void DoPass()
		{
			foreach(ClassGroup group in SharedState.Instance.ClassGroups.Values)
			{
				DoPassOnGroup(group);
			}
		}

		private static void DoPassOnGroup(this ClassGroup group)
		{
			if(TryGetBaseTypeDefinitions(group.Types,out List<TypeDefinition>? baseTypes))
			{
				List<ITypeDefOrRef> interfaces = baseTypes.First().GetAllInterfaces().ToList();
				foreach(ITypeDefOrRef @interface in interfaces)
				{
					if (baseTypes.AllImplementThisInterface(@interface.FullName))
					{
						group.Interface.AddInterfaceImplementation(@interface);
					}
				}
			}
		}

		private static bool TryGetBaseTypeDefinitions(this IEnumerable<TypeDefinition> types, [NotNullWhen(true)] out List<TypeDefinition>? baseTypes)
		{
			if(types.All(t => t.HasTypeDefinitionBaseType()))
			{
				baseTypes = types.Select(t => t.GetBaseTypeDefinition()).ToList();
				return true;
			}
			baseTypes = null;
			return false;
		}

		private static TypeDefinition? TryGetBaseTypeDefinition(this TypeDefinition type)
		{
			return type.BaseType as TypeDefinition;
		}

		private static bool HasTypeDefinitionBaseType(this TypeDefinition type)
		{
			return type.TryGetBaseTypeDefinition() != null;
		}

		private static TypeDefinition GetBaseTypeDefinition(this TypeDefinition type)
		{
			return type.TryGetBaseTypeDefinition() ?? throw new Exception($"{type.Name} did not have a TypeDefinition base type");
		}

		private static bool AllImplementThisInterface(this IEnumerable<TypeDefinition> types, string interfaceFullName)
		{
			return types.All(t => t.Implements(interfaceFullName));
		}

		private static IEnumerable<ITypeDefOrRef> GetAllInterfaces(this TypeDefinition type)
		{
			return type.Interfaces.Select(t => t.Interface).Where(i => i != null).Select(j => j!);
		}
	}
}
