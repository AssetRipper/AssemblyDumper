using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.IO.Serialization;
using System.Text.Json.Nodes;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass107_DeserializeMethods
{
#nullable disable
	private static IMethodDefOrRef tryDeserializeAssetMethod;
	private static IMethodDefOrRef tryDeserializePPtrMethod;

	private static TypeSignature jsonNodeSignature;
#nullable enable
	public static void DoPass()
	{
		Initialize();
		foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
		{
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				instance.FillDeserializeMethod();
			}
		}
	}

	private static void Initialize()
	{
		tryDeserializeAssetMethod = SharedState.Instance.Importer.ImportMethod<IUnityAssetDeserializer>(m => m.Signature!.GenericParameterCount == 1);
		tryDeserializePPtrMethod = SharedState.Instance.Importer.ImportMethod<IUnityAssetDeserializer>(m => m.Signature!.GenericParameterCount == 2);

		jsonNodeSignature = SharedState.Instance.Importer.ImportType<JsonNode>().ToTypeSignature();
	}

	private static void FillDeserializeMethod(this GeneratedClassInstance instance)
	{
		MethodDefinition method = instance.Type.GetMethodByName(nameof(UnityAssetBase.Deserialize));
		CilInstructionCollection processor = method.GetProcessor();

		//if (deserializer.TryDeserialize(this, node, options))
		//    return jsonNode;
		{
			CilInstructionLabel jumpTarget = new();
			CilLocalVariable jsonNodeLocal = processor.AddLocalVariable(jsonNodeSignature);
			processor.Add(CilOpCodes.Ldarg_2);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldarg_3);
			IMethodDescriptor deserializeMethodToCall;
			if (instance.Group.IsPPtr)
			{
				TypeDefinition parameterType = Pass080_PPtrConversions.PPtrsToParameters[instance.Type];
				deserializeMethodToCall = tryDeserializePPtrMethod.MakeGenericInstanceMethod(instance.Type.ToTypeSignature(), parameterType.ToTypeSignature());
			}
			else
			{
				deserializeMethodToCall = tryDeserializeAssetMethod.MakeGenericInstanceMethod(instance.Type.ToTypeSignature());
			}
			processor.Add(CilOpCodes.Callvirt, deserializeMethodToCall);
			processor.Add(CilOpCodes.Brfalse, jumpTarget);
			processor.Add(CilOpCodes.Ret);
			jumpTarget.Instruction = processor.Add(CilOpCodes.Nop);
		}

		processor.Add(CilOpCodes.Ret);
	}
}