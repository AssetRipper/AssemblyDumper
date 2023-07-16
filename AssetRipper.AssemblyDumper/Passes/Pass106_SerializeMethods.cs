using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.IO.Serialization;
using System.Text.Json.Nodes;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass106_SerializeMethods
{
	private const string SerializeReleaseFields = nameof(UnityAssetBase.SerializeReleaseFields);
	private const string SerializeEditorFields = nameof(UnityAssetBase.SerializeEditorFields);
	private const string SerializeAllFields = nameof(UnityAssetBase.SerializeAllFields);
	private static string SerializeMethodName => EmittingRelease is null
		? SerializeAllFields
		: EmittingRelease.Value
			? SerializeReleaseFields
			: SerializeEditorFields;

	/// <summary>
	/// True: <see cref="UnityAssetBase.SerializeReleaseFields"/><br/>
	/// False: <see cref="UnityAssetBase.SerializeEditorFields"/><br/>
	/// Null: <see cref="UnityAssetBase.SerializeAllFields"/>
	/// </summary>
	private static bool? EmittingRelease { get; set; }

	private static readonly Dictionary<string, IMethodDescriptor> methodDictionary = new();

	/// <summary>
	/// Uses original names for robustness and clarity
	/// </summary>
	/// <remarks>
	/// These fields are excluded from meta files even though they don't have flags indicating that.
	/// </remarks>
	private static readonly HashSet<string> AdditionalFieldsToSkipInImporters = new()
	{
		"m_ObjectHideFlags",
		"m_ExtensionPtr",
		"m_PrefabParentObject",
		"m_CorrespondingSourceObject",
		"m_PrefabInternal",
		"m_PrefabAsset",
		"m_PrefabInstance",
	};

#nullable disable
	private static IMethodDefOrRef trySerializeAssetMethod;
	private static IMethodDefOrRef trySerializePPtrMethod;

	private static TypeSignature jsonNodeSignature;
#nullable enable

	public static void DoPass()
	{
		methodDictionary.Clear();
		Initialize();
		EmittingRelease = false;
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				bool isImporter = instance.InheritsFromAssetImporter();
				instance.FillEditorMethod(isImporter);
			}
		}
		CreateHelperClass();
		methodDictionary.Clear();

		EmittingRelease = true;
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				bool isImporter = instance.InheritsFromAssetImporter();
				instance.FillReleaseMethod(isImporter);
			}
		}
		CreateHelperClass();
		methodDictionary.Clear();

		EmittingRelease = null;
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				instance.FillAllFieldsMethod();
			}
		}
		CreateHelperClass();
		methodDictionary.Clear();
	}

	private static void Initialize()
	{
		trySerializeAssetMethod = SharedState.Instance.Importer.ImportMethod<IUnityAssetSerializer>(m => m.Signature!.GenericParameterCount == 1);
		trySerializePPtrMethod = SharedState.Instance.Importer.ImportMethod<IUnityAssetSerializer>(m => m.Signature!.GenericParameterCount == 2);

		jsonNodeSignature = SharedState.Instance.Importer.ImportType<JsonNode>().ToTypeSignature();
	}

	private static void CreateHelperClass()
	{
		TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.HelpersNamespace, $"{SerializeMethodName}Helpers");
		type.IsPublic = false;
		foreach ((string _, IMethodDescriptor method) in methodDictionary.OrderBy(pair => pair.Key))
		{
			if (method is MethodDefinition methodDefinition && methodDefinition.DeclaringType is null)
			{
				type.Methods.Add(methodDefinition);
			}
		}
		Console.WriteLine($"\t{type.Methods.Count} {SerializeMethodName} helper methods");
	}

	private static bool GetActualIgnoreInMetaFiles(this UniversalNode node)
	{
		return node.IgnoreInMetaFiles || AdditionalFieldsToSkipInImporters.Contains(node.OriginalName);
	}

	private static void FillEditorMethod(this GeneratedClassInstance instance, bool isImporter)
	{
		instance.FillWithDefault();
	}

	private static void FillReleaseMethod(this GeneratedClassInstance instance, bool isImporter)
	{
		instance.FillWithDefault();
	}

	private static void FillAllFieldsMethod(this GeneratedClassInstance instance)
	{
		instance.FillWithDefault();
	}

	private static void FillWithDefault(this GeneratedClassInstance instance)
	{
		MethodDefinition method = instance.Type.GetMethodByName(SerializeMethodName);
		CilInstructionCollection processor = method.GetProcessor();

		//if (serializer.TrySerialize(this, options, out JsonNode jsonNode))
		//    return jsonNode;
		{
			CilInstructionLabel jumpTarget = new();
			CilLocalVariable jsonNodeLocal = processor.AddLocalVariable(jsonNodeSignature);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Ldloca, jsonNodeLocal);
			IMethodDescriptor serializeMethodToCall;
			if (instance.Group.IsPPtr)
			{
				TypeDefinition parameterType = Pass080_PPtrConversions.PPtrsToParameters[instance.Type];
				serializeMethodToCall = trySerializePPtrMethod.MakeGenericInstanceMethod(instance.Type.ToTypeSignature(), parameterType.ToTypeSignature());
			}
			else
			{
				serializeMethodToCall = trySerializeAssetMethod.MakeGenericInstanceMethod(instance.Type.ToTypeSignature());
			}
			processor.Add(CilOpCodes.Callvirt, serializeMethodToCall);
			processor.Add(CilOpCodes.Brfalse, jumpTarget);
			processor.Add(CilOpCodes.Ldloc, jsonNodeLocal);
			processor.Add(CilOpCodes.Ret);
			jumpTarget.Instruction = processor.Add(CilOpCodes.Nop);
		}

		processor.Add(CilOpCodes.Ldnull);
		processor.Add(CilOpCodes.Ret);
	}
}
