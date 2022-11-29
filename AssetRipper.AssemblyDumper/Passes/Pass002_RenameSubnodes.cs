using AssetRipper.AssemblyDumper.Utils;
using System.Text.RegularExpressions;

namespace AssetRipper.AssemblyDumper.Passes
{
	public static class Pass002_RenameSubnodes
	{
		private static readonly Regex badCharactersRegex = new Regex(@"[<>\[\]\s&\(\):\.]", RegexOptions.Compiled);
		public const string GuidName = "GUID";
		public const string Utf8StringName = "Utf8String";
		public const string NestedStringName = "NestedString";
		public const string IntegerStringName = "IntegerString";
		private const string OffsetPtrName = "OffsetPtr";
		private const string KeyframeName = "Keyframe";
		private const string AnimationCurveName = "AnimationCurve";
		private const string ColorRGBAName = "ColorRGBA";
		private const string PackedBitVectorName = "PackedBitVector";
		private const string VFXEntryExposedName = "VFXEntryExposed";
		private const string VFXEntryExpressionValueName = "VFXEntryExpressionValue";
		private const string VFXFieldName = "VFXField";
		private const string VFXPropertySheetSerializedBaseName = "VFXPropertySheetSerializedBase";
		private const string TilemapRefCountedDataName = "TilemapRefCountedData";
		private const string SpriteAtlasAssetName = "SpriteAtlasAsset";
		private const string SpriteAtlasAssetDataName = "SpriteAtlasAssetData";
		private const string SpriteAtlasAssetImporterDataFieldName = "m_ImporterData";
		private const string Vector4FloatName = "Vector4Float";
		private const string Vector3FloatName = "Vector3Float";

		public static void DoPass()
		{
			foreach (VersionedList<UniversalClass> classList in SharedState.Instance.ClassInformation.Values)
			{
				foreach (UniversalClass? unityClass in classList.Values)
				{
					if (unityClass is not null)
					{
						unityClass.CorrectInheritedTypeNames();
						unityClass.EditorRootNode?.FixNamesRecursively();
						unityClass.ReleaseRootNode?.FixNamesRecursively();
						unityClass.EditorRootNode?.DoSecondaryRenamingRecursively(true);
						unityClass.ReleaseRootNode?.DoSecondaryRenamingRecursively(false);
					}
				}
			}
		}

		/// <summary>
		/// Corrects the root nodes of classes to have the correct Type Name.<br/>
		/// For example, Behaviour uses Component as its type name in the root nodes
		/// </summary>
		/// <param name="unityClass"></param>
		private static void CorrectInheritedTypeNames(this UniversalClass unityClass)
		{
			if (unityClass.EditorRootNode != null && unityClass.EditorRootNode.TypeName != unityClass.Name)
			{
				//Console.WriteLine($"Correcting editor type name from {unityClass.EditorRootNode.TypeName} to {unityClass.Name}");
				unityClass.EditorRootNode.TypeName = unityClass.Name;
				unityClass.EditorRootNode.OriginalTypeName = unityClass.Name;
			}
			if (unityClass.ReleaseRootNode != null && unityClass.ReleaseRootNode.TypeName != unityClass.Name)
			{
				//Console.WriteLine($"Correcting release type name from {unityClass.ReleaseRootNode.TypeName} to {unityClass.Name}");
				unityClass.ReleaseRootNode.TypeName = unityClass.Name;
				unityClass.ReleaseRootNode.OriginalTypeName = unityClass.Name;
			}
		}

		/// <summary>
		/// Fix all type and field names to be valid if decompiled<br/>
		/// For example, it uses a regex to replace invalid characters with an underscore, ie data[0] to data_0_
		/// </summary>
		/// <param name="node"></param>
		private static void FixNamesRecursively(this UniversalNode node)
		{
			node.Name = GetValidFieldName(node.Name!);
			if (node.NodeType == NodeType.Type) //don't rename special type names like long long, map, or Array
			{
				node.TypeName = GetValidTypeName(node.TypeName!);
			}
			if (node.SubNodes != null)
			{
				foreach (UniversalNode subnode in node.SubNodes)
				{
					subnode?.FixNamesRecursively();
				}
			}
		}

