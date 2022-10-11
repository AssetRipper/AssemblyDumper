using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Recompiler;

internal class Program
{
	private static bool GeneratePackageOnBuild = false;
	private static bool GenerateDocumentationFile = false;
	private static bool IsTrimmable = false;
	private const string AssetRipperProjectPath = @"E:\repos\AssetRipper";
	private static string CsProjContent = $"""
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<DisableImplicitNamespaceImports>true</DisableImplicitNamespaceImports>
		<Nullable>enable</Nullable>
		<IsTrimmable>{IsTrimmable}</IsTrimmable>
		<RootNamespace>AssetRipper</RootNamespace>

		<AssemblyName>AssetRipper.SourceGenerated</AssemblyName>
		<Copyright>Copyright © 2022</Copyright>
		<Authors>ds5678</Authors>
		<Company>AssetRipper</Company>
		<Version>0.0.0.0</Version>
		<AssemblyVersion>0.0.0.0</AssemblyVersion>

		<PackageId>AssetRipper.SourceGenerated</PackageId>
		<PackageTags>C# unity unity3d</PackageTags>
		<RepositoryUrl>https://github.com/AssetRipper/AssetRipper</RepositoryUrl>
		<PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>
		<RepositoryType>git</RepositoryType>
		<PackageProjectUrl>https://github.com/AssetRipper/AssetRipper</PackageProjectUrl>
		<Copyright>Copyright (c) 2022 ds5678</Copyright>
		<Description>Managed library for handling Unity versions</Description>
		<GeneratePackageOnBuild>{GeneratePackageOnBuild}</GeneratePackageOnBuild>
		<GenerateDocumentationFile>{GenerateDocumentationFile}</GenerateDocumentationFile>
		<DocumentationFile>bin\AssetRipper.SourceGenerated.xml</DocumentationFile>

		<NoWarn>1591</NoWarn>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="{AssetRipperProjectPath}\AssetRipper.Assets\AssetRipper.Assets.csproj" />
		<ProjectReference Include="{AssetRipperProjectPath}\AssetRipper.Numerics\AssetRipper.Numerics.csproj" />
	</ItemGroup>

</Project>
""";
	static void Main(string[] args)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		//string assemblyPath = @"E:\repos\AssemblyDumper\AssetRipper.AssemblyDumper\bin\Debug\net6.0\AssetRipper.SourceGenerated.dll";
		string assemblyPath = args[0];
		//string outputDirectory = "Output";
		string outputDirectory = args[1];
		ClearOrCreateDirectory(outputDirectory);
		UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, true, ".NETCoreApp, Version=6.0");
		WholeProjectDecompiler decompiler = new WholeProjectDecompiler(resolver);
		decompiler.Settings.SetLanguageVersion(LanguageVersion.CSharp10_0);
		decompiler.Settings.UseSdkStyleProjectFormat = true;
		decompiler.Settings.UseNestedDirectoriesForNamespaces = true;
		decompiler.ProgressIndicator = new ProgressIndicator();
		decompiler.DecompileProject(new PEFile(assemblyPath), outputDirectory);
		DeleteBadFilesAndDirectories(outputDirectory);
		WriteCsProjFile(outputDirectory);
		stopwatch.Stop();
		Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds/1000} seconds!");
	}

	private static void DeleteBadFilesAndDirectories(string outputDirectory)
	{
		Directory.Delete(Path.Combine(outputDirectory, "Microsoft"), true);
		Directory.Delete(Path.Combine(outputDirectory, "System"), true);
		Directory.Delete(Path.Combine(outputDirectory, "Properties"), true);
		File.Delete(Path.Combine(outputDirectory, "AssetRipper.SourceGenerated.csproj"));
	}

	private static void WriteCsProjFile(string outputDirectory)
	{
		File.WriteAllText(Path.Combine(outputDirectory, "AssetRipper.SourceGenerated.csproj"), CsProjContent);
	}

	private static void ClearOrCreateDirectory(string directory)
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, true);
		}
		Directory.CreateDirectory(directory);
	}

	private sealed class ProgressIndicator : IProgress<DecompilationProgress>
	{
		private readonly object lockObject = new();
		private int completed = 0;
		public void Report(DecompilationProgress value)
		{
			lock(lockObject)
			{
				completed++;
				Console.WriteLine($"{completed}/{value.TotalNumberOfFiles} File: {value.Status}");
			}
		}
	}
}