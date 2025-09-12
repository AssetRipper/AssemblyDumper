using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using System.Runtime.InteropServices;

namespace AssetRipper.AssemblyCreationTools.Types
{
	public static class FunctionPointerStructCreator
	{
		public static TypeDefinition Create(AssemblyBuilder builder, string @namespace, string name, TypeSignature returnType, Dictionary<string, TypeSignature> parameters)
		{
			TypeDefinition type = StructCreator.CreateEmptyStruct(builder, @namespace, name);
			FieldDefinition field = type.AddField(builder.Importer.IntPtr, "address");
			MethodDefinition constructor = type.AddConstructor(field);
			FunctionPointerTypeSignature functionPointerSignature = FunctionPointerCreator.CreateUnmanaged(returnType, parameters.Values);
			TypeSignature managedDelegateSignature = ResolveManagedDelegateSignature(builder.Importer, returnType, parameters.Values.ToArray());
			type.AddConversionToIntPtr(builder.Importer, field);
			type.AddConversionFromIntPtr(builder.Importer, field, constructor);
			type.AddConversionToFunctionPointer(builder.Importer, field, functionPointerSignature);
			type.AddConversionFromFunctionPointer(builder.Importer, constructor, functionPointerSignature);
			MethodDefinition managedDelegateConversionMethod = type.AddConversionToManagedDelegate(builder.Importer, field, managedDelegateSignature);
			type.AddInvokeMethod(builder.Importer, managedDelegateConversionMethod, managedDelegateSignature, returnType, parameters);
			return type;
		}

		private static MethodDefinition AddConstructor(this TypeDefinition type, FieldDefinition field)
		{
			MethodDefinition constructor = type.AddMethod(
				".ctor",
				MethodAttributes.Public |
				MethodAttributes.HideBySig |
				MethodAttributes.SpecialName |
				MethodAttributes.RuntimeSpecialName,
				type.DeclaringModule!.CorLibTypeFactory.Void);

			constructor.AddParameter(type.DeclaringModule.CorLibTypeFactory.IntPtr, "address");

			CilInstructionCollection processor = constructor.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
			return constructor;
		}

		private static MethodDefinition AddConversionToManagedDelegate(this TypeDefinition type, CachedReferenceImporter importer, FieldDefinition field, TypeSignature managedDelegateSignature)
		{
			MethodDefinition conversion = type.AddEmptyConversion(type.ToTypeSignature(), managedDelegateSignature, false);

			IMethodDefOrRef genericMarshalMethod = importer.ImportMethod(typeof(Marshal), m => m.Name == nameof(Marshal.GetDelegateForFunctionPointer) && m.GenericParameters.Count == 1);
			MethodSpecification instanceMarshalMethod = genericMarshalMethod.MakeGenericInstanceMethod(managedDelegateSignature);

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Call, instanceMarshalMethod);
			processor.Add(CilOpCodes.Ret);
			return conversion;
		}

