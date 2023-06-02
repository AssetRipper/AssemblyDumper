using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets.Collections;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass507_InjectedProperties
	{
		public const string TargetSceneName = "TargetScene";
		public const string SpriteInformationName = "SpriteInformation";

		public static void DoPass()
		{
			SceneAssetTargetScene();
			TextureSpriteInformation();
		}

		private static void SceneAssetTargetScene()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[1032]; //SceneAsset
			TypeSignature propertyType = SharedState.Instance.Importer.ImportType<SceneDefinition>().ToTypeSignature();
			PropertyInjector.InjectFullProperty(group, propertyType, TargetSceneName, true);
		}

		private static void TextureSpriteInformation()
		{
			ClassGroup group = SharedState.Instance.ClassGroups[28]; //Texture2D
			TypeSignature keyType = SharedState.Instance.ClassGroups[213].Interface.ToTypeSignature();//ISprite
			TypeSignature valueType = SharedState.Instance.ClassGroups[687078895].Interface.ToTypeSignature();//ISpriteAtlas
			ITypeDefOrRef dictionaryType = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>));
			TypeSignature propertySignature = dictionaryType.MakeGenericInstanceType(keyType, valueType);
			const string propertyName = SpriteInformationName;
			byte[] nullableData = new byte[3]
			{
				(byte)NullableAnnotation.MaybeNull,//Dictionary
				(byte)NullableAnnotation.NotNull,//Key
				(byte)NullableAnnotation.MaybeNull,//Value
			};

			PropertyDefinition interfaceProperty = group.Interface.AddFullProperty(propertyName, InterfaceUtils.InterfacePropertyDeclaration, propertySignature);
			interfaceProperty.AddNullableAttribute(nullableData);

			foreach (TypeDefinition type in group.Types)
			{
				FieldDefinition field = type.AddField(propertySignature, $"m_{propertyName}");
				PropertyDefinition property = type.ImplementFullProperty(propertyName, InterfaceUtils.InterfacePropertyImplementation, null, field);
				
				field.AddNullableAttribute(nullableData);
				property.AddNullableAttribute(nullableData);
				property.GetMethod!.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
				property.SetMethod?.AddNullableContextAttribute(NullableAnnotation.MaybeNull);
			}
		}
	}
}
