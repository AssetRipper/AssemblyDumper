using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDumper.Utils;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass54_FillDependencyMethods
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 54: Fill Fetch Dependency Methods");

			ITypeDefOrRef commonPPtrTypeRef = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			ITypeDefOrRef unityObjectBaseInterfaceRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			GenericInstanceTypeSignature unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef.ToTypeSignature());
			IMethodDefOrRef emptyArray = SharedState.Module.ImportSystemMethod<System.Array>(method => method.Name == "Empty");

			MethodSpecification emptyArrayMethod = MethodUtils.MakeGenericInstanceMethod(emptyArray, unityObjectBasePPtrRef);

			foreach (TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				var method = type.GetDependencyMethod();
				CilInstructionCollection processor = method.CilMethodBody.Instructions;
				processor.Add(CilOpCodes.Call, emptyArrayMethod);
				processor.Add(CilOpCodes.Ret);
			}
		}

		private static MethodDefinition GetDependencyMethod(this TypeDefinition type)
		{
			return type.Methods.Single(x => x.Name == "FetchDependencies");
		}
	}
}