		/// <summary>
		/// Fixes the string to be a valid field name
		/// </summary>
		/// <param name="originalName"></param>
		/// <returns>A new string with the valid content</returns>
		public static string GetValidFieldName(string originalName)
		{
			if (string.IsNullOrWhiteSpace(originalName))
			{
				throw new ArgumentException("Nodes cannot have a null or whitespace name", nameof(originalName));
			}
			string result = originalName.ReplaceBadCharacters();
			if (char.IsDigit(result[0]) || !result.StartsWith("m_", StringComparison.Ordinal))
			{
				result = "m_" + result;
			}
			if (char.IsLower(result[2]))
			{
				string remaining = result.Length > 3 ? result.Substring(3) : "";
				result = $"m_{char.ToUpperInvariant(result[2])}{remaining}";
			}
			return result;
		}

		/// <summary>
		/// Fixes the string to be a valid type name
		/// </summary>
		/// <param name="originalName"></param>
		/// <returns>A new string with the valid content</returns>
		private static string GetValidTypeName(string originalName)
		{
			if (string.IsNullOrWhiteSpace(originalName))
			{
				throw new ArgumentException("Nodes cannot have a null or whitespace type name", nameof(originalName));
			}
			string result = originalName.ReplaceBadCharacters();
			if (char.IsDigit(result[0]))
			{
				result = "_" + result;
			}
			if (char.IsLower(result[0]) && result.Length > 1)
			{
				result = char.ToUpperInvariant(result[0]) + result.Substring(1);
			}
			return result;
		}

		private static string ReplaceBadCharacters(this string str) => badCharactersRegex.Replace(str, "_");

		private static void DoSecondaryRenamingRecursively(this UniversalNode node, bool isEditor)
		{
			if (node == null)
			{
				return;
			}

			if (node.SubNodes != null)
			{
				foreach (UniversalNode subnode in node.SubNodes)
				{
					subnode.DoSecondaryRenamingRecursively(isEditor);
				}
			}

			node.DoSecondaryRenaming(isEditor);
		}

