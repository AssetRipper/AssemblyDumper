namespace AssetRipper.AssemblyCreationTools.Methods
{
	public static class ConstructorUtils
	{
		/// <summary>
		/// Gets the default constructor for a <see cref="TypeDefinition"/>. Throws an exception if one doesn't exist.
		/// </summary>
		public static MethodDefinition GetDefaultConstructor(this TypeDefinition _this) => _this.GetConstructor(0);

		/// <summary>
		/// Imports the default constructor for a type. Throws an exception if one doesn't exist.
		/// </summary>
		public static IMethodDefOrRef ImportDefaultConstructor<T>(this CachedReferenceImporter importer)
		{
			return importer.ImportConstructor<T>(0);
		}

		/// <summary>
		/// Gets the static constructor for a <see cref="TypeDefinition"/>. Throws an exception if one doesn't exist.
		/// </summary>
		public static MethodDefinition GetStaticConstructor(this TypeDefinition _this)
		{
			return _this.Methods.Single(m => m.IsConstructor && m.IsStatic);
		}

		/// <summary>
		/// Get all the instance constructors for a type
		/// </summary>
		/// <param name="type">The definition for the type</param>
		/// <returns>All the instance constructors</returns>
		public static IEnumerable<MethodDefinition> GetInstanceConstructors(this TypeDefinition type)
		{
			return type.Methods.Where(m => !m.IsStatic && m.IsConstructor);
		}

		/// <summary>
		/// Gets the constructor with that number of parameters. Throws an exception if there's not exactly one.
		/// </summary>
		public static MethodDefinition GetConstructor(this TypeDefinition _this, int numParameters)
		{
			return _this.Methods.Single(m => !m.IsStatic && m.IsConstructor && m.Parameters.Count == numParameters);
		}

		/// <summary>
		/// Imports the constructor with that number of parameters. Throws an exception if there's not exactly one.
		/// </summary>
		public static IMethodDefOrRef ImportConstructor<T>(this CachedReferenceImporter importer, int numParameters)
		{
			return importer.ImportMethod<T>(m => !m.IsStatic && m.IsConstructor && m.Parameters.Count == numParameters);
		}

		/// <summary>
		/// Imports the constructor with that number of parameters. Throws an exception if there's not exactly one.
		/// </summary>
		public static IMethodDefOrRef ImportConstructor<T>(this CachedReferenceImporter importer, Func<MethodDefinition, bool> func)
		{
			return importer.ImportMethod<T>(m => !m.IsStatic && m.IsConstructor && func.Invoke(m));
		}

		public static MethodDefinition AddEmptyConstructor(this TypeDefinition typeDefinition, bool isStaticConstructor = false)
		{
			return isStaticConstructor
				? typeDefinition.AddMethod(
					".cctor",
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName | MethodAttributes.Static,
					typeDefinition.Module!.CorLibTypeFactory.Void)
				: typeDefinition.AddMethod(
					".ctor",
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
					typeDefinition.Module!.CorLibTypeFactory.Void);
		}

		/// <summary>
		/// Warning: base class must also have a default constructor
		/// </summary>
		public static MethodDefinition AddDefaultConstructor(this TypeDefinition typeDefinition, CachedReferenceImporter importer)
		{
			MethodDefinition defaultConstructor = typeDefinition.AddEmptyConstructor();
			CilInstructionCollection processor = defaultConstructor.CilMethodBody!.Instructions;
			
			IMethodDefOrRef baseConstructor;
			if (typeDefinition.BaseType is null)
			{
				baseConstructor = importer.ImportDefaultConstructor<object>();
			}
			else
			{
				if (typeDefinition.BaseType is TypeDefinition baseType)
				{
					baseConstructor = baseType.GetDefaultConstructor();
				}
				else
				{
					MethodDefinition baseConstructorDefinition = importer.LookupType(typeDefinition.BaseType.FullName)?.GetDefaultConstructor()
						?? throw new Exception($"Could not get default constructor for {typeDefinition.BaseType.FullName}");
					baseConstructor = importer.UnderlyingImporter.ImportMethod(baseConstructorDefinition);
				}
			}

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, baseConstructor);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			return defaultConstructor;
		}
	}
}