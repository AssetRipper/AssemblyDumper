﻿using AsmResolver.DotNet.Builder;
using System.IO;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass998_SaveAssembly
	{
		public static void DoPass()
		{
			AssemblyDefinition assembly = SharedState.Instance.Assembly;

			string filePath = Path.Combine(Environment.CurrentDirectory, assembly.Name!.ToString() + ".dll");

			DotNetDirectoryFactory factory = new DotNetDirectoryFactory();
			//factory.MetadataBuilderFlags |= MetadataBuilderFlags.NoStringsStreamOptimization; //Check later, but currently less than 1% difference
			ManagedPEImageBuilder builder = new ManagedPEImageBuilder(factory);

			//Console.WriteLine($"Saving assembly to {filePath}");
			try
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				assembly.Write(filePath, builder);
				Console.WriteLine($"\t{GetTypeCount()} top level types.");
				Console.WriteLine($"\t{GetMethodCount()} methods.");
				Console.WriteLine($"\t{GetPropertyCount()} properties.");
				Console.WriteLine($"\t{GetFieldCount()} fields.");
			}
			catch (AggregateException aggregateException)
			{
				Console.WriteLine("AggregateException thrown");
				aggregateException = aggregateException.Flatten();

				foreach (Exception? error in aggregateException.InnerExceptions)
				{
					Console.WriteLine();
					Console.WriteLine(error.ToString());
					Console.WriteLine();
				}
			}
		}

		private static int GetTypeCount()
		{
			return SharedState.Instance.Module.TopLevelTypes.Count;
		}

		private static int GetMethodCount()
		{
			int sum = 0;
			foreach (TypeDefinition type in SharedState.Instance.Module.TopLevelTypes)
			{
				sum += GetMethodCount(type);
			}
			return sum;
		}

		private static int GetMethodCount(TypeDefinition type)
		{
			int sum = type.Methods.Count;
			foreach (TypeDefinition nestedType in type.NestedTypes)
			{
				sum += GetMethodCount(nestedType);
			}
			return sum;
		}

		private static int GetPropertyCount()
		{
			int sum = 0;
			foreach (TypeDefinition type in SharedState.Instance.Module.TopLevelTypes)
			{
				sum += GetPropertyCount(type);
			}
			return sum;
		}

		private static int GetPropertyCount(TypeDefinition type)
		{
			int sum = type.Properties.Count;
			foreach (TypeDefinition nestedType in type.NestedTypes)
			{
				sum += GetPropertyCount(nestedType);
			}
			return sum;
		}

		private static int GetFieldCount()
		{
			int sum = 0;
			foreach (TypeDefinition type in SharedState.Instance.Module.TopLevelTypes)
			{
				sum += GetFieldCount(type);
			}
			return sum;
		}

		private static int GetFieldCount(TypeDefinition type)
		{
			int sum = type.Fields.Count;
			foreach (TypeDefinition nestedType in type.NestedTypes)
			{
				sum += GetFieldCount(nestedType);
			}
			return sum;
		}
	}
}