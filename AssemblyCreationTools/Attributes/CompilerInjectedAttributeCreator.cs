using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyCreationTools.Attributes
{
	public static class CompilerInjectedAttributeCreator
	{
		//https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md
		public static TypeDefinition CreateEmbeddedAttribute(CachedReferenceImporter importer, out MethodDefinition constructor)
		{
			TypeDefinition type = AttributeCreator.CreateDefaultAttribute(importer, "Microsoft.CodeAnalysis", "EmbeddedAttribute");
			type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic | TypeAttributes.Sealed;
			constructor = type.GetDefaultConstructor();
			type.AddCompilerGeneratedAttribute(importer);
			type.AddCustomAttribute(constructor);//The embedded attribute is attributed with itself
			return type;
		}

		public static TypeDefinition CreateNullableContextAttribute(CachedReferenceImporter importer, MethodDefinition embeddedAttributeConstructor)
		{
			TypeDefinition type = AttributeCreator.CreateSingleValueAttribute(
				importer,
				"System.Runtime.CompilerServices",
				"NullableContextAttribute",
				"Flag",
				importer.UInt8,
				false,
				out _);
			type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic | TypeAttributes.Sealed;
			type.AddCompilerGeneratedAttribute(importer);
			type.AddCustomAttribute(embeddedAttributeConstructor);
			type.AddAttributeTargetsAttribute(
				importer,
				AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate,
				false,
				false);
			return type;
		}

		public static TypeDefinition CreateNullableAttribute(CachedReferenceImporter importer, MethodDefinition embeddedAttributeConstructor)
		{
			TypeDefinition type = AttributeCreator.CreateSingleValueAttribute(
				importer,
				"System.Runtime.CompilerServices",
				"NullableAttribute",
				"NullableFlags",
				importer.UInt8.MakeSzArrayType(),
				false,
				out _);
			type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic | TypeAttributes.Sealed;
			type.AddCompilerGeneratedAttribute(importer);
			type.AddCustomAttribute(embeddedAttributeConstructor);
			type.AddAttributeTargetsAttribute(
				importer,
				AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter,
				false,
				false);


			IMethodDefOrRef defaultAttributeConstructor = importer.ImportDefaultConstructor<Attribute>();
			MethodDefinition singleByteConstructor = type.AddEmptyConstructor();
			singleByteConstructor.AddParameter(importer.UInt8);

			FieldDefinition field = type.Fields.Single();//byte[]

			var processor = singleByteConstructor.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, defaultAttributeConstructor);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldc_I4_1);
			processor.Add(CilOpCodes.Newarr, importer.UInt8.ToTypeDefOrRef());
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldc_I4_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Stelem_I1);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);

			return type;
		}

		/// <summary>
		/// Applies the <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>
		/// </summary>
		/// <param name="_this">The entity on which to apply the attribute</param>
		/// <param name="importer">The importer for the module containing the entity</param>
		/// <returns>The resulting custom attribute</returns>
		public static CustomAttribute AddCompilerGeneratedAttribute(this IHasCustomAttribute _this, CachedReferenceImporter importer)
		{
			IMethodDefOrRef constructor = importer.ImportDefaultConstructor<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			return attribute;
		}

		/// <summary>
		/// Applies the <see cref="System.Runtime.CompilerServices.ExtensionAttribute"/>
		/// </summary>
		/// <param name="_this">The entity on which to apply the attribute</param>
		/// <param name="importer">The importer for the module containing the entity</param>
		/// <returns>The resulting custom attribute</returns>
		public static CustomAttribute AddExtensionAttribute(this IHasCustomAttribute _this, CachedReferenceImporter importer)
		{
			IMethodDefOrRef constructor = importer.ImportDefaultConstructor<System.Runtime.CompilerServices.ExtensionAttribute>();
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			return attribute;
		}

		public static CustomAttribute AddMemberNotNullWhenAttribute(this IHasCustomAttribute _this, CachedReferenceImporter importer, bool returnValue, string memberName)
		{
			IMethodDefOrRef constructor = importer.ImportConstructor<System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute>(
				m => m.Parameters.Count == 2 && m.Parameters[1].ParameterType is CorLibTypeSignature);
			CustomAttribute attribute = _this.AddCustomAttribute(constructor);
			attribute.AddFixedArgument(importer.Boolean, returnValue);
			attribute.AddFixedArgument(importer.String, memberName);
			return attribute;
		}
	}
}
