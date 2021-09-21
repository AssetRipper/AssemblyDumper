using AssemblyDumper.Unity;
using Mono.Cecil;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AssemblyDumper.Passes
{
	public static class Pass00_Initialize
	{
		private const string AssemblyFileName = "UnityStructs.dll";
		private const string SystemRuntimeFilePath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0-rc.1.21451.13\ref\net6.0\System.Runtime.dll";
		private const string JsonPath = "info.json";
		/// <summary>
		/// Used to determine which version of the AssemblyDumper generated an assembly
		/// </summary>
		private static Version currentGeneratedVersion = new Version(0, 0, 0, 0);

		/// <summary>
		/// Read the information json, system assembly, and AssetRipperCommon. Then create a new assembly.
		/// </summary>
		/// <param name="jsonPath"></param>
		public static void DoPass()
		{
			Logger.Info("Pass 0: Initialize");

			using var sr = new StreamReader(JsonPath);
			using JsonTextReader reader = new JsonTextReader(sr);
			JsonSerializer serializer = new JsonSerializer();
			SharedState.Info = serializer.Deserialize<UnityInfo>(reader);

			AssemblyNameDefinition assemblyName = new AssemblyNameDefinition(AssemblyFileName, currentGeneratedVersion);
			AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(assemblyName, AssemblyFileName, ModuleKind.Dll);

			SystemTypeGetter.Assembly = AssemblyDefinition.ReadAssembly(SystemRuntimeFilePath);
			Logger.Info(SystemTypeGetter.Assembly.Name.FullName);

			CommonTypeGetter.Assembly = AssemblyDefinition.ReadAssembly(typeof(AssetRipper.Core.UnityObjectBase).Assembly.Location);
			Logger.Info(CommonTypeGetter.Assembly.Name.FullName);

			assembly.MainModule.AssemblyReferences.Clear();
			assembly.MainModule.AssemblyReferences.Add(SystemTypeGetter.Assembly.Name);
			assembly.MainModule.AssemblyReferences.Add(CommonTypeGetter.Assembly.Name);
			
			SharedState.Assembly = assembly;
			SharedState.RootNamespace = SharedState.Info.Version.Replace('.', '_');

			CommonTypeGetter.Initialize(assembly.MainModule);
		}
	}
}
