namespace AssetRipper.AssemblyCreationTools.Methods
{
	public static class MethodCreator
	{
		public static MethodDefinition AddMethod(this TypeDefinition type, string methodName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			MethodDefinition method = CreateMethod(methodName, methodAttributes, returnType);
			type.Methods.Add(method);
			return method;
		}

		/// <summary>
		/// Creates an empty conversion method
		/// </summary>
		/// <param name="declaringType"></param>
		/// <param name="parameterType"></param>
		/// <param name="returnType"></param>
		/// <param name="isImplicit"></param>
		/// <returns>A new method with the correct name, return type, and parameter, but with an empty method body</returns>
		public static MethodDefinition AddEmptyConversion(this TypeDefinition declaringType, TypeSignature parameterType, TypeSignature returnType, bool isImplicit)
		{
			MethodDefinition conversion = declaringType.AddMethod(
				isImplicit ? "op_Implicit" : "op_Explicit",
				MethodAttributes.Public |
				MethodAttributes.HideBySig |
				MethodAttributes.SpecialName |
				MethodAttributes.Static,
				returnType);

			conversion.AddParameter(parameterType, "value");

			return conversion;
		}

		/// <summary>
		/// Creates a parameterless extern method
		/// </summary>
		/// <param name="declaringType">The type this method will be added to</param>
		/// <param name="returnType">The return type signature of this method</param>
		/// <param name="name">The name of this method and the native export it references</param>
		/// <param name="moduleReference">The native module this method references</param>
		/// <param name="attributes">The implementation attributes for this method</param>
		/// <returns></returns>
		public static MethodDefinition AddExternalMethod(this TypeDefinition declaringType, TypeSignature returnType, string name, ModuleReference moduleReference, ImplementationMapAttributes attributes)
		{
			MethodSignature signature = MethodSignature.CreateStatic(returnType);
			MethodDefinition method = new MethodDefinition(
				name,
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl,
				signature);
			method.PreserveSignature = true;
			method.ImplementationMap = new ImplementationMap(
				moduleReference,
				null,
				attributes);
			declaringType.Methods.Add(method);
			return method;
		}

		public static MethodDefinition CreateMethod(string methodName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
			MethodSignature methodSignature = isStatic
				? MethodSignature.CreateStatic(returnType)
				: MethodSignature.CreateInstance(returnType);
			MethodDefinition result = new MethodDefinition(methodName, methodAttributes, methodSignature);

			result.CilMethodBody = new CilMethodBody(result);

			return result;
		}

		public static Parameter AddParameter(this MethodDefinition method, TypeSignature parameterSignature, string parameterName)
		{
			ParameterDefinition parameterDefinition = new ParameterDefinition((ushort)(method.Signature!.ParameterTypes.Count + 1), parameterName, default);
			method.Signature.ParameterTypes.Add(parameterSignature);
			method.ParameterDefinitions.Add(parameterDefinition);

			method.Parameters.PullUpdatesFromMethodSignature();
			return method.Parameters.Single(parameter => parameter.Name == parameterName && parameter.ParameterType == parameterSignature);
		}

		public static Parameter AddParameter(this MethodDefinition method, TypeSignature parameterSignature)
		{
			method.Signature!.ParameterTypes.Add(parameterSignature);
			method.Parameters.PullUpdatesFromMethodSignature();
			return method.Parameters[method.Parameters.Count - 1];
		}

		public static ParameterDefinition GetOrAddReturnTypeParameterDefinition(this MethodDefinition method)
		{
			if (method.ParameterDefinitions.Count > 0 && method.ParameterDefinitions[0].Sequence == 0)
			{
				return method.ParameterDefinitions[0];
			}
			else
			{
				ParameterDefinition parameterDefinition = new ParameterDefinition(0, default, default);
				method.ParameterDefinitions.Insert(0, parameterDefinition);
				return parameterDefinition;
			}
		}
	}
}
