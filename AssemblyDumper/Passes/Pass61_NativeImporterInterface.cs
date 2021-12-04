using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

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
			TypeReference nativeImporterInterface = SharedState.Module.ImportCommonType<AssetRipper.Core.Classes.Meta.Importers.INativeFormatImporter>();
			if (SharedState.TypeDictionary.TryGetValue("NativeFormatImporter", out TypeDefinition type))
			{
				type.Interfaces.Add(new InterfaceImplementation(nativeImporterInterface));
				var getter = type.ImplementGetter();
				var setter = type.ImplementSetter();
				PropertyDefinition property = new PropertyDefinition(PropertyName, PropertyAttributes.None, SystemTypeGetter.Int64);
				property.GetMethod = getter;
				property.SetMethod = setter;
				type.Properties.Add(property);
			}
			else
			{
				throw new Exception("NativeFormatImporter not found");
			}
		}

		private static MethodDefinition ImplementGetter(this TypeDefinition type)
		{
			MethodDefinition getter = new MethodDefinition(GetterName, InterfacePropertyImplementationAttributes, SystemTypeGetter.Int64);
			type.Methods.Add(getter);
			ILProcessor processor = getter.Body.GetILProcessor();
			if (type.HasField())
			{
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Ldfld, type.GetField());
			}
			else
			{
				processor.EmitDefaultValue(SystemTypeGetter.Int64);
			}
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
			return getter;
		}

		private static MethodDefinition ImplementSetter(this TypeDefinition type)
		{
			MethodDefinition setter = new MethodDefinition(SetterName, InterfacePropertyImplementationAttributes, SystemTypeGetter.Void);
			type.Methods.Add(setter);
			ParameterDefinition value = new ParameterDefinition("value", ParameterAttributes.None, SystemTypeGetter.Int64);
			setter.Parameters.Add(value);
			ILProcessor processor = setter.Body.GetILProcessor();
			if (type.HasField())
			{
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Ldarg, value);
				processor.Emit(OpCodes.Stfld, type.GetField());
			}
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
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
