using AssemblyDumper.Utils;
using AssetRipper.Core.Interfaces;

namespace AssemblyDumper.Passes
{
	public static class Pass30_ImplementHasNameInterface
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot | 
			MethodAttributes.Virtual;
		const string FieldName = "m_Name";

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 30: Implement the Has Name Interface");
			ITypeDefOrRef hasName = SharedState.Importer.ImportCommonType<IHasName>();
			foreach(TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				if (type.HasNameField())
				{
					type.Interfaces.Add(new InterfaceImplementation(hasName));
					type.ImplementFullProperty("Name", InterfacePropertyImplementationAttributes, SystemTypeGetter.String, type.GetFieldByName(FieldName));
				}
			}
		}

		private static bool HasNameField(this TypeDefinition type)
		{
			return type.Fields.Any(field => field.Name == FieldName);
		}
	}
}
