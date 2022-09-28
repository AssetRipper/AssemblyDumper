using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core.IO;
using System.Text;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass052_InterfacePropertiesAndMethods
	{
		private static readonly SignatureComparer signatureComparer = new()
		{
			AcceptNewerAssemblyVersionNumbers = true,
			IgnoreAssemblyVersionNumbers = true
		};

		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				//129 is PlayerSettings. It accounts for far too much of the assembly already
				if (group.ID != 129)
				{
					group.ImplementProperties();
				}
			}
		}

		private static void ImplementProperties(this ClassGroupBase group)
		{
			Dictionary<string, (string, TypeSignature, bool)> propertyDictionary = group.GetPropertyDictionary();
			HashSet<string> differingFieldNames = group.GetDifferingFieldNames();

			foreach ((string propertyName, (string fieldName, TypeSignature propertyTypeSignature, bool hasConflictingTypes)) in propertyDictionary)
			{
				bool missingOnSomeVersions = differingFieldNames.Contains(fieldName);
				bool isValueType = propertyTypeSignature.IsValueType;

				PropertyDefinition propertyDeclaration = group.Interface.AddInterfacePropertyDeclaration(propertyName, propertyTypeSignature);
				InterfaceProperty interfaceProperty = new InterfaceProperty(propertyDeclaration, group);
				group.InterfaceProperties.Add(interfaceProperty);

				foreach (GeneratedClassInstance instance in group.Instances)
				{
					TypeDefinition type = instance.Type;
					FieldDefinition? field = type.TryGetFieldByName(fieldName, true);
					if (hasConflictingTypes || missingOnSomeVersions)
					{
						TypeSignature? fieldType = field?.Signature?.FieldType;
						bool presentAndMatchesType = fieldType is not null && instance.Class.ContainsField(fieldName) &&
							(!hasConflictingTypes || signatureComparer.Equals(fieldType, propertyTypeSignature));

						PropertyDefinition property = presentAndMatchesType
							? type.ImplementInterfaceProperty(propertyName, propertyTypeSignature, field)
							: type.ImplementInterfaceProperty(propertyName, propertyTypeSignature, null);

						ClassProperty classProperty = presentAndMatchesType
							? new ClassProperty(property, field, interfaceProperty, instance)
							: new ClassProperty(property, null, interfaceProperty, instance);
						instance.Properties.Add(classProperty);

						if (!isValueType && presentAndMatchesType && propertyTypeSignature is SzArrayTypeSignature && field is not null)
						{
							property.FixNullableArraySetMethod(field);
						}
					}
					else
					{
						PropertyDefinition property = type.ImplementInterfaceProperty(propertyName, propertyTypeSignature, field);
						ClassProperty classProperty = new ClassProperty(property, field, interfaceProperty, instance);
						instance.Properties.Add(classProperty);
					}
				}
			}
		}

		private static HashSet<string> GetDifferingFieldNames(this ClassGroupBase group)
		{
			List<(GeneratedClassInstance, List<string>)> data = new();
			List<string> allFieldNames = new();
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				List<string> instanceFieldNames = instance.Class.GetFieldNames().ToList();
				data.Add((instance, instanceFieldNames));
				allFieldNames.AddRange(instanceFieldNames);
			}
			return allFieldNames.Distinct().Where(f => data.Any(pair => !pair.Item2.Contains(f))).ToHashSet();
		}

		/// <summary>
		/// Field name : List of field types
		/// </summary>
		private static Dictionary<string, List<TypeSignature>> GetFieldTypeListDictionary(this ClassGroupBase group)
		{
			Dictionary<string, List<TypeSignature>> result = new();
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				foreach (string fieldName in instance.Class.GetFieldNames())
				{
					TypeSignature fieldType = instance.Type.GetFieldByName(fieldName, true).Signature!.FieldType;
					List<TypeSignature> typeList = result.GetOrAdd(fieldName);
					if (!typeList.Any(sig => signatureComparer.Equals(sig, fieldType)))
					{
						typeList.Add(fieldType);
					}
				}
			}
			return result;
		}

		private static Dictionary<string, T> SortStringDictionary<T>(this Dictionary<string, T> dictionary)
		{
			var keyList = dictionary.Keys.ToList();
			keyList.Sort();
			return keyList.ToDictionary(key => key, key => dictionary[key]);
		}

		/// <summary>
		/// Property name : field name, property type, type conflict
		/// </summary>
		private static Dictionary<string, (string, TypeSignature, bool)> GetPropertyDictionary(this ClassGroupBase group)
		{
			Dictionary<string, List<TypeSignature>> fieldTypeDictionary = group.GetFieldTypeListDictionary().SortStringDictionary();
			Dictionary<string, (string, TypeSignature, bool)> propertyDictionary = new();

			foreach ((string fieldName, List<TypeSignature> fieldTypeList) in fieldTypeDictionary)
			{
				string propertyName = GeneratedInterfaceUtils.GetPropertyNameFromFieldName(fieldName, group.ID);
				if (fieldTypeList.Count == 1)
				{
					propertyDictionary.Add(propertyName, (fieldName, fieldTypeList[0], false));
				}
				else if (TryGetCommonInheritor(fieldTypeList, out TypeSignature? baseInterface))
				{
					propertyDictionary.Add(propertyName, (fieldName, baseInterface, false));
				}
				else if (TryGetCommonGenericInstance(fieldTypeList, out TypeSignature? accessTypeSignature))
				{
					propertyDictionary.Add(propertyName, (fieldName, accessTypeSignature, false));
				}
				else
				{
					foreach (TypeSignature fieldType in fieldTypeList)
					{
						string fieldTypeName = GetName(fieldType);
						propertyDictionary.Add($"{propertyName}_{fieldTypeName}", (fieldName, fieldType, true));
					}
				}
			}

			return propertyDictionary;
		}

		private static bool TryGetCommonGenericInstance(List<TypeSignature> fieldTypeList, [NotNullWhen(true)] out TypeSignature? accessTypeSignature)
		{
			accessTypeSignature = null;
			if (fieldTypeList.TryCast(out List<GenericInstanceTypeSignature>? genericInstanceFields))
			{
				if (genericInstanceFields.All(genericInstance => genericInstance.GenericType.Name == "AssetList`1"))
				{
					List<TypeSignature> typeArguments = genericInstanceFields.Select(genericInstance => genericInstance.TypeArguments.Single()).ToList();
					if (TryGetCommonInheritor(typeArguments, out TypeSignature? commonInterface))
					{
						accessTypeSignature = SharedState.Instance.Importer.ImportTypeSignature(typeof(AccessListBase<>)).MakeGenericInstanceType(commonInterface);
					}
				}
				else if (genericInstanceFields.All(genericInstance => genericInstance.GenericType.Name == "AssetDictionary`2"))
				{
					List<TypeSignature> keyTypeArguments = genericInstanceFields.Select(genericInstance => genericInstance.TypeArguments[0]).ToList();
					List<TypeSignature> valueTypeArguments = genericInstanceFields.Select(genericInstance => genericInstance.TypeArguments[1]).ToList();
					if (keyTypeArguments.TryGetEqualityOrCommonInheritor(out TypeSignature? commonKeyType)
						&& valueTypeArguments.TryGetEqualityOrCommonInheritor(out TypeSignature? commonValueType))
					{
						accessTypeSignature = SharedState.Instance.Importer.ImportTypeSignature(typeof(AccessDictionaryBase<,>))
							.MakeGenericInstanceType(commonKeyType, commonValueType);
					}
				}
			}
			return accessTypeSignature != null;
		}

		private static bool TryGetEqualityOrCommonInheritor(this List<TypeSignature> types, [NotNullWhen(true)] out TypeSignature? commonType)
		{
			return types.TryGetEquality(out commonType) || types.TryGetCommonInheritor(out commonType);
		}

		private static bool TryGetEquality(this List<TypeSignature> types, [NotNullWhen(true)] out TypeSignature? commonType)
		{
			TypeSignature first = types.First();
			if (types.All(type => signatureComparer.Equals(type, first)))
			{
				commonType = first;
				return true;
			}
			else
			{
				commonType = null;
				return false;
			}
		}

		private static bool TryGetCommonInheritor(this List<TypeSignature> types, [NotNullWhen(true)] out TypeSignature? baseInterface)
		{
			if (types.Count == 0)
			{
				throw new ArgumentException(null, nameof(types));
			}

			if (TryGetTypeDefinitionsForTypeSignatures(types, out List<TypeDefinition>? typeDefinitions))
			{
				ClassGroupBase group = SharedState.Instance.TypesToGroups[typeDefinitions[0]];
				if (!typeDefinitions.Any(def => !group.ContainsTypeDefinition(def)))
				{
					baseInterface = group.GetSingularTypeOrInterface().ToTypeSignature();
					return true;
				}
			}
			baseInterface = null;
			return false;
		}

		private static bool ContainsTypeDefinition(this ClassGroupBase group, TypeDefinition type)
		{
			//any instance where an instance's type definition is equal or the group interface is equal
			return group.Instances.Any(instance => signatureComparer.Equals(type, instance.Type)) || signatureComparer.Equals(type, group.Interface);
		}

		private static bool TryGetTypeDefinitionForTypeSignature(TypeSignature typeSignature, [NotNullWhen(true)] out TypeDefinition? typeDefinition)
		{
			typeDefinition = (typeSignature as TypeDefOrRefSignature)?.Type as TypeDefinition;
			return typeDefinition != null;
		}

		private static TypeDefinition? TryGetTypeDefinitionForTypeSignature(TypeSignature typeSignature)
		{
			return (typeSignature as TypeDefOrRefSignature)?.Type as TypeDefinition;
		}

		private static bool TryGetTypeDefinitionsForTypeSignatures(List<TypeSignature> typeSignatures, [NotNullWhen(true)] out List<TypeDefinition>? typeDefinitions)
		{
			typeDefinitions = new List<TypeDefinition>(typeSignatures.Count);
			foreach (TypeSignature typeSignature in typeSignatures)
			{
				if (TryGetTypeDefinitionForTypeSignature(typeSignature, out TypeDefinition? typeDefinition))
				{
					typeDefinitions.Add(typeDefinition);
				}
				else
				{
					typeDefinitions = null;
					return false;
				}
			}
			return true;
		}

		private static PropertyDefinition AddInterfacePropertyDeclaration(this TypeDefinition @interface, string propertyName, TypeSignature propertyType)
		{
			return ShouldUseFullProperty(propertyType)
				? @interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType)
				: @interface.AddGetterProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType);
		}

		private static bool ShouldUseFullProperty(TypeSignature propertyType)
		{
			return propertyType is SzArrayTypeSignature or CorLibTypeSignature || propertyType.IsValueType;
		}

		private static PropertyDefinition ImplementInterfaceProperty(this TypeDefinition declaringType, string propertyName, TypeSignature propertyType, FieldDefinition? field)
		{
			if (ShouldUseFullProperty(propertyType))
			{
				return declaringType.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, propertyType, field);
			}
			else if (field is null)
			{
				return declaringType.ImplementGetterProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, propertyType, field);
			}
			else
			{
				if (propertyType is GenericInstanceTypeSignature accessListBaseSignature && accessListBaseSignature.GenericType.Name == "AccessListBase`1")
				{
					GenericInstanceTypeSignature fieldType = field.Signature?.FieldType as GenericInstanceTypeSignature ?? throw new Exception();
					TypeSignature underlyingType = fieldType.TypeArguments.Single();
					TypeSignature baseType = accessListBaseSignature.TypeArguments.Single();
					GenericInstanceTypeSignature accessListSignature = SharedState.Instance.Importer
						.ImportTypeSignature(typeof(AccessList<,>))
						.MakeGenericInstanceType(underlyingType, baseType);
					IMethodDefOrRef constructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, accessListSignature, 1);

					PropertyDefinition property = declaringType.AddGetterProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, propertyType);
					CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Newobj, constructor);
					processor.Add(CilOpCodes.Ret);
					return property;
				}
				else if (propertyType is GenericInstanceTypeSignature accessDictionaryBaseSignature && accessDictionaryBaseSignature.GenericType.Name == "AccessDictionaryBase`2")
				{
					GenericInstanceTypeSignature fieldType = field.Signature?.FieldType as GenericInstanceTypeSignature ?? throw new Exception();
					TypeSignature keyNormalType = fieldType.TypeArguments[0];
					TypeSignature valueNormalType = fieldType.TypeArguments[1];
					TypeSignature keyBaseType = accessDictionaryBaseSignature.TypeArguments[0];
					TypeSignature valueBaseType = accessDictionaryBaseSignature.TypeArguments[1];
					GenericInstanceTypeSignature accessDictionarySignature = SharedState.Instance.Importer
						.ImportTypeSignature(typeof(AccessDictionary<,,,>))
						.MakeGenericInstanceType(keyNormalType, valueNormalType, keyBaseType, valueBaseType);
					IMethodDefOrRef constructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, accessDictionarySignature, 1);

					PropertyDefinition property = declaringType.AddGetterProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, propertyType);
					CilInstructionCollection processor = property.GetMethod!.CilMethodBody!.Instructions;
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldfld, field);
					processor.Add(CilOpCodes.Newobj, constructor);
					processor.Add(CilOpCodes.Ret);
					return property;
				}
				else
				{
					return declaringType.ImplementGetterProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, propertyType, field);
				}
			}
		}

		private static void FixNullableArraySetMethod(this PropertyDefinition property, FieldDefinition field)
		{
			TypeSignature elementType = ((SzArrayTypeSignature)field.Signature!.FieldType).BaseType;
			MethodSpecification emptyArrayMethod = SharedState.Instance.Importer
				.ImportMethod(typeof(Array), m => m.Name == nameof(Array.Empty))
				.MakeGenericInstanceMethod(elementType);

			CilInstructionCollection processor = property.SetMethod!.CilMethodBody!.Instructions;
			processor.Clear();
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1); //value

			CilInstructionLabel label = new();
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Brtrue, label);
			processor.Add(CilOpCodes.Pop);
			processor.Add(CilOpCodes.Call, emptyArrayMethod);

			label.Instruction = processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
		}

		private static IEnumerable<string> GetFieldNames(this UniversalClass universalClass)
		{
			return universalClass.EditorRootNode.GetFieldNames().Union(universalClass.ReleaseRootNode.GetFieldNames()).Distinct();
		}

		private static IEnumerable<string> GetFieldNames(this UniversalNode? rootNode)
		{
			return rootNode?.SubNodes.Select(node => node.Name) ?? Enumerable.Empty<string>();
		}

		private static string GetName(TypeSignature type)
		{
			if (type is CorLibTypeSignature)
			{
				return type.Name ?? throw new NullReferenceException();
			}
			else if (type is TypeDefOrRefSignature normalType)
			{
				string asmName = normalType.Name;
				int index = asmName.IndexOf('`');
				return index > -1 ? asmName.Substring(0, index) : asmName;
			}
			else if (type is SzArrayTypeSignature arrayType)
			{
				return $"{GetName(arrayType.BaseType)}_Array";
			}
			else if (type is GenericInstanceTypeSignature genericInstanceType)
			{
				string baseTypeName = GetName(genericInstanceType.GenericType.ToTypeSignature());
				StringBuilder sb = new();
				sb.Append(baseTypeName);
				foreach (TypeSignature typeArgument in genericInstanceType.TypeArguments)
				{
					sb.Append('_');
					sb.Append(GetName(typeArgument));
				}
				return sb.ToString();
			}
			else
			{
				throw new NotSupportedException($"GetName not support for {type.FullName} of type {type.GetType()}");
			}
		}

		private static bool TryCast<T, TCast>(this List<T> originalList, [NotNullWhen(true)] out List<TCast>? castedList)
		{
			castedList = new List<TCast>(originalList.Count);
			foreach (T element in originalList)
			{
				if (element is TCast castedElement)
				{
					castedList.Add(castedElement);
				}
				else
				{
					castedList = null;
					return false;
				}
			}
			return true;
		}
	}
}
