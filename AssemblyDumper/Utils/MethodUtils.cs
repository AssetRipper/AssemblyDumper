using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class MethodUtils
	{
		public static MemberReference MakeConstructorOnGenericType(GenericInstanceTypeSignature instanceType, int paramCount)
		{
			//Get ctor
			var constructorDefinition = instanceType.GenericType.Resolve().Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == paramCount);

			return new MemberReference(instanceType.ToTypeDefOrRef(), ".ctor", constructorDefinition.Signature);
		}

		public static MemberReference MakeMethodOnGenericType(GenericInstanceTypeSignature instanceType, MethodDefinition definition)
		{
			return new MemberReference(instanceType.ToTypeDefOrRef(), definition.Name, definition.Signature);
		}

		public static MethodSpecification MakeGenericInstanceMethod(IMethodDefOrRef method, params TypeSignature[] typeArguments)
		{
			return MakeGenericInstanceMethod(method, new GenericInstanceMethodSignature(typeArguments));
		}

		private static MethodSpecification MakeGenericInstanceMethod(IMethodDefOrRef method, GenericInstanceMethodSignature instanceMethodSignature)
		{
			return SharedState.Importer.ImportMethod(new MethodSpecification(method, instanceMethodSignature));
		}
	}
}
