using Mono.Cecil;
using System;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class MethodUtils
	{
		public static MethodReference MakeConstructorOnGenericType(GenericInstanceType instanceType, int paramCount)
		{
			//Get ctor
			var ctor = instanceType.Resolve().Methods.First(m => m.Name == ".ctor" && m.Parameters.Count == paramCount);

			return MakeMethodOnGenericType(ctor, instanceType);
		}

		public static MethodReference MakeMethodOnGenericType(MethodDefinition definition, GenericInstanceType instanceType)
		{
			//https://www.py4u.net/discuss/1996785
			var genericMethod = new MethodReference(definition.Name, definition.ReturnType)
			{
				DeclaringType = instanceType,
				HasThis = definition.HasThis,
				ExplicitThis = definition.ExplicitThis,
				CallingConvention = definition.CallingConvention,
			};
			definition.Parameters.ToList().ForEach(genericMethod.Parameters.Add);
			definition.GenericParameters.ToList().ForEach(genericMethod.GenericParameters.Add);
			return genericMethod;
		}

		public static MethodReference MakeMethodReferenceOnGenericType(MethodReference self, params TypeReference[] arguments)
		{
			var reference = new MethodReference(self.Name, self.ReturnType)
			{
				DeclaringType = self.DeclaringType.MakeGenericType(arguments),
				HasThis = self.HasThis,
				ExplicitThis = self.ExplicitThis,
				CallingConvention = self.CallingConvention,
			};

			foreach (var parameter in self.Parameters)
				reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

			foreach (var generic_parameter in self.GenericParameters)
				reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

			return reference;
		}

		private static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
		{
			if (self.GenericParameters.Count != arguments.Length)
				throw new ArgumentException();

			var instance = new GenericInstanceType(self);
			foreach (var argument in arguments)
				instance.GenericArguments.Add(argument);

			return instance;
		}
	}
}
