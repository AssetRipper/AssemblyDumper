using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.DataStructures;
using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass055_CreateEnumProperties
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
				{
					if (interfaceProperty.TryGetEnumFullName(out string? fullName) && Pass040_AddEnums.EnumDictionary.TryGetValue(fullName, out (TypeDefinition, EnumHistory) tuple))
					{
						CreateEnumProperty(group, interfaceProperty, tuple.Item1);
					}
				}
			}
		}

		private static void CreateEnumProperty(ClassGroupBase group, InterfaceProperty interfaceProperty, TypeDefinition enumType)
		{
			ElementType enumElementType = ((CorLibTypeSignature)enumType.GetFieldByName("value__").Signature!.FieldType).ElementType;
			string propertyName = $"{interfaceProperty.Definition.Name}E";

			interfaceProperty.SpecialDefinition = group.Interface.AddFullProperty(
				propertyName,
				InterfaceUtils.InterfacePropertyDeclaration,
				enumType.ToTypeSignature());

			foreach (ClassProperty classProperty in interfaceProperty.Implementations)
			{
				if (classProperty.BackingField?.Signature?.FieldType is CorLibTypeSignature fieldTypeSignature
					&& fieldTypeSignature.ElementType.IsFixedSizeInteger())
				{
					classProperty.SpecialDefinition = classProperty.Class.Type.AddFullProperty(
						propertyName,
						InterfaceUtils.InterfacePropertyImplementation,
						enumType.ToTypeSignature());
					classProperty.SpecialDefinition.GetMethod!.GetProcessor().FillGetter(classProperty.BackingField, fieldTypeSignature.ElementType, enumElementType);
					classProperty.SpecialDefinition.SetMethod!.GetProcessor().FillSetter(classProperty.BackingField, fieldTypeSignature.ElementType, enumElementType);
					if (classProperty.Class.Type.IsAbstract)
					{
						classProperty.SpecialDefinition.AddDebuggerBrowsableNeverAttribute();//Properties in base classes are redundant in the debugger.
					}
				}
				else
				{
					classProperty.SpecialDefinition = classProperty.Class.Type.ImplementFullProperty(
						propertyName,
						InterfaceUtils.InterfacePropertyImplementation,
						enumType.ToTypeSignature(),
						null);
					classProperty.SpecialDefinition.AddDebuggerBrowsableNeverAttribute();//Dummy properties should not be visible in the debugger.
				}
			}
		}

		private static bool TryGetEnumFullName(this InterfaceProperty interfaceProperty, [NotNullWhen(true)] out string? fullName)
		{
			if (interfaceProperty.TryGetOverridenEnumFullName(out string? overriden))
			{
				fullName = overriden;
			}
			else if (interfaceProperty.History is not null && interfaceProperty.History.TypeFullName.Count == 1)
			{
				fullName = interfaceProperty.History.TypeFullName[0].Value.ToString();
			}
			else
			{
				fullName = null;
			}
			return fullName is not null;
		}

		private static bool TryGetOverridenEnumFullName(this InterfaceProperty interfaceProperty, out string? fullName)
		{
			if (interfaceProperty.Group switch { _ => false })
			{
				fullName = null;
				return true;
			}

			fullName = interfaceProperty.Group switch
			{
				ClassGroup classGroup => classGroup.ID switch
				{
					1 when interfaceProperty.Name is "StaticEditorFlags_C1" => "UnityEditor.StaticEditorFlags",
					117 => interfaceProperty.Name switch
					{
						"ColorSpace_C117" => "UnityEngine.ColorSpace",
						"Dimension_C117" => "UnityEngine.Rendering.TextureDimension",
						"LightmapFormat_C117" => "UnityEditor.TextureUsageMode",
						_ => null,
					},
					187 when interfaceProperty.Name is "ColorSpace_C187" => "UnityEngine.ColorSpace",
					1006 => interfaceProperty.Name switch
					{
						"Alignment_C1006" => "UnityEngine.SpriteAlignment",
						"AlphaUsage_C1006" => "UnityEditor.TextureImporterAlphaSource",
						"SpriteMeshType_C1006" => "UnityEngine.SpriteMeshType",
						_ => null,
					},
					_ => null,
				},
				SubclassGroup subclassGroup => subclassGroup.Name switch
				{
					"SubMesh" when interfaceProperty.Name is "Topology" or "IsTriStrip" => "UnityEngine.MeshTopology",
					"TextureImporterBumpMapSettings" when interfaceProperty.Name is "NormalMapFilter" => "UnityEditor.TextureImporterNormalFilter",
					"TextureImporterMipMapSettings" when interfaceProperty.Name is "MipMapMode" => "UnityEditor.TextureImporterMipFilter",
					"TextureSettings" => interfaceProperty.Name switch
					{
						"FilterMode" => "UnityEngine.FilterMode",
						"TextureCompression" => "UnityEditor.TextureImporterCompression",
						_ => null,
					},
					"TierGraphicsSettings" => interfaceProperty.Name switch
					{
						"HdrMode" => "UnityEngine.Rendering.CameraHDRMode",
						"RealtimeGICPUUsage" => "UnityEngine.Rendering.RealtimeGICPUUsage",
						"RenderingPath" => "UnityEngine.RenderingPath",
						_ => null,
					},
					"TierGraphicsSettingsEditor" => interfaceProperty.Name switch
					{
						"HdrMode" => "UnityEngine.Rendering.CameraHDRMode",
						"RealtimeGICPUUsage" => "UnityEngine.Rendering.RealtimeGICPUUsage",
						"RenderingPath" => "UnityEngine.RenderingPath",
						"StandardShaderQuality" => "UnityEditor.Rendering.ShaderQuality",
						_ => null,
					},
					"TierSettings" when interfaceProperty.Name is "Tier" => "UnityEngine.Rendering.GraphicsTier",
					"TransitionConstant" when interfaceProperty.Name is "InterruptionSource" => "UnityEditor.Animations.TransitionInterruptionSource",
					"UVModule" => interfaceProperty.Name switch
					{
						"AnimationType" => "UnityEngine.ParticleSystemAnimationType",
						"Mode" => "UnityEngine.ParticleSystemAnimationMode",
						"RowMode" => "UnityEngine.ParticleSystemAnimationRowMode",
						"TimeMode" => "UnityEngine.ParticleSystemAnimationTimeMode",
						_ => null,
					},
					"ValueConstant" when interfaceProperty.Name is "Type" => "UnityEngine.AnimatorControllerParameterType",
					"VariantInfo" when interfaceProperty.Name is "PassType" => "UnityEngine.Rendering.PassType",
					_ => null,
				},
				_ => throw new(),
			};
			return fullName is not null;
		}

		private static CilInstruction? AddConversion(this CilInstructionCollection processor, ElementType from, ElementType to)
		{
			if (from == to)
			{
				return null;
			}

			CilOpCode opCode = to switch
			{
				//ElementType.I1 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I1 : CilOpCodes.Conv_Ovf_I1_Un,
				//ElementType.U1 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U1 : CilOpCodes.Conv_Ovf_U1_Un,
				//ElementType.I2 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I2 : CilOpCodes.Conv_Ovf_I2_Un,
				//ElementType.U2 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U2 : CilOpCodes.Conv_Ovf_U2_Un,
				//ElementType.I4 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I4 : CilOpCodes.Conv_Ovf_I4_Un,
				//ElementType.U4 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U4 : CilOpCodes.Conv_Ovf_U4_Un,
				//ElementType.I8 => from.IsSigned() ? CilOpCodes.Conv_Ovf_I8 : CilOpCodes.Conv_Ovf_I8_Un,
				//ElementType.U8 => from.IsSigned() ? CilOpCodes.Conv_Ovf_U8 : CilOpCodes.Conv_Ovf_U8_Un,
				ElementType.I1 => CilOpCodes.Conv_I1,
				ElementType.U1 => CilOpCodes.Conv_U1,
				ElementType.I2 => CilOpCodes.Conv_I2,
				ElementType.U2 => CilOpCodes.Conv_U2,
				ElementType.I4 => CilOpCodes.Conv_I4,
				ElementType.U4 => CilOpCodes.Conv_U4,
				ElementType.I8 => CilOpCodes.Conv_I8,
				ElementType.U8 => CilOpCodes.Conv_U8,
				_ => throw new ArgumentOutOfRangeException(nameof(to)),
			};

			return processor.Add(opCode);
		}

		private static void FillGetter(this CilInstructionCollection processor, FieldDefinition field, ElementType fieldType, ElementType enumType)
		{
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.AddConversion(fieldType, enumType);
			processor.Add(CilOpCodes.Ret);
		}

		private static void FillSetter(this CilInstructionCollection processor, FieldDefinition field, ElementType fieldType, ElementType enumType)
		{
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1);
			processor.AddConversion(enumType, fieldType);
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
