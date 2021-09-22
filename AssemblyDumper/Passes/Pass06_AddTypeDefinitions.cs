using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass06_AddTypeDefinitions
	{
		public static void DoPass()
		{
			Logger.Info("Pass 6: Add Type Definitions");
			var assembly = SharedState.Assembly;
			foreach (var pair in SharedState.ClassDictionary)
			{
				var typeDef = assembly.CreateType(pair.Value);
				if(typeDef != null)
					SharedState.TypeDictionary.Add(pair.Key, typeDef);
			}
		}

		private static TypeDefinition CreateType(this AssemblyDefinition _this, UnityClass @class)
		{
			string name = @class.Name;
			if (SystemTypeGetter.primitiveNamesCsharp.Contains(name))
				return null;
			TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

			if (@class.IsAbstract) typeAttributes |= TypeAttributes.Abstract;
			if (@class.IsSealed) typeAttributes |= TypeAttributes.Sealed;

			TypeDefinition typeDef = new TypeDefinition(SharedState.Classesnamespace, name, typeAttributes);
			if (@class.IsEditorOnly) typeDef.AddCustomAttribute(CommonTypeGetter.EditorOnlyAttributeConstructor);
			if (@class.IsStripped) typeDef.AddCustomAttribute(CommonTypeGetter.StrippedAttributeConstructor);
			//typeDef.AddCustomAttribute(CommonTypeGetter.PersistentIDAttributeConstructor, SharedState.PersistentTypeIDDefinition, @class.TypeID);
			//typeDef.AddCustomAttribute(CommonTypeGetter.ByteSizeAttributeConstructor, SystemTypeGetter.Int32(_this.MainModule), @class.Size);

			_this.MainModule.Types.Add(typeDef);
			
			return typeDef;
		}

		private static void AddCustomAttribute(this TypeDefinition _this, MethodReference constructor)
		{
			_this.CustomAttributes.Add(new CustomAttribute(constructor));
		}

		private static void AddCustomAttribute(this TypeDefinition _this, MethodReference constructor, TypeReference param1Type, object param1Value)
		{
			var attrDef = new CustomAttribute(constructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(param1Type, param1Value));
			_this.CustomAttributes.Add(attrDef);
		}
	}
}
