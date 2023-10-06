using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.Assets.Generics;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class DictionaryDependencyNode : SingleDependencyNode
	{
		public DictionaryDependencyNode(GenericInstanceTypeSignature typeSignature, DependencyNode? parent = null) : base(parent)
		{
			TypeSignature = typeSignature;
			GenericInstanceTypeSignature pairType = SharedState.Instance.Importer.ImportType(typeof(AssetPair<,>)).MakeGenericInstanceType(
				typeSignature.TypeArguments[0],
				typeSignature.TypeArguments[1]);
			Child = new PairDependencyNode(pairType, this);
		}

		public override string PathContent => "[]";

		public override char StateFieldTypeCharacter => 'D';

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			FieldDefinition stateField = context.Type.AddField(context.CorLibTypeFactory.Int32, StateFieldName, visibility: FieldVisibility.Private);

			CilInstructionLabel gotoNextCaseLabel = new();
			CilInstructionLabel endLabel = new();

			//Store list in a local variable
			CilLocalVariable local = context.Processor.AddLocalVariable(TypeSignature);
			parentContext.EmitLoad(context);
			context.Processor.Add(CilOpCodes.Stloc, local);

			context.Processor.Add(CilOpCodes.Ldarg_0);
			context.Processor.Add(CilOpCodes.Ldfld, stateField);
			context.Processor.Add(CilOpCodes.Ldloc, local);
			context.Processor.Add(CilOpCodes.Callvirt, GetAssetDictionaryCountMethod());
			context.Processor.Add(CilOpCodes.Bge, gotoNextCaseLabel);

			Child.Apply(context, new ParentContext()
			{
				EmitLoad = c =>
				{
					context.Processor.Add(CilOpCodes.Ldloc, local);
					c.Processor.Add(CilOpCodes.Ldarg_0);
					c.Processor.Add(CilOpCodes.Ldfld, stateField);
					context.Processor.Add(CilOpCodes.Callvirt, GetAssetDictionaryGetPairMethod());
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

		public override GenericInstanceTypeSignature TypeSignature { get; }

		private IMethodDefOrRef GetAssetDictionaryCountMethod()
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == $"get_{nameof(AssetDictionary<int, int>.Count)}");
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, TypeSignature, method);
		}

		private IMethodDefOrRef GetAssetDictionaryGetPairMethod()
		{
			MethodDefinition method = SharedState.Instance.Importer.LookupMethod(typeof(AssetDictionary<,>), m => m.Name == nameof(AssetDictionary<int, int>.GetPair));
			return MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, TypeSignature, method);
		}
	}
}
