using AsmResolver.DotNet.Signatures;

namespace AssemblyDumper.Utils
{
	internal static class PropertyCreator
	{
		public static PropertyDefinition AddFullProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			PropertyDefinition property = type.AddEmptyProperty(propertyName, methodAttributes, returnTypeSignature, propertyAttributes);
			property.AddGetMethod(propertyName, methodAttributes, returnTypeSignature);
			property.AddSetMethod(propertyName, methodAttributes, returnTypeSignature);
			return property;
		}

		public static PropertyDefinition ImplementFullProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, FieldDefinition field, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			TypeSignature returnType = returnTypeSignature ?? field.Signature.FieldType;
			PropertyDefinition property = type.AddFullProperty(propertyName, methodAttributes, returnType, propertyAttributes);
			property.FillGetter(field, returnType);
			property.FillSetter(field);
			return property;
		}

		public static PropertyDefinition AddGetterProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			PropertyDefinition property = type.AddEmptyProperty(propertyName, methodAttributes, returnTypeSignature, propertyAttributes);
			property.AddGetMethod(propertyName, methodAttributes, returnTypeSignature);
			return property;
		}

		public static PropertyDefinition ImplementGetterProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, FieldDefinition field, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			TypeSignature returnType = returnTypeSignature ?? field.Signature.FieldType;
			PropertyDefinition property = type.AddGetterProperty(propertyName, methodAttributes, returnType, propertyAttributes);
			property.FillGetter(field, returnType);
			return property;
		}

		private static PropertyDefinition AddEmptyProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
			PropertySignature methodSignature =
				isStatic ?
				PropertySignature.CreateStatic(returnTypeSignature) :
				PropertySignature.CreateInstance(returnTypeSignature);

			PropertyDefinition property = new PropertyDefinition(propertyName, propertyAttributes, methodSignature);
			type.Properties.Add(property);

			return property;
		}

		private static MethodDefinition AddGetMethod(this PropertyDefinition property, string propertyName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			if(property.GetMethod != null)
				throw new ArgumentException("Property already has a get method",nameof(property));
			MethodDefinition getter = property.DeclaringType.AddMethod($"get_{propertyName}", methodAttributes, returnType);
			property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
			return getter;
		}

		private static MethodDefinition AddSetMethod(this PropertyDefinition property, string propertyName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			if (property.SetMethod != null)
				throw new ArgumentException("Property already has a set method", nameof(property));
			MethodDefinition setter = property.DeclaringType.AddMethod($"set_{propertyName}", methodAttributes, SystemTypeGetter.Void);
			setter.AddParameter("value", returnType);
			property.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
			return setter;
		}

		private static MethodDefinition FillGetter(this PropertyDefinition property, FieldDefinition field, TypeSignature returnType = null)
		{
			MethodDefinition getter = property.GetMethod;

			var processor = getter.CilMethodBody.Instructions;
			if (field != null)
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
			}
			else if (returnType != null)
			{
				processor.AddDefaultValue(returnType);
			}
			else
			{
				throw new System.Exception($"{nameof(field)} and {nameof(returnType)} cannot both be null");
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return getter;
		}

		private static MethodDefinition FillSetter(this PropertyDefinition property, FieldDefinition field)
		{
			MethodDefinition setter = property.SetMethod;

			var processor = setter.CilMethodBody.Instructions;
			if (field != null)
			{
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldarg_1); //value
				processor.Add(CilOpCodes.Stfld, field);
			}
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return setter;
		}
	}
}
