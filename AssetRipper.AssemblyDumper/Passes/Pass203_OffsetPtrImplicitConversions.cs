﻿using AssetRipper.AssemblyCreationTools.Methods;

namespace AssetRipper.AssemblyDumper.Passes
{
	/// <summary>
	/// Adds implicit conversion and ToString override to OffsetPtr types.
	/// </summary>
	public static class Pass203_OffsetPtrImplicitConversions
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			foreach ((string name, SubclassGroup group) in SharedState.Instance.SubclassGroups)
			{
				if (name.StartsWith("OffsetPtr"))
				{
					foreach (TypeDefinition type in group.Types)
					{
						type.AddImplicitConversion();
						type.AddToStringOverride();
					}
				}
			}
		}

		private static void AddImplicitConversion(this TypeDefinition type)
		{
			FieldDefinition field = type.GetField();

			MethodDefinition implicitMethod = type.AddMethod("op_Implicit", ConversionAttributes, field.Signature!.FieldType);
			implicitMethod.AddParameter(type.ToTypeSignature(), "value");

			CilInstructionCollection processor = implicitMethod.CilMethodBody!.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddToStringOverride(this TypeDefinition type)
		{
			const MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

			CorLibTypeSignature stringType = type.Module!.CorLibTypeFactory.String;
			ITypeDefOrRef objectType = type.Module.CorLibTypeFactory.Object.ToTypeDefOrRef();

			MethodDefinition toStringMethod = type.AddMethod(nameof(ToString), attributes, stringType);

			CilInstructionCollection processor = toStringMethod.CilMethodBody!.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, type.GetField());
			processor.Add(CilOpCodes.Callvirt, new MemberReference(objectType, nameof(ToString), MethodSignature.CreateInstance(stringType)));
			processor.Add(CilOpCodes.Ret);
		}

		private static FieldDefinition GetField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == "m_Data");
		}
	}
}
