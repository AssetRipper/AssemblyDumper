using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core;
using AssetRipper.Core.Interfaces;
using AssetRipper.Core.IO.Asset;

namespace AssetRipper.AssemblyDumper.Passes
{
	/// <summary>
	/// Implements the IMonoBehaviour interface. Also fixes the read and yaml methods
	/// </summary>
	public static class Pass501_MonoBehaviourImplementation
	{
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static TypeSignature serializableStructureSignature;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		public static void DoPass()
		{
			serializableStructureSignature = SharedState.Instance.Importer.ImportTypeSignature<AssetRipper.Core.Structure.Assembly.Serializable.SerializableStructure>();

			InjectHelpers(out TypeDefinition monoBehaviourHelperType, out TypeDefinition monoScriptHelperType);

			TypeDefinition monoScriptInterface = SharedState.Instance.ClassGroups[115].Interface;
			ClassGroup group = SharedState.Instance.ClassGroups[114];
			group.Interface.AddInterfaceImplementation<IMonoBehaviourBase>(SharedState.Instance.Importer);

			MethodDefinition readStructureMethod = monoBehaviourHelperType.MakeReadStructureMethod(group.Interface, monoScriptInterface);

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				FieldDefinition structureField = instance.Type.AddStructureField();
				instance.Type.ImplementFullProperty(nameof(IMonoBehaviourBase.Structure), InterfacePropertyImplementationAttributes, null, structureField);
				instance.Type.FixReadMethods(readStructureMethod);
				instance.Type.FixExportMethods(structureField, monoBehaviourHelperType);
				//instance.Type.FixFetchDependencies(structureField, monoBehaviourHelperType);
			}
		}

		private static FieldDefinition AddStructureField(this TypeDefinition type)
		{
			FieldDefinition result = new FieldDefinition("m_Structure", FieldAttributes.Private, new FieldSignature(serializableStructureSignature));
			type.Fields.Add(result);
			return result;
		}

