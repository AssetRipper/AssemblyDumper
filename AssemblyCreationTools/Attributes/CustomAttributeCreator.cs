namespace AssetRipper.AssemblyCreationTools.Attributes
{
	public static class CustomAttributeCreator
	{
		public static CustomAttribute AddCustomAttribute<T>(this IHasCustomAttribute _this, IMethodDefOrRef constructor, SzArrayTypeSignature paramType, T[] paramValue)
		{
			CustomAttribute attribute = IHasCustomAttributeExtensions.AddCustomAttribute(_this, constructor);
			attribute.AddFixedArgument(paramType, paramValue);
			return attribute;
		}

		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor, params (TypeSignature, object)[] parameters)
		{
			CustomAttribute attribute = IHasCustomAttributeExtensions.AddCustomAttribute(_this, constructor);
			for (int i = 0; i < parameters.Length; i++)
			{
				attribute.AddFixedArgument(parameters[i].Item1, parameters[i].Item2);
			}
			return attribute;
		}

		public static CustomAttributeArgument AddFixedArgument<T>(this CustomAttribute attribute, TypeSignature paramType, T[] paramValue)
		{
			CustomAttributeArgument argument = new CustomAttributeArgument(paramType, paramValue.Select(e => (object?)e));
			attribute.Signature!.FixedArguments.Add(argument);
			return argument;
		}

		public static CustomAttributeNamedArgument AddNamedArgument(this CustomAttribute attribute, TypeSignature memberType, string memberName, TypeSignature paramType, object paramValue, CustomAttributeArgumentMemberType memberKind)
		{
			CustomAttributeArgument argument = new CustomAttributeArgument(paramType, paramValue);
			CustomAttributeNamedArgument namedArgument = new CustomAttributeNamedArgument(memberKind, memberName, memberType, argument);
			attribute.Signature!.NamedArguments.Add(namedArgument);
			return namedArgument;
		}
	}
}
