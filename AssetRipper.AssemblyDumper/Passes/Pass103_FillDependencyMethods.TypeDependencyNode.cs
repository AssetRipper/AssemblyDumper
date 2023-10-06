using AssetRipper.AssemblyCreationTools.Fields;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class TypeDependencyNode : DependencyNode
	{
		public TypeDependencyNode(GeneratedClassInstance classInstance, DependencyNode? parent = null) : base(parent)
		{
			ClassInstance = classInstance;
			List<FieldDependencyNode> children = new();
			foreach (ClassProperty property in classInstance.Properties.Where(p => p.BackingField is not null))
			{
				FieldDependencyNode child = new(property, this);
				if (child.AnyPPtrs)
				{
					children.Add(child);
				}
			}
			Children = children.Count > 0 ? children : Array.Empty<FieldDependencyNode>();
		}

		public GeneratedClassInstance ClassInstance { get; }

		public override IReadOnlyList<FieldDependencyNode> Children { get; }

		public override string PathContent => "";

		public override char StateFieldTypeCharacter => 'T';

		public override TypeSignature TypeSignature => ClassInstance.Type.ToTypeSignature();

		public void ApplyAsRoot(DependencyMethodContext context)
		{
			if (Parent is not null)
			{
				throw new InvalidOperationException("This node is not a root node");
			}
			Apply(context, new ParentContext()
			{
				EmitLoad = c =>
				{
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, c.ThisField);
				},
				EmitIncrementStateAndGotoNextCase = c =>
				{
					//This is the root node, so it has no parent to increment the state of.
					c.Processor.Add(CilOpCodes.Ldc_I4_0);
					c.Processor.Add(CilOpCodes.Ret);
				},
				EmitIncrementStateAndReturnTrue = c =>
				{
					c.EmitReturnTrue();
				},
			});
		}

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			if (Children.Count == 1)
			{
				FieldDependencyNode child = Children[0];
				child.Apply(context, new ParentContext()
				{
					EmitLoad = c =>
					{
						parentContext.EmitLoad(c);
						c.Processor.Add(CilOpCodes.Ldfld, child.Field);
					},
					EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
					EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
				});
				return;
			}

			FieldDefinition stateField = context.Type.AddField(context.CorLibTypeFactory.Int32, StateFieldName, visibility: FieldVisibility.Private);
			CilInstructionLabel[] cases = new CilInstructionLabel[Children.Count];
			for (int i = 0; i < cases.Length; i++)
			{
				cases[i] = new();
			}
			CilInstructionLabel defaultCase = new();
			CilInstructionLabel endLabel = new();

			context.Processor.Add(CilOpCodes.Ldarg_0);
			context.Processor.Add(CilOpCodes.Ldfld, stateField);
			context.Processor.Add(CilOpCodes.Switch, cases);
			context.Processor.Add(CilOpCodes.Br, defaultCase);
			for (int i = 0; i < cases.Length; i++)
			{
				FieldDependencyNode child = Children[i];
				cases[i].Instruction = context.Processor.Add(CilOpCodes.Nop);
				Children[i].Apply(context, new ParentContext()
				{
					EmitLoad = c =>
					{
						parentContext.EmitLoad(c);
						c.Processor.Add(CilOpCodes.Ldfld, child.Field);
					},
					EmitIncrementStateAndGotoNextCase = c =>
					{
						c.Processor.Add(CilOpCodes.Ldarg_0);
						c.Processor.Add(CilOpCodes.Ldc_I4, i + 1);
						c.Processor.Add(CilOpCodes.Stfld, stateField);
						if (i + 1 < cases.Length)
						{
							c.Processor.Add(CilOpCodes.Br, cases[i + 1]);
						}
						else
						{
							c.Processor.Add(CilOpCodes.Br, defaultCase);
						}
					},
					EmitIncrementStateAndReturnTrue = c =>
					{
						c.Processor.Add(CilOpCodes.Ldarg_0);
						c.Processor.Add(CilOpCodes.Ldc_I4, i + 1);
						c.Processor.Add(CilOpCodes.Stfld, stateField);
						if (i + 1 < cases.Length)
						{
							c.EmitReturnTrue();
						}
						else
						{
							parentContext.EmitIncrementStateAndReturnTrue(c);
						}
					},
				});
				context.Processor.Add(CilOpCodes.Br, endLabel);
			}
			defaultCase.Instruction = context.Processor.Add(CilOpCodes.Nop);
			parentContext.EmitIncrementStateAndGotoNextCase(context);
			endLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
		}
	}
}
