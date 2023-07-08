using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass080_PPtrConversions
	{
		public static IReadOnlyDictionary<TypeDefinition, TypeDefinition> PPtrsToParameters => pptrsToParameters;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static ITypeDefOrRef commonPPtrType;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private static readonly Dictionary<TypeDefinition, TypeDefinition> pptrsToParameters = new();

		public static void DoPass()
		{
			commonPPtrType = SharedState.Instance.Importer.ImportType(typeof(PPtr<>));

			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.Name.StartsWith("PPtr_"))
				{
					ITypeDefOrRef pptrTypeImported = SharedState.Instance.Importer.ImportType(typeof(IPPtr<>));

					bool usingMarkerInterface = !GetInterfaceParameterTypeDefinition(group, out TypeDefinition parameterType);
					group.Interface.AddPPtrInterfaceImplementation(parameterType, pptrTypeImported);

					pptrsToParameters.Add(group.Interface, parameterType);

					foreach (GeneratedClassInstance instance in group.Instances)
					{
						DoPassOnInstance(pptrTypeImported, usingMarkerInterface, parameterType, instance);
					}
				}
			}
		}

		private static void DoPassOnInstance(ITypeDefOrRef pptrTypeImported, bool usingMarkerInterface, TypeDefinition parameterType, GeneratedClassInstance instance)
		{
			TypeDefinition pptrType = instance.Type;
			pptrType.ImplementPPtrInterface();

			GenericInstanceTypeSignature implicitConversionResultType;

			if (usingMarkerInterface)
			{
				TypeDefinition instanceParameterType = GetInstanceParameterTypeDefinition(instance);
				instance.Type.AddPPtrInterfaceImplementation(instanceParameterType, pptrTypeImported);
				implicitConversionResultType = commonPPtrType.MakeGenericInstanceType(instanceParameterType.ToTypeSignature());
				pptrsToParameters.Add(instance.Type, instanceParameterType);
			}
			else
			{
				implicitConversionResultType = commonPPtrType.MakeGenericInstanceType(parameterType.ToTypeSignature());
				pptrsToParameters.Add(instance.Type, parameterType);
			}
			pptrType.AddImplicitConversion(implicitConversionResultType);
			pptrType.AddImplicitConversion<IUnityObjectBase>();
		}

		private static void AddPPtrInterfaceImplementation(this TypeDefinition type, TypeDefinition parameterType, ITypeDefOrRef pptrTypeImported)
		{
			GenericInstanceTypeSignature pptrInterface = pptrTypeImported.MakeGenericInstanceType(parameterType.ToTypeSignature());
			type.AddInterfaceImplementation(pptrInterface.ToTypeDefOrRef());
		}

		private static void ImplementPPtrInterface(this TypeDefinition pptrType)
		{
			pptrType.ImplementFullProperty(
				nameof(IPPtr.FileID),
				InterfaceUtils.InterfacePropertyImplementation,
				SharedState.Instance.Importer.Int32,
				pptrType.GetFieldByName("m_FileID_"));

			FieldDefinition pathidField = pptrType.GetFieldByName("m_PathID_");
			PropertyDefinition property = pptrType.AddFullProperty(nameof(IPPtr.PathID), InterfaceUtils.InterfacePropertyImplementation, SharedState.Instance.Importer.Int64);
			CilInstructionCollection getProcessor = property.GetMethod!.CilMethodBody!.Instructions;
			getProcessor.Add(CilOpCodes.Ldarg_0);
			getProcessor.Add(CilOpCodes.Ldfld, pathidField);
			if (pathidField.IsInt32Type())
			{
				getProcessor.Add(CilOpCodes.Conv_I8);
			}

			getProcessor.Add(CilOpCodes.Ret);
			CilInstructionCollection setProcessor = property.SetMethod!.CilMethodBody!.Instructions;
			setProcessor.Add(CilOpCodes.Ldarg_0);
			setProcessor.Add(CilOpCodes.Ldarg_1);
			if (pathidField.IsInt32Type())
			{
				setProcessor.Add(CilOpCodes.Conv_Ovf_I4);
			}

			setProcessor.Add(CilOpCodes.Stfld, pathidField);
			setProcessor.Add(CilOpCodes.Ret);
		}

		private static bool IsInt32Type(this FieldDefinition field) => field.Signature!.FieldType is CorLibTypeSignature signature && signature.ElementType == ElementType.I4;

		private static MethodDefinition AddImplicitConversion(this TypeDefinition pptrType, GenericInstanceTypeSignature resultTypeSignature)
		{
			return pptrType.AddConversion(resultTypeSignature, false);
		}

		private static MethodDefinition AddImplicitConversion<T>(this TypeDefinition pptrType)
		{
			ITypeDefOrRef importedInterface = SharedState.Instance.Importer.ImportType<T>();
			GenericInstanceTypeSignature resultPPtrSignature = commonPPtrType.MakeGenericInstanceType(importedInterface.ToTypeSignature());
			return pptrType.AddImplicitConversion(resultPPtrSignature);
		}

		private static MethodDefinition AddConversion(this TypeDefinition pptrType, GenericInstanceTypeSignature resultTypeSignature, bool isExplicit)
		{
			IMethodDefOrRef constructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, resultTypeSignature, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID_");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID_");

			MethodDefinition method = pptrType.AddEmptyConversion(pptrType.ToTypeSignature(), resultTypeSignature, !isExplicit);

			CilInstructionCollection processor = method.CilMethodBody!.Instructions;

			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, fileID);
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, pathID);
			if (pathID.IsInt32Type())
			{
				processor.Add(CilOpCodes.Conv_I8);
			}

			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);

			return method;
		}

		internal static bool GetInterfaceParameterTypeDefinition(SubclassGroup pptrGroup, out TypeDefinition type)
		{
			string parameterTypeName = pptrGroup.Name.Substring(5);
			if (SharedState.Instance.NameToTypeID.TryGetValue(parameterTypeName, out HashSet<int>? idList) && idList.Count == 1)
			{
				type = SharedState.Instance.ClassGroups[idList.First()].Interface;
				return true;
			}
			else
			{
				type = SharedState.Instance.MarkerInterfaces[parameterTypeName];
				return false;
			}
		}

		internal static TypeDefinition GetInstanceParameterTypeDefinition(GeneratedClassInstance pptrInstance)
		{
			string parameterTypeName = pptrInstance.Name.Substring(5);
			if (SharedState.Instance.NameToTypeID.TryGetValue(parameterTypeName, out HashSet<int>? list))
			{
				List<GeneratedClassInstance> instances = new();

				foreach (int id in list)
				{
					ClassGroup group = SharedState.Instance.ClassGroups[id];
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						if (instance.VersionRange.Intersects(pptrInstance.VersionRange) && parameterTypeName == instance.Name)
						{
							instances.Add(instance);
						}
					}
				}

				if (instances.Count == 0)
				{
					throw new Exception($"Could not find type {parameterTypeName} on version {pptrInstance.VersionRange.Start} to {pptrInstance.VersionRange.End}");
				}
				else if (instances.Count == 1)
				{
					return instances[0].Type;
				}
				else if (instances.Select(instance => instance.Group).Distinct().Count() == 1)
				{
					return instances[0].Group.Interface;
				}
				else
				{
					return SharedState.Instance.MarkerInterfaces[parameterTypeName];
				}
			}
			else
			{
				throw new Exception($"Could not find {parameterTypeName} in the name dictionary");
			}
		}
	}
}
