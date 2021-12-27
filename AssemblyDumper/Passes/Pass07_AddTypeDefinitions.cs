using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.Attributes;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass07_AddTypeDefinitions
	{
		private static IMethodDefOrRef EditorOnlyAttributeConstructor { get; set; }
		private static IMethodDefOrRef ReleaseOnlyAttributeConstructor { get; set; }
		private static IMethodDefOrRef StrippedAttributeConstructor { get; set; }
		private static IMethodDefOrRef PersistentIDAttributeConstructor { get; set; }
		private static IMethodDefOrRef OriginalNameAttributeConstructor { get; set; }

		public static void DoPass()
		{
			Console.WriteLine("Pass 7: Add Type Definitions");

			EditorOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorOnlyAttribute>();
			ReleaseOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<ReleaseOnlyAttribute>();
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
			if (@class.IsReleaseOnly) typeDef.AddCustomAttribute(ReleaseOnlyAttributeConstructor);
			if (@class.IsStripped) typeDef.AddCustomAttribute(StrippedAttributeConstructor);
			typeDef.AddCustomAttribute(PersistentIDAttributeConstructor, SystemTypeGetter.Int32, @class.TypeID);

			_this.ManifestModule.TopLevelTypes.Add(typeDef);

			return typeDef;
		}
	}
}