		private static void DoSecondaryRenaming(this UniversalNode node, bool isEditor)
		{
			if (node.TypeName == "string")
			{
				ChangeStringToUtf8String(node);
			}
			else if (node.TypeName == OffsetPtrName)
			{
				node.TypeName = $"{OffsetPtrName}_{node.GetSubNodeByName("m_Data").TypeName}";
			}
			else if (node.TypeName == KeyframeName)
			{
				string valueTypeName = node.GetSubNodeByName("m_Value").TypeName;
				string elementTypeName = valueTypeName == "float" ? nameof(Single) : valueTypeName;
				node.TypeName = $"{KeyframeName}_{elementTypeName}";
			}
			else if (node.TypeName == AnimationCurveName)
			{
				if (node.TryGetSubNodeByTypeAndName("vector", "m_Curve", out UniversalNode? curveNode))
				{
					UniversalNode keyframeNode = curveNode.SubNodes[0].SubNodes[1];

					if (keyframeNode.TypeName.StartsWith($"{KeyframeName}_"))
					{
						string elementType = keyframeNode.TypeName.Substring(KeyframeName.Length + 1);
						node.TypeName = $"{AnimationCurveName}_{elementType}";
					}
				}
			}
			else if (node.TypeName == ColorRGBAName)
			{
				node.TypeName = node.SubNodes.Count switch
				{
					4 => $"{ColorRGBAName}f",
					1 => $"{ColorRGBAName}32",
					_ => throw new NotSupportedException(),
				};
			}
			else if (node.TypeName == PackedBitVectorName)
			{
				//The packed bit vectors are constant throughout all the unity versions and identifiable by their number of fields
				node.TypeName = node.SubNodes.Count switch
				{
					5 => $"{PackedBitVectorName}_Single",
					3 => $"{PackedBitVectorName}_Int32",
					2 => $"{PackedBitVectorName}_Quaternionf",
					_ => throw new NotSupportedException(),
				};
			}
			else if (node.IsVFXEntryExposed(out string? vfxEntryExposedElement))
			{
				node.TypeName = $"{VFXEntryExposedName}_{vfxEntryExposedElement}";
			}
			else if (node.IsVFXEntryExpressionValue(out string? vfxEntryExpressionValueElement))
			{
				node.TypeName = $"{VFXEntryExpressionValueName}_{vfxEntryExpressionValueElement}";
			}
			else if (node.IsVFXField(out string? vfxFieldElement))
			{
				node.TypeName = $"{VFXFieldName}_{vfxFieldElement}";
			}
			else if (node.IsVFXPropertySheetSerializedBase(out string? vfxPropertySheetElement))
			{
				node.TypeName = $"{VFXPropertySheetSerializedBaseName}_{vfxPropertySheetElement}";
			}
			else if (node.TypeName == TilemapRefCountedDataName)
			{
				if (node.TryGetSubNodeByName("m_Data", out UniversalNode? subnode))
				{
					string elementType = subnode.TypeName.ReplaceBadCharacters();
					node.TypeName = $"{TilemapRefCountedDataName}_{elementType}";
				}
			}
			else if (node.TypeName == "Blend2dDataConstant")
			{
				// On Unity 4 versions, Blend1d and Blend2d were initially both called Blend2d
				if (node.SubNodes.Count == 1 && node.SubNodes[0].Name == "m_ChildThresholdArray")
				{
					node.TypeName = "Blend1dDataConstant";
				}
			}
			else if (node.IsSpriteAtlasAsset(out UniversalNode? spriteAtlasAssetImporterDataFieldNode))
			{
				spriteAtlasAssetImporterDataFieldNode.TypeName = SpriteAtlasAssetDataName;
			}
			else if (node.IsLightProbes(out UniversalNode? bakedCoefficientsNode))
			{
				bakedCoefficientsNode.TypeName = "SphericalHarmonicsL2";
				foreach (UniversalNode child in bakedCoefficientsNode.SubNodes)
				{
					if (child.Name.Length == "m_Sh_0_".Length)
					{
						child.Name = $"m_Sh_{child.Name.Substring(4)}";
					}
				}
			}
			else if (node.TypeName == "Hash128")
			{
				//So that the fields get ordered sequentially
				for (int i = 0; i < 10; i++)
				{
					node.TryRenameSubNode($"m_Bytes_{i}_", $"m_Bytes__{i}");
				}
				for (int i = 10; i < 16; i++)
				{
					node.TryRenameSubNode($"m_Bytes_{i}_", $"m_Bytes_{i}");
				}
			}
			else if (node.IsAssetServerCache(out UniversalNode? modifiedItemTypeNode))
			{
				modifiedItemTypeNode.TypeName = "ModifiedItem";
			}
			else if (node.TypeName == "NameToObjectMap")
			{
				if (node.TryGetSubNodeByTypeAndName("map", "m_ObjectToName", out UniversalNode? subnode))
				{
					//array, pair, first
					UniversalNode typeNode = subnode.SubNodes[0].SubNodes[1].SubNodes[0];
					string typeName = typeNode.TypeName;
					node.TypeName = typeName.StartsWith("PPtr_", StringComparison.Ordinal)
						? $"NameToObjectMap_{typeName.Substring(5)}"
						: throw new NotSupportedException();
				}
			}
			else if (node.TypeName == "BuildTargetSettings")
			{
				node.TypeName = node.SubNodes.Any(n => n.Name == "m_MaxTextureSize")
					? "TextureImporterPlatformSettings"
					: "BuildTargetSettings_Material";
			}
			else if (node.TypeName == "Google")
			{
				if (node.SubNodes.Any(n => n.Name == "m_EnableTransitionView"))
				{
					node.TypeName = "GoogleCardboard";
				}
				else if (node.SubNodes.Any(n => n.Name == "m_UseSustainedPerformanceMode"))
				{
					node.TypeName = "GoogleDayDream";
				}
			}
			else if (node.TypeName == "InputImportSettings")
			{
				if (node.SubNodes.Any(n => n.Name == "m_Value"))
				{
					node.TypeName = "InputImportSettings_SubstanceValue";
				}
			}
			else if (node.TypeName == "MultiModeParameter")
			{
				if (!node.SubNodes.Any(n => n.Name == "m_Value"))
				{
					node.TypeName = "MultiModeParameter_MeshSpawn";
				}
			}
			else if (node.TypeName == "Output")
			{
				if (node.SubNodes.Any(n => n.Name == "m_HasEmptyFontData"))
				{
					node.TypeName = "FontOutput";
				}
				else if (node.SubNodes.Any(n => n.Name == "m_PreviewData"))
				{
					node.TypeName = "AudioImporterOutputOld";
					node.Name = "m_OutputOld";
				}
			}
			else if (node.TypeName == "PlatformSettingsData")
			{
				node.TypeName = node.SubNodes.Any(n => n.Name == "m_Enabled")
					? "PlatformSettingsData_Plugin"
					: "PlatformSettingsData_Editor";
			}
			else if (node.TypeName == "AlbedoSwatchInfo")
			{
				node.TypeName = "AlbedoSwitchInfo";
			}
			else if (node.TypeName == "GraphicsSettings" || node.TypeName == "RenderManager")//id 30
			{
				node.TryRenameSubNode("m_BuildTargetShaderSettings", "m_TierSettings");//before 5.5
				node.TryRenameSubNode("m_AlbedoSwatchInfos", "m_AlbedoSwitchInfos");//Unity spelling is great
			}
			else if (node.TypeName == "PlatformShaderSettings")//before 5.5
			{
				node.TypeName = "TierGraphicsSettingsEditor";
			}
			else if (node.TypeName == "BuildTargetShaderSettings")
			{
				//This class was changed in 5.5 to TierSettings, so we rename it for consistency.
				//Starting in 2022.2.0b10, a similiar class with this name was added,
				//but it conflicts with TierSettings, hence this rename is meant to only apply on the old versions.
				if (node.TryRenameSubNode("m_ShaderSettings", "m_Settings"))
				{
					node.TypeName = "TierSettings";
				}
			}
			else if (node.TypeName == "BuildTargetSettings")
			{
				node.TypeName = node.SubNodes.Any(n => n.Name == "m_MaxTextureSize") //only for TextureImporter before 5.5
					? "TextureImporterPlatformSettings"
					: "MaterialBuildTargetSettings";
			}
			else if (node.TypeName == "TextureImporter")
			{
				node.TryRenameSubNode("m_BuildTargetSettings", "m_PlatformSettings");
			}
			else if (node.TypeName == "ExposedReferenceTable")
			{
				node.TryRenameSubNode("m_References", isEditor ? "m_References_Editor" : "m_References_Release");
			}
			else if (node.TypeName == "ExtensionPropertyValue")
			{
				node.RenameSubNode("m_PluginName", isEditor ? "m_PluginNameNode_Editor" : "m_PluginNameNode_Release");
				node.RenameSubNode("m_ExtensionName", isEditor ? "m_ExtensionNameNode_Editor" : "m_ExtensionNameNode_Release");
				node.RenameSubNode("m_PropertyName", isEditor ? "m_PropertyNameNode_Editor" : "m_PropertyNameNode_Release");
			}
			else if (node.TypeName == "BoneInfluence")
			{
				node.TypeName = "BoneWeights4";
			}
			else if (node.TypeName == "FileSize" && node.SubNodes.Count == 0)//StreamedResource.m_Offset 2020.1+
			{
				node.TypeName = "UInt64";
			}
			else if (node.TypeName.StartsWith("PPtr_", StringComparison.Ordinal))
			{
				node.TypeName = node.TypeName[..^1];
				node.TryRenameSubNode("m_FileID", "m_FileID_");
				node.TryRenameSubNode("m_PathID", "m_PathID_");
				if (node.Name == "m_PrefabParentObject")//3.5 - 2018.2
				{
					node.Name = "m_CorrespondingSourceObject";
				}
				/*else if (node.Name == "m_PrefabInternal")//3.5 - 2018.3
				{
					node.Name = "m_PrefabAsset";
				}*/
			}
			else if (node.TypeName == "IntPoint")
			{
				node.TypeName = "Vector2Long";
			}
			else if (node.TypeName == "Int2_storage")
			{
				node.TypeName = "Vector2Int";
			}
			else if (node.TypeName == "Int3_storage")
			{
				node.TypeName = "Vector3Int";
			}
			else if (node.TypeName == "Float3")
			{
				node.TypeName = Vector3FloatName;
			}
			else if (node.TypeName == "Float4")
			{
				node.TypeName = Vector4FloatName;
			}
			else if (node.TypeName == "Fixed_bitset")
			{
				node.TypeName = "FixedBitset";
			}
			else if (node.Name == "m_Image_data")
			{
				node.Name = "m_ImageData";
			}
			else if (node.Name == "m_TextureFormat")
			{
				node.Name = "m_Format";//For better linking with documentation
			}
			else if (node.Name == "m_TextureDimension")
			{
				node.Name = "m_Dimension";//For better linking with documentation
			}
			else if (node.Name == "m_ObjectHideFlags")
			{
				node.Name = "m_HideFlags";//For better linking with documentation
			}
			else if (node.TypeName == "Mesh")
			{
				if (node.TryGetSubNodeByTypeAndName("vector", "m_Shapes", out UniversalNode? meshBlendShapesListNode))
				{
					meshBlendShapesListNode.Name = "m_ShapesList";
				}
			}
			else if (node.TypeName == "MeshRenderer")
			{
				node.TryRenameSubNode("m_StitchSeams", "m_StitchLightmapSeams"); // Early 2017.2 betas
			}
			else if (node.TypeName == "ComputeShaderPlatformVariant")// 2020 and later
			{
				node.TypeName = "ComputeShaderVariant";
				node.TryRenameSubNode("m_Kernels", "m_KernelParents");
			}
			else if (node.TypeName == "BuildSettings")
			{
				node.TryRenameSubNode("m_Levels", "m_Scenes"); // Earlier than 5.2
			}
			else if (node.TypeName == "ParticleSystem")
			{
				if (node.TryGetSubNodeByName("m_StartDelay", out UniversalNode? particleSystemStartDelayNode))
				{
					particleSystemStartDelayNode.Name = particleSystemStartDelayNode.TypeName == "float"
						? "m_StartDelay_float"
						: "m_StartDelay_MinMaxCurve";
				}
			}
			else if (node.TypeName == "Child" && node.SubNodes.Any(n => n.Name == "m_IsAnim"))
			{
				node.TypeName = "ChildMotion";
			}
			else if (node.TypeName == "BlendTree")
			{
				node.TryRenameSubNode("m_BlendEvent", "m_BlendParameter");
				node.TryRenameSubNode("m_BlendEventY", "m_BlendParameterY");
			}
			else if (node.TypeName == "JointAngleLimit2D")
			{
				node.TypeName = "JointAngleLimits2D";
			}
			else if (node.TypeName == "PlatformSettings" && node.SubNodes.Any(n => n.Name == "m_MaxTextureSize"))
			{
				node.TypeName = "TextureImporterPlatformSettings";
			}
			else if (node.TypeName == "Condition" && node.SubNodes.Any(n => n.Name == "m_ConditionEvent"))
			{
				node.TypeName = "AnimatorCondition";
			}
			else if (node.TypeName == "VisualEffectResource" && node.TryGetSubNodeByTypeAndName("VisualEffectSettings", "m_Infos", out UniversalNode? visualEffectResourceInfosNode))
			{
				visualEffectResourceInfosNode.Name = "m_Settings";
			}
			else if (node.TypeName == "DataTemplate")
			{
				node.TryRenameSubNode("m_IsDataTemplate", "m_IsPrefabAsset");
			}
			else if (node.IsPrefab(out UniversalNode? parentPrefabNode, out UniversalNode? isPrefabParentNode))
			{
				parentPrefabNode.Name = "m_SourcePrefab";
				isPrefabParentNode.Name = "m_IsPrefabAsset";
			}
			else if (node.TypeName == "VertexData" && node.TryGetSubNodeByName("m_DataSize", out UniversalNode? vertexDataDataSizeNode))
			{
				vertexDataDataSizeNode.Name = "m_Data";
			}
			else if (node.TypeName == "AnimatorEvent")
			{
				node.TypeName = "AnimatorControllerParameter";
			}
			else if (node.TypeName == "AnimatorController")
			{
				node.TryRenameSubNode("m_AnimatorEvents", "m_AnimatorParameters");
				node.TryRenameSubNode("m_Layers", "m_AnimatorLayers");
			}
			else if (node.TypeName == "AnimatorControllerLayer")
			{
				node.TryRenameSubNode("m_HumanMask", "m_Mask");
			}
			else if (node.Name == "m__int__m_LayerBlendingMode")//LayerConstant
			{
				node.Name = "m_LayerBlendingMode";
			}
			else if (node.Name.StartsWith("m__SInt32__m_", StringComparison.Ordinal))//AudioClip 3.5 - 5 exclusive
			{
				node.Name = node.Name.Substring("m__SInt32__".Length);
			}
			else if (node.TypeName == "AnimationClip")
			{
				node.TryRenameSubNode("m_AnimationClipSettings", "m_MuscleClipInfo");
			}
			else if (node.TypeName == "GenericBinding")
			{
				node.TryRenameSubNode("m_TypeID", "m_ClassID");
			}
			else if (node.TypeName == "ClipMuscleConstant")
			{
				node.TypeName = "MuscleClipConstant";
				if (node.TryGetSubNodeByName("m_AverageSpeed", out UniversalNode? averageSpeedNode))
				{
					averageSpeedNode.Name = averageSpeedNode.TypeName switch
					{
						Vector4FloatName => "m_AverageSpeed4",
						Vector3FloatName => "m_AverageSpeed3",
						_ => throw new NotSupportedException(),
					};
				}
			}
			else if (node.TypeName == "Xform")
			{
				if (node.TryGetSubNodeByName("m_T", out UniversalNode? tNode))
				{
					tNode.Name = tNode.TypeName switch
					{
						Vector4FloatName => "m_T4",
						Vector3FloatName => "m_T3",
						_ => throw new NotSupportedException(),
					};
				}
				if (node.TryGetSubNodeByName("m_S", out UniversalNode? sNode))
				{
					sNode.Name = sNode.TypeName switch
					{
						Vector4FloatName => "m_S4",
						Vector3FloatName => "m_S3",
						_ => throw new NotSupportedException(),
					};
				}
			}
			else if (node.TypeName == "ValueArray")
			{
				if (node.TryGetSubNodeByName("m_PositionValues", out UniversalNode? positionVectorNode))
				{
					UniversalNode positionNode = positionVectorNode.GetSubNodeByName("m_Array").GetSubNodeByName("m_Data");
					positionVectorNode.Name = positionNode.TypeName switch
					{
						Vector4FloatName => "m_PositionValues4",
						Vector3FloatName => "m_PositionValues3",
						_ => throw new NotSupportedException(positionNode.TypeName),
					};
				}
				if (node.TryGetSubNodeByName("m_ScaleValues", out UniversalNode? scaleVectorNode))
				{
					UniversalNode scaleNode = scaleVectorNode.GetSubNodeByName("m_Array").GetSubNodeByName("m_Data");
					scaleVectorNode.Name = scaleNode.TypeName switch
					{
						Vector4FloatName => "m_ScaleValues4",
						Vector3FloatName => "m_ScaleValues3",
						_ => throw new NotSupportedException(scaleNode.TypeName),
					};
				}
			}
			else if (node.TypeName == "Gradient")
			{
				if (node.TryGetSubNodeByName("m_Color_0_", out _))
				{
					node.TypeName = "GradientOld";
				}
			}
			else if (node.TypeName == "GradientNEW")
			{
				node.TypeName = "Gradient";
			}
			else if (node.TypeName == "HumanLayerConstant" || node.TypeName == "LayerConstant")
			{
				node.TypeName = "LayerConstant";
				node.TryRenameSubNode("m_StateMachineMotionSetIndex", "m_StateMachineSynchronizedLayerIndex");
			}
			else if (node.TypeName == "ControllerConstant")
			{
				node.TryRenameSubNode("m_HumanLayerArray", "m_LayerArray");
			}
			else if (node.TypeName == "StateMachineConstant")
			{
				node.TryRenameSubNode("m_MotionSetCount", "m_SynchronizedLayerCount");
			}
			else if (node.TypeName == "Heightmap")
			{
				node.TryRenameSubNode("m_EnableSurfaceMaskTextureCompression", "m_EnableHolesTextureCompression");
				node.TryRenameSubNode("m_SurfaceMask", "m_Holes");
				node.TryRenameSubNode("m_SurfaceMaskLOD", "m_HolesLOD");
			}
			else if (node.TypeName == "ShapeModule")
			{
				if (node.TryGetSubNodeByName("m_Arc", out UniversalNode? arcNode) && arcNode.TypeName != "float")
				{
					arcNode.Name = "m_ArcParameter";
				}
				if (node.TryGetSubNodeByName("m_Radius", out UniversalNode? radiusNode) && radiusNode.TypeName != "float")
				{
					radiusNode.Name = "m_RadiusParameter";
				}
			}
		}

