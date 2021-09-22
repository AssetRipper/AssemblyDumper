using AssemblyDumper.Unity;
using Mono.Cecil;
using System.Collections.Generic;

namespace AssemblyDumper
{
	public static class SharedState
	{
		public static AssemblyDefinition Assembly { get; set; }
		public static UnityInfo Info { get; set; }
		public static Dictionary<string, TypeDefinition> TypeDictionary { get; } = new Dictionary<string, TypeDefinition>();
		public static Dictionary<string, UnityClass> ClassDictionary { get; set; }


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



	}
}
