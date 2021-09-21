using System.IO;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass99_SaveAssembly
	{
		public const string DllName = "UnityStructs.dll";
		public static void DoPass() => DoPass(DllName); 
		public static void DoPass(string filePath)
		{
			var assembly = SharedState.Assembly;
			FileInfo fileInfo = new FileInfo(filePath);
			DirectoryInfo directory = fileInfo.Directory;
			if (!directory.Exists) Directory.CreateDirectory(directory.FullName);

			var reference = assembly.MainModule.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Private.CoreLib");
			if (reference != null)
				assembly.MainModule.AssemblyReferences.Remove(reference);

			Logger.Info($"Saving assembly to {filePath}");
			assembly.Write(filePath);
		}
	}
}
