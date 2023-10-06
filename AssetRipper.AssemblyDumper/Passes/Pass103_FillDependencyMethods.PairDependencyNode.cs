using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class PairDependencyNode : DependencyNode
	{
		public PairDependencyNode(GenericInstanceTypeSignature typeSignature, DependencyNode? parent = null) : base(parent)
		{
			TypeSignature = typeSignature;
			Key = new(typeSignature.TypeArguments[0], this);
			Value = new(typeSignature.TypeArguments[1], this);
		}

		public KeyDependencyNode Key { get; }
		public ValueDependencyNode Value { get; }

		public override string PathContent => "";

		public override char StateFieldTypeCharacter => 'P';

		public override IEnumerable<DependencyNode> Children
		{
			get
			{
				yield return Key;
				yield return Value;
			}
		}

		public override bool AnyPPtrs => Key.AnyPPtrs || Value.AnyPPtrs;

		public override TypeSignature TypeSignature { get; }

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			if (Key.AnyPPtrs)
			{
				if (Value.AnyPPtrs)
				{
					FieldDefinition stateField = context.Type.AddField(context.CorLibTypeFactory.Boolean, StateFieldName, visibility: FieldVisibility.Private);
					CilInstructionLabel valueLabel = new();
					CilInstructionLabel endLabel = new();

					context.Processor.Add(CilOpCodes.Ldarg_0);
					context.Processor.Add(CilOpCodes.Ldfld, stateField);
					context.Processor.Add(CilOpCodes.Brtrue, valueLabel);

					Key.Apply(context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetKeyAccessor(Key.TypeSignature, Value.TypeSignature));
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
					Value.Apply(context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetValueAccessor(Key.TypeSignature, Value.TypeSignature));
						},
						EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
						EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
					});

					endLabel.Instruction = context.Processor.Add(CilOpCodes.Nop);
				}
				else
				{
					Key.Apply(context, new ParentContext()
					{
						EmitLoad = c =>
						{
							parentContext.EmitLoad(c);
							c.Processor.Add(CilOpCodes.Callvirt, GetKeyAccessor(Key.TypeSignature, Value.TypeSignature));
						},
						EmitIncrementStateAndGotoNextCase = parentContext.EmitIncrementStateAndGotoNextCase,
						EmitIncrementStateAndReturnTrue = parentContext.EmitIncrementStateAndReturnTrue,
					});
				}
			}
			else if (Value.AnyPPtrs)
			{
				Value.Apply(context, new ParentContext()
				{
					EmitLoad = c =>
					{
						parentContext.EmitLoad(c);
						c.Processor.Add(CilOpCodes.Callvirt, GetValueAccessor(Key.TypeSignature, Value.TypeSignature));
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
