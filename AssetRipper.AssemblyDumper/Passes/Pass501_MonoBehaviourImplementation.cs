using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;
using AssetRipper.AssemblyDumper.InjectedTypes;
using AssetRipper.Assets;

namespace AssetRipper.AssemblyDumper.Passes
{
	/// <summary>
	/// Adds to the IMonoBehaviour and IScriptedImporter interfaces. Also fixes the read and yaml methods
	/// </summary>
	public static class Pass501_MonoBehaviourImplementation
	{
		public static void DoPass()
		{
			TypeDefinition monoBehaviourHelperType = SharedState.Instance.InjectHelperType(typeof(MonoBehaviourHelper));
			TypeSignature propertyType = SharedState.Instance.Importer.ImportTypeSignature<IUnityAssetBase>();

			//MonoBehaviour
			ApplyChangesToGroup(SharedState.Instance.ClassGroups[114], monoBehaviourHelperType, propertyType);

			//ScriptedImporter
			ApplyChangesToGroup(SharedState.Instance.ClassGroups[2089858483], monoBehaviourHelperType, propertyType);
		}

		private static void ApplyChangesToGroup(ClassGroup group, TypeDefinition monoBehaviourHelperType, TypeSignature propertyType)
		{
			const string propertyName = "Structure";
			const string fieldName = "m_" + propertyName;

			group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType)
				.AddNullableAttributesForMaybeNull();

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				FieldDefinition structureField = instance.Type.AddField(propertyType, fieldName, visibility: FieldVisibility.Internal);
				structureField
					.AddNullableAttributesForMaybeNull()
					.AddDebuggerBrowsableNeverAttribute();

				PropertyDefinition property = instance.Type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, null, structureField)
					.AddNullableAttributesForMaybeNull();

				DocumentationHandler.AddPropertyDefinitionLine(property, "The custom structure of this asset, based on the instance fields of its MonoScript.");

				instance.Type.FixExportMethods(structureField, monoBehaviourHelperType);

				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Editor");
				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Release");
				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Standard");
			}
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

		private static void AddWalkStructure(this TypeDefinition type, FieldDefinition field, TypeDefinition monoBehaviourHelperType, string walkType)
		{
			string targetMethodName = walkType switch
			{
				"Editor" => nameof(UnityAssetBase.WalkEditor),
				"Release" => nameof(UnityAssetBase.WalkRelease),
				"Standard" => nameof(UnityAssetBase.WalkStandard),
				_ => throw new ArgumentException(null, nameof(walkType)),
			};

			MethodDefinition method = type.Methods.Single(m => m.Name == targetMethodName);

			string injectedMethodName = walkType switch
			{
				"Editor" => nameof(MonoBehaviourHelper.MaybeWalkStructureEditor),
				"Release" => nameof(MonoBehaviourHelper.MaybeWalkStructureRelease),
				"Standard" => nameof(MonoBehaviourHelper.MaybeWalkStructureStandard),
				_ => throw new ArgumentException(null, nameof(walkType)),
			};

			IMethodDefOrRef walkStructureMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == injectedMethodName);

			CilInstructionCollection processor = method.CilMethodBody!.Instructions;

			int insertIndex = FindLastNop(processor) + 1;

			//Insert the opcodes in reverse order, so that we don't have to adjust the insert index.
			processor.Insert(insertIndex, CilOpCodes.Call, walkStructureMethod);
			processor.Insert(insertIndex, CilOpCodes.Ldarg_1);//walker
			processor.Insert(insertIndex, CilOpCodes.Ldfld, field);//the structure field
			processor.Insert(insertIndex, CilOpCodes.Ldarg_0);//this
			processor.Insert(insertIndex, CilOpCodes.Ldarg_0);//this

			static int FindLastNop(CilInstructionCollection processor)
			{
				for (int i = processor.Count - 1; i >= 0; i--)
				{
					if (processor[i].OpCode == CilOpCodes.Nop)
					{
						return i;
					}
				}
				throw new Exception("No nop found");
			}
		}
	}
}
