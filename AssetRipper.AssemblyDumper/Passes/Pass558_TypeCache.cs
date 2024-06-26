﻿using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass558_TypeCache
	{
		public static void DoPass()
		{
			TypeDefinition newTypeDef = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "ClassIDTypeMap");

			GenericInstanceTypeSignature readOnlyDictionarySignature = SharedState.Instance.Importer.ImportType(typeof(IReadOnlyDictionary<,>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.ImportTypeSignature<Type>(), Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature());
			GenericInstanceTypeSignature dictionarySignature = SharedState.Instance.Importer.ImportType(typeof(Dictionary<,>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.ImportTypeSignature<Type>(), Pass556_CreateClassIDTypeEnum.ClassIdTypeDefintion!.ToTypeSignature());
			IMethodDefOrRef dictionaryConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, dictionarySignature, 0);
			IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(SharedState.Instance.Importer, dictionarySignature, SharedState.Instance.Importer.LookupMethod(typeof(Dictionary<,>), m => m.Name == "Add"));
			IMethodDefOrRef getTypeFromHandleMethod = SharedState.Instance.Importer.ImportMethod(typeof(Type), m => m.Name == nameof(Type.GetTypeFromHandle));

			const string propertyName = "Dictionary";
			FieldDefinition field = newTypeDef.AddField(readOnlyDictionarySignature, $"<{propertyName}>k__BackingField", true, FieldVisibility.Private);
			field.Attributes |= FieldAttributes.InitOnly;
			field.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			MethodDefinition staticConstructor = newTypeDef.AddEmptyConstructor(true);
			CilInstructionCollection processor = staticConstructor.GetProcessor();
			processor.Add(CilOpCodes.Newobj, dictionaryConstructor);
			foreach ((int id, ClassGroup group) in SharedState.Instance.ClassGroups)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldtoken, group.Interface);
				processor.Add(CilOpCodes.Call, getTypeFromHandleMethod);
				processor.Add(CilOpCodes.Ldc_I4, id);
				processor.Add(CilOpCodes.Call, addMethod);

				foreach (TypeDefinition type in group.Types)
				{
					processor.Add(CilOpCodes.Dup);
					processor.Add(CilOpCodes.Ldtoken, type);
					processor.Add(CilOpCodes.Call, getTypeFromHandleMethod);
					processor.Add(CilOpCodes.Ldc_I4, id);
					processor.Add(CilOpCodes.Call, addMethod);
				}
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			newTypeDef.ImplementGetterProperty(
					propertyName,
					MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName,
					readOnlyDictionarySignature,
					field)
				.GetMethod!.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);
		}
	}
}
