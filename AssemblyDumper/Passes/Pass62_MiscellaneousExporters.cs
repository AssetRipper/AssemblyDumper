using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;
using AssetRipper.Core.Classes.Font;

namespace AssemblyDumper.Passes
{
	public static class Pass62_MiscellaneousExporters
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 62: Miscellaneous Exporters");

			SharedState.TypeDictionary["TextAsset"].ImplementTextAsset();
			SharedState.TypeDictionary["Font"].ImplementFontAsset();
			SharedState.TypeDictionary["MovieTexture"].ImplementMovieTexture();
		}

		private static void ImplementTextAsset(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<ITextAsset>();
			type.ImplementGetterProperty(nameof(ITextAsset.Script), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, type.GetFieldByName("m_Script"));
		}

		private static void ImplementFontAsset(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<IFont>();
			type.ImplementFullProperty(nameof(IFont.FontData), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.UInt8.MakeSzArrayType(), type.GetFieldByName("m_FontData"));
		}

		private static void ImplementMovieTexture(this TypeDefinition type)
		{
			type.AddInterfaceImplementation<IMovieTexture>();
			type.ImplementFullProperty(nameof(IMovieTexture.MovieData), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.UInt8.MakeSzArrayType(), type.TryGetFieldByName("m_MovieData", true));
		}
	}
}
