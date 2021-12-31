using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDumper.Utils;
using AssetRipper.Core.Classes;
using AssetRipper.Core.Classes.EditorBuildSettings;
using AssetRipper.Core.Classes.EditorSettings;
using System;
using System.Linq;

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
			if(type.TryGetFieldByName("scenes", out FieldDefinition field))
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

		private static void ImplementInterfacePropertyForgiving(this TypeDefinition type, string propertyName, TypeSignature returnType, string fieldName = null)
		{
			fieldName ??= $"m_{propertyName}";
			if (type.TryGetFieldByName(fieldName, out var field))
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
			if(type.TryGetFieldByName("guid", out FieldDefinition guidField))
			{
				//specific to common
				MethodDefinition implicitConversion = guidField.Signature.FieldType.Resolve().Methods.Single(m => m.Name == "op_Implicit");
				//common to specific
				MethodDefinition explicitConversion = guidField.Signature.FieldType.Resolve().Methods.Single(m => m.Name == "op_Explicit");

				PropertyDefinition property = type.AddFullProperty(nameof(IEditorScene.GUID), InterfaceUtils.InterfacePropertyImplementation, unityGuid.ToTypeSignature());
				var getProcessor = property.GetMethod.CilMethodBody.Instructions;
				getProcessor.Add(CilOpCodes.Ldarg_0);
				getProcessor.Add(CilOpCodes.Ldfld, guidField);
				getProcessor.Add(CilOpCodes.Call, implicitConversion);
				getProcessor.Add(CilOpCodes.Ret);
				var setProcessor = property.SetMethod.CilMethodBody.Instructions;
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

			SzArrayTypeSignature fieldType = field.Signature.FieldType as SzArrayTypeSignature;
			TypeSignature elementType = fieldType.BaseType;
			MethodDefinition constructor = elementType.Resolve().GetDefaultConstructor();

			MethodDefinition initializeScenesMethod = type.AddMethod(nameof(IEditorBuildSettings.InitializeScenesArray), InterfaceUtils.InterfaceMethodImplementation, SystemTypeGetter.Void);
			initializeScenesMethod.AddParameter("length", SystemTypeGetter.Int32);
			initializeScenesMethod.CilMethodBody.InitializeLocals = true;
			CilInstructionCollection processor = initializeScenesMethod.CilMethodBody.Instructions;

			//Create empty array and local for it
			processor.Add(CilOpCodes.Ldarg_1); //Load length argument
			processor.Add(CilOpCodes.Newarr, elementType.ToTypeDefOrRef()); //Create new array of kvp with given count
			var arrayLocal = new CilLocalVariable(fieldType); //Create local
			processor.Owner.LocalVariables.Add(arrayLocal); //Add to method
			processor.Add(CilOpCodes.Stloc, arrayLocal); //Store array in local

			//Make local and store length in it
			var countLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(countLocal); //Add to method
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			processor.Add(CilOpCodes.Ldlen); //Get length
			processor.Add(CilOpCodes.Stloc, countLocal); //Store it

			//Make an i
			var iLocal = new CilLocalVariable(SystemTypeGetter.Int32); //Create local
			processor.Owner.LocalVariables.Add(iLocal); //Add to method
			processor.Add(CilOpCodes.Ldc_I4_0); //Load 0 as an int32
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in count

			//Create a label for a dummy instruction to jump back to
			CilInstructionLabel jumpTargetLabel = new CilInstructionLabel();
			CilInstructionLabel loopConditionStartLabel = new CilInstructionLabel();

			//Create an empty, unconditional branch which will jump down to the loop condition.
			//This converts the do..while loop into a for loop.
			var unconditionalBranch = processor.Add(CilOpCodes.Br, loopConditionStartLabel);

			//Now we just read pair, increment i, compare against count, and jump back to here if it's less
			jumpTargetLabel.Instruction = processor.Add(CilOpCodes.Nop); //Create a dummy instruction to jump back to

			//Create element at index i of array
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Load array local
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Newobj, constructor); //Create instance
			processor.Add(CilOpCodes.Stelem, elementType.ToTypeDefOrRef()); //Store in array

			//Increment i
			processor.Add(CilOpCodes.Ldloc, iLocal); //Load i local
			processor.Add(CilOpCodes.Ldc_I4_1); //Load constant 1 as int32
			processor.Add(CilOpCodes.Add); //Add 
			processor.Add(CilOpCodes.Stloc, iLocal); //Store in i local

			//Jump to start of loop if i < count
			loopConditionStartLabel.Instruction = processor.Add(CilOpCodes.Ldloc, iLocal); //Load i
			processor.Add(CilOpCodes.Ldloc, countLocal); //Load count
			processor.Add(CilOpCodes.Blt, jumpTargetLabel); //Jump back up if less than

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldloc, arrayLocal); //Store array in local
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
