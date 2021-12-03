﻿using System;
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

		public static void DoPass()
		{
			Console.WriteLine("Pass 10: Add Fields");

			ReleaseOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<ReleaseOnlyAttribute>();
			EditorOnlyAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorOnlyAttribute>();
			TransferMetaFlagsDefinition = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Files.SerializedFiles.Parser.TransferMetaFlags>();
			EditorMetaFlagsAttributeConstructor = SharedState.Module.ImportCommonConstructor<EditorMetaFlagsAttribute>(1);
			ReleaseMetaFlagsAttributeConstructor = SharedState.Module.ImportCommonConstructor<ReleaseMetaFlagsAttribute>(1);
			AssetDictionaryType = SharedState.Module.ImportCommonType("AssetRipper.Core.IO.AssetDictionary`2");

			foreach (var (name, unityClass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				if (unityClass.EditorRootNode == null && unityClass.ReleaseRootNode == null)
					continue; //No fields.

				var editorFields = unityClass.EditorRootNode?.SubNodes ?? new();
				var releaseFields = unityClass.ReleaseRootNode?.SubNodes ?? new();

				var releaseIdx = 0;
				foreach (var editorField in editorFields)
				{
					UnityNode releaseField = null;
					if (releaseIdx < releaseFields.Count)
						releaseField = releaseFields[releaseIdx];

					while (releaseIdx < releaseFields.Count && releaseField != null && editorFields.All(f => f.Name != releaseField.Name))
					{
						//Release-only field at this index.
						var releaseOnlyFieldType = ResolveFieldType(type, releaseField);

						if (releaseOnlyFieldType != null)
						{
							var releaseOnlyFieldDef = new FieldDefinition(releaseField.Name, FieldAttributes.Public, releaseOnlyFieldType);
							releaseOnlyFieldDef.AddReleaseFlagAttribute(releaseField.MetaFlag);
							releaseOnlyFieldDef.AddCustomAttribute(ReleaseOnlyAttributeConstructor);
							type.Fields.Add(releaseOnlyFieldDef);
						}

						//Increment release index and fetch new field.
						releaseIdx++;

						if (releaseIdx < releaseFields.Count)
							releaseField = releaseFields[releaseIdx];
					}

					var isInReleaseToo = editorField.Name == releaseField?.Name;
					if (isInReleaseToo)
						//Move to next release field if this one is the one we want, otherwise stay at this one to check the next field - this one is editor-only.
						releaseIdx++;

					if (CheckIfFieldInBaseType(unityClass, editorField.Name))
					{
						continue;
					}

					var fieldType = ResolveFieldType(type, editorField);

					if (fieldType == null)
						continue; //Skip field, can't resolve type.

					var fieldDef = new FieldDefinition(editorField.Name, FieldAttributes.Public, fieldType);

					if (!isInReleaseToo)
						fieldDef.AddCustomAttribute(EditorOnlyAttributeConstructor);
					else
						fieldDef.AddReleaseFlagAttribute(releaseField.MetaFlag);

					fieldDef.AddEditorFlagAttribute(editorField.MetaFlag);
					type.Fields.Add(fieldDef);
				}

				//Check for release-only fields left at the end (e.g. MeshRenderer)
				while (releaseIdx < releaseFields.Count)
				{
					var releaseField = releaseFields[releaseIdx];

					//Release-only field at this index.
					var releaseOnlyFieldType = ResolveFieldType(type, releaseField);

					if (releaseOnlyFieldType != null)
					{
						var releaseOnlyFieldDef = new FieldDefinition(releaseField.Name, FieldAttributes.Public, releaseOnlyFieldType);
						releaseOnlyFieldDef.AddReleaseFlagAttribute(releaseField.MetaFlag);
						releaseOnlyFieldDef.AddCustomAttribute(ReleaseOnlyAttributeConstructor);
						type.Fields.Add(releaseOnlyFieldDef);
					}

					//Increment release index and fetch new field.
					releaseIdx++;
				}
			}
		}

		private static TypeReference ResolveFieldType(TypeDefinition type, UnityNode editorField)
		{
			var fieldType = type.Module.GetPrimitiveType(editorField.TypeName);

			if (fieldType == null && SharedState.TypeDictionary.TryGetValue(editorField.TypeName, out var result))
				fieldType = result;

			if (fieldType == null)
			{
				switch (editorField.TypeName)
				{
					case "vector":
					case "set":
					case "staticvector":
						var listTypeNode = editorField.SubNodes[0].SubNodes[1];
						var listType = ResolveFieldType(type, listTypeNode);

						if (listType != null)
							return listType.MakeArrayType();

						Console.WriteLine($"WARNING: Could not resolve vector parameter {listTypeNode.TypeName}");
						return null;
					case "map":
					{
						var pairNode = editorField.SubNodes[0].SubNodes[1];

						var firstType = ResolveFieldType(type, pairNode.SubNodes[0]);
						var secondType = ResolveFieldType(type, pairNode.SubNodes[1]);

						if (firstType == null || secondType == null)
						{
							Console.WriteLine($"WARNING: Could not resolve one of the parameters in a map: first is {pairNode.SubNodes[0].TypeName}, second is {pairNode.SubNodes[1].TypeName}");
							return null;
						}

						return AssetDictionaryType.MakeGenericInstanceType(firstType, secondType);
					}
					case "pair":
					{
						var firstType = ResolveFieldType(type, editorField.SubNodes[0]);
						var secondType = ResolveFieldType(type, editorField.SubNodes[1]);

						if (firstType == null || secondType == null)
						{
							Console.WriteLine($"WARNING: Could not resolve one of the parameters in a pair: first is {editorField.SubNodes[0].TypeName}, second is {editorField.SubNodes[1].TypeName}");
							return null;
						}

						var kvpType = SystemTypeGetter.KeyValuePair;
						return kvpType.MakeGenericInstanceType(firstType, secondType);
					}
					case "TypelessData":
						return SystemTypeGetter.UInt8.MakeArrayType();
					case "Array":
						var arrayTypeNode = editorField.SubNodes[1];
						var arrayType = ResolveFieldType(type, arrayTypeNode);

						if (arrayType != null)
							return arrayType.MakeArrayType();

						Console.WriteLine($"WARNING: Could not resolve array parameter {arrayTypeNode.TypeName}");
						return null;
				}
			}

			if (fieldType == null)
			{
				Console.WriteLine($"WARNING: Could not resolve field type {editorField.TypeName}");
				return null;
			}

			return type.Module.ImportReference(fieldType);
		}

		private static void AddReleaseFlagAttribute(this FieldDefinition _this, int flags)
		{
			var attrDef = new CustomAttribute(ReleaseMetaFlagsAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(TransferMetaFlagsDefinition, flags));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddEditorFlagAttribute(this FieldDefinition _this, int flags)
		{
			var attrDef = new CustomAttribute(EditorMetaFlagsAttributeConstructor);
			attrDef.ConstructorArguments.Add(new CustomAttributeArgument(TransferMetaFlagsDefinition, flags));
			_this.CustomAttributes.Add(attrDef);
		}

		private static void AddCustomAttribute(this FieldDefinition _this, MethodReference constructor)
		{
			_this.CustomAttributes.Add(new CustomAttribute(constructor));
		}

		private static bool CheckIfFieldInBaseType(UnityClass unityClass, string fieldName)
		{
			var baseTypeName = unityClass.Base;
			while (!string.IsNullOrEmpty(baseTypeName))
			{
				var baseType = SharedState.ClassDictionary[baseTypeName];

				if (baseType.EditorRootNode?.SubNodes.Any(n => n.Name == fieldName) == true)
					return true;

				baseTypeName = baseType.Base;
			}

			return false;
		}
	}
}