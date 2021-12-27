using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System;
using System.Linq;
using AssetRipper.Core.Classes.Meta.Importers;

namespace AssemblyDumper.Passes
{
	public static class Pass61_NativeImporterInterface
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;
		const string PropertyName = "MainObjectFileID";
		const string FieldName = "m_" + PropertyName;
		const string GetterName = "get_" + PropertyName;
		const string SetterName = "set_" + PropertyName;

		public static void DoPass()
		{
			Console.WriteLine("Pass 61: Implement Native Format Importer Interface");
			ITypeDefOrRef nativeImporterInterface = SharedState.Importer.ImportCommonType<INativeFormatImporter>();
			if (SharedState.TypeDictionary.TryGetValue("NativeFormatImporter", out TypeDefinition type))
			{
				type.Interfaces.Add(new InterfaceImplementation(nativeImporterInterface));
				PropertyDefinition property = type.AddFullProperty(PropertyName, InterfacePropertyImplementationAttributes, SystemTypeGetter.Int64);
				property.FillGetter();
				property.FillSetter();
			}
			else
			{
				throw new Exception("NativeFormatImporter not found");
			}
		}

		private static MethodDefinition FillGetter(this PropertyDefinition property)
		{
			MethodDefinition getter = property.GetMethod;
			
			CilInstructionCollection processor = getter.CilMethodBody.Instructions;
			if (property.DeclaringType.HasField())
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, property.DeclaringType.GetField());
			}
			else
			{
				processor.AddDefaultValue(SystemTypeGetter.Int64);
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return getter;
		}

		private static MethodDefinition FillSetter(this PropertyDefinition property)
		{
			MethodDefinition setter = property.SetMethod;
			
			CilInstructionCollection processor = setter.CilMethodBody.Instructions;
			if (property.DeclaringType.HasField())
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldarg_1); //value
				processor.Add(CilOpCodes.Stfld, property.DeclaringType.GetField());
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return setter;
		}

		private static FieldDefinition GetField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == FieldName);
		}

		private static bool HasField(this TypeDefinition type)
		{
			return type.Fields.Any(field => field.Name == FieldName);
		}
	}
}
