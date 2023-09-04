using AsmResolver;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass557_CreateSourceTpkClass
	{
		public static void DoPass()
		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "SourceTpk");
			AddDataProperty(type);
			AddVersionsProperty(type);
		}

		private static void AddVersionsProperty(TypeDefinition type)
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
			foreach (UnityVersion version in SharedState.Instance.SourceVersions)
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

			int numberOfVersions = SharedState.Instance.SourceVersions.Length;
			DocumentationHandler.AddTypeDefinitionLine(type, "Type Trees are used in the AssetRipper source generation.");
			DocumentationHandler.AddTypeDefinitionLine(type, $"This data is sourced from {numberOfVersions} versions of Unity.");
			DocumentationHandler.AddTypeDefinitionLine(type, $"See: {SeeXmlTagGenerator.MakeHRef(@"https://github.com/AssetRipper/TypeTreeDumps")}");

			Console.WriteLine($"\t{numberOfVersions} source versions.");
		}

		private static void AddDataProperty(TypeDefinition type)
		{
			byte[] data = File.ReadAllBytes("uncompressed.tpk");

			TypeDefinition nestedType = new TypeDefinition(null, $"__StaticArrayInitTypeSize={data.Length}",
				TypeAttributes.NestedPrivate |
				TypeAttributes.ExplicitLayout |
				TypeAttributes.AnsiClass |
				TypeAttributes.Sealed);
			SharedState.Instance.PrivateImplementationDetails.NestedTypes.Add(nestedType);

			nestedType.BaseType = SharedState.Instance.Importer.ImportType(typeof(ValueType));
			nestedType.ClassLayout = new ClassLayout(1, (uint)data.Length);
			nestedType.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			FieldDefinition privateImplementationField = SharedState.Instance.PrivateImplementationDetails.AddField(nestedType.ToTypeSignature(), "sourceTpkData", true, FieldVisibility.Internal);
			privateImplementationField.IsInitOnly = true;
			privateImplementationField.FieldRva = new DataSegment(data);
			privateImplementationField.HasFieldRva = true;
			privateImplementationField.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			TypeDefinition internalType = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "SourceTpkData");
			internalType.IsPublic = false;
			FieldDefinition field = internalType.AddField(SharedState.Instance.Importer.UInt8.MakeSzArrayType(), "data", true, FieldVisibility.Internal);
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

			//Property
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

			long value = default;
			ReadOnlySpan<byte> span = MemoryMarshal.AsBytes(new ReadOnlySpan<long>(in value));
			ReadOnlySpan<byte> span2 = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
		}
	}
}
