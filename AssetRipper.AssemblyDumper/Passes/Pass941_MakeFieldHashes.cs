using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;
using AssetRipper.Assets.Utils;
using System.Diagnostics;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass941_MakeFieldHashes
{
	private const string MethodName = "TryGetPath";

	public static void DoPass()
	{
		List<(int, MethodDefinition)> groupMethods = new();
		foreach ((ClassGroup group, Dictionary<uint, string> hashes) in HashAllFieldPaths())
		{
			if (hashes.Count > 0)
			{
				MethodDefinition method = MakeMethodForGroup(group, hashes);
				groupMethods.Add((group.ID, method));
				if (Pass001_MergeMovedGroups.Changes.TryGetValue(group.ID, out IReadOnlyList<int>? aliasIDs))
				{
					foreach (int aliasID in aliasIDs)
					{
						groupMethods.Add((aliasID, method));
					}
				}
			}
		}
		groupMethods.Sort((a, b) => a.Item1.CompareTo(b.Item1));

		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "FieldHashes");
			DocumentationHandler.AddTypeDefinitionLine(type, $"CRC32 field path hashes for all source generated classes.");
			type.AddNullableContextAttribute(NullableAnnotation.NotNull);
			MethodDefinition nullHelperMethod = MakeNullHelperMethod(type);

			MethodDefinition method = type.AddMethod(MethodName, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, SharedState.Instance.Importer.Boolean);
			Parameter idParameter = method.AddParameter(Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature(), "classID");
			Parameter hashParameter = method.AddParameter(SharedState.Instance.Importer.UInt32, "hash");
			Parameter outParameter = method.AddParameter(SharedState.Instance.Importer.String.MakeByReferenceType(), "path");
			ParameterDefinition outParameterDefinition = outParameter.Definition!;
			outParameterDefinition.Attributes |= ParameterAttributes.Out;
			outParameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
			outParameterDefinition.AddCustomAttribute(SharedState.Instance.Importer.ImportConstructor<NotNullWhenAttribute>(1), SharedState.Instance.Importer.Boolean, true);

			CilInstructionCollection processor = method.GetProcessor();
			processor.EmitIdSwitchStatement(groupMethods, nullHelperMethod);
		}

		foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
		{
			MakeFieldPathsTypeForGroup(group);
		}
	}

	private static void EmitIdSwitchStatement(this CilInstructionCollection processor, List<(int, MethodDefinition)> groupMethods, MethodDefinition nullHelperMethod)
	{
		GenericInstanceTypeSignature uintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
		int count = groupMethods.Count;

		CilLocalVariable switchCondition = processor.AddLocalVariable(Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature());

		processor.Add(CilOpCodes.Ldarg_0);//classID
		processor.Add(CilOpCodes.Stloc, switchCondition);

		CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count).Select(i => new CilInstructionLabel()).ToArray();
		CilInstructionLabel defaultNop = new CilInstructionLabel();
		for (int i = 0; i < count; i++)
		{
			processor.Add(CilOpCodes.Ldloc, switchCondition);
			processor.Add(CilOpCodes.Ldc_I4, groupMethods[i].Item1);
			processor.Add(CilOpCodes.Beq, nopInstructions[i]);
		}
		processor.Add(CilOpCodes.Br, defaultNop);
		for (int i = 0; i < count; i++)
		{
			nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);

			processor.Add(CilOpCodes.Ldarg_1);//hash
			processor.Add(CilOpCodes.Ldarg_2);//path
			processor.Add(CilOpCodes.Call, groupMethods[i].Item2);
			processor.Add(CilOpCodes.Ret);
		}
		defaultNop.Instruction = processor.Add(CilOpCodes.Nop);
		processor.Add(CilOpCodes.Ldarg_2);//path
		processor.Add(CilOpCodes.Call, nullHelperMethod);
		processor.Add(CilOpCodes.Ret);
		processor.OptimizeMacros();
	}

	private static MethodDefinition MakeNullHelperMethod(TypeDefinition type)
	{
		MethodDefinition nullHelperMethod = type.AddMethod("NullHelper", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, SharedState.Instance.Importer.Boolean);
		Parameter outParameter = nullHelperMethod.AddParameter(SharedState.Instance.Importer.String.MakeByReferenceType(), "value");
		ParameterDefinition outParameterDefinition = outParameter.Definition!;
		outParameterDefinition.Attributes |= ParameterAttributes.Out;
		outParameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
		outParameterDefinition.AddCustomAttribute(SharedState.Instance.Importer.ImportConstructor<NotNullWhenAttribute>(1), SharedState.Instance.Importer.Boolean, true);

		CilInstructionCollection processor = nullHelperMethod.GetProcessor();
		processor.Add(CilOpCodes.Ldarg_0);
		processor.Add(CilOpCodes.Initobj, SharedState.Instance.Importer.String.ToTypeDefOrRef());
		processor.Add(CilOpCodes.Ldc_I4_0);
		processor.Add(CilOpCodes.Ret);
		return nullHelperMethod;
	}

	private static MethodDefinition MakeMethodForGroup(ClassGroupBase group, Dictionary<uint, string> hashes)
	{
		TypeDefinition type = group.GetOrCreateMainClass();

		GenericInstanceTypeSignature uintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
		IMethodDefOrRef dictionaryConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, uintStringDictionary, 0);
		IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, uintStringDictionary, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "Add"));
		IMethodDefOrRef tryGetValue = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, uintStringDictionary, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "TryGetValue"));

		FieldDefinition field = type.AddField(uintStringDictionary, "s_FieldPathDictionary", true, FieldVisibility.Private);
		field.Attributes |= FieldAttributes.InitOnly;
		DocumentationHandler.AddFieldDefinitionLine(field, $"CRC32 field path hashes for {SeeXmlTagGenerator.MakeCRef(group.Interface)} classes.");

		//Static constructor
		{
			MethodDefinition staticConstructor = type.GetOrCreateStaticConstructor();
			CilInstructionCollection processor = staticConstructor.GetProcessor();
			processor.Pop();//The return instruction.
			processor.Add(CilOpCodes.Newobj, dictionaryConstructor);
			foreach ((uint hash, string str) in hashes)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldc_I4, (int)hash);
				processor.Add(CilOpCodes.Ldstr, str);
				processor.Add(CilOpCodes.Call, addMethod);
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();
		}

		//Method
		{
			MethodDefinition method = type.AddMethod(MethodName, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, SharedState.Instance.Importer.Boolean);
			Parameter hashParameter = method.AddParameter(SharedState.Instance.Importer.UInt32, "hash");
			Parameter outParameter = method.AddParameter(SharedState.Instance.Importer.String.MakeByReferenceType(), "path");
			ParameterDefinition outParameterDefinition = outParameter.Definition!;
			outParameterDefinition.Attributes |= ParameterAttributes.Out;
			outParameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
			outParameterDefinition.AddCustomAttribute(SharedState.Instance.Importer.ImportConstructor<NotNullWhenAttribute>(1), SharedState.Instance.Importer.Boolean, true);

			CilInstructionCollection processor = method.GetProcessor();
			processor.Add(CilOpCodes.Ldsfld, field);
			processor.Add(CilOpCodes.Ldarg_0);//hash
			processor.Add(CilOpCodes.Ldarg_1);//path
			processor.Add(CilOpCodes.Callvirt, tryGetValue);
			processor.Add(CilOpCodes.Ret);

			DocumentationHandler.AddMethodDefinitionLine(method, $"Try get field path from a {SeeXmlTagGenerator.MakeCRef(group.Interface)} class for a CRC32 hash.");

			return method;
		}
	}

	private static void MakeFieldPathsTypeForGroup(ClassGroupBase group)
	{
		TypeDefinition type = group.GetOrCreateMainClass();

		GenericInstanceTypeSignature readonlyStringList = SharedState.Instance.Importer.ImportType(typeof(IReadOnlyList<>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.String);
		GenericInstanceTypeSignature stringList = SharedState.Instance.Importer.ImportType(typeof(List<>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.String);
		IMethodDefOrRef listConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, stringList, 0);
		IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, stringList, SharedState.Instance.Importer.LookupMethod(typeof(List<>), m => m.Name == "Add"));

		const string propertyName = "FieldPaths";//Not currently in use by any subclasses.
		FieldDefinition field = type.AddField(readonlyStringList, $"<{propertyName}>k__BackingField", true, FieldVisibility.Private);
		field.Attributes |= FieldAttributes.InitOnly;
		field.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

		MethodDefinition staticConstructor = type.GetOrCreateStaticConstructor();
		CilInstructionCollection processor = staticConstructor.GetProcessor();
		processor.Pop();//The return instruction.
		bool emittedListConstructor = false;
		foreach (string str in group.GetOrderedFieldPaths())
		{
			if (!emittedListConstructor)
			{
				processor.Add(CilOpCodes.Newobj, listConstructor);
				emittedListConstructor = true;
			}
			processor.Add(CilOpCodes.Dup);
			processor.Add(CilOpCodes.Ldstr, str);
			processor.Add(CilOpCodes.Call, addMethod);
		}
		if (!emittedListConstructor)
		{
			MethodSpecification emptyStringArray = SharedState.Instance.Importer.ImportMethod<Array>(method => method.Name == nameof(Array.Empty))
				.MakeGenericInstanceMethod(SharedState.Instance.Importer.String);
			processor.Add(CilOpCodes.Call, emptyStringArray);
		}
		processor.Add(CilOpCodes.Stsfld, field);
		processor.Add(CilOpCodes.Ret);

		PropertyDefinition property = type.ImplementGetterProperty(
				propertyName,
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName,
				readonlyStringList,
				field);
		property.GetMethod!.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);
		DocumentationHandler.AddPropertyDefinitionLine(property, $"List of field paths for {SeeXmlTagGenerator.MakeCRef(group.Interface)} classes.");
	}

	private static List<(ClassGroup, Dictionary<uint, string>)> HashAllFieldPaths()
	{
		Dictionary<GeneratedClassInstance, Dictionary<uint, string>> instanceDictionary = new();
		HashSet<GeneratedClassInstance> fullyImplemented = new();
		foreach (GeneratedClassInstance instance in SharedState.Instance.ClassGroups.Values.SelectMany(g => g.Instances))
		{
			Dictionary<uint, string> hashes = GetFieldPaths(instance.Class).Distinct().ToDictionary(CrcUtils.CalculateDigestUTF8, str => str);
			if (!instance.Class.IsAbstract)
			{
				fullyImplemented.Add(instance);
			}
			instanceDictionary.Add(instance, hashes);
		}
		while (fullyImplemented.Count < instanceDictionary.Count)
		{
			foreach ((GeneratedClassInstance instance, Dictionary<uint, string> hashes) in instanceDictionary)
			{
				if (fullyImplemented.Contains(instance))
				{
					continue;
				}
				if (instance.Derived.All(fullyImplemented.Contains))
				{
					foreach ((uint hash, string path) in instance.Derived.Select(derived => instanceDictionary[derived]).SelectMany(pair => pair))
					{
						hashes.TryAdd(hash, path);
					}
					fullyImplemented.Add(instance);
				}
			}
		}
		List<(ClassGroup, Dictionary<uint, string>)> groupList = new();
		foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values.OrderBy(g => g.ID))
		{
			Dictionary<uint, string> groupHashes = new();
			foreach ((uint hash, string path) in group.Instances.Select(instance => instanceDictionary[instance]).SelectMany(pair => pair).OrderBy(pair => pair.Value))
			{
				groupHashes.TryAdd(hash, path);
			}
			groupList.Add((group, groupHashes));
		}
		return groupList;
	}

	private static IOrderedEnumerable<string> GetOrderedFieldPaths(this ClassGroupBase group)
	{
		return group
			.Classes
			.SelectMany(GetFieldPaths)
			.Distinct()
			.Order();
	}

	private static IEnumerable<string> GetFieldPaths(UniversalClass c)
	{
		if (c.ReleaseRootNode is null)
		{
			Debug.Assert(c.EditorRootNode is not null);
			return GetFieldPaths(c.EditorRootNode);
		}
		else if (c.EditorRootNode is null)
		{
			return GetFieldPaths(c.ReleaseRootNode);
		}
		else
		{
			return GetFieldPaths(c.ReleaseRootNode).Concat(GetFieldPaths(c.EditorRootNode));
		}
	}

	private static IEnumerable<string> GetFieldPaths(UniversalNode rootNode)
	{
		List<string> result = new();
		Stack<(UniversalNode, int, string)> nodeStack = new();
		for (int i = rootNode.SubNodes.Count - 1; i >= 0; i--)
		{
			UniversalNode child = rootNode.SubNodes[i];
			NodeType childType = child.NodeType;
			if (childType is NodeType.Type)
			{
				if (child.TypeName is Pass002_RenameSubnodes.Utf8StringName)
				{
					result.Add(child.OriginalName);
				}
				else if (IsNotPPtr(child))
				{
					nodeStack.Push((child, 0, child.OriginalName));
				}
			}
			else if (childType.IsPrimitive())
			{
				result.Add(child.OriginalName);
			}
		}

		while (nodeStack.Count > 0)
		{
			(UniversalNode parent, int childIndex, string parentPath) = nodeStack.Pop();
			while (true)
			{
				if (childIndex >= parent.SubNodes.Count)
				{
					break;
				}
				UniversalNode child = parent.SubNodes[childIndex];
				childIndex++;
				NodeType childType = child.NodeType;
				if (childType is NodeType.Type)
				{
					nodeStack.Push((parent, childIndex, parentPath));
					nodeStack.Push((child, 0, $"{parentPath}.{child.OriginalName}"));
					break;
				}
				else if (childType.IsPrimitive())
				{
					result.Add($"{parentPath}.{child.OriginalName}");
				}
			}
		}
		return result;

		static bool IsNotPPtr(UniversalNode child)
		{
			return child.SubNodes.Count != 2 || child.SubNodes[0].OriginalName is not "m_FileID" || child.SubNodes[1].OriginalName is not "m_PathID";
		}
	}
}
