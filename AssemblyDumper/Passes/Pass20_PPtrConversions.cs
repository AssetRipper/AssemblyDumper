using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass20_PPtrConversions
	{
		private static TypeReference commonPPtrType;
		private static TypeReference unityObjectBaseInterface;
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 20: PPtr Conversions");

			commonPPtrType = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");
			unityObjectBaseInterface = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();

			foreach (string name in SharedState.ClassDictionary.Keys)
			{
				if (name.StartsWith("PPtr<"))
				{
					string parameterTypeName = name.Substring(5, name.LastIndexOf('>') - 5);
					TypeDefinition parameterType = SharedState.TypeDictionary[parameterTypeName];
					TypeDefinition pptrType = SharedState.TypeDictionary[name];
					AddImplicitConversion(pptrType, parameterType);
					AddExplicitConversion(pptrType);
				}
			}
		}

		private static void AddImplicitConversion(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			GenericInstanceType conversionResultType = commonPPtrType.MakeGenericInstanceType(parameterType);
			MethodReference constructor = MethodUtils.MakeConstructorOnGenericType(conversionResultType, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");
			
			var implicitMethod = new MethodDefinition("op_Implicit", ConversionAttributes, conversionResultType);
			pptrType.Methods.Add(implicitMethod);
			implicitMethod.Body.InitLocals = true;
			var processor = implicitMethod.Body.GetILProcessor();

			var value = new ParameterDefinition("value", ParameterAttributes.None, pptrType);
			implicitMethod.Parameters.Add(value);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, fileID);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, pathID);
			processor.Emit(OpCodes.Newobj, constructor);
			processor.Emit(OpCodes.Ret);
		}

		private static void AddExplicitConversion(TypeDefinition pptrType)
		{
			GenericInstanceType conversionResultType = commonPPtrType.MakeGenericInstanceType(unityObjectBaseInterface);
			MethodReference constructor = MethodUtils.MakeConstructorOnGenericType(conversionResultType, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");

			var implicitMethod = new MethodDefinition("op_Explicit", ConversionAttributes, conversionResultType);
			pptrType.Methods.Add(implicitMethod);
			implicitMethod.Body.InitLocals = true;
			var processor = implicitMethod.Body.GetILProcessor();

			var value = new ParameterDefinition("value", ParameterAttributes.None, pptrType);
			implicitMethod.Parameters.Add(value);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, fileID);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, pathID);
			processor.Emit(OpCodes.Newobj, constructor);
			processor.Emit(OpCodes.Ret);
		}
	}
}
