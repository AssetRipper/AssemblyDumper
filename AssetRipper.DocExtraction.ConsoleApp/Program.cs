﻿using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.DataStructures;
using AssetRipper.DocExtraction.MetaData;
using AssetRipper.VersionUtilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetRipper.DocExtraction.ConsoleApp;

internal static class Program
{
	static void Main(string[] args)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		//ExtractAndSaveIndividually(@"F:\TypeTreeDumps\UnityInstallations", @"E:\UnityDocumentation");
		ExtractAndSaveConsolidated(@"F:\TypeTreeDumps\UnityInstallations", @"consolidated.json");
		
		stopwatch.Stop();
		Console.WriteLine($"Finished in {stopwatch.ElapsedMilliseconds} ms");
	}

	private static void ExtractDocumentation2020()
	{
		string engineXmlPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEngine.xml";
		string editorXmlPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEditor.xml";
		string engineDllPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEngine.dll";
		string editorDllPath = @"C:\Users\jrpri\Documents\UnityInstallations\2020.2.0f1\Editor\Data\Managed\UnityEditor.dll";
		UnityVersion unityVersion = new UnityVersion(2020, 2, 0, UnityVersionType.Final, 1);
		string outputDirectory = Environment.CurrentDirectory;
		string versionString = unityVersion.IsLess(5) ? unityVersion.ToStringWithoutType() : unityVersion.ToString();
		DocumentationFile documentationFile = DocumentationExtractor.ExtractDocumentation(versionString, engineXmlPath, editorXmlPath, engineDllPath, editorDllPath);
		documentationFile.SaveAsJson(Path.Combine(outputDirectory, $"{versionString}.json"));
	}

	private static void ExtractAndSaveIndividually(string inputDirectory, string outputDirectory)
	{
		Directory.CreateDirectory(outputDirectory);
		foreach (DocumentationFile documentationFile in ExtractAllDocumentation(inputDirectory))
		{
			documentationFile.SaveAsJson(Path.Combine(outputDirectory, $"{documentationFile.UnityVersion}.json"));
			Console.WriteLine(documentationFile.UnityVersion);
		}
	}

	private static void ExtractAndSaveConsolidated(string inputDirectory, string outputPath)
	{
		HistoryFile historyFile = new();
		Dictionary<string, ClassHistory> classes = historyFile.Classes;
		Dictionary<string, EnumHistory> enums = historyFile.Enums;
		Dictionary<string, StructHistory> structs = historyFile.Structs;
		foreach (DocumentationFile documentationFile in ExtractAllDocumentation(inputDirectory))
		{
			UnityVersion version = UnityVersion.Parse(documentationFile.UnityVersion);
			foreach (ClassDocumentation @class in documentationFile.Classes)
			{
				if (classes.TryGetValue(@class.FullName.ToString(), out ClassHistory? classHistory))
				{
					classHistory.Add(version, @class);
				}
				else
				{
					classes.Add(@class.FullName.ToString(), ClassHistory.From(version, @class));
				}
			}
			foreach (StructDocumentation @struct in documentationFile.Structs)
			{
				if (structs.TryGetValue(@struct.FullName.ToString(), out StructHistory? structHistory))
				{
					structHistory.Add(version, @struct);
				}
				else
				{
					structs.Add(@struct.FullName.ToString(), StructHistory.From(version, @struct));
				}
			}
			foreach (EnumDocumentation @enum in documentationFile.Enums)
			{
				if (enums.TryGetValue(@enum.FullName.ToString(), out EnumHistory? enumHistory))
				{
					enumHistory.Add(version, @enum);
				}
				else
				{
					enums.Add(@enum.FullName.ToString(), EnumHistory.From(version, @enum));
				}
			}
			Console.WriteLine(documentationFile.UnityVersion);
		}
		historyFile.SaveAsJson(outputPath);
	}

	private static IEnumerable<DocumentationFile> ExtractAllDocumentation(string inputDirectory)
	{
		foreach ((UnityVersion unityVersion, string versionFolder) in GetUnityDirectories(inputDirectory))
		{
			string engineXmlPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEngine.xml");
			string editorXmlPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEditor.xml");
			string engineDllPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEngine.dll");
			string editorDllPath = Path.Combine(versionFolder, @"Editor\Data\Managed\UnityEditor.dll");
			string versionString = unityVersion.IsLess(5) ? unityVersion.ToStringWithoutType() : unityVersion.ToString();
			yield return DocumentationExtractor.ExtractDocumentation(versionString, engineXmlPath, editorXmlPath, engineDllPath, editorDllPath);
		}
	}

	private static List<(UnityVersion, string)> GetUnityDirectories(string inputDirectory)
	{
		List<(UnityVersion, string)> list = new();
		foreach (string versionFolder in Directory.GetDirectories(inputDirectory))
		{
			UnityVersion unityVersion = UnityVersion.Parse(Path.GetFileName(versionFolder));
			if (unityVersion.Major < 3)
			{
				continue;
			}
			else
			{
				list.Add((unityVersion, versionFolder));
			}
		}
		list.Sort((pair1, pair2) => pair1.Item1.CompareTo(pair2.Item1));
		return list;
	}
}