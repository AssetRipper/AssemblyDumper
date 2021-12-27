using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System.Linq;
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
