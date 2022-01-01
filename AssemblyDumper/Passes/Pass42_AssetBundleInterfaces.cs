using AssemblyDumper.Utils;
using AssetRipper.Core.Classes.AssetBundle;
using AssetRipper.Core.Interfaces;
using AssetRipper.Core.IO;

namespace AssemblyDumper.Passes
{
	public static class Pass42_AssetBundleInterfaces
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 42: AssetBundle Interfaces");
			ImplementAssetInfo();
			ImplementAssetBundle();
		}

		private static void ImplementAssetInfo()
		{
			TypeDefinition type = SharedState.TypeDictionary["AssetInfo"];

			TypeSignature returnTypeSignature = SharedState.Importer.ImportTypeSignature(CommonTypeGetter.LookupCommonType<IAssetInfo>().Properties.Single().Signature.ReturnType);
			FieldDefinition field = type.GetFieldByName("asset");
			MethodDefinition conversionMethod = PPtrUtils.GetExplicitConversion<IUnityObjectBase>(field.Signature.FieldType.Resolve());

			type.AddInterfaceImplementation<IAssetInfo>();
			CilInstructionCollection processor = type.AddGetterProperty(nameof(IAssetInfo.AssetPtr), InterfaceUtils.InterfacePropertyImplementation, returnTypeSignature).GetMethod.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Call, conversionMethod);
			processor.Add(CilOpCodes.Ret);
		}

		private static void ImplementAssetBundle()
		{
			TypeDefinition type = SharedState.TypeDictionary["AssetBundle"];
			type.AddInterfaceImplementation<IAssetBundle>();
			type.ImplementAssetBundleNameProperty();
			type.ImplementGetAssetsMethod();
		}

		private static void ImplementAssetBundleNameProperty(this TypeDefinition type)
		{
			if(type.TryGetFieldByName("m_AssetBundleName", out FieldDefinition field))
			{
				type.ImplementFullProperty(nameof(IAssetBundle.AssetBundleName), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, field);
			}
			else
			{
				type.ImplementFullProperty(nameof(IAssetBundle.AssetBundleName), InterfaceUtils.InterfacePropertyImplementation, SystemTypeGetter.String, null);
			}
		}

		private static void ImplementGetAssetsMethod(this TypeDefinition type)
		{
			var returnTypeSignature = SharedState.Importer.ImportTypeSignature(
				CommonTypeGetter.LookupCommonType<IAssetBundle>()
				.Methods
				.Single(m => m.Name == nameof(IAssetBundle.GetAssets))
				.Signature
				.ReturnType);
			MethodDefinition method = type.AddMethod(nameof(IAssetBundle.GetAssets), InterfaceUtils.InterfaceMethodImplementation, returnTypeSignature);

			FieldDefinition field = type.GetFieldByName("m_Container");
			GenericInstanceTypeSignature fieldType = (GenericInstanceTypeSignature)field.Signature.FieldType;
			TypeSignature typeArgument1 = fieldType.TypeArguments[0];
			TypeSignature typeArgument2 = fieldType.TypeArguments[1];
			TypeSignature typeArgument3 = SharedState.Importer.ImportCommonType<IAssetInfo>().ToTypeSignature();
			IMethodDefOrRef extensionMethod = SharedState.Importer.ImportCommonMethod(typeof(AssetDictionaryExtensions), m => m.Name == nameof(AssetDictionaryExtensions.ToCastedArray));
			MethodSpecification extensionMethodInstance = MethodUtils.MakeGenericInstanceMethod(extensionMethod, typeArgument1, typeArgument2, typeArgument3);

			CilInstructionCollection processor = method.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Call, extensionMethodInstance);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
