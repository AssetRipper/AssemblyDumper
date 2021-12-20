using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AssemblyDumper.Utils
{
	internal static class PropertyCreator
	{
		public static PropertyDefinition AddFullProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, ITypeDefOrRef returnType, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			return type.AddFullProperty(propertyName, methodAttributes, returnType.ToTypeSignature(), propertyAttributes);
		}

		public static PropertyDefinition AddFullProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			PropertyDefinition property = type.AddEmptyProperty(propertyName, methodAttributes, returnTypeSignature, propertyAttributes);

			MethodDefinition getter = type.ImplementGetter(propertyName, methodAttributes, returnTypeSignature);
			MethodDefinition setter = type.ImplementSetter(propertyName, methodAttributes, returnTypeSignature);

			property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
			property.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));

			return property;
		}

		public static PropertyDefinition AddGetterProperty(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnTypeSignature, PropertyAttributes propertyAttributes = PropertyAttributes.None)
		{
			PropertyDefinition property = type.AddEmptyProperty(propertyName, methodAttributes, returnTypeSignature, propertyAttributes);

			MethodDefinition getter = type.ImplementGetter(propertyName, methodAttributes, returnTypeSignature);

			property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));

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

		private static MethodDefinition ImplementGetter(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			MethodDefinition getter = type.AddMethod($"get_{propertyName}", methodAttributes, returnType);
			return getter;
		}

		private static MethodDefinition ImplementSetter(this TypeDefinition type, string propertyName, MethodAttributes methodAttributes, TypeSignature returnType)
		{
			MethodDefinition setter = type.AddMethod($"set_{propertyName}", methodAttributes, SystemTypeGetter.Void);
			setter.AddParameter("value", returnType);
			return setter;
		}
	}
}
