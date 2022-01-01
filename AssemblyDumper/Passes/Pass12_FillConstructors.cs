using AssemblyDumper.Utils;

namespace AssemblyDumper.Passes
{
	public static class Pass12_FillConstructors
	{
		private static IMethodDefOrRef emptyArray;
		public static void DoPass()
		{
			Console.WriteLine("Pass 12: Fill Constructors");
			emptyArray = SharedState.Importer.ImportSystemMethod<Array>(method => method.Name == "Empty");
			foreach(TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				type.FillDefaultConstructor();
				type.FillLayoutInfoConstructor();
				if(type.TryGetAssetInfoConstructor(out MethodDefinition assetInfoConstructor))
				{
					type.FillAssetInfoConstructor(assetInfoConstructor);
				}
			}
		}

		private static MethodDefinition GetDefaultConstructor(this TypeDefinition type)
		{
			if (type is null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			return type.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 0 && !x.IsStatic).Single();
		}

		private static TypeDefinition GetResolvedBaseType(this TypeDefinition type)
		{
			if(type?.BaseType == null)
			{
				return null;
			}
			if(type.BaseType is TypeDefinition baseTypeDefinition)
			{
				return baseTypeDefinition;
			}
			TypeDefinition resolvedBaseType = type.BaseType.Resolve();
			if(resolvedBaseType == null)
			{
				throw new Exception($"Could not resolve base type {type.BaseType} of derived type {type} from module {type.Module} in assembly {type.Module.Assembly}");
			}
			return resolvedBaseType;
		}

		private static IMethodDefOrRef GetDefaultConstructor(this GenericInstanceTypeSignature type)
		{
			return MethodUtils.MakeConstructorOnGenericType(type, 0);
		}

		private static MethodDefinition GetLayoutInfoConstructor(this TypeDefinition type)
		{
			return type.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == "LayoutInfo").Single();
		}

		private static bool TryGetAssetInfoConstructor(this TypeDefinition type, out MethodDefinition constructor)
		{
			constructor = type.Methods.FirstOrDefault(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == "AssetInfo");
			return constructor != null;
		}

		private static void FillDefaultConstructor(this TypeDefinition type)
		{
			MethodDefinition constructor = GetDefaultConstructor(type);
			CilInstructionCollection processor = constructor.CilMethodBody.Instructions;
			processor.Clear();
			IMethodDefOrRef baseConstructor = SharedState.Importer.ImportMethod(type.GetResolvedBaseType().GetDefaultConstructor());
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, baseConstructor);
			foreach(FieldDefinition field in type.Fields)
			{
				if(field.IsStatic || field.Signature.FieldType.IsValueType)
					continue;
				
				if(field.Signature.FieldType is GenericInstanceTypeSignature generic)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType is SzArrayTypeSignature array)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					var method = MethodUtils.MakeGenericInstanceMethod(emptyArray, array.BaseType);
					processor.Add(CilOpCodes.Call, method);
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if(field.Signature.FieldType.ToTypeDefOrRef() is TypeDefinition typeDef)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(typeDef));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if(field.Signature.FieldType.FullName == "System.String")
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldstr, "");
					processor.Add(CilOpCodes.Stfld, field);
				}
				else
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.Signature.FieldType.Name} while filling default constructors.");
				}
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void FillLayoutInfoConstructor(this TypeDefinition type)
		{
			MethodDefinition constructor = GetLayoutInfoConstructor(type);
			Parameter parameter = constructor.Parameters[0];
			CilInstructionCollection processor = constructor.CilMethodBody.Instructions;
			processor.Clear();
			IMethodDefOrRef baseConstructor = SharedState.Importer.ImportMethod(type.GetResolvedBaseType().GetLayoutInfoConstructor());
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg, parameter);
			processor.Add(CilOpCodes.Call, baseConstructor);
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.IsStatic || field.Signature.FieldType.IsValueType)
					continue;

				if (field.Signature.FieldType is GenericInstanceTypeSignature generic)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType is SzArrayTypeSignature array)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					var method = MethodUtils.MakeGenericInstanceMethod(emptyArray, array.BaseType);
					processor.Add(CilOpCodes.Call, method);
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType.ToTypeDefOrRef() is TypeDefinition typeDef)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldarg, parameter);
					processor.Add(CilOpCodes.Newobj, GetLayoutInfoConstructor(typeDef));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType.FullName == "System.String")
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldstr, "");
					processor.Add(CilOpCodes.Stfld, field);
				}
				else
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.Signature.FieldType.Name} while filling layout info constructors.");
				}
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static void FillAssetInfoConstructor(this TypeDefinition type, MethodDefinition constructor)
		{
			Parameter parameter = constructor.Parameters[0];
			CilInstructionCollection processor = constructor.CilMethodBody.Instructions;
			processor.Clear();
			type.GetResolvedBaseType().TryGetAssetInfoConstructor(out MethodDefinition baseConstructorDefinition);
			IMethodDefOrRef baseConstructor = SharedState.Importer.ImportMethod(baseConstructorDefinition);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg, parameter);
			processor.Add(CilOpCodes.Call, baseConstructor);
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.IsStatic || field.Signature.FieldType.IsValueType)
					continue;

				if (field.Signature.FieldType is GenericInstanceTypeSignature generic)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType is SzArrayTypeSignature array)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					var method = MethodUtils.MakeGenericInstanceMethod(emptyArray, array.BaseType);
					processor.Add(CilOpCodes.Call, method);
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType.ToTypeDefOrRef() is TypeDefinition typeDef)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					if (typeDef.TryGetAssetInfoConstructor(out MethodDefinition fieldAssetInfoConstructor))
					{
						processor.Add(CilOpCodes.Ldarg, parameter);
						processor.Add(CilOpCodes.Newobj, fieldAssetInfoConstructor);
					}
					else
					{
						processor.Add(CilOpCodes.Newobj, GetDefaultConstructor(typeDef));
					}
					processor.Add(CilOpCodes.Stfld, field);
				}
				else if (field.Signature.FieldType.FullName == "System.String")
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldstr, "");
					processor.Add(CilOpCodes.Stfld, field);
				}
				else
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.Signature.FieldType.Name} while filling asset info constructors.");
				}
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}
	}
}
