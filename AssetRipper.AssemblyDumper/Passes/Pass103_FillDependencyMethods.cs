using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyDumper.AST;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;
using System.Collections;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	public static void DoPass()
	{
		TypeDefinition injectedBaseType = SharedState.Instance.InjectHelperType(typeof(FetchDependenciesEnumerableBase<>));
		FieldDefinition currentField = injectedBaseType.Fields.First(t => t.Name == "_current");
		FieldDefinition thisField = injectedBaseType.Fields.First(t => t.Name == "_this");

		ITypeDefOrRef commonPPtrTypeRef = SharedState.Instance.Importer.ImportType(typeof(PPtr));
		ITypeDefOrRef ienumerableRef = SharedState.Instance.Importer.ImportType(typeof(IEnumerable<>));
		ITypeDefOrRef valueTupleRef = SharedState.Instance.Importer.ImportType(typeof(ValueTuple<,>));
		GenericInstanceTypeSignature tupleGenericInstance = valueTupleRef.MakeGenericInstanceType(SharedState.Instance.Importer.String, commonPPtrTypeRef.ToTypeSignature());
		IMethodDescriptor emptyEnumerableMethod = SharedState.Instance.Importer.ImportMethod(typeof(Enumerable), m => m.Name == nameof(Enumerable.Empty))
			.MakeGenericInstanceMethod(tupleGenericInstance);
		GenericInstanceTypeSignature returnType = ienumerableRef.MakeGenericInstanceType(tupleGenericInstance);
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				TypeNode rootNode = new(instance);

				MethodDefinition method = instance.Type.AddMethod(nameof(UnityAssetBase.FetchDependencies), Pass063_CreateEmptyMethods.OverrideMethodAttributes, returnType);
				CilInstructionCollection processor = method.GetProcessor();
				if (rootNode.AnyPPtrs)
				{
					MethodDefinition enumerableConstructor = MakeEnumerableType(injectedBaseType, currentField, thisField, instance, rootNode);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Newobj, enumerableConstructor);
					processor.Add(CilOpCodes.Ret);
				}
				else
				{
					processor.Add(CilOpCodes.Call, emptyEnumerableMethod);
					processor.Add(CilOpCodes.Ret);
				}
			}
		}
	}

	private static MethodDefinition MakeEnumerableType(TypeDefinition injectedBaseType, FieldDefinition currentField, FieldDefinition thisField, GeneratedClassInstance instance, TypeNode rootNode)
	{
		ITypeDefOrRef baseType = injectedBaseType.MakeGenericInstanceType(instance.Type.ToTypeSignature()).ToTypeDefOrRef();
		TypeDefinition enumerableType = new(
			null,
			"FetchDependenciesEnumerable",
			TypeAttributes.NestedPrivate | TypeAttributes.Sealed,
			baseType);
		instance.Type.NestedTypes.Add(enumerableType);

		MethodDefinition enumerableConstructor = enumerableType.AddEmptyConstructor();
		enumerableConstructor.AddParameter(instance.Type.ToTypeSignature(), "_this");
		{
			CilInstructionCollection processor = enumerableConstructor.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			IMethodDefOrRef baseConstructor = new MemberReference(baseType, ".ctor", injectedBaseType.GetConstructor(1).Signature);
			processor.Add(CilOpCodes.Call, baseConstructor);
			processor.Add(CilOpCodes.Ret);
		}

		// CreateNew
		{
			MethodDefinition createNewMethod = enumerableType.AddMethod("CreateNew", MethodAttributes.FamilyAndAssembly | MethodAttributes.Virtual | MethodAttributes.HideBySig, enumerableType.ToTypeSignature());
			{
				CilInstructionCollection processor = createNewMethod.GetProcessor();
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, new MemberReference(baseType, thisField.Name, thisField.Signature));
				processor.Add(CilOpCodes.Newobj, enumerableConstructor);
				processor.Add(CilOpCodes.Ret);
			}
		}

		// MoveNext
		{
			MethodDefinition moveNextMethod = enumerableType.AddMethod(nameof(IEnumerator.MoveNext), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, SharedState.Instance.Importer.Boolean);
			DependencyMethodContext context = new()
			{
				Processor = moveNextMethod.GetProcessor(),
				Type = enumerableType,
				CurrentField = new MemberReference(baseType, currentField.Name, currentField.Signature),
				ThisField = new MemberReference(baseType, thisField.Name, thisField.Signature),
			};
			TypeNodeHelper.ApplyAsRoot(rootNode, context);
			context.Processor.OptimizeMacros();
		}

		return enumerableConstructor;
	}
}