		private static bool TryRenameSubNode(this UniversalNode node, string currentName, string newName)
		{
			if (node.TryGetSubNodeByName(currentName, out UniversalNode? subNode))
			{
				subNode.Name = newName;
				return true;
			}
			return false;
		}

		private static void RenameSubNode(this UniversalNode node, string currentName, string newName)
		{
			node.GetSubNodeByName(currentName).Name = newName;
		}

		private static void ChangeStringToUtf8String(UniversalNode node)
		{
			node.TypeName = Utf8StringName;
			List<UniversalNode> subnodes = node.SubNodes!;
			if (subnodes.Count != 1)
			{
				throw new Exception($"String has {subnodes.Count} subnodes");
			}
			UniversalNode subnode = subnodes[0];
			if (subnode.TypeName == "Array")
			{
				subnode.Name = "m_Data";
			}
			else if (subnode.TypeName == Utf8StringName)
			{
				//ExposedReferenceTable on late 2019 and after
				node.TypeName = NestedStringName;
			}
			else if (subnode.TypeName == "SInt32")
			{
				//ExposedReferenceTable on 2017 - early 2019
				node.TypeName = IntegerStringName;
			}
			else
			{
				//Console.WriteLine($"String subnode has typename: {subnode.TypeName}");
				throw new NotSupportedException($"String subnode has typename: {subnode.TypeName}");
			}
		}

