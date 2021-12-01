using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;

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
			Logger.Info("Pass 30: Implement the Has Name Interface");
			TypeReference hasName = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IHasName>();
			foreach(TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				if (type.HasNameField())
				{
					type.Interfaces.Add(new InterfaceImplementation(hasName));
					var getter = type.ImplementGetter();
					var setter = type.ImplementSetter();
					PropertyDefinition property = new PropertyDefinition("Name", PropertyAttributes.None, SystemTypeGetter.String);
					property.GetMethod = getter;
					property.SetMethod = setter;
					type.Properties.Add(property);
				}
			}
		}

		private static MethodDefinition ImplementGetter(this TypeDefinition type)
		{
			MethodDefinition getter = new MethodDefinition("get_Name", InterfacePropertyImplementationAttributes, SystemTypeGetter.String);
			type.Methods.Add(getter);
			ILProcessor processor = getter.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, type.GetNameField());
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
			return getter;
		}

		private static MethodDefinition ImplementSetter(this TypeDefinition type)
		{
			MethodDefinition setter = new MethodDefinition("set_Name", InterfacePropertyImplementationAttributes, SystemTypeGetter.Void);
			type.Methods.Add(setter);
			ParameterDefinition value = new ParameterDefinition("value", ParameterAttributes.None, SystemTypeGetter.String);
			setter.Parameters.Add(value);
			ILProcessor processor = setter.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldarg, value);
			processor.Emit(OpCodes.Stfld, type.GetNameField());
			processor.Emit(OpCodes.Ret);
			processor.Body.Optimize();
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
