using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;
using AssetRipper.Core.Classes.EditorBuildSettings;
using AssetRipper.Core.Classes.EditorSettings;

namespace AssemblyDumper.Passes
{
	public static class Pass40_BuildSettingsInterfaces
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 40: Build Settings Interfaces");
			ImplementBuildSettingsInterface();
			ImplementEditorSettingsInterface();
			ImplementEditorSceneInterface();
			ImplementEditorBuildSettingsInterface();
		}

		private static void ImplementBuildSettingsInterface()
		{
			TypeDefinition type = SharedState.TypeDictionary["BuildSettings"];
			type.AddInterfaceImplementation<IBuildSettings>();
			type.ImplementFullProperty(nameof(IBuildSettings.Version), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, type.GetFieldByName("m_Version"));
			if(type.TryGetFieldByName("scenes", out FieldDefinition? field))
			{
				type.ImplementFullProperty(nameof(IBuildSettings.Scenes), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String.MakeSzArrayType(), field);
			}
			else if (type.TryGetFieldByName("levels", out field))
			{
				type.ImplementFullProperty(nameof(IBuildSettings.Scenes), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String.MakeSzArrayType(), field);
			}
			else
			{
				throw new Exception("Could not find the scenes field for BuildSettings");
			}
		}

		private static void ImplementEditorSettingsInterface()
		{
			TypeDefinition type = SharedState.TypeDictionary["EditorSettings"];
			type.AddInterfaceImplementation<IEditorSettings>();
			type.ImplementInterfacePropertyForgiving("ExternalVersionControlSupport", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("SerializationMode", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("SpritePackerPaddingPower", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("EtcTextureCompressorBehavior", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("EtcTextureFastCompressor", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("EtcTextureNormalCompressor", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("EtcTextureBestCompressor", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("ProjectGenerationIncludedExtensions", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("ProjectGenerationRootNamespace", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("UserGeneratedProjectSuffix", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("EnableTextureStreamingInEditMode", SystemTypeGetter.Boolean);
			type.ImplementInterfacePropertyForgiving("EnableTextureStreamingInPlayMode", SystemTypeGetter.Boolean);
			type.ImplementInterfacePropertyForgiving("AsyncShaderCompilation", SystemTypeGetter.Boolean);
			type.ImplementInterfacePropertyForgiving("AssetPipelineMode", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("CacheServerMode", SystemTypeGetter.Int32);
			type.ImplementInterfacePropertyForgiving("CacheServerEndpoint", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("CacheServerNamespacePrefix", SystemTypeGetter.String);
			type.ImplementInterfacePropertyForgiving("CacheServerEnableDownload", SystemTypeGetter.Boolean);
			type.ImplementInterfacePropertyForgiving("CacheServerEnableUpload", SystemTypeGetter.Boolean);
		}

		private static void ImplementInterfacePropertyForgiving(this TypeDefinition type, string propertyName, TypeSignature returnType, string? fieldName = null)
		{
			fieldName ??= $"m_{propertyName}";
			if (type.TryGetFieldByName(fieldName, out FieldDefinition? field))
			{
				type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, returnType, field);
			}
			else
			{
				type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, returnType, null);
			}
		}

		private static void ImplementEditorSceneInterface()
		{
			TypeDefinition type = SharedState.TypeDictionary["EditorScene"];
			type.AddInterfaceImplementation<IEditorScene>();
			type.ImplementFullProperty(nameof(IEditorScene.Enabled), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.Boolean, type.GetFieldByName("enabled"));
			type.ImplementFullProperty(nameof(IEditorScene.Path), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, type.GetFieldByName("path"));

			ITypeDefOrRef unityGuid = SharedState.Importer.ImportCommonType<AssetRipper.Core.Classes.Misc.UnityGUID>();
			if(type.TryGetFieldByName("guid", out FieldDefinition? guidField))
			{
				//specific to common
				MethodDefinition implicitConversion = guidField.Signature!.FieldType.Resolve()!.Methods.Single(m => m.Name == "op_Implicit");
				//common to specific
				MethodDefinition explicitConversion = guidField.Signature.FieldType.Resolve()!.Methods.Single(m => m.Name == "op_Explicit");

				PropertyDefinition property = type.AddFullProperty(nameof(IEditorScene.GUID), InterfaceUtils.InterfacePropertyImplementation, unityGuid.ToTypeSignature());
				CilInstructionCollection getProcessor = property.GetMethod!.CilMethodBody!.Instructions;
				getProcessor.Add(CilOpCodes.Ldarg_0);
				getProcessor.Add(CilOpCodes.Ldfld, guidField);
				getProcessor.Add(CilOpCodes.Call, implicitConversion);
				getProcessor.Add(CilOpCodes.Ret);
				CilInstructionCollection setProcessor = property.SetMethod!.CilMethodBody!.Instructions;
				setProcessor.Add(CilOpCodes.Ldarg_0);
				setProcessor.Add(CilOpCodes.Ldarg_1);
				setProcessor.Add(CilOpCodes.Call, explicitConversion);
				setProcessor.Add(CilOpCodes.Stfld, guidField);
				setProcessor.Add(CilOpCodes.Ret);
			}
			else
			{
				type.ImplementFullProperty(nameof(IEditorScene.GUID), InterfaceUtils.InterfacePropertyImplementation, unityGuid.ToTypeSignature(), null);
			}
		}

		private static void ImplementEditorBuildSettingsInterface()
		{
			TypeDefinition type = SharedState.TypeDictionary["EditorBuildSettings"];
			type.AddInterfaceImplementation<IEditorBuildSettings>();
			TypeSignature returnType = SharedState.Importer.ImportCommonType<IEditorScene>().MakeSzArrayType();
			FieldDefinition field = type.GetFieldByName("m_Scenes");
			type.ImplementGetterProperty(nameof(IEditorBuildSettings.Scenes), InterfaceUtils.InterfacePropertyImplementation, returnType, field);

			//InitializeScenesArray method

			MethodDefinition initializeScenesMethod = type.AddMethod(nameof(IEditorBuildSettings.InitializeScenesArray), InterfaceUtils.InterfaceMethodImplementation, SystemTypeGetter.Void);
			initializeScenesMethod.AddParameter("length", SystemTypeGetter.Int32);
			initializeScenesMethod.CilMethodBody!.InitializeLocals = true;
			CilInstructionCollection processor = initializeScenesMethod.CilMethodBody.Instructions;

			processor.AddInitializeArrayField(field);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
