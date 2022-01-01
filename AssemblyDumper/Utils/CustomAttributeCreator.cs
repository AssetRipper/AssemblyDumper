using AsmResolver.DotNet.Signatures;

namespace AssemblyDumper.Utils
{
	public static class CustomAttributeCreator
	{
		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor)
		{
			if (constructor is ICustomAttributeType usableConstructor)
			{
				var attributeSignature = new CustomAttributeSignature();
				var attrDef = new CustomAttribute(usableConstructor, attributeSignature);
				_this.CustomAttributes.Add(attrDef);
				return attrDef;
			}
			else
			{
				throw new ArgumentException("Constructor is not ICustomAttributeType", nameof(constructor));
			}
		}

		public static CustomAttribute AddCustomAttribute(this IHasCustomAttribute _this, IMethodDefOrRef constructor, TypeSignature param1Type, object param1Value)
		{
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			attribute.AddFixedArgument(param1Type, param1Value);
			return attribute;
		}

		public static CustomAttributeArgument AddFixedArgument(this CustomAttribute attribute, TypeSignature paramType, object paramValue)
		{
			var argument = new CustomAttributeArgument(paramType, paramValue);
			attribute.Signature.FixedArguments.Add(argument);
			return argument;
		}
	}
}
