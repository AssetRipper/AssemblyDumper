using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using AssetRipper.Core.Math;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass22_VectorImplicitConversions
	{
		const MethodAttributes ConversionAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
		public static void DoPass()
		{
			System.Console.WriteLine("Pass 22: Vector Implicit Conversions");
			if (SharedState.TypeDictionary.TryGetValue("float2", out TypeDefinition float2Type)) //not sure if this exists
			{
				AddConversion<Vector2f>(float2Type, 2);
			}
			if (SharedState.TypeDictionary.TryGetValue("float3", out TypeDefinition float3Type))
			{
				AddConversion<Vector3f>(float3Type, 3);
			}
			if (SharedState.TypeDictionary.TryGetValue("float4", out TypeDefinition float4Type))
			{
				AddConversion<Vector4f>(float4Type, 4);
			}
			if (SharedState.TypeDictionary.TryGetValue("int2_storage", out TypeDefinition int2storageType))
			{
				AddConversion<Vector2i>(int2storageType, 2);
			}
			if (SharedState.TypeDictionary.TryGetValue("int3_storage", out TypeDefinition int3storageType))
			{
				AddConversion<Vector3i>(int3storageType, 3);
			}
			if (SharedState.TypeDictionary.TryGetValue("Vector2f", out TypeDefinition vector2Type))
			{
				AddConversion<Vector2f>(vector2Type, 2);
			}
			if (SharedState.TypeDictionary.TryGetValue("Vector3f", out TypeDefinition vector3Type))
			{
				AddConversion<Vector3f>(vector3Type, 3);
			}
			if (SharedState.TypeDictionary.TryGetValue("Vector4f", out TypeDefinition vector4Type))
			{
				AddConversion<Vector4f>(vector4Type, 4);
			}
			if (SharedState.TypeDictionary.TryGetValue("Quaternionf", out TypeDefinition quaternionType))
			{
				AddConversion<Quaternionf>(quaternionType, 4);
			}
		}

		private static void AddConversion<T>(TypeDefinition type, int size)
		{
			ITypeDefOrRef commonType = SharedState.Module.ImportCommonType<T>();
			IMethodDefOrRef constructor = SharedState.Module.ImportCommonConstructor<T>(size);

			MethodDefinition implicitMethod = type.AddMethod("op_Implicit", ConversionAttributes, commonType);
			implicitMethod.AddParameter("value", type);

			implicitMethod.CilMethodBody.InitializeLocals = true;
			var processor = implicitMethod.CilMethodBody.Instructions;

			FieldDefinition x = type.Fields.Single(field => field.Name == "x");
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, x);
			FieldDefinition y = type.Fields.Single(field => field.Name == "y");
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, y);

			if(size > 2)
			{
				FieldDefinition z = type.Fields.Single(field => field.Name == "z");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, z);
			}
			if(size > 3)
			{
				FieldDefinition w = type.Fields.Single(field => field.Name == "w");
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldfld, w);
			}

			processor.Add(CilOpCodes.Newobj, constructor);
			processor.Add(CilOpCodes.Ret);
		}
	}
}
