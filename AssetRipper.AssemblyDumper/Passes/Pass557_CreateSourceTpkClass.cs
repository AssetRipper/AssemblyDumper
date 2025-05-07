﻿using AsmResolver;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass557_CreateSourceTpkClass
	{
		public static void DoPass()
		{
			//Type Tree
			{
				TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "SourceTpk");
				FieldDefinition field = CreateInternalStorageClass("SourceTpkData", SharedState.Instance.TpkData);
				AddGetStreamMethod(type, field);
				AddDataProperty(type, field);
				AddVersionsProperty(type, SharedState.Instance.SourceVersions);

				DocumentationHandler.AddTypeDefinitionLine(type, "Type Trees are used in the AssetRipper source generation.");
				DocumentationHandler.AddTypeDefinitionLine(type, $"This data is sourced from {SharedState.Instance.SourceVersions.Length} versions of Unity.");
				DocumentationHandler.AddTypeDefinitionLine(type, $"See: {SeeXmlTagGenerator.MakeHRef(@"https://github.com/AssetRipper/TypeTreeDumps")}");
			}

			//Engine Assets
			{
				TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "EngineAssetsTpk");
				FieldDefinition field = CreateInternalStorageClass("EngineAssetsTpkData", File.ReadAllBytes("engine_assets.tpk"));
				AddGetStreamMethod(type, field);
				AddDataProperty(type, field);

				DocumentationHandler.AddTypeDefinitionLine(type, "Engine Assets are embedded during the AssetRipper source generation.");
				DocumentationHandler.AddTypeDefinitionLine(type, $"This data is sourced from many versions of Unity.");
				DocumentationHandler.AddTypeDefinitionLine(type, $"See: {SeeXmlTagGenerator.MakeHRef(@"https://github.com/AssetRipper/DocumentationDumps")}");
			}

			//Assemblies
			{
				TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "ReferenceAssembliesJson");
				FieldDefinition field = CreateInternalStorageClass("ReferenceAssembliesJsonData", File.ReadAllBytes("assemblies.json"));
				AddGetStreamMethod(type, field);
				AddDataProperty(type, field);
			}
		}

		private static void AddVersionsProperty(TypeDefinition type, IReadOnlyCollection<UnityVersion> versions)
		{
			GenericInstanceTypeSignature readOnlySet = SharedState.Instance.Importer.ImportType(typeof(IReadOnlySet<>))
							.MakeGenericInstanceType(SharedState.Instance.Importer.ImportTypeSignature<UnityVersion>());
			GenericInstanceTypeSignature unityVersionHashSet = SharedState.Instance.Importer.ImportType(typeof(HashSet<>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.ImportTypeSignature<UnityVersion>());
			IMethodDefOrRef hashsetConstructor = MethodUtils.MakeConstructorOnGenericType(SharedState.Instance.Importer, unityVersionHashSet, 0);
			IMethodDefOrRef addMethod = MethodUtils.MakeMethodOnGenericType(
				SharedState.Instance.Importer,
				unityVersionHashSet,
				SharedState.Instance.Importer.LookupMethod(typeof(HashSet<>), m => m.Name == nameof(HashSet<int>.Add)));

			IMethodDefOrRef unityVersionConstructor = SharedState.Instance.Importer.ImportConstructor<UnityVersion>(5);

			const string propertyName = "Versions";
			FieldDefinition field = type.AddField(readOnlySet, $"<{propertyName}>k__BackingField", true, FieldVisibility.Private);

			field.Attributes |= FieldAttributes.InitOnly;
			field.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			MethodDefinition staticConstructor = type.GetOrCreateStaticConstructor();
			CilInstructionCollection processor = staticConstructor.CilMethodBody!.Instructions;
			processor.Pop();//pop the ret
			processor.Add(CilOpCodes.Newobj, hashsetConstructor);
			foreach (UnityVersion version in versions)
			{
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Major);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Minor);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Build);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.Type);
				processor.Add(CilOpCodes.Ldc_I4, (int)version.TypeNumber);
				processor.Add(CilOpCodes.Newobj, unityVersionConstructor);
				processor.Add(CilOpCodes.Call, addMethod);
				processor.Add(CilOpCodes.Pop);
			}
			processor.Add(CilOpCodes.Stsfld, field);
			processor.Add(CilOpCodes.Ret);

			processor.OptimizeMacros();

			type.ImplementGetterProperty(
					propertyName,
					MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName,
					readOnlySet,
					field)
				.GetMethod!.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			Console.WriteLine($"\t{versions.Count} source versions.");
		}

		private static void AddDataProperty(TypeDefinition type, FieldDefinition field)
		{
			TypeSignature propertySignature = SharedState.Instance.Importer.ImportType(typeof(ReadOnlySpan<>))
				.MakeGenericInstanceType(SharedState.Instance.Importer.UInt8);

			PropertyDefinition property = type.AddGetterProperty("Data", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, propertySignature);

			MethodDefinition method = property.GetMethod!;
			CilInstructionCollection processor = method.GetProcessor();

			MemberReference reference = new MemberReference(propertySignature.ToTypeDefOrRef(), ".ctor", SharedState.Instance.Importer.ImportMethod(typeof(ReadOnlySpan<>),
				m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType is SzArrayTypeSignature).Signature);
			processor.Add(CilOpCodes.Ldsfld, field);
			processor.Add(CilOpCodes.Newobj, reference);
			processor.Add(CilOpCodes.Ret);
		}

		private static void AddGetStreamMethod(TypeDefinition type, FieldDefinition field)
		{
			ITypeDefOrRef returnType = SharedState.Instance.Importer.ImportType(typeof(MemoryStream));

			MethodDefinition method = type.AddMethod("GetStream", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, returnType.ToTypeSignature());
			CilInstructionCollection processor = method.GetProcessor();

			MemberReference reference = new MemberReference(returnType, ".ctor", SharedState.Instance.Importer.ImportMethod(typeof(MemoryStream), m =>
			{
				return m.IsConstructor
					&& m.Parameters.Count == 2
					&& m.Parameters[0].ParameterType is SzArrayTypeSignature
					&& m.Parameters[1].ParameterType is CorLibTypeSignature;
			})
				.Signature);
			processor.Add(CilOpCodes.Ldsfld, field);
			processor.Add(CilOpCodes.Ldc_I4_0);//Not writable
			processor.Add(CilOpCodes.Newobj, reference);
			processor.Add(CilOpCodes.Ret);
		}

		private static FieldDefinition CreateInternalStorageClass(string className, byte[] data)
		{
			FieldDefinition field;
			TypeDefinition internalType = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, className);
			internalType.IsPublic = false;

			FieldDefinition privateImplementationField = AddStoredDataField(data);

			field = internalType.AddField(SharedState.Instance.Importer.UInt8.MakeSzArrayType(), "data", true, FieldVisibility.Internal);
			field.IsInitOnly = true;

			//Static Constructor
			{
				MethodDefinition staticConstructor = internalType.GetOrCreateStaticConstructor();
				CilInstructionCollection processor = staticConstructor.CilMethodBody!.Instructions;
				processor.Pop();//pop the ret
				processor.Add(CilOpCodes.Ldc_I4, data.Length);
				processor.Add(CilOpCodes.Newarr, SharedState.Instance.Importer.UInt8.ToTypeDefOrRef());
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldtoken, privateImplementationField);
				processor.Add(CilOpCodes.Call, SharedState.Instance.Importer.ImportMethod(typeof(RuntimeHelpers), m => m.Name == nameof(RuntimeHelpers.InitializeArray)));
				processor.Add(CilOpCodes.Stsfld, field);
				processor.Add(CilOpCodes.Ret);
			}

			return field;
		}

		/// <summary>
		/// Adds a byte array field to the PrivateImplementationDetails class.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="data">The data contained within the field.</param>
		/// <returns>The field's <see cref="FieldDefinition"/>.</returns>
		private static FieldDefinition AddStoredDataField(byte[] data)
		{
			TypeDefinition nestedType = GetOrCreateStaticArrayInitType(data.Length);

			FieldDefinition privateImplementationField = SharedState.Instance.PrivateImplementationDetails.AddField(nestedType.ToTypeSignature(), HashDataToBase64(data), true, FieldVisibility.Internal);
			privateImplementationField.IsInitOnly = true;
			privateImplementationField.FieldRva = new DataSegment(data);
			privateImplementationField.HasFieldRva = true;
			privateImplementationField.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			return privateImplementationField;

			//This might not be the correct way to choose a field name, but I think the specification allows it.
			//In any case, ILSpy handles it the way we want, which is all that matters.
			static string HashDataToBase64(byte[] data)
			{
				byte[] hash = SHA256.HashData(data);
				return Convert.ToBase64String(hash, Base64FormattingOptions.None);
			}
		}

		private static TypeDefinition GetOrCreateStaticArrayInitType(int length)
		{
			string name = $"__StaticArrayInitTypeSize={length}";

			foreach (TypeDefinition nestedType in SharedState.Instance.PrivateImplementationDetails.NestedTypes)
			{
				if (nestedType.Name == name)
				{
					return nestedType;
				}
			}

			TypeDefinition result = new TypeDefinition(null, name,
				TypeAttributes.NestedPrivate |
				TypeAttributes.ExplicitLayout |
				TypeAttributes.AnsiClass |
				TypeAttributes.Sealed);
			SharedState.Instance.PrivateImplementationDetails.NestedTypes.Add(result);

			result.BaseType = SharedState.Instance.Importer.ImportType(typeof(ValueType));
			result.ClassLayout = new ClassLayout(1, (uint)length);
			result.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			return result;
		}
	}
}
