using AssemblyDumper.Passes;
using CommandLine;
using System;
using System.IO;

namespace AssemblyDumper
{
	public static class Program
	{
		private static void Run(Options options)
		{
			Console.WriteLine("Making a new dll");
#if DEBUG
			try
			{
#endif
				Pass00_Initialize.DoPass(options.JsonPath.FullName, options.SystemRuntimeAssembly.FullName, options.SystemCollectionsAssembly.FullName);
				Pass01_CreateBasicTypes.DoPass();
				Pass02_RenameSubnodes.DoPass();
				Pass04_ExtractDependentNodeTrees.DoPass();
				Pass05_UnifyFieldsOfAbstractTypes.DoPass();
				//After this point, class dictionary does not change

				Pass07_AddTypeDefinitions.DoPass();
				Pass08_ApplyInheritance.DoPass();
				Pass10_AddFields.DoPass();

				Pass11_AddConstructors.DoPass();
				Pass12_FillConstructors.DoPass();

				Pass20_PPtrConversions.DoPass();
				Pass21_GuidImplicitConversion.DoPass();
				Pass22_VectorImplicitConversions.DoPass();
				Pass23_OffsetPtrImplicitConversions.DoPass();
				Pass24_Hash128ImplicitConversion.DoPass();

				Pass25_ObjectHideFlags.DoPass();

				Pass30_ImplementHasNameInterface.DoPass();
				Pass31_ComponentInterface.DoPass();
				Pass32_MonoScriptInterface.DoPass();
				Pass33_BehaviourInterface.DoPass();
				Pass34_GameObjectInterface.DoPass();
				Pass35_TransformInterface.DoPass();
				Pass36_PrefabInstanceInterface.DoPass();

				Pass40_BuildSettingsInterfaces.DoPass();

				Pass49_CreateEmptyMethods.DoPass();
				Pass50_FillReadMethods.DoPass();
				Pass51_FillWriteMethods.DoPass();
				Pass52_FillYamlMethods.DoPass();
				Pass53_FillTypeTreeMethods.DoPass();
				Pass54_FillDependencyMethods.DoPass();

				Pass60_AddMarkerInterfaces.DoPass();
				Pass61_NativeImporterInterface.DoPass();

				Pass70_FixPPtrYaml.DoPass();
				Pass71_MonoBehaviourImplementation.DoPass();

				Pass90_MakeAssetFactory.DoPass();
				Pass91_MakeImporterFactory.DoPass();
				Pass92_MakeSceneObjectFactory.DoPass();
				Pass95_UnityVersionHandler.DoPass();

				Pass98_ApplyAssemblyAttributes.DoPass();
				Pass99_SaveAssembly.DoPass(options.OutputDirectory);
				Console.WriteLine("Done!");
#if DEBUG
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
#endif
		}

		internal class Options
		{
			[Value(0, Required = true, HelpText = "Information Json to parse")]
			public FileInfo JsonPath { get; set; }

			[Option('o', "output", HelpText = "Directory to export to. Will not be cleared if already exists.")]
			public DirectoryInfo OutputDirectory { get; set; }

			[Option("runtime", HelpText = "System.Runtime.dll from Net 6")]
			public FileInfo SystemRuntimeAssembly { get; set; }

			[Option("collections", HelpText = "System.Collections.dll from Net 6")]
			public FileInfo SystemCollectionsAssembly { get; set; }
		}

		public static void Main(string[] args)
		{
			CommandLine.Parser.Default.ParseArguments<Options>(args)
				.WithParsed(options =>
				{
					if (ValidateOptions(options))
					{
						Run(options);
					}
					else
					{
						Environment.ExitCode = 1;
					}
				});
		}

		private static bool ValidateOptions(Options options)
		{
			try
			{
				if (options.JsonPath == null || !options.JsonPath.Exists)
					return false;
				if (options.SystemRuntimeAssembly == null)
					options.SystemRuntimeAssembly = new FileInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0\ref\net6.0\System.Runtime.dll");
				if (options.SystemCollectionsAssembly == null)
					options.SystemCollectionsAssembly = new FileInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0\ref\net6.0\System.Collections.dll");
				if (options.OutputDirectory == null)
					options.OutputDirectory = new DirectoryInfo(Environment.CurrentDirectory);

				return options.SystemRuntimeAssembly.Exists && options.SystemCollectionsAssembly.Exists;
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"Failed to initialize the paths.");
				System.Console.WriteLine(ex.ToString());
				return false;
			}
		}
	}
}