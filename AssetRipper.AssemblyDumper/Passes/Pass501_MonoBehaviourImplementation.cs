﻿using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;

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

			TypeDefinition monoBehaviourHelperType = SharedState.Instance.InjectHelperType(typeof(MonoBehaviourHelper));

			TypeSignature propertyType = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetBase>();
			const string propertyName = "Structure";
			const string fieldName = "m_" + propertyName;

			group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType)
				.AddNullableAttributesForMaybeNull();

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				FieldDefinition structureField = instance.Type.AddStructureField(fieldName, propertyType);
				instance.Type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, null, structureField)
					.AddNullableAttributesForMaybeNull();
				structureField.AddNullableAttributesForMaybeNull();

				instance.Type.FixExportMethods(structureField, monoBehaviourHelperType);
			}
		}

		private static FieldDefinition AddStructureField(this TypeDefinition type, string fieldName, TypeSignature fieldType)
		{
			FieldDefinition result = new FieldDefinition(fieldName, FieldAttributes.Private, fieldType);
			type.Fields.Add(result);
			return result;
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
	}
}
