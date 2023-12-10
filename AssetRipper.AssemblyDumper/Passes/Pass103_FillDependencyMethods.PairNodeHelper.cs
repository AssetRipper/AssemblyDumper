using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyDumper.AST;
using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private static class PairNodeHelper
	{
		public static void Apply(PairNode node, DependencyMethodContext context, ParentContext parentContext)
		{
			if (node.Key.AnyPPtrs)
			{
				if (node.Value.AnyPPtrs)
				{
					FieldDefinition stateField = context.Type.AddField(context.CorLibTypeFactory.Boolean, NodeHelper.GetStateFieldName(node), visibility: FieldVisibility.Private);
					CilInstructionLabel valueLabel = new();
					CilInstructionLabel endLabel = new();

					context.Processor.Add(CilOpCodes.Ldarg_0);
					context.Processor.Add(CilOpCodes.Ldfld, stateField);
					context.Processor.Add(CilOpCodes.Brtrue, valueLabel);

					NodeHelper.Apply(node.Key, context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetKeyAccessor(node.Key.TypeSignature, node.Value.TypeSignature));
						},
						EmitIncrementStateAndGotoNextCase = c =>
						{
							c.Processor.Add(CilOpCodes.Ldarg_0);
							c.Processor.Add(CilOpCodes.Ldc_I4_1);
							c.Processor.Add(CilOpCodes.Stfld, stateField);
							c.Processor.Add(CilOpCodes.Br, valueLabel);
						},
						EmitIncrementStateAndReturnTrue = c =>
						{
							c.Processor.Add(CilOpCodes.Ldarg_0);
							c.Processor.Add(CilOpCodes.Ldc_I4_1);
							c.Processor.Add(CilOpCodes.Stfld, stateField);
							c.EmitReturnTrue();
						},
					});

					valueLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
					NodeHelper.Apply(node.Value, context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetValueAccessor(node.Key.TypeSignature, node.Value.TypeSignature));
						},
						EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
						EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
					});

					endLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
				}
				else
				{
					NodeHelper.Apply(node.Key, context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetKeyAccessor(node.Key.TypeSignature, node.Value.TypeSignature));
						},
						EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
						EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
					});
				}
			}
			else if (node.Value.AnyPPtrs)
			{
				NodeHelper.Apply(node.Value, context, new ParentContext()
				{
					EmitLoad = c =>
					{
						parentContext.EmitLoad(c);
						c.Processor.Add(CilOpCodes.Callvirt, GetValueAccessor(node.Key.TypeSignature, node.Value.TypeSignature));
					},
					EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
					EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
				});
			}
			else
			{
				throw new InvalidOperationException("Neither Key nor Value have any PPtrs");
			}
		}

		private static IMethodDefOrRef GetKeyAccessor(TypeSignature keySignature, TypeSignature valueSignature)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetPair<,>), m => m.Name == $"get_{nameof(AssetPair<int, int>.Key)}");
			GenericInstanceTypeSignature assetPairTypeSignature = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>)).MakeGenericInstanceType(keySignature, valueSignature);
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetPairTypeSignature, method);
		}

		private static IMethodDefOrRef GetValueAccessor(TypeSignature keySignature, TypeSignature valueSignature)
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetPair<,>), m => m.Name == $"get_{nameof(AssetPair<int, int>.Value)}");
			GenericInstanceTypeSignature assetPairTypeSignature = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>)).MakeGenericInstanceType(keySignature, valueSignature);
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, assetPairTypeSignature, method);
		}
	}
}
