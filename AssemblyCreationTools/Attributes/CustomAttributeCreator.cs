namespace AssetRipper.AssemblyCreationTools.Attributes
{
	public static class CustomAttributeCreator
	{
		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor)
		{
			if (constructor is ICustomAttributeType usableConstructor)
			{
				CustomAttributeSignature attributeSignature = new CustomAttributeSignature();
				CustomAttribute attrDef = new CustomAttribute(usableConstructor, attributeSignature);
				_this.CustomAttributes.Add(attrDef);
				return attrDef;
			}
			else
			{
				throw new ArgumentException("Constructor is not ICustomAttributeType", nameof(constructor));
			}
		}

		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor, TypeSignature paramType, object paramValue)
		{
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			attribute.AddFixedArgument(paramType, paramValue);
			return attribute;
		}

		public static CustomAttribute AddCustomAttribute<T>(this IHasCustomAttribute _this, IMethodDefOrRef constructor, SzArrayTypeSignature paramType, T[] paramValue)
		{
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			attribute.AddFixedArgument(paramType, paramValue);
			return attribute;
		}

		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor, params (TypeSignature, object)[] parameters)
		{
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			for(int i = 0; i < parameters.Length; i++)
			{
				attribute.AddFixedArgument(parameters[i].Item1, parameters[i].Item2);
			}
			return attribute;
		}

		public static CustomAttributeArgument AddFixedArgument(this CustomAttribute attribute, TypeSignature paramType, object paramValue)
		{
			CustomAttributeArgument argument = new CustomAttributeArgument(paramType, paramValue);
			attribute.Signature!.FixedArguments.Add(argument);
			return argument;
		}

		public static CustomAttributeArgument AddFixedArgument<T>(this CustomAttribute attribute, TypeSignature paramType, T[] paramValue)
		{
			CustomAttributeArgument argument = new CustomAttributeArgument(paramType, paramValue.Select(e => (object?)e));
			attribute.Signature!.FixedArguments.Add(argument);
			return argument;
		}

		public static CustomAttributeNamedArgument AddNamedArgument(this CustomAttribute attribute, TypeSignature memberType, string memberName, TypeSignature paramType, object paramValue, bool isProperty = true)
		{
			CustomAttributeArgument argument = new CustomAttributeArgument(paramType, paramValue);
			CustomAttributeArgumentMemberType memberTypeEnum = isProperty ? CustomAttributeArgumentMemberType.Property : CustomAttributeArgumentMemberType.Field;
			CustomAttributeNamedArgument namedArgument = new CustomAttributeNamedArgument(memberTypeEnum, memberName, memberType, argument);
			attribute.Signature!.NamedArguments.Add(namedArgument);
			return namedArgument;
		}
	}
}
