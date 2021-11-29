using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass01_CreateBasicTypes
	{
		private const TypeAttributes StaticClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract;
		private const MethodAttributes StaticConstructorAttributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static;
		public static void DoPass()
		{
			Logger.Info("Pass 1: Create Basic Types");
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
			var addMethod = MethodUtils.MakeMethodOnGenericType(uintStringDictionary.Resolve().Methods.First(m => m.Name == "Add"), uintStringDictionary);

			FieldDefinition field = new FieldDefinition("dictionary", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, uintStringDictionary);
			newTypeDef.Fields.Add(field);

			var staticConstructor = new MethodDefinition(".cctor", StaticConstructorAttributes, SystemTypeGetter.Void);
			newTypeDef.Methods.Add(staticConstructor);
			var processor = staticConstructor.Body.GetILProcessor();
			processor.Emit(OpCodes.Newobj, dictionaryConstructor);
			foreach(UnityString unityString in SharedState.Strings)
			{
				processor.Emit(OpCodes.Dup);
				processor.Emit(OpCodes.Ldc_I4, (int)unityString.Index);
				processor.Emit(OpCodes.Ldstr, unityString.String);
				processor.Emit(OpCodes.Call, addMethod);
			}
			processor.Emit(OpCodes.Stsfld, field);
			processor.Emit(OpCodes.Ret);
		}

		private static TypeDefinition CreateEmptyStaticClass(AssemblyDefinition assembly, string @namespace, string name)
		{
			TypeDefinition newTypeDef = new TypeDefinition(@namespace, name, StaticClassAttributes, SystemTypeGetter.Object);
			assembly.MainModule.Types.Add(newTypeDef);

			return newTypeDef;
		}
	}
}
