using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass062_FillConstructors
	{
#nullable disable
		private static IMethodDefOrRef emptyArray;
		private static IMethodDefOrRef emptyString;
#nullable enable
		public static void DoPass()
		{
			emptyArray = SharedState.Instance.Importer.ImportMethod<Array>(method => method.Name == nameof(Array.Empty));
			emptyString = SharedState.Instance.Importer.ImportMethod<Utf8String>(method => method.Name == $"get_{nameof(Utf8String.Empty)}");
			foreach ((int id, ClassGroup classGroup) in SharedState.Instance.ClassGroups)
			{
				foreach (TypeDefinition type in classGroup.Types)
				{
					MethodDefinition assetInfoConstructor = type.GetAssetInfoConstructor();
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
			ArgumentNullException.ThrowIfNull(type);
			if (type.BaseType == null)
			{
				throw new ArgumentException(null, nameof(type));
			}

			if (type.BaseType is TypeDefinition baseTypeDefinition)
			{
				return baseTypeDefinition;
			}
			TypeDefinition? resolvedBaseType = SharedState.Instance.Importer.LookupType(type.BaseType.FullName);
			return resolvedBaseType ?? throw new Exception($"Could not resolve base type {type.BaseType} of derived type {type} from module {type.DeclaringModule} in assembly {type.DeclaringModule!.Assembly}");
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
				if (field.IsStatic || field.IsPrivate || field.Signature!.FieldType.IsValueType)
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
				else if (field.Signature.FieldType is TypeDefOrRefSignature { Namespace: "AssetRipper.Primitives", Name: nameof(Utf8String) })
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Call, emptyString);
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
