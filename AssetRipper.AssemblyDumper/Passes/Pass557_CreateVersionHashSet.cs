﻿using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass557_CreateVersionHashSet
	{
		public static void DoPass()
		{
			TypeDefinition newTypeDef = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "SourceVersions");

			GenericInstanceTypeSignature unityVersionHashSet = SharedState.Instance.Importer.ImportType(typeof(HashSet<>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.ImportTypeSignature<UnityVersion>());
			IMethodDefOrRef hashsetConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, unityVersionHashSet, 0);
			IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				unityVersionHashSet,
				SharedState.Instance.Importer.LookupMethod(typeof(HashSet<>), m => m.Name == nameof(HashSet<int>.Add)));

			IMethodDefOrRef unityVersionConstructor = SharedState.Instance.Importer.ImportConstructor<UnityVersion>(5);

			FieldDefinition field = newTypeDef.AddField(unityVersionHashSet, "versions", true);
			field.Attributes |= FieldAttributes.InitOnly;

			MethodDefinition staticConstructor = newTypeDef.AddEmptyConstructor(true);
			CilInstructionCollection processor = staticConstructor.GetProcessor();
			processor.Add(CilOpCodes.Newobj, hashsetConstructor);
			foreach (UnityVersion version in SharedState.Instance.SourceVersions)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Major);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Minor);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Build);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Type);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.TypeNumber);
				processor.Add(CilOpCodes.Newobj, unityVersionConstructor);
				processor.Add(CilOpCodes.Call, addMethod);
				processor.Add(CilOpCodes.Pop);
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			Console.WriteLine($"\t{SharedState.Instance.SourceVersions.Length} source versions.");
		}
	}
}