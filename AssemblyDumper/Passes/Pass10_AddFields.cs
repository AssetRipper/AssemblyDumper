using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;
using AssetRipper.Core.Attributes;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Passes
{
	public static class Pass10_AddFields
	{
		private static MethodReference ReleaseOnlyAttributeConstructor { get; set; }
		private static MethodReference EditorOnlyAttributeConstructor { get; set; }
		private static TypeReference TransferMetaFlagsDefinition { get; set; }
		private static MethodReference EditorMetaFlagsAttributeConstructor { get; set; }
		private static MethodReference ReleaseMetaFlagsAttributeConstructor { get; set; }
		private static TypeReference AssetDictionaryType { get; set; }

		private static void InitializeImports()
		{
			ReleaseOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<ReleaseOnlyAttribute>();
			EditorOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorOnlyAttribute>();
			TransferMetaFlagsDefinition = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TransferMetaFlags>();
			EditorMetaFlagsAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorMetaFlagsAttribute>(1);
			ReleaseMetaFlagsAttributeConstructor = SharedState.Module.ImportCommonConstructor<ReleaseMetaFlagsAttribute>(1);
			AssetDictionaryType = SharedState.Module.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");
		}

		public static void DoPass()
		{
			Console.WriteLine("Pass 10: Add Fields");

			InitializeImports();

			foreach ((string name, UnityClass unityClass) in SharedState.ClassDictionary)
			{
				ProcessNodeInformation(name, unityClass);
			}
		}

		private static void ProcessNodeInformation(string name, UnityClass unityClass)
		{
			TypeDefinition type = SharedState.TypeDictionary[name];

			if (unityClass.EditorRootNode == null && unityClass.ReleaseRootNode == null)
				return; //No fields.

			GetFieldNodeSets(unityClass, out List<UnityNode> releaseOnly, out List<UnityNode> editorOnly, out List<(UnityNode, UnityNode)> releaseAndEditor);

			foreach(UnityNode releaseOnlyField in releaseOnly)
			{
				TypeReference releaseOnlyFieldType = ResolveFieldType(releaseOnlyField);
				type.AddReleaseOnlyField(releaseOnlyField, releaseOnlyFieldType);
			}

			foreach (UnityNode editorOnlyField in editorOnly)
			{
				TypeReference editorOnlyFieldType = ResolveFieldType(editorOnlyField);
				type.AddEditorOnlyField(editorOnlyField, editorOnlyFieldType);
			}

			foreach ((UnityNode releaseField, UnityNode editorField) in releaseAndEditor)
			{
				TypeReference fieldType = ResolveFieldType(releaseField);
				type.AddNormalField(releaseField, editorField, fieldType);
			}
		}

		private static void GetFieldNodeSets(UnityClass unityClass, out List<UnityNode> releaseOnly, out List<UnityNode> editorOnly, out List<(UnityNode, UnityNode)> releaseAndEditor)
		{
			List<UnityNode> editorNodes = unityClass.GetNonInheritedEditorNodes();
			List<UnityNode> releaseNodes = unityClass.GetNonInheritedReleaseNodes();

			Dictionary<string, UnityNode> releaseFields = releaseNodes.ToDictionary(x => x.Name, x => x);
			Dictionary<string, UnityNode> editorFields = editorNodes.ToDictionary(x => x.Name, x => x);

			List<UnityNode> releaseOnlyResult = releaseNodes.Where(node => !editorFields.ContainsKey(node.Name)).ToList();
			//Need to use a result local field here becuase out parameters can't be used in lambda expressions
			editorOnly = editorNodes.Where(node => !releaseFields.ContainsKey(node.Name)).ToList();

			releaseAndEditor = releaseNodes.
				Where(anyRelease => !releaseOnlyResult.Contains(anyRelease)).
				Select(releaseWithEditor => (releaseWithEditor, editorFields[releaseWithEditor.Name])).
				ToList();

			releaseOnly = releaseOnlyResult;
		}

		private static List<UnityNode> GetNonInheritedEditorNodes(this UnityClass unityClass)
		{
			List<UnityNode> editorNodes = unityClass.EditorRootNode?.SubNodes ?? new();
			return editorNodes.Where(node => !IsFieldInBaseType(unityClass, node.Name)).ToList();
		}

		private static List<UnityNode> GetNonInheritedReleaseNodes(this UnityClass unityClass)
		{
			List<UnityNode> releaseNodes = unityClass.ReleaseRootNode?.SubNodes ?? new();
			return releaseNodes.Where(node => !IsFieldInBaseType(unityClass, node.Name)).ToList();
		}

		private static void AddReleaseOnlyField(this TypeDefinition type, UnityNode releaseNode, TypeReference fieldType)
		{
			FieldDefinition fieldDefinition = new FieldDefinition(releaseNode.Name, FieldAttributes.Public, fieldType);
			fieldDefinition.AddReleaseFlagAttribute(releaseNode.MetaFlag);
			fieldDefinition.AddCustomAttribute(ReleaseOnlyAttributeConstructor);
			type.Fields.Add(fieldDefinition);
		}

		private static void AddEditorOnlyField(this TypeDefinition type, UnityNode editorNode, TypeReference fieldType)
		{
			FieldDefinition fieldDefinition = new FieldDefinition(editorNode.Name, FieldAttributes.Public, fieldType);
			fieldDefinition.AddCustomAttribute(EditorOnlyAttributeConstructor);
			fieldDefinition.AddEditorFlagAttribute(editorNode.MetaFlag);
			type.Fields.Add(fieldDefinition);
		}

		private static void AddNormalField(this TypeDefinition type, UnityNode releaseNode, UnityNode editorNode, TypeReference fieldType)
		{
			FieldDefinition fieldDefinition = new FieldDefinition(editorNode.Name, FieldAttributes.Public, fieldType);
			fieldDefinition.AddReleaseFlagAttribute(releaseNode.MetaFlag);
			fieldDefinition.AddEditorFlagAttribute(editorNode.MetaFlag);
			type.Fields.Add(fieldDefinition);
		}

		private static TypeReference ResolveFieldType(UnityNode editorField)
		{
			TypeReference fieldType = SharedState.Module.GetPrimitiveType(editorField.TypeName);

			if (fieldType == null && SharedState.TypeDictionary.TryGetValue(editorField.TypeName, out TypeDefinition result))
				fieldType = result;

			if (fieldType == null)
			{
				switch (editorField.TypeName)
				{
					case "vector":
					case "set":
					case "staticvector":
						UnityNode arrayNode = editorField.SubNodes[0];
						return ResolveArrayType(arrayNode);
					case "map":
						return ResolveDictionaryType(editorField);
					case "pair":
						return ResolvePairType(editorField);
					case "TypelessData":
						return SystemTypeGetter.UInt8.MakeArrayType();
					case "Array":
						return ResolveArrayType(editorField);
				}
			}

			if (fieldType == null)
			{
				throw new Exception($"Could not resolve field type {editorField.TypeName}");
			}

			return SharedState.Module.ImportReference(fieldType);
		}

		private static TypeReference ResolveDictionaryType(UnityNode dictionaryNode)
		{
			UnityNode pairNode = dictionaryNode.SubNodes[0].SubNodes[1];
			ResolvePairElementTypes(pairNode, out TypeReference firstType, out TypeReference secondType);
			return AssetDictionaryType.MakeGenericInstanceType(firstType, secondType);
		}

		private static TypeReference ResolvePairType(UnityNode pairNode)
		{
			ResolvePairElementTypes(pairNode, out TypeReference firstType, out TypeReference secondType);
			TypeReference kvpType = SystemTypeGetter.KeyValuePair;
			return kvpType.MakeGenericInstanceType(firstType, secondType);
		}

		private static void ResolvePairElementTypes(UnityNode pairNode, out TypeReference firstType, out TypeReference secondType)
		{
			firstType = ResolveFieldType(pairNode.SubNodes[0]);
			secondType = ResolveFieldType(pairNode.SubNodes[1]);

			if (firstType == null || secondType == null)
			{
				throw new Exception($"Could not resolve one of the parameters in a pair: first is {pairNode.SubNodes[0].TypeName}, second is {pairNode.SubNodes[1].TypeName}");
			}
		}

		private static TypeReference ResolveArrayType(UnityNode arrayNode)
		{
			UnityNode arrayTypeNode = arrayNode.SubNodes[1];
			TypeReference arrayType = ResolveFieldType(arrayTypeNode);

			if (arrayType == null)
			{
				throw new Exception($"Could not resolve array parameter {arrayTypeNode.TypeName}");
			}
			
			return arrayType.MakeArrayType();
		}

		private static void AddReleaseFlagAttribute(this FieldDefinition _this, int flags)
		{
			CustomAttribute attrDef = new CustomAttribute(ReleaseMetaFlagsAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(TransferMetaFlagsDefinition, flags));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddEditorFlagAttribute(this FieldDefinition _this, int flags)
		{
			CustomAttribute attrDef = new CustomAttribute(EditorMetaFlagsAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(TransferMetaFlagsDefinition, flags));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddCustomAttribute(this FieldDefinition _this, MethodReference constructor)
		{
			_this.CustomAttributes.Add(new CustomAttribute(constructor));
		}

		private static bool IsFieldInBaseType(UnityClass unityClass, string fieldName)
		{
			string baseTypeName = unityClass.Base;
			while (!string.IsNullOrEmpty(baseTypeName))
			{
				UnityClass baseType = SharedState.ClassDictionary[baseTypeName];

				if (baseType.EditorRootNode?.SubNodes.Any(n => n.Name == fieldName) == true)
					return true;

				if (baseType.ReleaseRootNode?.SubNodes.Any(n => n.Name == fieldName) == true)
					return true;

				baseTypeName = baseType.Base;
			}

			return false;
		}
	}
}