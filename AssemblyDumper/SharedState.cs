using AssemblyDumper.Unity;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper
{
	public static class SharedState
	{
		public static AssemblyDefinition Assembly { get; set; }
		public static string Version { get; private set; }
		public static List<UnityString> Strings { get; private set; }
		public static Dictionary<string, UnityClass> ClassDictionary { get; private set; }
		public static Dictionary<string, TypeDefinition> TypeDictionary { get; } = new Dictionary<string, TypeDefinition>();


		//Namespaces
		public static string RootNamespace { get; set; } = "AssemblyDumper";
		public static string AttributesNamespace => RootNamespace + ".Attributes";
		public static string Classesnamespace => RootNamespace + ".Classes";
		public static string EnumsNamespace => RootNamespace + ".Enums";
		public static string ExamplesNamespace => RootNamespace + ".Examples";
		public static string InterfacesNamespace => RootNamespace + ".Interfaces";
		public static string IONamespace => RootNamespace + ".IO";
		public static string UtilsNamespace => RootNamespace + ".Utils";


		//Enums
		public static TypeDefinition PersistentTypeIDDefinition { get; set; }
		public static TypeDefinition CommonStringEnumDefinition { get; set; }


		public static void Initialize(UnityInfo info)
		{
			Version = info.Version;
			Strings = info.Strings;
			ClassDictionary = info.Classes.ToDictionary(x => x.Name, x => x);
		}
	}
}
