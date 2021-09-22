using AssemblyDumper.Unity;
using Mono.Cecil;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AssemblyDumper.Passes
{
	public static class Pass00_Initialize
	{
		/// <summary>
		/// Used to determine which version of the AssemblyDumper generated an assembly
		/// </summary>
		private static Version currentGeneratedVersion = new Version(0, 0, 0, 0);

		/// <summary>
		/// Read the information json, system assembly, and AssetRipperCommon. Then create a new assembly.
		/// </summary>
		public static void DoPass(string jsonPath, string systemRuntimeFilePath, string systemCollectionsFilePath)
		{
			Logger.Info("Pass 0: Initialize");

			using var sr = new StreamReader(jsonPath);
			using JsonTextReader reader = new JsonTextReader(sr);
			JsonSerializer serializer = new JsonSerializer();
			var info = serializer.Deserialize<UnityInfo>(reader);
			SharedState.Initialize(info);

			string AssemblyFileName = SharedState.Version.Replace('.', '_');

			AssemblyNameDefinition assemblyName = new AssemblyNameDefinition(AssemblyFileName, currentGeneratedVersion);
			AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(assemblyName, AssemblyFileName, ModuleKind.Dll);

			SystemTypeGetter.RuntimeAssembly = AssemblyDefinition.ReadAssembly(systemRuntimeFilePath);
			SystemTypeGetter.CollectionsAssembly = AssemblyDefinition.ReadAssembly(systemCollectionsFilePath);
			//Logger.Info(SystemTypeGetter.RuntimeAssembly.Name.FullName);

			CommonTypeGetter.Assembly = AssemblyDefinition.ReadAssembly(typeof(AssetRipper.Core.UnityObjectBase).Assembly.Location);
			//Logger.Info(CommonTypeGetter.Assembly.Name.FullName);

			assembly.MainModule.AssemblyReferences.Clear();
			assembly.MainModule.AssemblyReferences.Add(SystemTypeGetter.RuntimeAssembly.Name);
			assembly.MainModule.AssemblyReferences.Add(SystemTypeGetter.CollectionsAssembly.Name);
			assembly.MainModule.AssemblyReferences.Add(CommonTypeGetter.Assembly.Name);
			
			SharedState.Assembly = assembly;
			SharedState.RootNamespace = AssemblyFileName;

			CommonTypeGetter.Initialize(assembly.MainModule);
			SystemTypeGetter.Initialize(assembly.MainModule);
		}
	}
}
