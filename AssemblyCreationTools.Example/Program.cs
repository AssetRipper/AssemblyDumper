using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyCreationTools.Example
{
	internal static class Program
	{
		const string exampleAssemblyName = "ExampleGeneratedAssembly";
		static void Main(string[] args)
		{
			try
			{
				AssemblyBuilder builder = new AssemblyBuilder(exampleAssemblyName, new Version(), KnownCorLibs.SystemRuntime_v6_0_0_0);
				builder.AddTestEnum();
				builder.Assembly.Write($"{exampleAssemblyName}.dll");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			Console.WriteLine("Done!");
			Console.ReadLine();
		}

		public static TypeDefinition AddTestEnum(this AssemblyBuilder builder)
		{
			return EnumCreator.CreateFromArray(builder, "Examples", "TestEnum", new string[] { "Test1", "Test2", "Test3", "Test4" });
		}
	}
}
