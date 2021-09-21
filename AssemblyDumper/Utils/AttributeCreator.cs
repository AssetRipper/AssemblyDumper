using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

namespace AssemblyDumper.Utils
{
	public static class AttributeCreator
	{
		public static TypeDefinition CreateDefaultAttribute(AssemblyDefinition assembly, string @namespace, string name, AttributeTargets targets)
		{
			var module = assembly.MainModule;

			TypeReference systemAttributeReference = module.ImportSystemType("System.Attribute");
			TypeReference attributeTargetsReference = module.ImportSystemType("System.AttributeTargets");

			MethodReference constructorMethod = SystemTypeGetter.LookupSystemType("System.AttributeUsageAttribute").GetConstructors().First(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.Name == "AttributeTargets");

			var attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			module.Types.Add(attributeDefinition);
			ConstructorUtils.AddDefaultConstructor(attributeDefinition);

			var attrDef = new CustomAttribute(module.ImportReference(constructorMethod));
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(attributeTargetsReference, targets));
			attributeDefinition.CustomAttributes.Add(attrDef);

			return attributeDefinition;
		}

		public static TypeDefinition CreateSingleValueAttribute(AssemblyDefinition assembly, string @namespace, string name, AttributeTargets targets, string fieldName, TypeReference fieldType, object defaultValue, bool hasDefaultConstructor, out MethodDefinition singleParamConstructor)
		{
			var module = assembly.MainModule;

			TypeReference systemAttributeReference = module.ImportSystemType("System.Attribute");
			TypeReference attributeTargetsReference = module.ImportSystemType("System.AttributeTargets");

			MethodReference usageConstructorMethod = module.ImportReference(SystemTypeGetter.LookupSystemType("System.AttributeUsageAttribute").GetConstructors().First(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.Name == "AttributeTargets"));
			MethodReference defaultAttributeConstructor = module.ImportReference(SystemTypeGetter.LookupSystemType("System.Attribute").GetDefaultConstructor());

			var attributeDefinition = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.BeforeFieldInit, systemAttributeReference);

			module.Types.Add(attributeDefinition);
			if(hasDefaultConstructor)
			{
				var defaultConstructor = new MethodDefinition(
					".ctor",
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
					module.ImportReference(typeof(void))
				);

				var defaultProcessor = defaultConstructor.Body.GetILProcessor();
				defaultProcessor.Emit(OpCodes.Ldarg_0);
				defaultProcessor.Emit(OpCodes.Call, defaultAttributeConstructor);
				defaultProcessor.Emit(OpCodes.Ret);

				attributeDefinition.Methods.Add(defaultConstructor);
			}


			var attrDef = new CustomAttribute(usageConstructorMethod);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(attributeTargetsReference, targets));
			attributeDefinition.CustomAttributes.Add(attrDef);


			var field = new FieldDefinition(fieldName, FieldAttributes.Public, fieldType);
			if(defaultValue != null) field.Constant = defaultValue;
			attributeDefinition.Fields.Add(field);


			singleParamConstructor = new MethodDefinition(
				".ctor",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				module.ImportReference(typeof(void))
			);
			singleParamConstructor.Parameters.Add(new ParameterDefinition(fieldName, ParameterAttributes.None, fieldType));

			var processor = singleParamConstructor.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, defaultAttributeConstructor);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldarg_1);
			processor.Emit(OpCodes.Stfld, field);
			processor.Emit(OpCodes.Ret);

			attributeDefinition.Methods.Add(singleParamConstructor);


			return attributeDefinition;
		}
	}
}
