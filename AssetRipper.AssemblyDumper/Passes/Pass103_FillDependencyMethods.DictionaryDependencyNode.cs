using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyDumper.AST;
using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private static class DictionaryDependencyNodeHelper
	{
		public static void Apply(DictionaryNode node, DependencyMethodContext context, ParentContext parentContext)
		{
			FieldDefinition stateField = context.Type.AddField(context.CorLibTypeFactory.Int32, NodeHelper.GetStateFieldName(node), visibility: FieldVisibility.Private);

			CilInstructionLabel gotoNextCaseLabel = new();
			CilInstructionLabel endLabel = new();

			//Store list in a local variable
			CilLocalVariable local = context.Processor.AddLocalVariable(node.TypeSignature);
			parentContext.EmitLoad(context);
			context.Processor.Add(CilOpCodes.Stloc, local);

			context.Processor.Add(CilOpCodes.Ldarg_0);
			context.Processor.Add(CilOpCodes.Ldfld, stateField);
			context.Processor.Add(CilOpCodes.Ldloc, local);
			context.Processor.Add(CilOpCodes.Callvirt, GetAssetDictionaryCountMethod(node));
			context.Processor.Add(CilOpCodes.Bge, gotoNextCaseLabel);

			NodeHelper.Apply(node.Child, context, new ParentContext()
			{
				EmitLoad = c =>
				{
					context.Processor.Add(CilOpCodes.Ldloc, local);
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, stateField);
					context.Processor.Add(CilOpCodes.Callvirt, GetAssetDictionaryGetPairMethod(node));
				},
				EmitIncrementStateAndGotoNextCase = c =>
				{
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, stateField);
					c.Processor.Add(CilOpCodes.Ldc_I4_1);
					c.Processor.Add(CilOpCodes.Add);
					c.Processor.Add(CilOpCodes.Stfld, stateField);
					parentContext.EmitIncrementStateAndGotoNextCase(c);
				},
				EmitIncrementStateAndReturnTrue = c =>
				{
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, stateField);
					c.Processor.Add(CilOpCodes.Ldc_I4_1);
					c.Processor.Add(CilOpCodes.Add);
					c.Processor.Add(CilOpCodes.Stfld, stateField);
					c.EmitReturnTrue();
				},
			});
			context.Processor.Add(CilOpCodes.Br, endLabel);

			gotoNextCaseLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
			parentContext.EmitIncrementStateAndGotoNextCase(context);

			endLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
		}

		private static IMethodDefOrRef GetAssetDictionaryCountMethod(DictionaryNode node)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == $"get_{nameof(AssetDictionary<int, int>.Count)}");
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, node.TypeSignature, method);
		}

		private static IMethodDefOrRef GetAssetDictionaryGetPairMethod(DictionaryNode node)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.GetPair));
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, node.TypeSignature, method);
		}
	}
}
