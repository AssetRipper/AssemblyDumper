using AssetRipper.Core.Interfaces;
using AssetRipper.Core.Parser.Utils;
using AssetRipper.Core.Structure.Assembly;
using AssetRipper.Core.Structure.Assembly.Managers;
using AssetRipper.Core.Structure.Assembly.Serializable;

namespace AssetRipper.AssemblyDumper
{
	internal static class MonoScriptHelper
	{
		public static SerializableType GetBehaviourType(this IUnityObjectBase monoScript, string originalAssemblyName, string @namespace, string @class)
		{
			ScriptIdentifier scriptID = HasNamespace(monoScript.SerializedFile.Version)
				? monoScript.GetScriptIdentifier(originalAssemblyName, @namespace, @class)
				: monoScript.GetScriptIdentifierWithoutNamespace(originalAssemblyName, @class);
			return GetSerializableType(monoScript, scriptID);
		}

		/// <summary>
		/// 3.0.0 and greater
		/// </summary>
		private static bool HasNamespace(UnityVersion version) => version.IsGreaterEqual(3);

		private static string GetAssemblyNameFixed(string originalAssemblyName) => FilenameUtils.FixAssemblyName(originalAssemblyName);

		private static ScriptIdentifier GetScriptIdentifier(this IUnityObjectBase monoScript, string originalAssemblyName, string @namespace, string @class)
		{
			return monoScript.GetAssemblyManager().GetScriptID(GetAssemblyNameFixed(originalAssemblyName), @namespace, @class);
		}

		//This needs used for versions before Unity 3
		private static ScriptIdentifier GetScriptIdentifierWithoutNamespace(this IUnityObjectBase monoScript, string originalAssemblyName, string @class)
		{
			return monoScript.GetAssemblyManager().GetScriptID(GetAssemblyNameFixed(originalAssemblyName), @class);
		}

		private static bool IsScriptIdentifierValid(IUnityObjectBase monoScript, ScriptIdentifier scriptId)
		{
			return monoScript.GetAssemblyManager().IsValid(scriptId);
		}

		private static SerializableType ForceGetSerializableType(IUnityObjectBase monoScript, ScriptIdentifier scriptId)
		{
			return monoScript.GetAssemblyManager().GetSerializableType(scriptId);
		}

		private static SerializableType GetSerializableType(IUnityObjectBase monoScript, ScriptIdentifier scriptId)
		{
#pragma warning disable CS8603 // Possible null reference return.
			return IsScriptIdentifierValid(monoScript, scriptId)
				? ForceGetSerializableType(monoScript, scriptId)
				: null;
#pragma warning restore CS8603 // Possible null reference return.
		}

		private static IAssemblyManager GetAssemblyManager(this IUnityObjectBase monoScript)
		{
			return monoScript.SerializedFile.Collection.AssemblyManager;
		}
	}
}
