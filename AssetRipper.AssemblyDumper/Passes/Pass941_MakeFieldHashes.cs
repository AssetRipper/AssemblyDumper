using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass941_MakeFieldHashes
{
	public static void DoPass()
	{
		Dictionary<int, Dictionary<uint, string>> dict = new();
		List<(int, FieldDefinition)> dictionaries = new();
		foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values.OrderBy(g => g.ID))
		{
			Dictionary<uint, string> hashes = group.GetOrderedFieldPaths().ToDictionary(str => CrcUtils.CalculateDigestUTF8(str), str => str);
			if (hashes.Count > 0)
			{
				dictionaries.Add((group.ID, MakeDictionaryForGroup(group, hashes)));
			}
			dict.Add(group.ID, hashes);
		}

		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "FieldHashes");
			type.AddNullableContextAttribute(NullableAnnotation.NotNull);
			MethodDefinition nullHelperMethod = MakeNullHelperMethod(type);

			MethodDefinition method = type.AddMethod("TryGetPath", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, SharedState.Instance.Importer.Boolean);
			Parameter idParameter = method.AddParameter(Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature(), "classID");
			Parameter hashParameter = method.AddParameter(SharedState.Instance.Importer.UInt32, "hash");
			Parameter outParameter = method.AddParameter(SharedState.Instance.Importer.String.MakeByReferenceType(), "path");
			ParameterDefinition outParameterDefinition = outParameter.Definition!;
			outParameterDefinition.Attributes |= ParameterAttributes.Out;
			outParameterDefinition.AddNullableAttribute(NullableAnnotation.MaybeNull);
			outParameterDefinition.AddCustomAttribute(SharedState.Instance.Importer.ImportConstructor<NotNullWhenAttribute>(1), SharedState.Instance.Importer.Boolean, true);

			CilInstructionCollection processor = method.GetProcessor();
			processor.EmitIdSwitchStatement(dictionaries, nullHelperMethod);
		}
	}

	private static void EmitIdSwitchStatement(this CilInstructionCollection processor, List<(int, FieldDefinition)> dictionaries, MethodDefinition nullHelperMethod)
	{
		GenericInstanceTypeSignature uintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
		IMethodDefOrRef tryGetValue = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, uintStringDictionary, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "TryGetValue"));
		int count = dictionaries.Count;

		CilLocalVariable switchCondition = processor.AddLocalVariable(Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature());

		processor.Add(CilOpCodes.Ldarg_0);//classID
		processor.Add(CilOpCodes.Stloc, switchCondition);

		CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count).Select(i => new CilInstructionLabel()).ToArray();
		CilInstructionLabel defaultNop = new CilInstructionLabel();
		for (int i = 0; i < count; i++)
		{
			processor.Add(CilOpCodes.Ldloc, switchCondition);
			processor.Add(CilOpCodes.Ldc_I4, dictionaries[i].Item1);
			processor.Add(CilOpCodes.Beq, nopInstructions[i]);
		}
		processor.Add(CilOpCodes.Br, defaultNop);
		for (int i = 0; i < count; i++)
		{
			nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);

			processor.Add(CilOpCodes.Ldsfld, dictionaries[i].Item2);
			processor.Add(CilOpCodes.Ldarg_1);//hash
			processor.Add(CilOpCodes.Ldarg_2);//path
			processor.Add(CilOpCodes.Callvirt, tryGetValue);
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

	private static FieldDefinition MakeDictionaryForGroup(ClassGroup group, Dictionary<uint, string> hashes)
	{
		TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, group.Namespace, $"{group.Name}FieldHashes");
		type.IsPublic = false;

		GenericInstanceTypeSignature uintStringDictionary = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
			.MakeGenericInstanceType(SharedState.Instance.Importer.UInt32, SharedState.Instance.Importer.String);
		IMethodDefOrRef dictionaryConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, uintStringDictionary, 0);
		IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, uintStringDictionary, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "Add"));

		FieldDefinition field = type.AddField(uintStringDictionary, "dictionary", true);
		field.Attributes |= FieldAttributes.InitOnly;

		MethodDefinition? staticConstructor = type.AddEmptyConstructor(true);
		CilInstructionCollection processor = staticConstructor.GetProcessor();
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
		return field;
	}

	private static IOrderedEnumerable<string> GetOrderedFieldPaths(this ClassGroup group)
	{
		return group
			.Classes
			.SelectMany(c =>
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
		})
			.Distinct()
			.Order();
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
				if (IsNotPPtr(child))
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
