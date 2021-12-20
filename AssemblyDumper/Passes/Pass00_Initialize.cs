using AsmResolver.DotNet;
using AssemblyDumper.Unity;
using System;
using System.IO;
using System.Text.Json;

namespace AssemblyDumper.Passes
{
	public static class Pass00_Initialize
	{
		/// <summary>
		/// Used to determine which version of the AssemblyDumper generated an assembly
		/// </summary>
		private static Version currentGeneratedVersion = new Version(0, 0, 0, 0);

		/// <summary>
		/// References System.Runtime.dll, Version=6.0.0.0, PublicKeyToken=B03F5F7F11D50A3A. This is used by .NET
		/// assemblies targeting .NET 6.0.
		/// </summary>
		public static readonly AssemblyReference SystemRuntime_v6_0_0_0 = new AssemblyReference("System.Runtime",
			new Version(6, 0, 0, 0), false, new byte[]
			{
				0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A
			});

		/// <summary>
		/// Read the information json, system assembly, and AssetRipperCommon. Then create a new assembly.
		/// </summary>
		public static void DoPass(string jsonPath, string systemRuntimeFilePath, string systemCollectionsFilePath)
		{
			Console.WriteLine("Pass 0: Initialize");

			using var stream = File.OpenRead(jsonPath);
			var info = JsonSerializer.Deserialize<UnityInfo>(stream);
			SharedState.Initialize(info);

			string AssemblyFileName = '_' + SharedState.Version.Replace('.', '_');

			AssemblyDefinition assembly = new AssemblyDefinition(AssemblyFileName, currentGeneratedVersion);
			ModuleDefinition module = new ModuleDefinition(AssemblyFileName, SystemRuntime_v6_0_0_0);
			assembly.Modules.Add(module);

			AssemblyDefinition runtimeAssembly = AssemblyDefinition.FromFile(systemRuntimeFilePath);
			AssemblyDefinition collectionsAssembly = AssemblyDefinition.FromFile(systemCollectionsFilePath);
			AssemblyDefinition commonAssembly = AssemblyDefinition.FromFile(typeof(AssetRipper.Core.UnityObjectBase).Assembly.Location);

			SystemTypeGetter.RuntimeAssembly = runtimeAssembly;
			SystemTypeGetter.CollectionsAssembly = collectionsAssembly;
			CommonTypeGetter.CommonAssembly = commonAssembly;

			module.MetadataResolver.AssemblyResolver.AddToCache(runtimeAssembly, runtimeAssembly);
			module.MetadataResolver.AssemblyResolver.AddToCache(collectionsAssembly, collectionsAssembly);
			module.MetadataResolver.AssemblyResolver.AddToCache(commonAssembly, commonAssembly);

			SharedState.Assembly = assembly;
			SharedState.RootNamespace = AssemblyFileName;
			SharedState.Importer = new ReferenceImporter(module);

			CommonTypeGetter.Initialize(assembly.ManifestModule);
			SystemTypeGetter.Initialize(assembly.ManifestModule);
		}
	}
}