		private static MethodDefinition AddConversionToFunctionPointer(this TypeDefinition type, CachedReferenceImporter importer, FieldDefinition field, FunctionPointerTypeSignature functionPointerSignature)
		{
			MethodDefinition conversion = type.AddEmptyConversion(type.ToTypeSignature(), functionPointerSignature, true);

			CilLocalVariable local = new CilLocalVariable(functionPointerSignature);
			conversion.CilMethodBody!.LocalVariables.Add(local);

			IMethodDefOrRef intPtrConversionMethod = importer.ImportMethod(typeof(IntPtr), m => m.Name == "op_Explicit" && m.Signature!.ReturnType is PointerTypeSignature);

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Call, intPtrConversionMethod);
			processor.Add(CilOpCodes.Stloc, local);
			processor.Add(CilOpCodes.Ldloc, local);
			processor.Add(CilOpCodes.Ret);
			return conversion;
		}

		private static MethodDefinition AddConversionFromFunctionPointer(this TypeDefinition type, CachedReferenceImporter importer, MethodDefinition constructor, FunctionPointerTypeSignature functionPointerSignature)
		{
			MethodDefinition conversion = type.AddEmptyConversion(functionPointerSignature, type.ToTypeSignature(), true);

			//CilLocalVariable local = new CilLocalVariable(type.ToTypeSignature());
			//conversion.CilMethodBody!.LocalVariables.Add(local);

			IMethodDefOrRef intPtrConversionMethod = importer.ImportMethod(typeof(IntPtr), m => m.Name == "op_Explicit" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType is PointerTypeSignature);

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, intPtrConversionMethod);
			processor.Add(CilOpCodes.Newobj, constructor);
			//processor.Add(CilOpCodes.Stloc, local);
			//processor.Add(CilOpCodes.Ldloc, local);
			processor.Add(CilOpCodes.Ret);
			return conversion;
		}

		private static MethodDefinition AddConversionToIntPtr(this TypeDefinition type, CachedReferenceImporter importer, FieldDefinition field)
		{
			MethodDefinition conversion = type.AddEmptyConversion(type.ToTypeSignature(), importer.IntPtr, false);

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Ret);
			return conversion;
		}

		private static MethodDefinition AddConversionFromIntPtr(this TypeDefinition type, CachedReferenceImporter importer, FieldDefinition field, MethodDefinition constructor)
		{
			MethodDefinition conversion = type.AddEmptyConversion(importer.IntPtr, type.ToTypeSignature(), false);

			CilLocalVariable local = new CilLocalVariable(type.ToTypeSignature());
			conversion.CilMethodBody!.LocalVariables.Add(local);

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
			return conversion;
		}

		private static MethodDefinition AddInvokeMethod(
			this TypeDefinition type,
			CachedReferenceImporter importer,
			MethodDefinition explicitConversion,
			TypeSignature managedDelegateSignature,
			TypeSignature returnType,
			Dictionary<string, TypeSignature> parameterDictionary)
		{
			MethodDefinition conversion = type.AddMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig, returnType);

			Parameter[] parameters = new Parameter[parameterDictionary.Count];
			int i = 0;
			foreach (var parameterDescription in parameterDictionary)
			{
				parameters[i] = conversion.AddParameter(parameterDescription.Value, parameterDescription.Key);
				i++;
			}

			//CilLocalVariable local = new CilLocalVariable(type.ToTypeSignature());
			//conversion.CilMethodBody!.LocalVariables.Add(local);

			IMethodDefOrRef invokeMethod;
			if (managedDelegateSignature is GenericInstanceTypeSignature genericDelegateSignature)
			{
				invokeMethod = MethodUtils.MakeMethodOnGenericType(importer, genericDelegateSignature, m => m.Name == "Invoke");
			}
			else
			{
				invokeMethod = importer.ImportMethod<Action>(m => m.Name == "Invoke");
			}

			CilInstructionCollection processor = conversion.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldobj, type);
			processor.Add(CilOpCodes.Call, explicitConversion);
			for (int j = 0; j < parameters.Length; j++)
			{
				processor.Add(CilOpCodes.Ldarg, parameters[j]);
			}
			processor.Add(CilOpCodes.Callvirt, invokeMethod);
			//processor.Add(CilOpCodes.Stloc, local);
			//processor.Add(CilOpCodes.Ldloc, local);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return conversion;
		}

		private static TypeSignature ResolveManagedDelegateSignature(CachedReferenceImporter importer, TypeSignature returnType, TypeSignature[] parameterTypes)
		{
			bool useFunc = returnType.FullName != typeof(void).FullName;
			if (useFunc)
			{
				TypeSignature funcType = importer.ImportTypeSignature(GetFuncType(parameterTypes.Length));
				TypeSignature[] funcTypeParameters = new TypeSignature[parameterTypes.Length + 1];
				Array.Copy(parameterTypes, 0, funcTypeParameters, 0, parameterTypes.Length);
				funcTypeParameters[parameterTypes.Length] = returnType;
				return funcType.MakeGenericInstanceType(funcTypeParameters);
			}
			else
			{
				TypeSignature actionType = importer.ImportTypeSignature(GetActionType(parameterTypes.Length));
				return parameterTypes.Length == 0 ? actionType : actionType.MakeGenericInstanceType(parameterTypes);
			}
		}

		private static Type GetActionType(int parameterCount)
		{
			return parameterCount switch
			{
				0 => typeof(Action),
				1 => typeof(Action<>),
				2 => typeof(Action<,>),
				3 => typeof(Action<,,>),
				4 => typeof(Action<,,,>),
				5 => typeof(Action<,,,,>),
				6 => typeof(Action<,,,,,>),
				7 => typeof(Action<,,,,,,>),
				8 => typeof(Action<,,,,,,,>),
				9 => typeof(Action<,,,,,,,,>),
				10 => typeof(Action<,,,,,,,,,>),
				11 => typeof(Action<,,,,,,,,,,>),
				12 => typeof(Action<,,,,,,,,,,,>),
				13 => typeof(Action<,,,,,,,,,,,,>),
				14 => typeof(Action<,,,,,,,,,,,,,>),
				15 => typeof(Action<,,,,,,,,,,,,,,>),
				16 => typeof(Action<,,,,,,,,,,,,,,,>),
				_ => throw new ArgumentOutOfRangeException(nameof(parameterCount)),
			};
		}

		private static Type GetFuncType(int parameterCount)
		{
			return parameterCount switch
			{
				0 => typeof(Func<>),
				1 => typeof(Func<,>),
				2 => typeof(Func<,,>),
				3 => typeof(Func<,,,>),
				4 => typeof(Func<,,,,>),
				5 => typeof(Func<,,,,,>),
				6 => typeof(Func<,,,,,,>),
				7 => typeof(Func<,,,,,,,>),
				8 => typeof(Func<,,,,,,,,>),
				9 => typeof(Func<,,,,,,,,,>),
				10 => typeof(Func<,,,,,,,,,,>),
				11 => typeof(Func<,,,,,,,,,,,>),
				12 => typeof(Func<,,,,,,,,,,,,>),
				13 => typeof(Func<,,,,,,,,,,,,,>),
				14 => typeof(Func<,,,,,,,,,,,,,,>),
				15 => typeof(Func<,,,,,,,,,,,,,,,>),
				16 => typeof(Func<,,,,,,,,,,,,,,,,>),
				_ => throw new ArgumentOutOfRangeException(nameof(parameterCount)),
			};
		}
	}
}
