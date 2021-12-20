/*
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using System;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class AttributeCreator
	{
		public static TypeDefinition CreateDefaultAttribute(AssemblyDefinition assembly, string @namespace, string name, AttributeTargets targets)
		{
			var module = assembly.ManifestModule;

			ITypeDefOrRef systemAttributeReference = module.ImportSystemType("System.Attribute");
			ITypeDefOrRef attributeTargetsReference = module.ImportSystemType("System.AttributeTargets");

			IMethodDefOrRef constructorMethod = SystemTypeGetter.LookupSystemType("System.AttributeUsageAttribute").GetConstructors().First(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.Name == "AttributeTargets");

			var attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			module.TopLevelTypes.Add(attributeDefinition);
			ConstructorUtils.AddDefaultConstructor(attributeDefinition);

			var attrDef = new CustomAttribute(module.ImportMethod(constructorMethod));
			attrDef. ConstructorArguments.Add(new CustomAttributeArgument(attributeTargetsReference, targets));
			attributeDefinition.CustomAttributes.Add(attrDef);

			return attributeDefinition;
		}

		public static TypeDefinition CreateSingleValueAttribute(AssemblyDefinition assembly, string @namespace, string name, AttributeTargets targets, string fieldName, ITypeDefOrRef fieldType, object defaultValue, bool hasDefaultConstructor, out MethodDefinition singleParamConstructor)
		{
			var module = assembly.ManifestModule;

			ITypeDefOrRef systemAttributeReference = module.ImportSystemType("System.Attribute");
			ITypeDefOrRef attributeTargetsReference = module.ImportSystemType("System.AttributeTargets");

			IMethodDefOrRef usageConstructorMethod = module.ImportReference(SystemTypeGetter.LookupSystemType("System.AttributeUsageAttribute").GetConstructors().First(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.Name == "AttributeTargets"));
			IMethodDefOrRef defaultAttributeConstructor = module.ImportReference(SystemTypeGetter.LookupSystemType("System.Attribute").GetDefaultConstructor());

			var attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			module.TopLevelTypes.Add(attributeDefinition);
			if (hasDefaultConstructor)
			{
				var defaultConstructor = new MethodDefinition(
					".ctor",
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
					module.ImportReference(typeof(void))
				);

				var defaultProcessor = defaultConstructor.CilMethodBody.Instructions;
				defaultprocessor.Add(CilOpCodes.Ldarg_0);
				defaultprocessor.Add(CilOpCodes.Call, defaultAttributeConstructor);
				defaultprocessor.Add(CilOpCodes.Ret);

				attributeDefinition.Methods.Add(defaultConstructor);
			}


			var attrDef = new CustomAttribute(usageConstructorMethod);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(attributeTargetsReference, targets));
			attributeDefinition.CustomAttributes.Add(attrDef);


			var field = new FieldDefinition(fieldName, FieldAttributes.Public, fieldType);
			if (defaultValue != null) field.Constant = defaultValue;
			attributeDefinition.Fields.Add(field);


			singleParamConstructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				module.ImportReference(typeof(void))
			);
			singleParamConstructor.Parameters.Add(new ParameterDefinition(fieldName, ParameterAttributes.None, fieldType));

			var processor = singleParamConstructor.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, defaultAttributeConstructor);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);

			attributeDefinition.Methods.Add(singleParamConstructor);


			return attributeDefinition;
		}
	}
}
*/