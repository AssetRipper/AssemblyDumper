using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class ConstructorUtils
	{
		/// <summary>
		/// Gets the default constructor for a type reference. Throws an exception if one doesn't exist.
		/// </summary>
		public static MethodDefinition GetDefaultConstructor(this TypeReference _this) => _this.GetConstructor(0);

		/// <summary>
		/// Gets the constructor with that number of parameters. Throws an exception if there's not exactly one.
		/// </summary>
		public static MethodDefinition GetConstructor(this TypeReference _this, int numParameters)
		{
			return _this.Resolve().Methods.Where(x => x.IsConstructor && x.Parameters.Count == numParameters && !x.IsStatic).Single();
		}

		/// <summary>
		/// Warning: base class must also have a default constructor
		/// </summary>
		public static MethodDefinition AddDefaultConstructor(TypeDefinition typeDefinition)
		{
			var module = typeDefinition.Module;
			var defaultConstructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				module.ImportReference(typeof(void))
			);

			var processor = defaultConstructor.Body.GetILProcessor();

			MethodReference baseConstructor;
			if (typeDefinition.BaseType == null)
			{
				baseConstructor = module.ImportSystemDefaultConstructor("System.Object");
			}
			else
			{
				if (typeDefinition.BaseType is TypeDefinition baseType)
				{
					baseConstructor = module.ImportReference(baseType.GetDefaultConstructor());
				}
				else if (typeDefinition.BaseType.Namespace.StartsWith("AssetRipper"))
				{
					baseConstructor = module.ImportCommonConstructor(typeDefinition.BaseType.FullName);
				}
				else
				{
					baseConstructor = module.ImportSystemDefaultConstructor(typeDefinition.BaseType.FullName);
				}
			}

			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseConstructor);

			processor.Emit(OpCodes.Ret);

			typeDefinition.Methods.Add(defaultConstructor);
			return defaultConstructor;
		}
	}
}