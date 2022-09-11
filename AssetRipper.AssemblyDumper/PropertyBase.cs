using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper
{
	internal abstract class PropertyBase
	{
		protected PropertyBase(PropertyDefinition definition)
		{
			Definition = definition;
		}

		public PropertyDefinition Definition { get; }
		public PropertyDefinition? SpecialDefinition { get; set; }
		public DataMemberHistory? History { get; set; }
		public MethodDefinition? HasMethod { get; set; }
	}
}
