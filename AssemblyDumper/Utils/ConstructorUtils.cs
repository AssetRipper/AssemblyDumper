using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class ConstructorUtils
	{
		/// <summary>
		/// Gets the default constructor for a type reference. Throws an exception if one doesn't exist.
		/// </summary>
		public static MethodDefinition GetDefaultConstructor(this TypeDefinition _this) => _this.GetConstructor(0);

		/// <summary>
		/// Gets the constructor with that number of parameters. Throws an exception if there's not exactly one.
		/// </summary>
		public static MethodDefinition GetConstructor(this TypeDefinition _this, int numParameters)
		{
			return _this.Methods.Where(x => x.IsConstructor && x.Parameters.Count == numParameters && !x.IsStatic).Single();
		}

		/// <summary>
		/// Warning: base class must also have a default constructor
		/// </summary>
		public static MethodDefinition AddDefaultConstructor(TypeDefinition typeDefinition)
		{
			var module = typeDefinition.Module;
			
			var defaultConstructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
				MethodSignature.CreateInstance(SystemTypeGetter.Void)
			);

			defaultConstructor.CilMethodBody = new(defaultConstructor);
			var processor = defaultConstructor.CilMethodBody.Instructions;

			IMethodDefOrRef baseConstructor;
			if (typeDefinition.BaseType == null)
			{
				baseConstructor = module.ImportSystemDefaultConstructor("System.Object");
			}
			else
			{
				if (typeDefinition.BaseType is TypeDefinition baseType)
				{
					baseConstructor = baseType.GetDefaultConstructor();
				}
				else if (typeDefinition.BaseType.Namespace.ToString().StartsWith("AssetRipper"))
				{
					baseConstructor = module.ImportCommonConstructor(typeDefinition.BaseType.FullName);
				}
				else
				{
					baseConstructor = module.ImportSystemDefaultConstructor(typeDefinition.BaseType.FullName);
				}
			}

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, baseConstructor);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			typeDefinition.Methods.Add(defaultConstructor);
			return defaultConstructor;
		}

		/// <summary>
		/// Get the constructors for a type
		/// </summary>
		/// <param name="type">The definition for the type</param>
		/// <returns>All the instance constructors</returns>
		public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition type)
		{
			return type.Methods.Where(m => m.IsConstructor && !m.IsStatic);
		}
	}
}