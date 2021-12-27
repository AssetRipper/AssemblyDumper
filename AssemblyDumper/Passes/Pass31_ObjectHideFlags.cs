using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass31_ObjectHideFlags
	{
		const MethodAttributes PropertyOverrideAttributes =
			MethodAttributes.Public |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.ReuseSlot |
			MethodAttributes.Virtual;
		const string FieldName = "m_ObjectHideFlags";

		public static void DoPass()
		{
			Console.WriteLine("Pass 31: Object Hide Flags");
			if (!SharedState.TypeDictionary.TryGetValue("Object", out TypeDefinition type))
			{
				throw new Exception("TypeDictionary has no Object");
			}
			type.AddHideFlagsProperty();
		}

		private static void AddHideFlagsProperty(this TypeDefinition type)
		{
			FieldDefinition field = type.GetHideFlagsField();
			ITypeDefOrRef hideFlagsEnum = SharedState.Importer.ImportCommonType<AssetRipper.Core.Classes.Object.HideFlags>();
			PropertyDefinition property = type.AddFullProperty("ObjectHideFlags", PropertyOverrideAttributes, hideFlagsEnum);
			property.FillGetter(field);
			property.FillSetter(field);
		}

		private static MethodDefinition FillGetter(this PropertyDefinition property, FieldDefinition field)
		{
			MethodDefinition getter = property.GetMethod;

			var processor = getter.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldfld, field);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return getter;
		}

		private static MethodDefinition FillSetter(this PropertyDefinition property, FieldDefinition field)
		{
			MethodDefinition setter = property.SetMethod;

			var processor = setter.CilMethodBody.Instructions;
			processor.Add(CilOpCodes.Ldarg_0);
			processor.Add(CilOpCodes.Ldarg_1); //value
			processor.Add(CilOpCodes.Stfld, field);
			processor.Add(CilOpCodes.Ret);
			processor.OptimizeMacros();
			return setter;
		}

		private static FieldDefinition GetHideFlagsField(this TypeDefinition type)
		{
			return type.Fields.Single(field => field.Name == FieldName);
		}
	}
}
