using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System.Linq;
using AssetRipper.Core.Interfaces;

namespace AssemblyDumper.Passes
{
	public static class Pass30_ImplementHasNameInterface
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot | 
			MethodAttributes.Virtual;
		const string FieldName = "m_Name";

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 30: Implement the Has Name Interface");
			ITypeDefOrRef hasName = SharedState.Importer.ImportCommonType<IHasName>();
			foreach(TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				if (type.HasNameField())
				{
					type.Interfaces.Add(new InterfaceImplementation(hasName));
					
					PropertyDefinition property = type.AddFullProperty("Name", InterfacePropertyImplementationAttributes, SystemTypeGetter.String);
					property.FillGetter();
					property.FillSetter();
				}
			}
		}

		private static MethodDefinition FillGetter(this PropertyDefinition property)
		{
			MethodDefinition getter = property.GetMethod;
			
			var processor = getter.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, property.DeclaringType.GetNameField());
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return getter;
		}

		private static MethodDefinition FillSetter(this PropertyDefinition property)
		{
			MethodDefinition setter = property.SetMethod;
			
			var processor = setter.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1); //value
			processor.Add(CilOpCodes.Stfld, property.DeclaringType.GetNameField());
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return setter;
		}

		private static FieldDefinition GetNameField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == FieldName);
		}

		private static bool HasNameField(this TypeDefinition type)
		{
			return type.Fields.Any(field => field.Name == FieldName);
		}
	}
}
