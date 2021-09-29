using Mono.Cecil;

namespace AssemblyDumper
{
	public class FieldInformation
	{
		public string Name { get; set; }
		public TypeReference FieldType { get; set; }
		public bool OnRelease { get; set; }
		public bool OnDebug { get; set; }
		public bool Inherited { get; set; }
		public FieldDefinition Definition { get; set; }
	}
}