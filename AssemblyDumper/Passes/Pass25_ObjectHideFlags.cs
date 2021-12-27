using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System;

namespace AssemblyDumper.Passes
{
	public static class Pass25_ObjectHideFlags
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
			Console.WriteLine("Pass 25: Object Hide Flags");
			if (!SharedState.TypeDictionary.TryGetValue("Object", out TypeDefinition type))
			{
				throw new Exception("TypeDictionary has no Object");
			}
			type.AddHideFlagsProperty();
		}

		private static void AddHideFlagsProperty(this TypeDefinition type)
		{
			ITypeDefOrRef hideFlagsEnum = SharedState.Importer.ImportCommonType<AssetRipper.Core.Classes.Object.HideFlags>();
			type.ImplementFullProperty("ObjectHideFlags", PropertyOverrideAttributes, hideFlagsEnum.ToTypeSignature(), type.GetFieldByName(FieldName));
		}
	}
}
