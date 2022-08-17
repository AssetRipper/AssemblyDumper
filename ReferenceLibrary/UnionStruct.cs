using System.Runtime.InteropServices;

namespace ReferenceLibrary
{
	[StructLayout(LayoutKind.Explicit)]
	public struct UnionStruct
	{
		[FieldOffset(0)]
		public int integer;
		[FieldOffset(0)]
		public float single;
	}
}
