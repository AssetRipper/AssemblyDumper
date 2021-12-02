using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass54_FillDependencyMethods
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 54: Fill Fetch Dependency Methods");

			TypeReference commonPPtrTypeRef = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			TypeReference unityObjectBaseInterfaceRef = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			GenericInstanceType unityObjectBasePPtrRef = commonPPtrTypeRef.MakeGenericInstanceType(unityObjectBaseInterfaceRef);
			MethodReference emptyArray = SharedState.Module.ImportSystemMethod<System.Array>(method => method.Name == "Empty");
			var emptyArrayMethod = new GenericInstanceMethod(emptyArray);
			emptyArrayMethod.GenericArguments.Add(unityObjectBasePPtrRef);

			foreach (TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				var method = type.GetDependencyMethod();
				ILProcessor processor = method.Body.GetILProcessor();
				processor.Emit(OpCodes.Call, emptyArrayMethod);
				processor.Emit(OpCodes.Ret);
			}
		}

		private static MethodDefinition GetDependencyMethod(this TypeDefinition type)
		{
			return type.Methods.Single(x => x.Name == "FetchDependencies");
		}
	}
}
