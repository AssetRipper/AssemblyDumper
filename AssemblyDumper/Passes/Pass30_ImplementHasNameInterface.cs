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
				if (type.TryGetFieldByName(FieldName, out FieldDefinition? field))
				{
					type.Interfaces.Add(new InterfaceImplementation(hasName));
					type.ImplementFullProperty("Name", InterfacePropertyImplementationAttributes, SystemTypeGetter.String, field);
				}
			}
		}
	}
}
