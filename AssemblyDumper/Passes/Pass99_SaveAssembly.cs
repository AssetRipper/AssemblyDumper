using System.IO;

namespace AssemblyDumper.Passes
{
	public static class Pass99_SaveAssembly
	{
		public static void DoPass(DirectoryInfo outputDirectory)
		{
			System.Console.WriteLine("Pass 99: Save Assembly");
			var assembly = SharedState.Assembly;

			if (!outputDirectory.Exists) Directory.CreateDirectory(outputDirectory.FullName);

			string filePath = Path.Combine(outputDirectory.FullName, SharedState.Assembly.Name.Name + ".dll");

			//var reference = assembly.MainModule.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Private.CoreLib");
			//if (reference != null)
			//	assembly.MainModule.AssemblyReferences.Remove(reference);

			System.Console.WriteLine($"Saving assembly to {filePath}");
			assembly.Write(filePath);
		}
	}
}