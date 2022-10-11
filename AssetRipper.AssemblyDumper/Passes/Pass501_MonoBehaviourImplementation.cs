using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.IO.Reading;

namespace AssetRipper.AssemblyDumper.Passes
{
	/// <summary>
	/// Implements the IMonoBehaviour interface. Also fixes the read and yaml methods
	/// </summary>
	public static class Pass501_MonoBehaviourImplementation
	{
		public static void DoPass()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[114];

			InjectHelpers(out TypeDefinition monoBehaviourHelperType);

			TypeSignature propertyType = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetBase>();
			const string propertyName = "Structure";
			const string fieldName = "m_" + propertyName;

			group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType);

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				FieldDefinition structureField = instance.Type.AddStructureField(fieldName, propertyType);
				instance.Type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, null, structureField);
				instance.Type.FixExportMethods(structureField, monoBehaviourHelperType);
			}
		}

		private static FieldDefinition AddStructureField(this TypeDefinition type, string fieldName, TypeSignature fieldType)
		{
			FieldDefinition result = new FieldDefinition(fieldName, FieldAttributes.Private, fieldType);
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

		private static void InjectHelpers(out TypeDefinition monoBehaviourHelperType)
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(MonoBehaviourHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			foreach (TypeDefinition type in result.ClonedTopLevelTypes)
			{
				type.Namespace = SharedState.HelpersNamespace;
				SharedState.Instance.Module.TopLevelTypes.Add(type);
			}
			monoBehaviourHelperType = result.ClonedTopLevelTypes.Single(t => t.Name == nameof(MonoBehaviourHelper));
		}
	}
}
