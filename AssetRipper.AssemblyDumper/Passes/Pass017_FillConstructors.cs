using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Core.Parser.Asset;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass017_FillConstructors
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static IMethodDefOrRef emptyArray;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public static void DoPass()
		{
			emptyArray = SharedState.Instance.Importer.ImportMethod<Array>(method => method.Name == nameof(Array.Empty));
			IMethodDefOrRef makeDummyAssetInfo = SharedState.Instance.Importer.ImportMethod<AssetInfo>(method =>
				method.Name == nameof(AssetInfo.MakeDummyAssetInfo)
				&& method.Parameters.Count == 1);
			foreach ((int id, ClassGroup classGroup) in SharedState.Instance.ClassGroups)
			{
				foreach (TypeDefinition type in classGroup.Types)
				{
					MethodDefinition assetInfoConstructor = type.GetAssetInfoConstructor();
					type.FillClassDefaultConstructor(id, assetInfoConstructor, makeDummyAssetInfo);
					type.FillClassAssetInfoConstructor(assetInfoConstructor);
				}
			}
			foreach (SubclassGroup subclassGroup in SharedState.Instance.SubclassGroups.Values)
			{
				foreach (TypeDefinition type in subclassGroup.Types)
				{
					type.FillSubclassDefaultConstructor();
				}
			}
		}

		private static TypeDefinition GetResolvedBaseType(this TypeDefinition type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			if (type.BaseType == null)
			{
				throw new ArgumentException(null, nameof(type));
			}

			if (type.BaseType is TypeDefinition baseTypeDefinition)
			{
				return baseTypeDefinition;
			}
			TypeDefinition? resolvedBaseType = SharedState.Instance.Importer.LookupType(type.BaseType.FullName);
			return resolvedBaseType ?? throw new Exception($"Could not resolve base type {type.BaseType} of derived type {type} from module {type.Module} in assembly {type.Module!.Assembly}");
		}

		private static IMethodDefOrRef GetDefaultConstructor(this GenericInstanceTypeSignature type)
		{
			return MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, type, 0);
		}

		private static MethodDefinition GetAssetInfoConstructor(this TypeDefinition type)
		{
			return type.Methods.First(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == nameof(AssetInfo));
		}

		private static void FillSubclassDefaultConstructor(this TypeDefinition type)
		{
			MethodDefinition constructor = type.GetDefaultConstructor();
			CilInstructionCollection processor = constructor.CilMethodBody!.Instructions;
			processor.Clear();
			IMethodDefOrRef baseConstructor = SharedState.Instance.Importer.UnderlyingImporter.ImportMethod(type.GetResolvedBaseType().GetDefaultConstructor());
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, baseConstructor);
			processor.AddFieldAssignments(type);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void FillClassDefaultConstructor(this TypeDefinition type, int id, MethodDefinition assetInfoConstructor, IMethodDefOrRef makeDummyAssetInfo)
		{
			MethodDefinition constructor = type.GetDefaultConstructor();
			CilInstructionCollection processor = constructor.CilMethodBody!.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldc_I4, id);
			processor.Add(CilOpCodes.Call, makeDummyAssetInfo);
			processor.Add(CilOpCodes.Call, assetInfoConstructor);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void FillClassAssetInfoConstructor(this TypeDefinition type, MethodDefinition constructor)
		{
			CilInstructionCollection processor = constructor.CilMethodBody!.Instructions;
			processor.Clear();
			MethodDefinition baseConstructorDefinition = type.GetResolvedBaseType().GetAssetInfoConstructor();
			IMethodDefOrRef baseConstructor = SharedState.Instance.Importer.UnderlyingImporter.ImportMethod(baseConstructorDefinition);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, baseConstructor);
			processor.AddFieldAssignments(type);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void AddFieldAssignments(this CilInstructionCollection processor, TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.IsStatic || field.Signature!.FieldType.IsValueType)
				{
					continue;
				}

				if (field.Signature.FieldType is GenericInstanceTypeSignature generic)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType is SzArrayTypeSignature array)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					MethodSpecification method = emptyArray.MakeGenericInstanceMethod(array.BaseType);
					processor.Add(CilOpCodes.Call, method);
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType.ToTypeDefOrRef() is TypeDefinition typeDef)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, typeDef.GetDefaultConstructor());
					processor.Add(CilOpCodes.Stfld, field);
				}
				else
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.Signature.FieldType.Name} while adding field assignments.");
				}
			}
		}
	}
}
