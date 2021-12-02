using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyDumper.Passes
{
	public static class Pass12_FillConstructors
	{
		private static MethodReference emptyArray;
		public static void DoPass()
		{
			Console.WriteLine("Pass 12: Fill Constructors");
			emptyArray = SharedState.Module.ImportSystemMethod<System.Array>(method => method.Name == "Empty");
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
			return type.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 0 && !x.IsStatic).Single();
		}

		private static MethodReference GetDefaultConstructor(this GenericInstanceType type)
		{
			return Utils.MethodUtils.MakeConstructorOnGenericType(type, 0);
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
			ILProcessor processor = constructor.Body.GetILProcessor();
			processor.Clear();
			MethodReference baseConstructor = SharedState.Module.ImportReference(type.BaseType.Resolve().GetDefaultConstructor());
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseConstructor);
			foreach(FieldDefinition field in type.Fields)
			{
				if(field.IsStatic || field.FieldType.IsValueType)
					continue;
				
				if(field.FieldType is GenericInstanceType generic)
				{
					processor.Emit(OpCodes.Ldarg_0);
					processor.Emit(OpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Emit(OpCodes.Stfld, field);
				}
				else if(field.FieldType is TypeDefinition typeDef)
				{
					processor.Emit(OpCodes.Ldarg_0);
					processor.Emit(OpCodes.Newobj, GetDefaultConstructor(typeDef));
					processor.Emit(OpCodes.Stfld, field);
				}
				else if(field.FieldType is ArrayType array)
				{
					processor.Emit(OpCodes.Ldarg_0);
					var method = new GenericInstanceMethod(emptyArray);
					method.GenericArguments.Add(array.ElementType);
					processor.Emit(OpCodes.Call, method);
					processor.Emit(OpCodes.Stfld, field);
				}
				else if(field.FieldType.FullName != "System.String")
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.FieldType.Name} while filling default constructors.");
				}
			}
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
		}

		private static void FillLayoutInfoConstructor(this TypeDefinition type)
		{
			MethodDefinition constructor = GetLayoutInfoConstructor(type);
			ParameterDefinition parameter = constructor.Parameters[0];
			ILProcessor processor = constructor.Body.GetILProcessor();
			processor.Clear();
			MethodReference baseConstructor = SharedState.Module.ImportReference(type.BaseType.Resolve().GetLayoutInfoConstructor());
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldarg, parameter);
			processor.Emit(OpCodes.Call, baseConstructor);
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.IsStatic || field.FieldType.IsValueType)
					continue;

				if (field.FieldType is GenericInstanceType generic)
				{
					processor.Emit(OpCodes.Ldarg_0);
					processor.Emit(OpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType is TypeDefinition typeDef)
				{
					processor.Emit(OpCodes.Ldarg_0);
					processor.Emit(OpCodes.Ldarg, parameter);
					processor.Emit(OpCodes.Newobj, GetLayoutInfoConstructor(typeDef));
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType is ArrayType array)
				{
					processor.Emit(OpCodes.Ldarg_0);
					var method = new GenericInstanceMethod(emptyArray);
					method.GenericArguments.Add(array.ElementType);
					processor.Emit(OpCodes.Call, method);
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType.FullName != "System.String")
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.FieldType.Name} while filling layout info constructors.");
				}
			}
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
		}

		private static void FillAssetInfoConstructor(this TypeDefinition type, MethodDefinition constructor)
		{
			ParameterDefinition parameter = constructor.Parameters[0];
			ILProcessor processor = constructor.Body.GetILProcessor();
			processor.Clear();
			type.BaseType.Resolve().TryGetAssetInfoConstructor(out MethodDefinition baseConstructorDefinition);
			MethodReference baseConstructor = SharedState.Module.ImportReference(baseConstructorDefinition);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldarg, parameter);
			processor.Emit(OpCodes.Call, baseConstructor);
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.IsStatic || field.FieldType.IsValueType)
					continue;

				if (field.FieldType is GenericInstanceType generic)
				{
					processor.Emit(OpCodes.Ldarg_0);
					processor.Emit(OpCodes.Newobj, GetDefaultConstructor(generic));
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType is TypeDefinition typeDef)
				{
					processor.Emit(OpCodes.Ldarg_0);
					if (typeDef.TryGetAssetInfoConstructor(out MethodDefinition fieldAssetInfoConstructor))
					{
						processor.Emit(OpCodes.Ldarg, parameter);
						processor.Emit(OpCodes.Newobj, fieldAssetInfoConstructor);
					}
					else
					{
						processor.Emit(OpCodes.Newobj, GetDefaultConstructor(typeDef));
					}
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType is ArrayType array)
				{
					processor.Emit(OpCodes.Ldarg_0);
					var method = new GenericInstanceMethod(emptyArray);
					method.GenericArguments.Add(array.ElementType);
					processor.Emit(OpCodes.Call, method);
					processor.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType.FullName != "System.String")
				{
					Console.WriteLine($"Warning: skipping {type.Name}.{field.Name} of type {field.FieldType.Name} while filling asset info constructors.");
				}
			}
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
		}
	}
}
