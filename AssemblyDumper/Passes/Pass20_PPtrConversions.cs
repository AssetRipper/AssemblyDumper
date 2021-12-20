using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass20_PPtrConversions
	{
		private static ITypeDefOrRef commonPPtrType;
		private static ITypeDefOrRef unityObjectBaseInterface;
		private static GenericInstanceTypeSignature unityObjectBasePPtr;
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 20: PPtr Conversions");

			commonPPtrType = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			unityObjectBaseInterface = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			unityObjectBasePPtr = commonPPtrType.MakeGenericInstanceType(unityObjectBaseInterface.ToTypeSignature());

			foreach (string name in SharedState.ClassDictionary.Keys)
			{
				if (name.StartsWith("PPtr_"))
				{
					string parameterTypeName = name.Substring(5, name.LastIndexOf('_') - 5);
					TypeDefinition parameterType = SharedState.TypeDictionary[parameterTypeName];
					TypeDefinition pptrType = SharedState.TypeDictionary[name];
					AddImplicitConversion(pptrType, parameterType);
					AddExplicitConversion(pptrType);
				}
			}
		}

		private static void AddImplicitConversion(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			GenericInstanceTypeSignature conversionResultType = commonPPtrType.MakeGenericInstanceType(parameterType.ToTypeSignature());
			MethodSpecification constructor = MethodUtils.MakeConstructorOnGenericType(conversionResultType, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");

			MethodDefinition implicitMethod = pptrType.AddMethod("op_Implicit", ConversionAttributes, conversionResultType);
			implicitMethod.AddParameter("value", pptrType);

			implicitMethod.CilMethodBody.InitializeLocals = true;
			var processor = implicitMethod.CilMethodBody.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, fileID);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, pathID);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddExplicitConversion(TypeDefinition pptrType)
		{
			MethodSpecification constructor = MethodUtils.MakeConstructorOnGenericType(unityObjectBasePPtr, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");

			MethodDefinition explicitMethod = pptrType.AddMethod("op_Explicit", ConversionAttributes, unityObjectBasePPtr);
			explicitMethod.AddParameter("value", pptrType);

			explicitMethod.CilMethodBody.InitializeLocals = true;
			var processor = explicitMethod.CilMethodBody.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, fileID);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, pathID);
			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
