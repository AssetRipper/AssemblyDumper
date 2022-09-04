using System.Diagnostics;
using System.IO;

namespace AssetRipper.DocExtraction.ConsoleApp;

internal static class Program
{
	static void Main(string[] args)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		ExtractDocumentation(@"F:\TypeTreeDumps\UnityInstallations", @"E:\UnityDocumentation");

		stopwatch.Stop();
		Console.WriteLine($"Finished in {stopwatch.ElapsedMilliseconds} ms");
	}

	private static void ExtractDocumentation2020()
	{
		string engineXmlPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEngine.xml";
		string editorXmlPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEditor.xml";
		string engineDllPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEngine.dll";
		string editorDllPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEditor.dll";
		string unityVersion = "2020.2.0f1";
		string outputDirectory = Environment.CurrentDirectory;
		DocumentationFile documentationFile = DocumentationExtractor.ExtractDocumentation(unityVersion, engineXmlPath, editorXmlPath, engineDllPath, editorDllPath);
		documentationFile.SaveAsJson(Path.Combine(outputDirectory, $"{unityVersion}.json"));
	}

	private static void ExtractDocumentation(string inputDirectory, string outputDirectory)
	{
		Directory.CreateDirectory(outputDirectory);
		foreach (string versionFolder in Directory.GetDirectories(inputDirectory))
		{
			string unityVersion = Path.GetFileName(versionFolder);
			if (unityVersion.StartsWith("2.", StringComparison.Ordinal))
			{
				continue;
			}

			string engineXmlPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEngine.xml");
			string editorXmlPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEditor.xml");
			string engineDllPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEngine.dll");
			string editorDllPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEditor.dll");

			DocumentationFile documentationFile = DocumentationExtractor.ExtractDocumentation(unityVersion, engineXmlPath, editorXmlPath, engineDllPath, editorDllPath);
			documentationFile.SaveAsJson(Path.Combine(outputDirectory, $"{unityVersion}.json"));
			Console.WriteLine(unityVersion);
		}
	}
}