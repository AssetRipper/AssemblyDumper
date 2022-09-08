using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.Core.Classes.Misc;
using AssetRipper.Core.Interfaces;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass080_PPtrConversions
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private static ITypeDefOrRef commonPPtrType;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		const MethodAttributes InterfacePropertyImplementationAttributes =
			MethodAttributes.Public |
			MethodAttributes.Final |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.NewSlot |
			MethodAttributes.Virtual;
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

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

					foreach (GeneratedClassInstance instance in group.Instances)
					{
						DoPassOnTypeDefinition(instance.Type, parameterType);
						if (usingMarkerInterface)
						{
							TypeDefinition instanceParameterType = GetInstanceParameterTypeDefinition(instance);
							instance.Type.AddPPtrInterfaceImplementation(instanceParameterType, pptrTypeImported);
						}
					}
				}
			}
		}

		private static void AddPPtrInterfaceImplementation(this TypeDefinition type, TypeDefinition parameterType, ITypeDefOrRef pptrTypeImported)
		{
			GenericInstanceTypeSignature pptrInterface = pptrTypeImported.MakeGenericInstanceType(parameterType.ToTypeSignature());
			type.AddInterfaceImplementation(pptrInterface.ToTypeDefOrRef());
		}

		private static void DoPassOnTypeDefinition(TypeDefinition pptrType, TypeDefinition parameterType)
		{
			pptrType.ImplementPPtrInterface();

			GenericInstanceTypeSignature implicitConversionResultType = commonPPtrType.MakeGenericInstanceType(parameterType.ToTypeSignature());

			pptrType.AddImplicitConversion(implicitConversionResultType);
			pptrType.AddExplicitConversion<IUnityObjectBase>();
		}

		private static void ImplementPPtrInterface(this TypeDefinition pptrType)
		{
			pptrType.ImplementFullProperty(nameof(IPPtr.FileIndex), InterfacePropertyImplementationAttributes, SharedState.Instance.Importer.Int32, pptrType.GetFieldByName("m_FileID"));

			FieldDefinition pathidField = pptrType.GetFieldByName("m_PathID");
			PropertyDefinition property = pptrType.AddFullProperty(nameof(IPPtr.PathIndex), InterfacePropertyImplementationAttributes, SharedState.Instance.Importer.Int64);
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

		private static bool IsInt32Type(this FieldDefinition field) => field.Signature!.FieldType.Name == "Int32";

		private static MethodDefinition AddImplicitConversion(this TypeDefinition pptrType, GenericInstanceTypeSignature resultTypeSignature)
		{
			return pptrType.AddConversion(resultTypeSignature, false);
		}

		private static MethodDefinition AddExplicitConversion(this TypeDefinition pptrType, GenericInstanceTypeSignature resultTypeSignature)
		{
			return pptrType.AddConversion(resultTypeSignature, true);
		}

		private static MethodDefinition AddExplicitConversion<T>(this TypeDefinition pptrType)
		{
			ITypeDefOrRef importedInterface = SharedState.Instance.Importer.ImportType<T>();
			GenericInstanceTypeSignature resultPPtrSignature = commonPPtrType.MakeGenericInstanceType(importedInterface.ToTypeSignature());
			return pptrType.AddExplicitConversion(resultPPtrSignature);
		}

		private static MethodDefinition AddConversion(this TypeDefinition pptrType, GenericInstanceTypeSignature resultTypeSignature, bool isExplicit)
		{
			IMethodDefOrRef constructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, resultTypeSignature, 2);

			FieldDefinition fileID = pptrType.Fields.Single(field => field.Name == "m_FileID");
			FieldDefinition pathID = pptrType.Fields.Single(f => f.Name == "m_PathID");

			string methodName = isExplicit ? "op_Explicit" : "op_Implicit";
			MethodDefinition method = pptrType.AddMethod(methodName, ConversionAttributes, resultTypeSignature);
			method.AddParameter(pptrType.ToTypeSignature(), "value");

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
			string parameterTypeName = pptrGroup.Name.Substring(5, pptrGroup.Name.LastIndexOf('_') - 5);
			if (SharedState.Instance.NameToTypeID.TryGetValue(parameterTypeName, out HashSet<int>? idList) && idList.Count == 1)
			{
				type = SharedState.Instance.ClassGroups[idList.First()].GetSingularTypeOrInterface();
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
			string parameterTypeName = pptrInstance.Name.Substring(5, pptrInstance.Name.LastIndexOf('_') - 5);
			if (SharedState.Instance.NameToTypeID.TryGetValue(parameterTypeName, out HashSet<int>? list))
			{
				GeneratedClassInstance? result = null;

				foreach (int id in list)
				{
					ClassGroup group = SharedState.Instance.ClassGroups[id];
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						if (instance.VersionRange.Contains(pptrInstance.VersionRange.Start) && parameterTypeName == instance.Name)
						{
							if (result is null)
							{
								result = instance;
							}
							else
							{
								return SharedState.Instance.MarkerInterfaces[parameterTypeName];
							}
						}
					}
				}

				return result is null
					? throw new Exception($"Could not find type {parameterTypeName} on version {pptrInstance.VersionRange.Start}")
					: result.Type;
			}
			else
			{
				throw new Exception($"Could not find {parameterTypeName} in the name dictionary");
			}
		}
	}
}
