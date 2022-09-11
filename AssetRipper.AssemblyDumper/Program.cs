﻿using AssetRipper.AssemblyDumper.Passes;

namespace AssetRipper.AssemblyDumper
{
	public static class Program
	{
		public static void Main()
		{
			RunGeneration();
		}

		private static void RunGeneration()
		{
			using (new TimingCookie("Initialization"))
			{
				TpkProcessor.IntitializeSharedState("uncompressed.tpk");
			}
			using (new TimingCookie("Pass 002: Rename Subnodes"))
			{
				Pass002_RenameSubnodes.DoPass();
			}
			using (new TimingCookie("Pass 003: Fix TextureImporter Nodes"))
			{
				Pass003_FixTextureImporterNodes.DoPass();
			}
			using (new TimingCookie("Pass 004: Fill Name to Type Id Dictionary"))
			{
				Pass004_FillNameToTypeIdDictionary.DoPass();
			}
			using (new TimingCookie("Pass 005: Split Abstract Classes"))
			{
				Pass005_SplitAbstractClasses.DoPass();
			}
			using (new TimingCookie("Pass 007: Extract Subclasses"))
			{
				Pass007_ExtractSubclasses.DoPass();
			}
			using (new TimingCookie("Pass 008: Divide Ambiguous PPtr"))
			{
				Pass008_DivideAmbiguousPPtr.DoPass();
			}
			using (new TimingCookie("Pass 009: Create Groups"))
			{
				Pass009_CreateGroups.DoPass();
			}
			using (new TimingCookie("Pass 010: Initialize Interfaces"))
			{
				Pass010_InitializeInterfacesAndFactories.DoPass();
			}
			using (new TimingCookie("Pass 011: Apply Inheritance"))
			{
				Pass011_ApplyInheritance.DoPass();
			}
			using (new TimingCookie("Pass 012: Apply Correct Type Attributes"))
			{
				Pass012_ApplyCorrectTypeAttributes.DoPass();
			}
			using (new TimingCookie("Pass 013: Unify Fields of Abstract Types"))
			{
				Pass013_UnifyFieldsOfAbstractTypes.DoPass();
			}
			using (new TimingCookie("Pass 015: Add Fields"))
			{
				Pass015_AddFields.DoPass();
			}
			using (new TimingCookie("Pass 016: Add Constructors"))
			{
				Pass016_AddConstructors.DoPass();
			}
			using (new TimingCookie("Pass 017: Fill Constructors"))
			{
				Pass017_FillConstructors.DoPass();
			}
			using (new TimingCookie("Pass 040: Add Enum Types"))
			{
				Pass040_AddEnums.DoPass();
			}
			using (new TimingCookie("Pass 045: Marker Interfaces"))
			{
				Pass045_AddMarkerInterfaces.DoPass();
			}
			using (new TimingCookie("Pass 052: Interface Properties and Methods"))
			{
				Pass052_InterfacePropertiesAndMethods.DoPass();
			}
			using (new TimingCookie("Pass 053: Has Methods and Nullable Attributes"))
			{
				Pass053_HasMethodsAndNullableAttributes.DoPass();
			}
			using (new TimingCookie("Pass 054: Assign Property Histories"))
			{
				Pass054_AssignPropertyHistories.DoPass();
			}
			using (new TimingCookie("Pass 055: Create Enum Properties"))
			{
				Pass055_CreateEnumProperties.DoPass();
			}
			using (new TimingCookie("Pass 080: PPtr Conversions"))
			{
				Pass080_PPtrConversions.DoPass();
			}
			using (new TimingCookie("Pass 099: Create Empty Methods"))
			{
				Pass099_CreateEmptyMethods.DoPass();
			}
			using (new TimingCookie("Pass 100: Filling Read Methods"))
			{
				Pass100_FillReadMethods.DoPass();
			}
			using (new TimingCookie("Pass 101: Filling Write Methods"))
			{
				Pass101_FillWriteMethods.DoPass();
			}
			using (new TimingCookie("Pass 102: Filling Yaml Methods"))
			{
				Pass102_FillYamlMethods.DoPass();
			}
			using (new TimingCookie("Pass 103: Filling Dependency Methods"))
			{
				Pass103_FillDependencyMethods.DoPass();
			}
			using (new TimingCookie("Pass 110: Class Name and ID Overrides"))
			{
				Pass110_ClassNameAndIdOverrides.DoPass();
			}
			using (new TimingCookie("Pass 201: GUID Explicit Conversion"))
			{
				Pass201_GuidConversionOperators.DoPass();
			}
			using (new TimingCookie("Pass 202: Vector Explicit Conversions"))
			{
				Pass202_VectorExplicitConversions.DoPass();
			}
			using (new TimingCookie("Pass 203: OffsetPtr Implicit Conversions"))
			{
				Pass203_OffsetPtrImplicitConversions.DoPass();
			}
			using (new TimingCookie("Pass 204: Hash128 Explicit Conversion"))
			{
				Pass204_Hash128ExplicitConversion.DoPass();
			}
			using (new TimingCookie("Pass 205: Color Explicit Conversions"))
			{
				Pass205_ColorExplicitConversions.DoPass();
			}
			using (new TimingCookie("Pass 206: BoneWeights4 Explicit Conversions"))
			{
				Pass206_BoneWeights4ExplicitConversions.DoPass();
			}
			using (new TimingCookie("Pass 300: Has Name Interface"))
			{
				Pass300_HasNameInterface.DoPass();
			}
			using (new TimingCookie("Pass 301: Has Hide Flags Interface"))
			{
				Pass301_HasHideFlagsInterface.DoPass();
			}
			using (new TimingCookie("Pass 302: Has Enabled Interface"))
			{
				Pass302_HasEnabledInterface.DoPass();
			}
			using (new TimingCookie("Pass 400: IEquatable Interface"))
			{
				Pass400_AddEqualityMethods.DoPass();
			}
			using (new TimingCookie("Pass 401: Equality Operators"))
			{
				Pass401_EqualityOperators.DoPass();
			}
			using (new TimingCookie("Pass 402: GetHashCode Methods"))
			{
				Pass402_GetHashCodeMethods.DoPass();
			}
			using (new TimingCookie("Pass 410: SetValues Methods"))
			{
				Pass410_SetValuesMethods.DoPass();
			}
			using (new TimingCookie("Pass 500: Fixing PPtr Yaml"))
			{
				Pass500_FixPPtrYaml.DoPass();
			}
			using (new TimingCookie("Pass 501: Fixing MonoBehaviour"))
			{
				Pass501_MonoBehaviourImplementation.DoPass();
			}
			using (new TimingCookie("Pass 502: Fixing Guid and Hash Yaml Export"))
			{
				Pass502_FixGuidAndHashYaml.DoPass();
			}
			using (new TimingCookie("Pass 503: Fixing Utf8String"))
			{
				Pass503_FixUtf8String.DoPass();
			}
			using (new TimingCookie("Pass 504: Fixing Shader Name"))
			{
				Pass504_FixShaderName.DoPass();
			}
			using (new TimingCookie("Pass 505: Fixing Old AudioClips"))
			{
				Pass505_FixOldAudioClip.DoPass();
			}
			using (new TimingCookie("Pass 506: Fixing UnityConnectSettings"))
			{
				Pass506_FixUnityConnectSettings.DoPass();
			}
			using (new TimingCookie("Pass 520: Custom Field Initializers"))
			{
				Pass520_CustomFieldInitializers.DoPass();
			}
			using (new TimingCookie("Pass 555: Create Common String"))
			{
				Pass555_CreateCommonString.DoPass();
			}
			using (new TimingCookie("Pass 556: Create ClassIDType Enum"))
			{
				Pass556_CreateClassIDTypeEnum.DoPass();
			}
			using (new TimingCookie("Pass 557: Create Source Version HashSet"))
			{
				Pass557_CreateVersionHashSet.DoPass();
			}
			using (new TimingCookie("Pass 558: Create Type to ClassIDType Dictionary"))
			{
				Pass558_TypeCache.DoPass();
			}
			using (new TimingCookie("Pass 900: Fill Type Tree Methods"))
			{
				Pass900_FillTypeTreeMethods.DoPass();
			}
			using (new TimingCookie("Pass 920: Interface Inheritance"))
			{
				Pass920_InterfaceInheritance.DoPass();
			}
			using (new TimingCookie("Pass 940: Make Asset Factory"))
			{
				Pass940_MakeAssetFactory.DoPass();
			}
			using (new TimingCookie("Pass 998: Write Assembly"))
			{
				Pass998_SaveAssembly.DoPass();
			}
			using (new TimingCookie("Pass 999: Generate Documentation"))
			{
				Pass999_Documentation.DoPass();
			}
		}
	}
}