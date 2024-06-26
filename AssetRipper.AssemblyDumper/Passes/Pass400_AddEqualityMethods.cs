﻿using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass400_AddEqualityMethods
	{
		public static void DoPass()
		{
			ITypeDefOrRef equatableInterface = SharedState.Instance.Importer.ImportType(typeof(IEquatable<>));
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.IsString)
				{
					continue;
				}
				group.Interface.AddInterfaceImplementation(equatableInterface.MakeGenericInstanceType(group.Interface.ToTypeSignature()).ToTypeDefOrRef());
				foreach (GeneratedClassInstance instance in group.Instances)
				{
					MethodDefinition equalsInterfaceMethod = instance.AddEqualsInterfaceMethod(group.Interface);
					instance.Type.AddEqualsTMethod();
					instance.Type.AddEqualsObjectMethod(group.Interface, equalsInterfaceMethod);
					instance.Type.AddInterfaceImplementation(equatableInterface.MakeGenericInstanceType(instance.Type.ToTypeSignature()).ToTypeDefOrRef());
				}
			}
		}

		private static MethodDefinition AddEqualsTMethod(this TypeDefinition type)
		{
			MethodDefinition method = type.AddMethod(
				nameof(IEquatable<int>.Equals),
				MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
				SharedState.Instance.Importer.Boolean);
			method.AddParameter(type.ToTypeSignature(), "other");
			method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);

			CilInstructionLabel returnFalseLabel = new();
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Brfalse, returnFalseLabel);

			foreach (FieldDefinition field in type.Fields.Where(f => !f.IsStatic))
			{
				if (field.Signature!.FieldType is CorLibTypeSignature corLibTypeSignature && corLibTypeSignature.IsValueType)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Bne_Un, returnFalseLabel);
				}
				else
				{
					EqualityMethods.MakeEqualityComparerGenericMethods(
						field.Signature.FieldType,
						SharedState.Instance.Importer,
						out IMethodDefOrRef defaultReference,
						out IMethodDefOrRef equalsReference);
					processor.Add(CilOpCodes.Call, defaultReference);
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Callvirt, equalsReference);
					processor.Add(CilOpCodes.Brfalse, returnFalseLabel);
				}
			}

			processor.Add(CilOpCodes.Ldc_I4_1);
			processor.Add(CilOpCodes.Ret);
			returnFalseLabel.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldc_I4_0);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static MethodDefinition AddEqualsInterfaceMethod(this GeneratedClassInstance instance, TypeDefinition @interface)
		{
			TypeDefinition type = instance.Type;
			MethodDefinition method = type.AddMethod(
				nameof(IEquatable<int>.Equals),
				MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
				SharedState.Instance.Importer.Boolean);
			method.AddParameter(@interface.ToTypeSignature(), "other");
			method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);

			CilInstructionLabel returnFalseLabel = new();
			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Brfalse, returnFalseLabel);

			foreach (ClassProperty classProperty in instance.Properties)
			{
				PropertyDefinition interfaceProperty = classProperty.Base.Definition;
				PropertyDefinition instanceProperty = classProperty.Definition;
				classProperty.GetEqualityComparer(out IMethodDefOrRef defaultReference, out IMethodDefOrRef equalsReference);

				processor.Add(CilOpCodes.Call, defaultReference);
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, instanceProperty.GetMethod!);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.Add(CilOpCodes.Callvirt, interfaceProperty.GetMethod!);
				processor.Add(CilOpCodes.Callvirt, equalsReference);
				processor.Add(CilOpCodes.Brfalse, returnFalseLabel);
			}

			processor.Add(CilOpCodes.Ldc_I4_1);
			processor.Add(CilOpCodes.Ret);
			returnFalseLabel.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldc_I4_0);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}

		private static MethodDefinition AddEqualsObjectMethod(this TypeDefinition type, TypeDefinition @interface, MethodDefinition equalsInterfaceMethod)
		{
			MethodDefinition method = type.AddMethod(
				nameof(object.Equals),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				SharedState.Instance.Importer.Boolean);
			method.AddParameter(SharedState.Instance.Importer.Object, "other");
			method.AddNullableContextAttribute(NullableAnnotation.MaybeNull);

			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Isinst, @interface);
			processor.Add(CilOpCodes.Call, equalsInterfaceMethod);
			processor.Add(CilOpCodes.Ret);
			return method;
		}
	}
}