		private static bool IsVFXEntryExposed(this UniversalNode node, [NotNullWhen(true)] out string? elementType)
		{
			List<UniversalNode> subnodes = node.SubNodes;
			if (node.TypeName == VFXEntryExposedName && subnodes.Any(n => n.Name == "m_Value"))
			{
				elementType = subnodes.Single(n => n.Name == "m_Value").TypeName!.ReplaceBadCharacters();
				return true;
			}

			elementType = null;
			return false;
		}

		private static bool IsVFXEntryExpressionValue(this UniversalNode node, [NotNullWhen(true)] out string? elementType)
		{
			List<UniversalNode> subnodes = node.SubNodes;
			if (node.TypeName == VFXEntryExpressionValueName && subnodes.Any(n => n.Name == "m_Value"))
			{
				elementType = subnodes.Single(n => n.Name == "m_Value").TypeName!.ReplaceBadCharacters();
				return true;
			}

			elementType = null;
			return false;
		}

		private static bool IsVFXField(this UniversalNode node, [NotNullWhen(true)] out string? elementType)
		{
			if (node.TypeName == VFXFieldName)
			{
				if (node.TryGetSubNodeByTypeAndName("vector", "m_Array", out UniversalNode? arrayNode))
				{
					elementType = arrayNode.SubNodes[0].SubNodes[1].TypeName;
					return true;
				}
			}

			elementType = null;
			return false;
		}

