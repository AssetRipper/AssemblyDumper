﻿using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass32_MonoScriptInterface
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;
		public static void DoPass()
		{
			Console.WriteLine("Pass 32: MonoScript Interface");
			ITypeDefOrRef componentInterface = SharedState.Importer.ImportCommonType<IMonoScript>();
			if (SharedState.TypeDictionary.TryGetValue("MonoScript", out TypeDefinition type))
			{
				type.Interfaces.Add(new InterfaceImplementation(componentInterface));
				type.ImplementGetterProperty("ClassName", InterfacePropertyImplementationAttributes, SystemTypeGetter.String, type.GetFieldByName("m_ClassName"));
				type.ImplementGetterProperty("Namespace", InterfacePropertyImplementationAttributes, SystemTypeGetter.String, type.GetFieldByName("m_Namespace"));
				type.ImplementGetterProperty("AssemblyName", InterfacePropertyImplementationAttributes, SystemTypeGetter.String, type.GetFieldByName("m_AssemblyName"));
				type.ImplementGetterProperty("ExecutionOrder", InterfacePropertyImplementationAttributes, SystemTypeGetter.Int32, type.GetFieldByName("m_ExecutionOrder"));
				type.ImplementHashProperty();
			}
			else
			{
				throw new Exception("MonoScript not found");
			}
		}

		private static void ImplementHashProperty(this TypeDefinition type)
		{
			TypeSignature returnTypeSignature = SharedState.Importer.ImportCommonType<AssetRipper.Core.Classes.Misc.Hash128>().ToTypeSignature();

			PropertyDefinition property = type.AddGetterProperty("PropertiesHash", InterfacePropertyImplementationAttributes, returnTypeSignature);
			CilInstructionCollection processor = property.GetMethod.CilMethodBody.Instructions;

			FieldDefinition field = type.GetFieldByName("m_PropertiesHash");
			if(field.Signature.FieldType.Name == "Hash128")
			{
				MethodDefinition conversionMethod = field.Signature.FieldType.Resolve().Methods.Single(m => m.Name == "op_Implicit");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
				processor.Add(CilOpCodes.Call, conversionMethod);
				processor.Add(CilOpCodes.Ret);
			}
			else if (field.Signature.FieldType.Name == "UInt32")
			{
				IMethodDefOrRef constructor = SharedState.Importer.ImportCommonMethod<AssetRipper.Core.Classes.Misc.Hash128>(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "UInt32");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
				processor.Add(CilOpCodes.Newobj, constructor);
				processor.Add(CilOpCodes.Ret);
			}
			else
			{
				throw new Exception($"Incompatible field type {field.Signature.FieldType.Name}");
			}
		}
	}
}