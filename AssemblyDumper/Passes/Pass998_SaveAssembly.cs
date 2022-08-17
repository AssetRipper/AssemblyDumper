using AsmResolver.DotNet.Builder;
using System.IO;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass998_SaveAssembly
	{
		public static void DoPass()
		{
			AssemblyDefinition? assembly = SharedState.Instance.Assembly;

			string filePath = Path.Combine(Environment.CurrentDirectory, assembly.Name!.ToString() + ".dll");

			DotNetDirectoryFactory factory = new DotNetDirectoryFactory();
			//factory.MetadataBuilderFlags |= MetadataBuilderFlags.NoStringsStreamOptimization; //Check later, but currently less than 1% difference
			ManagedPEImageBuilder builder = new ManagedPEImageBuilder(factory);

			//Console.WriteLine($"Saving assembly to {filePath}");
			try
			{
				if(File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				assembly.Write(filePath, builder);
			}
			catch(AggregateException aggregateException)
			{
				Console.WriteLine("AggregateException thrown");
				aggregateException = aggregateException.Flatten();
				
				foreach(Exception? error in aggregateException.InnerExceptions)
				{
					Console.WriteLine();
					Console.WriteLine(error.ToString());
					Console.WriteLine();
				}
			}
		}
	}
}