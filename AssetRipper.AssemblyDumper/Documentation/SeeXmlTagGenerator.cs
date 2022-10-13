namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class SeeXmlTagGenerator
	{
		private static string MakeCRef(string interior)
		{
			return $"<see cref=\"{interior}\"/>";
		}

		public static string MakeCRef(TypeDefinition type)
		{
			return MakeCRef(XmlUtils.GetStringReference(type));
		}

		public static string MakeCRefForClassInterface(int classID)
		{
			return MakeCRef(SharedState.Instance.ClassGroups[classID].Interface);
		}

		public static string MakeCRefForSubclassInterface(string name)
		{
			return MakeCRef(SharedState.Instance.SubclassGroups[name].Interface);
		}
	}
}
