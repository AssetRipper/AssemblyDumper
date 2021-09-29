using System.IO;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass99_SaveAssembly
	{
		public static void DoPass(DirectoryInfo outputDirectory)
		{
			Logger.Info("Pass 99: Save Assembly");
			var assembly = SharedState.Assembly;

			if (!outputDirectory.Exists) Directory.CreateDirectory(outputDirectory.FullName);

			string filePath = Path.Combine(outputDirectory.FullName, SharedState.Version + ".dll");

			var reference = assembly.MainModule.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Private.CoreLib");
			if (reference != null)
				assembly.MainModule.AssemblyReferences.Remove(reference);

			Logger.Info($"Saving assembly to {filePath}");
			assembly.Write(filePath);
		}
	}
}