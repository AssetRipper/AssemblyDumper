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

			PropertyDefinition interfaceProperty = group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertyType)
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

				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Editor");
				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Release");
				instance.Type.AddWalkStructure(structureField, monoBehaviourHelperType, "Standard");

				instance.Type.AddStructureFetchDependencies(structureField, monoBehaviourHelperType);

				instance.Type.AddStructureReset(structureField, monoBehaviourHelperType);

				instance.Type.AddStructureCopyValues(structureField, interfaceProperty, monoBehaviourHelperType);
			}
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

		private static void AddStructureFetchDependencies(this TypeDefinition type, FieldDefinition field, TypeDefinition monoBehaviourHelperType)
		{
			MethodDefinition method = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.FetchDependencies));
			IMethodDefOrRef fetchDependenciesMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.MaybeAppendStructureDependencies));
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop(); //pop the return value
			processor.Add(CilOpCodes.Ldarg_0);//this
			processor.Add(CilOpCodes.Ldfld, field);//the structure field
			processor.Add(CilOpCodes.Call, fetchDependenciesMethod);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddStructureReset(this TypeDefinition type, FieldDefinition field, TypeDefinition monoBehaviourHelperType)
		{
			MethodDefinition method = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.Reset));
			IMethodDefOrRef resetMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.ResetStructure));
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop(); //pop the return value
			processor.Add(CilOpCodes.Ldarg_0);//this
			processor.Add(CilOpCodes.Ldfld, field);//the structure field
			processor.Add(CilOpCodes.Call, resetMethod);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddStructureCopyValues(this TypeDefinition type, FieldDefinition field, PropertyDefinition interfaceProperty, TypeDefinition monoBehaviourHelperType)
		{
			MethodDefinition method = type.Methods.Single(m => m.Name == nameof(UnityAssetBase.CopyValues) && m.IsFinal && m.Parameters.Count == 2);
			IMethodDefOrRef copyValuesMethod = monoBehaviourHelperType.Methods.Single(m => m.Name == nameof(MonoBehaviourHelper.CopyStructureValues));
			CilInstructionCollection processor = method.CilMethodBody!.Instructions;
			processor.Pop(); //pop the return value
			processor.Add(CilOpCodes.Ldarg_0);//this
			processor.Add(CilOpCodes.Ldflda, field);//the structure field
			processor.Add(CilOpCodes.Ldarg_1);//source
			processor.Add(CilOpCodes.Callvirt, interfaceProperty.GetMethod!);//the Structure property
			processor.Add(CilOpCodes.Ldarg_2);//converter
			processor.Add(CilOpCodes.Call, copyValuesMethod);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
