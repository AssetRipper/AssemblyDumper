using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;
using AssetRipper.Core.Classes.GameObject;
using System;

namespace AssemblyDumper.Passes
{
	public static class Pass31_ComponentInterface
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;
		const string PropertyName = "GameObjectPtr";
		const string FieldName = "m_GameObject";

		public static void DoPass()
		{
			Console.WriteLine("Pass 31: Component Interface");
			ITypeDefOrRef componentInterface = SharedState.Importer.ImportCommonType<IComponent>();
			if (SharedState.TypeDictionary.TryGetValue("Component", out TypeDefinition type))
			{
				type.Interfaces.Add(new InterfaceImplementation(componentInterface));
				FieldDefinition field = type.GetFieldByName(FieldName);
				TypeSignature fieldType = field.Signature.FieldType;
				MethodDefinition explicitConversionMethod = PPtrUtils.GetExplicitConversion<IGameObject>(fieldType.Resolve());
				PropertyDefinition property = type.AddGetterProperty(PropertyName, InterfacePropertyImplementationAttributes, explicitConversionMethod.Signature.ReturnType);
				CilInstructionCollection processor = property.GetMethod.CilMethodBody.Instructions;
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, field);
				processor.Add(CilOpCodes.Call, explicitConversionMethod);
				processor.Add(CilOpCodes.Ret);
			}
			else
			{
				throw new Exception("Component not found");
			}
		}
	}
}
