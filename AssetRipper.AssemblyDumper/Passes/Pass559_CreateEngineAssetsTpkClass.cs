using AsmResolver;
using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using System.IO;
using System.Runtime.CompilerServices;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass559_CreateEngineAssetsTpkClass
	{
		public static void DoPass()
		{
			TypeDefinition type = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "EngineAssetsTpk");
			AddGetStreamMethod(type);
		}

		private static void AddGetStreamMethod(TypeDefinition type)
		{
			byte[] data = File.ReadAllBytes("engine_assets.tpk");

			TypeDefinition nestedType = new TypeDefinition(null, $"__StaticArrayInitTypeSize={data.Length}",
				TypeAttributes.NestedPrivate |
				TypeAttributes.ExplicitLayout |
				TypeAttributes.AnsiClass |
				TypeAttributes.Sealed);
			SharedState.Instance.PrivateImplementationDetails.NestedTypes.Add(nestedType);

			nestedType.BaseType = SharedState.Instance.Importer.ImportType(typeof(ValueType));
			nestedType.ClassLayout = new ClassLayout(1, (uint)data.Length);
			nestedType.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			FieldDefinition privateImplementationField = SharedState.Instance.PrivateImplementationDetails.AddField(nestedType.ToTypeSignature(), "engineAssetsTpkData", true, FieldVisibility.Internal);
			privateImplementationField.IsInitOnly = true;
			privateImplementationField.FieldRva = new DataSegment(data);
			privateImplementationField.HasFieldRva = true;
			privateImplementationField.AddCompilerGeneratedAttribute(SharedState.Instance.Importer);

			TypeDefinition internalType = StaticClassCreator.CreateEmptyStaticClass(SharedState.Instance.Module, SharedState.RootNamespace, "EngineAssetsTpkData");
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

			//GetStream
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
		}
	}
}
