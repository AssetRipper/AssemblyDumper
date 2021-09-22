using AssemblyDumper.Passes;
using CommandLine;
using System;
using System.IO;

namespace AssemblyDumper
{
	public static class Program
	{
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
					options.SystemRuntimeAssembly = new FileInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0-rc.1.21451.13\ref\net6.0\System.Runtime.dll");
				if (options.SystemCollectionsAssembly == null)
					options.SystemCollectionsAssembly = new FileInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0-rc.1.21451.13\ref\net6.0\System.Collections.dll");
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

		private static void Run(Options options)
		{
			Logger.Info("Making a new dll");
			try
			{
				Pass00_Initialize.DoPass(options.JsonPath.FullName, options.SystemRuntimeAssembly.FullName, options.SystemCollectionsAssembly.FullName);
				Pass04_ExtractDependentNodeTrees.DoPass();
				Pass06_AddTypeDefinitions.DoPass();
				Pass07_ApplyInheritance.DoPass();
				Pass08_AddDefaultConstructors.DoPass();
				
				Pass12_UnifyFieldsOfAbstractTypes.DoPass();
				Pass15_AddFields.DoPass();
				Pass98_ApplyAssemblyAttributes.DoPass();
				Pass99_SaveAssembly.DoPass(options.OutputDirectory);
				Logger.Info("Done!");
			}
			catch (Exception ex)
			{
				Logger.Info(ex.ToString());
			}
		}
	}
}
