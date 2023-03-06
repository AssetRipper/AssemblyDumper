using AssetRipper.AssemblyCreationTools;
using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.DataStructures;
using System.IO;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class SharedState : AssemblyBuilder
	{
		public const string AssemblyName = "AssetRipper.SourceGenerated";
		public const string RootNamespace = AssemblyName;
		public const string ClassesNamespace = RootNamespace + ".Classes";
		public const string EnumsNamespace = RootNamespace + ".Enums";
		public const string ExceptionsNamespace = RootNamespace + ".Exceptions";
		public const string InterfacesNamespace = RootNamespace + ".Interfaces";
		public const string HelpersNamespace = RootNamespace + ".Helpers";
		public const string MarkerInterfacesNamespace = RootNamespace + ".MarkerInterfaces";
		public const string NativeEnumsNamespace = RootNamespace + ".NativeEnums";
		public const string SubclassesNamespace = RootNamespace + ".Subclasses";

		public static string GetClassNamespace(int id) => $"{ClassesNamespace}.ClassID_{id}";
		public static string GetSubclassNamespace(string className) => $"{SubclassesNamespace}.{className}";

		private static SharedState? _instance;

		public static SharedState Instance => _instance ?? throw new NullReferenceException("ShareState.Instance not initialized");

		public UnityVersion MinVersion { get; }
		public UnityVersion MaxVersion { get; }
		public UnityVersion[] SourceVersions { get; }
		public UniversalCommonString CommonString { get; }
		public HistoryFile HistoryFile { get; }
		public Dictionary<int, VersionedList<UniversalClass>> ClassInformation { get; }
		public Dictionary<string, VersionedList<UniversalClass>> SubclassInformation { get; } = new();
		public Dictionary<int, ClassGroup> ClassGroups { get; } = new();
		public Dictionary<string, SubclassGroup> SubclassGroups { get; } = new();
		public Dictionary<TypeDefinition, ClassGroupBase> TypesToGroups { get; } = new();
		public Dictionary<string, HashSet<int>> NameToTypeID { get; } = new();
		public Dictionary<string, TypeDefinition> MarkerInterfaces { get; } = new();

		public IEnumerable<ClassGroupBase> AllGroups => ClassGroups.Values.Union<ClassGroupBase>(SubclassGroups.Values);
		public IEnumerable<TypeDefinition> AllTypes => TypesToGroups.Keys;
		public IEnumerable<TypeDefinition> AllNonInterfaceTypes => AllTypes.Where(t => !t.IsInterface);

		public MethodDefinition EmbeddedAttributeConstructor { get; }
		public MethodDefinition NullableAttributeConstructorByte { get; }
		public MethodDefinition NullableAttributeConstructorByteArray { get; }
		public MethodDefinition NullableContextAttributeConstructor { get; }

		private static readonly string referenceDirectory;

		static SharedState()
		{
			referenceDirectory = "";
			for (int i = 50; i >= 0; i--)
			{
				string path = @$"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.{i}\ref\net7.0\";
				if (Directory.Exists(path))
				{
					referenceDirectory = path;
					break;
				}
			}
			if (referenceDirectory.Length == 0)
			{
				throw new InvalidOperationException(".NET reference directory could not be found");
			}
		}

		private SharedState(
			UnityVersion[] sourceVersions,
			Dictionary<int, VersionedList<UniversalClass>> classes,
			UniversalCommonString commonString)
			: base(AssemblyName, new Version(0, 0, 0, 0), KnownCorLibs.SystemRuntime_v7_0_0_0)
		{
			SourceVersions = sourceVersions;
			CommonString = commonString;
			ClassInformation = classes;
			HistoryFile = HistoryFile.FromFile("consolidated.json");

			//input array is sequentially ordered
			MinVersion = sourceVersions[0].StripType();
			MaxVersion = sourceVersions[sourceVersions.Length - 1];

			AddReferenceModules();

			CompilerInjectedAttributeCreator.CreateEmbeddedAttribute(Importer, out MethodDefinition embeddedAttributeConstructor);
			EmbeddedAttributeConstructor = embeddedAttributeConstructor;
			NullableContextAttributeConstructor = CompilerInjectedAttributeCreator.CreateNullableContextAttribute(Importer, embeddedAttributeConstructor)
				.GetConstructor(1);
			TypeDefinition nullableAttributeType = CompilerInjectedAttributeCreator.CreateNullableAttribute(Importer, embeddedAttributeConstructor);
			NullableAttributeConstructorByte = nullableAttributeType.Methods
				.Single(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType is CorLibTypeSignature);
			NullableAttributeConstructorByteArray = nullableAttributeType.Methods
				.Single(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType is SzArrayTypeSignature);
		}

		public static void Initialize(
			UnityVersion[] sourceVersions,
			Dictionary<int, VersionedList<UniversalClass>> classes,
			UniversalCommonString commonString)
		{
			_instance = new SharedState(sourceVersions, classes, commonString);
			_instance.AddTargetFrameworkAttribute(".NET 7.0");
		}

		private void AddReferenceModules()
		{
			AddLocalReferenceModule("AssetRipper.Assets");
			AddLocalReferenceModule("AssetRipper.Yaml");
			AddLocalReferenceModule("AssetRipper.IO.Endian");
			AddLocalReferenceModule("AssetRipper.VersionUtilities");
			AddLocalReferenceModule("AssetRipper.IO.Files");
			AddLocalReferenceModule("AssetRipper.Numerics");
			AddSystemReferenceModule("System.Runtime");
			AddSystemReferenceModule("System.Numerics.Vectors");
			AddSystemReferenceModule("System.Linq");
			AddSystemReferenceModule("System.Collections");
			AddLocalReferenceModule("AssetRipper.AssemblyDumper");//needed for member cloning
		}

		private void AddLocalReferenceModule(string name)
		{
			string path = Path.Combine(AppContext.BaseDirectory, $"{name}.dll"); ;
			AddReferenceModule(path);
		}

		private void AddSystemReferenceModule(string name)
		{
			string path = $"{referenceDirectory}{name}.dll";
			AddReferenceModule(path);
		}

		private void AddReferenceModule(string path)
		{
			ModuleDefinition module = ModuleDefinition.FromFile(path);
			Importer.AddReferenceModule(module);
		}

		public static void Clear() => _instance = null;

		internal GeneratedClassInstance GetGeneratedInstanceForObjectType(string typeName, UnityVersion version)
		{
			if (NameToTypeID.TryGetValue(typeName, out HashSet<int>? list))
			{
				foreach (int id in list)
				{
					ClassGroup group = ClassGroups[id];
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						if (instance.VersionRange.Contains(version) && typeName == instance.Name)
						{
							return instance;
						}
					}
				}
				throw new Exception($"Could not find type {typeName} on version {version}");
			}
			else
			{
				throw new Exception($"Could not find {typeName} in the name dictionary");
			}
		}

		internal ClassGroup GetClassGroupForObjectType(string typeName, UnityVersion version)
		{
			if (NameToTypeID.TryGetValue(typeName, out HashSet<int>? list))
			{
				foreach (int id in list)
				{
					ClassGroup group = ClassGroups[id];
					foreach (GeneratedClassInstance instance in group.Instances)
					{
						if (instance.VersionRange.Contains(version) && typeName == instance.Name)
						{
							return group;
						}
					}
				}
				throw new Exception($"Could not find type {typeName} on version {version}");
			}
			else
			{
				throw new Exception($"Could not find {typeName} in the name dictionary");
			}
		}
	}
}
