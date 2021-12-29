using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass01_CreateBasicTypes
	{
		private const TypeAttributes StaticClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract;
		private const MethodAttributes StaticConstructorAttributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.RuntimeSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static;
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 1: Create Basic Types");
			CreateClassID();
			CreateCommonStringClass();
		}

		private static void CreateClassID()
		{
			Dictionary<string, int> classIdDictionary = SharedState.ClassDictionary.Values.ToDictionary(x => x.Name, x => x.TypeID);
			EnumCreator.CreateFromDictionary(SharedState.Assembly, SharedState.RootNamespace, "ClassIDType", classIdDictionary);
		}

		private static void CreateCommonStringClass()
		{
			TypeDefinition newTypeDef = CreateEmptyStaticClass(SharedState.Assembly, SharedState.RootNamespace, "CommonString");

			var uintStringDictionary = SystemTypeGetter.Dictionary.MakeGenericInstanceType(SystemTypeGetter.UInt32, SystemTypeGetter.String);
			var dictionaryConstructor = MethodUtils.MakeConstructorOnGenericType(uintStringDictionary, 0);
			var addMethod = MethodUtils.MakeMethodOnGenericType(uintStringDictionary, uintStringDictionary.Resolve().Methods.First(m => m.Name == "Add"));

			FieldDefinition field = new FieldDefinition("dictionary", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, FieldSignature.CreateStatic(uintStringDictionary));
			newTypeDef.Fields.Add(field);

			var staticConstructor = new MethodDefinition(".cctor", StaticConstructorAttributes, MethodSignature.CreateStatic(SystemTypeGetter.Void));
			newTypeDef.Methods.Add(staticConstructor);
			staticConstructor.CilMethodBody = new CilMethodBody(staticConstructor);
			var processor = staticConstructor.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Newobj, dictionaryConstructor);
			foreach(UnityString unityString in SharedState.Strings)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldc_I4, (int)unityString.Index);
				processor.Add(CilOpCodes.Ldstr, unityString.String);
				processor.Add(CilOpCodes.Call, addMethod);
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();
		}

		private static TypeDefinition CreateEmptyStaticClass(AssemblyDefinition assembly, string @namespace, string name)
		{
			TypeDefinition newTypeDef = new TypeDefinition(@namespace, name, StaticClassAttributes);
			newTypeDef.BaseType = SystemTypeGetter.Object.ToTypeDefOrRef();
			assembly.ManifestModule.TopLevelTypes.Add(newTypeDef);

			return newTypeDef;
		}
	}
}
