using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			//Make the constructor on the generic type
			//Cecil sucks.
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
	}
}