		private static void FixReadMethods(this TypeDefinition type, MethodDefinition readStructureMethod)
		{
			MethodDefinition readRelease = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadRelease));
			readRelease.AddReadStructure(readStructureMethod);
			MethodDefinition readEditor = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ReadEditor));
			readEditor.AddReadStructure(readStructureMethod);
		}

		private static void AddReadStructure(this MethodDefinition method, MethodDefinition readStructureMethod)
		{
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop();//Remove the return
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Call, readStructureMethod);
			processor.Add(CilOpCodes.Ret);
		}

		private static void FixExportMethods(this TypeDefinition type, FieldDefinition field, TypeDefinition monoBehaviourHelperType)
		{
			MethodDefinition exportRelease = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlRelease));
			exportRelease.AddExportStructureYaml(field, monoBehaviourHelperType);
			MethodDefinition exportEditor = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.ExportYamlEditor));
			exportEditor.AddExportStructureYaml(field, monoBehaviourHelperType);
		}

		private static void AddExportStructureYaml(this MethodDefinition method, FieldDefinition field, TypeDefinition monoBehaviourHelperType)
		{
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop();//Remove the return
			processor.Pop();//Remove the mapping node load
			IMethodDefOrRef exportStructureMethod = monoBehaviourHelperType.Methods
				.Single(m => m.Name == nameof(MonoBehaviourHelper.MaybeExportYamlForStructure));
			processor.Add(CilOpCodes.Ldarg_0);//this
			processor.Add(CilOpCodes.Ldfld, field);//the structure field
			processor.Add(CilOpCodes.Ldloc_0);//mapping node
			processor.Add(CilOpCodes.Ldarg_1);//container
			processor.Add(CilOpCodes.Call, exportStructureMethod);
			processor.Add(CilOpCodes.Ldloc_0);//mapping node
			processor.Add(CilOpCodes.Ret);
		}

		private static void FixFetchDependencies(this TypeDefinition type, FieldDefinition field, TypeDefinition monoBehaviourHelperType)
		{
			MethodDefinition method = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.FetchDependencies));
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop();//Remove the return
			IMethodDefOrRef fetchStructureMethod = monoBehaviourHelperType.Methods
				.Single(m => m.Name == nameof(MonoBehaviourHelper.MaybeFetchDependenciesForStructure));

			processor.Add(CilOpCodes.Ldarg_0);//this
			processor.Add(CilOpCodes.Ldfld, field);//the structure field
			processor.Add(CilOpCodes.Ldarg_1);//context
			processor.Add(CilOpCodes.Call, fetchStructureMethod);
			processor.Add(CilOpCodes.Call, Pass103_FillDependencyMethods.unityObjectBasePPtrListAddRange);
			processor.Add(CilOpCodes.Ldloc_0);
			processor.Add(CilOpCodes.Ret);
		}

		private static void InjectHelpers(out TypeDefinition monoBehaviourHelperType, out TypeDefinition monoScriptHelperType)
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(MonoScriptHelper))!, true);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(MonoBehaviourHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			foreach (TypeDefinition type in result.ClonedTopLevelTypes)
			{
				type.Namespace = SharedState.HelpersNamespace;
				SharedState.Instance.Module.TopLevelTypes.Add(type);
			}
			monoBehaviourHelperType = result.ClonedTopLevelTypes.Single(t => t.Name == nameof(MonoBehaviourHelper));
			monoScriptHelperType = result.ClonedTopLevelTypes.Single(t => t.Name == nameof(MonoScriptHelper));
		}

		private static MethodDefinition MakeReadStructureMethod(
			this TypeDefinition monoBehaviourHelperType,
			TypeDefinition monoBehaviourInterface,
			TypeDefinition monoScriptInterface)
		{
			MethodDefinition method = monoBehaviourHelperType.AddMethod("MaybeReadStructure", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, SharedState.Instance.Importer.Void);
			method.AddParameter(monoBehaviourInterface.ToTypeSignature(), "monoBehaviour");
			method.AddParameter(SharedState.Instance.Importer.ImportTypeSignature<AssetReader>(), "reader");

			CilInstructionCollection processor = method.GetProcessor();

			//if assembly manager not set, return
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Call, monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.IsAssemblyManagerSet)));
			CilInstructionLabel assemblyManagerSetJumpPoint = new CilInstructionLabel();
			processor.Add(CilOpCodes.Brtrue, assemblyManagerSetJumpPoint);
			processor.Add(CilOpCodes.Ret);
			assemblyManagerSetJumpPoint.Instruction = processor.Add(CilOpCodes.Nop);

			//create local for the IUnityObjectBase script
			TypeSignature iunityObjectBaseReference = SharedState.Instance.Importer.ImportTypeSignature<IUnityObjectBase>();
			CilLocalVariable objectLocal = new CilLocalVariable(iunityObjectBaseReference);
			method.CilMethodBody!.LocalVariables.Add(objectLocal);

			//IUnityObjectBase unityObjectBase = FindAsset(monoBehaviour, monoBehaviour.Script_C114);
			MethodDefinition getScriptPPtrVirtualMethod = monoBehaviourInterface.Methods.Single(m => m.Name == "get_Script_C114");
			MethodDefinition findAssetMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.FindAsset));
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Callvirt, getScriptPPtrVirtualMethod);
			processor.Add(CilOpCodes.Call, findAssetMethod);
			processor.Add(CilOpCodes.Stloc, objectLocal);

			//if unityObjectBase == null return
			processor.Add(CilOpCodes.Ldloc, objectLocal);
			processor.Add(CilOpCodes.Ldnull);
			processor.Add(CilOpCodes.Cgt_Un);
			CilInstructionLabel objectNotNullJumpPoint = new CilInstructionLabel();
			processor.Add(CilOpCodes.Brtrue, objectNotNullJumpPoint);
			processor.Add(CilOpCodes.Ret);
			objectNotNullJumpPoint.Instruction = processor.Add(CilOpCodes.Nop);

			//create local for the IMonoScript script
			CilLocalVariable scriptLocal = new CilLocalVariable(monoScriptInterface.ToTypeSignature());
			method.CilMethodBody!.LocalVariables.Add(scriptLocal);

			//IMonoScript monoScript = (IMonoScript)unityObjectBase;
			processor.Add(CilOpCodes.Ldloc, objectLocal);
			processor.Add(CilOpCodes.Castclass, monoScriptInterface);
			processor.Add(CilOpCodes.Stloc, scriptLocal);

			MethodDefinition injectedMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.ReadStructureInjected));
			MethodDefinition getScriptAssemblyNameVirtualMethod = monoScriptInterface.Methods.Single(m => m.Name == "get_AssemblyName_C115");
			MethodDefinition getScriptNamespaceVirtualMethod = monoScriptInterface.Methods.Single(m => m.Name == "get_Namespace_C115");
			MethodDefinition getScriptClassNameVirtualMethod = monoScriptInterface.Methods.Single(m => m.Name == "get_ClassName_C115");
			MethodDefinition getScriptNameVirtualMethod = monoScriptInterface.Methods.Single(m => m.Name == "get_Name_C115");
			MethodDefinition getMonoBehaviourNameVirtualMethod = monoBehaviourInterface.Methods.Single(m => m.Name == "get_Name_C114");

			//add this for later
			processor.Add(CilOpCodes.Ldarg_0);

			//ReadStructureInjected call
			processor.Add(CilOpCodes.Ldloc, scriptLocal);
			processor.Add(CilOpCodes.Ldloc, scriptLocal);
			processor.Add(CilOpCodes.Callvirt, getScriptAssemblyNameVirtualMethod);
			processor.Add(CilOpCodes.Ldloc, scriptLocal);
			processor.Add(CilOpCodes.Callvirt, getScriptNamespaceVirtualMethod);
			processor.Add(CilOpCodes.Ldloc, scriptLocal);
			processor.Add(CilOpCodes.Callvirt, getScriptClassNameVirtualMethod);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.Add(CilOpCodes.Ldloc, scriptLocal);
			processor.Add(CilOpCodes.Callvirt, getScriptNameVirtualMethod);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Callvirt, getMonoBehaviourNameVirtualMethod);
			processor.Add(CilOpCodes.Call, injectedMethod);

			//set the structure
			IMethodDefOrRef setMonoBehaviourStructureVirtualMethod = SharedState.Instance.Importer.ImportMethod<IMonoBehaviourBase>(m => m.Name == $"set_{nameof(IMonoBehaviourBase.Structure)}");
			processor.Add(CilOpCodes.Callvirt, setMonoBehaviourStructureVirtualMethod);

			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return method;
		}
	}
}