		private static bool IsVFXPropertySheetSerializedBase(this UniversalNode node, [NotNullWhen(true)] out string? elementType)
		{
			elementType = null;
			List<UniversalNode> subnodes = node.SubNodes;
			if (node.TypeName == VFXPropertySheetSerializedBaseName && subnodes.Any(n => n.Name == "m_Float"))
			{
				string floatFieldType = subnodes.Single(n => n.Name == "m_Float").TypeName!;
				if (floatFieldType.StartsWith($"{VFXFieldName}_{VFXEntryExposedName}"))
				{
					elementType = VFXEntryExposedName;
					return true;
				}
				else if (floatFieldType.StartsWith($"{VFXFieldName}_{VFXEntryExpressionValueName}"))
				{
					elementType = VFXEntryExpressionValueName;
					return true;
				}
				else
				{
					throw new NotSupportedException();
				}
			}

			return false;
		}

		private static bool IsSpriteAtlasAsset(this UniversalNode node, [NotNullWhen(true)] out UniversalNode? importerNode)
		{
			importerNode = node.TypeName == SpriteAtlasAssetName
				? node.SubNodes.SingleOrDefault(n => n.Name == SpriteAtlasAssetImporterDataFieldName)
				: null;
			return importerNode != null;
		}

		private static bool IsLightProbes(this UniversalNode node, [NotNullWhen(true)] out UniversalNode? bakedCoefficentsNode)
		{
			bakedCoefficentsNode = node.TypeName == "LightProbes"
				? node.SubNodes.SingleOrDefault(n => n.Name == "m_BakedCoefficients")?.SubNodes[0].SubNodes[1]
				: null;
			return bakedCoefficentsNode != null;
		}

		private static bool IsAssetServerCache(this UniversalNode node, [NotNullWhen(true)] out UniversalNode? modifiedItemTypeNode)
		{
			modifiedItemTypeNode = node.TypeName == "AssetServerCache"
				? node.SubNodes.SingleOrDefault(n => n.Name == "m_ModifiedItems")?.SubNodes[0].SubNodes[1].SubNodes[1]
				: null;
			return modifiedItemTypeNode != null;
		}

		private static bool IsPrefab(this UniversalNode node, [NotNullWhen(true)] out UniversalNode? parentPrefabNode, [NotNullWhen(true)] out UniversalNode? isPrefabParentNode)
		{
			if (node.TypeName == "Prefab")
			{
				isPrefabParentNode = node.SubNodes.SingleOrDefault(n => n.Name == "m_IsPrefabParent");
				parentPrefabNode = node.SubNodes.SingleOrDefault(n => n.Name == "m_ParentPrefab");
				return isPrefabParentNode != null && parentPrefabNode != null;
			}

			isPrefabParentNode = null;
			parentPrefabNode = null;
			return false;
		}
	}
}
