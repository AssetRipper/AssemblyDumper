﻿using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyDumper.AST;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private static class DictionaryNodeHelper
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
			context.Processor.Add(CilOpCodes.Callvirt, node.GetCount);
			context.Processor.Add(CilOpCodes.Bge, gotoNextCaseLabel);

			NodeHelper.Apply(node.Child, context, new ParentContext()
			{
				EmitLoad = c =>
				{
					context.Processor.Add(CilOpCodes.Ldloc, local);
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, stateField);
					context.Processor.Add(CilOpCodes.Callvirt, node.GetPair);
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
	}
}
