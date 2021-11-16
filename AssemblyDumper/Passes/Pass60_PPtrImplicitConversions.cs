using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyDumper.Passes
{
	public static class Pass60_PPtrImplicitConversions
	{
		private static TypeReference commonPPtrType;
		public static void DoPass()
		{
			Logger.Info("Pass 60: PPtr ImplicitConversions");

			commonPPtrType = SharedState.Module.ImportCommonType("AssetRipper.Core.Classes.Misc.PPtr`1");

			foreach ((string name, UnityClass klass) in SharedState.ClassDictionary)
			{
				if (name.StartsWith("PPtr<"))
				{
					string parameterTypeName = name.Substring(5, name.Length - 6);
					TypeDefinition parameterType = SharedState.TypeDictionary[parameterTypeName];
					TypeDefinition pptrType = SharedState.TypeDictionary[name];
					AddImplicitConversion(pptrType, parameterType);
				}
			}
		}

		private static void AddImplicitConversion(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			GenericInstanceType conversionResultType = commonPPtrType.MakeGenericInstanceType(parameterType);
			MethodReference constructor = MethodUtils.MakeConstructorOnGenericType(conversionResultType, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");
			
			var implicitMethod = new MethodDefinition("op_Implicit", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, SystemTypeGetter.Void);
			implicitMethod.ReturnType = conversionResultType;
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
