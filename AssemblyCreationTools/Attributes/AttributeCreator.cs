using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyCreationTools.Attributes
{
	public static class AttributeCreator
	{
		public static TypeDefinition CreateDefaultAttribute(CachedReferenceImporter importer, string @namespace, string name)
		{
			ITypeDefOrRef systemAttributeReference = importer.ImportType<Attribute>();

			TypeDefinition attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			importer.TargetModule.TopLevelTypes.Add(attributeDefinition);
			attributeDefinition.AddDefaultConstructor(importer);

			return attributeDefinition;
		}

		public static CustomAttribute AddAttributeTargetsAttribute(this TypeDefinition attributeDefinition, CachedReferenceImporter importer, AttributeTargets targets)
		{
			IMethodDefOrRef constructorMethod = importer.ImportMethod<AttributeUsageAttribute>(
					c => c.IsConstructor && c.Parameters.Count == 1 && c.Parameters[0].ParameterType.Name == nameof(AttributeTargets));

			TypeSignature attributeTargetsReference = importer.ImportTypeSignature<AttributeTargets>();

			return attributeDefinition.AddCustomAttribute(constructorMethod, attributeTargetsReference, targets);
		}

		public static void AddAttributeTargetsAttribute(this TypeDefinition attributeDefinition, CachedReferenceImporter importer, AttributeTargets targets, bool allowMultiple, bool inherited)
		{
			CustomAttribute customAttribute = attributeDefinition.AddAttributeTargetsAttribute(importer, targets);
			customAttribute.AddNamedArgument(importer.Boolean, "AllowMultiple", importer.Boolean, allowMultiple, CustomAttributeArgumentMemberType.Property);
			customAttribute.AddNamedArgument(importer.Boolean, "Inherited", importer.Boolean, inherited, CustomAttributeArgumentMemberType.Property);
		}

		public static TypeDefinition CreateSingleValueAttribute(CachedReferenceImporter importer, string @namespace, string name, string fieldName, TypeSignature fieldType, bool hasDefaultConstructor, out MethodDefinition singleParamConstructor)
		{
			ITypeDefOrRef systemAttributeReference = importer.ImportType<Attribute>();
			IMethodDefOrRef defaultAttributeConstructor = importer.ImportDefaultConstructor<Attribute>();

			var attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			importer.TargetModule.TopLevelTypes.Add(attributeDefinition);
			if (hasDefaultConstructor)
			{
				var defaultConstructor = new MethodDefinition(
					".ctor",
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
					MethodSignature.CreateInstance(importer.Void)
				);

				var defaultProcessor = defaultConstructor.CilMethodBody!.Instructions;
				defaultProcessor.Add(CilOpCodes.Ldarg_0);
				defaultProcessor.Add(CilOpCodes.Call, defaultAttributeConstructor);
				defaultProcessor.Add(CilOpCodes.Ret);

				attributeDefinition.Methods.Add(defaultConstructor);
			}

			var field = new FieldDefinition(fieldName, FieldAttributes.Public | FieldAttributes.InitOnly, new FieldSignature(fieldType));
			attributeDefinition.Fields.Add(field);

			singleParamConstructor = attributeDefinition.AddEmptyConstructor();
			singleParamConstructor.AddParameter(fieldType);

			var processor = singleParamConstructor.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, defaultAttributeConstructor);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);

			return attributeDefinition;
		}
	}
}
