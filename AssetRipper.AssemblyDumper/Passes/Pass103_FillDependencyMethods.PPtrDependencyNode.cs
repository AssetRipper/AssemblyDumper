namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private sealed class PPtrDependencyNode : DependencyNode
	{
		public PPtrDependencyNode(GeneratedClassInstance pptrType, DependencyNode parent) : base(parent)
		{
			ClassInstance = pptrType;
		}

		public override string PathContent => "";

		public GeneratedClassInstance ClassInstance { get; }

		public override TypeSignature TypeSignature => ClassInstance.Type.ToTypeSignature();

		public override bool AnyPPtrs => true;

		public override void Apply(DependencyMethodContext context, ParentContext parentContext)
		{
			IMethodDescriptor conversionMethod = ClassInstance.Type.Methods.Single(m => m.Name == "op_Implicit" && m.Signature?.ReturnType?.Name == "PPtr");
			context.Processor.Add(CilOpCodes.Ldarg_0);
			context.Processor.Add(CilOpCodes.Ldstr, FullPath);
			parentContext.EmitLoad(context);
			context.Processor.Add(CilOpCodes.Call, conversionMethod);
			context.Processor.Add(CilOpCodes.Newobj, context.TupleConstructor);
			context.Processor.Add(CilOpCodes.Stfld, context.CurrentField);
			parentContext.EmitIncrementStateAndReturnTrue(context);
		}
	}
}
