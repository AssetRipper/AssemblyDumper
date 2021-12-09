using AssemblyDumper.Unity;
using AssetRipper.Core.Attributes;
using Mono.Cecil;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass07_AddTypeDefinitions
	{
		private static MethodReference EditorOnlyAttributeConstructor { get; set; }
		private static MethodReference StrippedAttributeConstructor { get; set; }
		private static MethodReference PersistentIDAttributeConstructor { get; set; }
		private static MethodReference OriginalNameAttributeConstructor { get; set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 7: Add Type Definitions");

			EditorOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorOnlyAttribute>();
			StrippedAttributeConstructor = SharedState.Module.ImportCommonConstructor<StrippedAttribute>();
			PersistentIDAttributeConstructor = SharedState.Module.ImportCommonConstructor<PersistentIDAttribute>(1);
			OriginalNameAttributeConstructor = SharedState.Module.ImportCommonConstructor<OriginalNameAttribute>(1);

			var assembly = SharedState.Assembly;
			foreach (var pair in SharedState.ClassDictionary)
			{
				var typeDef = assembly.CreateType(pair.Value);
				if (typeDef != null)
					SharedState.TypeDictionary.Add(pair.Key, typeDef);
			}
		}

		private static TypeDefinition CreateType(this AssemblyDefinition _this, UnityClass @class)
		{
			string name = @class.Name;
			if (SystemTypeGetter.primitiveNamesCsharp.Contains(name))
				return null;
			TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;

			if (@class.IsAbstract)
				typeAttributes |= TypeAttributes.Abstract;
			else if (@class.DescendantCount == 1) 
				typeAttributes |= TypeAttributes.Sealed;

			TypeDefinition typeDef = new TypeDefinition(SharedState.Classesnamespace, name, typeAttributes);

			if(@class.GetOriginalTypeName(out string originalTypeName))
			{
				typeDef.AddCustomAttribute(OriginalNameAttributeConstructor, SystemTypeGetter.String, originalTypeName);
			}

			if (@class.IsEditorOnly) typeDef.AddCustomAttribute(EditorOnlyAttributeConstructor);
			if (@class.IsStripped) typeDef.AddCustomAttribute(StrippedAttributeConstructor);
			typeDef.AddCustomAttribute(PersistentIDAttributeConstructor, SystemTypeGetter.Int32, @class.TypeID);

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