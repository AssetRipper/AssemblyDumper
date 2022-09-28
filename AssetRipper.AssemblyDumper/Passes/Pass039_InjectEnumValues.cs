﻿using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass039_InjectEnumValues
	{
		private static readonly Dictionary<string, List<(string, long, string)>> injectedValues = new()
		{
			{ "UnityEngine.TextureFormat",
				new()
				{
					("DXT3", 11, ""),
				} },
			{ "UnityEditor.TextureImporterFormat",
				new()
				{
					("DXT3", 11, ""),
				} },
		};

		public static void DoPass()
		{
			Dictionary<string, EnumHistory> dictionary = SharedState.Instance.HistoryFile.Enums;
			foreach ((string fullName, List<(string, long, string)> list) in injectedValues)
			{
				EnumHistory history = dictionary[fullName];
				foreach ((string fieldName, long value, string description) in list)
				{
					if (history.Members.ContainsKey(fieldName))
					{
						Console.WriteLine($"{fullName} already has an entry for {fieldName}");
					}
					else
					{
						EnumMemberHistory member = new();
						member.Name = fieldName;
						member.NativeName.Add(UnityVersion.MinVersion, null);
						member.Value.Add(UnityVersion.MinVersion, value);
						member.DocumentationString.Add(UnityVersion.MinVersion, string.IsNullOrEmpty(description) ? "Injected" : $"Injected. {description}");
						member.ObsoleteMessage.Add(UnityVersion.MinVersion, null);
						member.Exists.Add(UnityVersion.MinVersion, true);
						history.Members.Add(fieldName, member);
					}
				}
			}
		}
	}
}