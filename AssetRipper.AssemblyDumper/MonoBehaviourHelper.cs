using AssetRipper.Core.Classes;
using AssetRipper.Core.Classes.Misc;
using AssetRipper.Core.Interfaces;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.Logging;
using AssetRipper.Core.Parser.Asset;
using AssetRipper.Core.Project;
using AssetRipper.Core.Structure.Assembly.Serializable;
using AssetRipper.Yaml;

namespace AssetRipper.AssemblyDumper
{
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
	internal static class MonoBehaviourHelper
	{
		/*
		public static void ReadStructure(this IMonoBehaviour monoBehaviour, AssetReader reader)
		{
			if (!monoBehaviour.IsAssemblyManagerSet())
			{
				return;
			}

			IPPtr pptr = monoBehaviour.ScriptPtr;
			IUnityObjectBase unityObjectBase = FindAsset(monoBehaviour, pptr);
			if (unityObjectBase == null)
			{
				return;
			}
			IMonoScript monoScript = (IMonoScript)unityObjectBase;
			
			monoBehaviour.Structure = ReadStructure(monoScript, originalAssemblyName, @namespace, @class, reader);
			return;
		}*/

		public static IUnityObjectBase FindAsset(IUnityObjectBase monoBehaviour, IPPtr pptr)
		{
			return new PPtr<IUnityObjectBase>(pptr.FileIndex, pptr.PathIndex).FindAsset(monoBehaviour.SerializedFile);
		}

		public static bool IsAssemblyManagerSet(this IUnityObjectBase monoBehaviour)
		{
			return monoBehaviour.SerializedFile.Collection.AssemblyManager.IsSet;
		}

		public static SerializableStructure ReadStructureInjected(
			IUnityObjectBase monoScript,
			Utf8StringBase originalAssemblyName,
			Utf8StringBase @namespace,
			Utf8StringBase className,
			AssetReader reader,
			Utf8StringBase monoScriptName,
			Utf8StringBase monoBehaviourName)
		{
			SerializableType behaviourType = monoScript.GetBehaviourType(originalAssemblyName.String, @namespace.String, className.String);
			if (behaviourType == null)
			{
				Logger.Log(LogType.Warning, LogCategory.Import, $"Unable to read {monoBehaviourName.String}, because valid definition for script {monoScriptName.String} wasn't found");
				return null;
			}

			SerializableStructure structure = behaviourType.CreateSerializableStructure();
			try
			{
				structure.Read(reader);
			}
			catch (System.Exception ex)
			{
				structure = null;
				Logger.Log(LogType.Error, LogCategory.Import, $"Unable to read {monoBehaviourName.String}, because script layout {monoScriptName.String} mismatch binary content");
				Logger.Log(LogType.Debug, LogCategory.Import, $"Stack trace: {ex}");
			}
			return structure;
		}

		public static void MaybeWriteStructure(this SerializableStructure structure, AssetWriter writer)
		{
			if (structure != null)
			{
				structure.Write(writer);
			}
		}

		public static void MaybeExportYamlForStructure(this SerializableStructure structure, YamlMappingNode node, IExportContainer container)
		{
			if (structure != null)
			{
				YamlMappingNode structureNode = (YamlMappingNode)structure.ExportYaml(container);
				node.Append(structureNode);
			}
		}

		public static IEnumerable<PPtr<IUnityObjectBase>> MaybeFetchDependenciesForStructure(this SerializableStructure structure, DependencyContext context)
		{
			if (structure != null)
			{
				foreach (PPtr<IUnityObjectBase> asset in context.FetchDependenciesFromDependent(structure, structure.Type.Name))
				{
					yield return asset;
				}
			}
		}
	}
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
}
