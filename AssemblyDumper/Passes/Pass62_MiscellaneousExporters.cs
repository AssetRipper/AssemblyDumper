using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;

namespace AssemblyDumper.Passes
{
	public static class Pass62_MiscellaneousExporters
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 62: Miscellaneous Exporters");

			SharedState.TypeDictionary["TextAsset"].ImplementTextAsset();
		}

		private static void ImplementTextAsset(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<ITextAsset>();
			type.ImplementGetterProperty(nameof(ITextAsset.Script), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, type.GetFieldByName("m_Script"));
		}
	}
}
