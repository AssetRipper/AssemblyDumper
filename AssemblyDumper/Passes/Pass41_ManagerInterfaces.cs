using AssemblyDumper.Utils;
using AssetRipper.Core.Classes.TagManager;

namespace AssemblyDumper.Passes
{
	public static class Pass41_ManagerInterfaces
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 41: Manager Interfaces");
			ImplementTagManager();
		}

		private static void ImplementTagManager()
		{
			TypeDefinition type = SharedState.TypeDictionary["TagManager"];
			type.AddInterfaceImplementation<ITagManager>();
			type.ImplementFullProperty(nameof(ITagManager.Tags), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String.MakeSzArrayType(), type.GetFieldByName("tags"));
		}
	}
}
