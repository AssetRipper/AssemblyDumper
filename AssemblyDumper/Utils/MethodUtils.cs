using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Utils
{
	//Still learning about handling generics with Asm
	public static class MethodUtils
	{
		public static MethodSpecification MakeConstructorOnGenericType(GenericInstanceTypeSignature instanceType, int paramCount)
		{
			//Get ctor
			var ctor = instanceType.GenericType.Resolve().Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == paramCount);

			return MakeMethodOnGenericType(ctor, instanceType);
		}

		public static MethodSpecification MakeGenericInstanceMethod(IMethodDefOrRef method, params TypeSignature[] typeArguments)
		{
			return new MethodSpecification(method, new GenericInstanceMethodSignature(typeArguments));
		}

		public static MethodSpecification MakeGenericInstanceMethod(IMethodDefOrRef method, IEnumerable<TypeSignature> typeArguments)
		{
			return new MethodSpecification(method, new GenericInstanceMethodSignature(typeArguments));
		}

		public static MethodSpecification MakeMethodOnGenericType(MethodDefinition definition, GenericInstanceTypeSignature instanceType)
		{
			if(instanceType.TypeArguments.Count == 0)
			{
				throw new ArgumentException(nameof(instanceType));
			}
			return MakeGenericInstanceMethod(SharedState.Importer.ImportMethod(definition), instanceType.TypeArguments);
		}
	}
}
