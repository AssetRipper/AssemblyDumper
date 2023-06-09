﻿using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass039_InjectEnumValues
	{
		private static readonly List<InjectedTypeData> injectedTypes = new()
		{
			new()
			{
				Name = "CurveLoopTypes",
				Summary = "Enum for AnimationCurve.PreInfinity and AnimationCurve.PostInfinity",
				Members = new()
				{
					("Constant", 0, ""),
					("Cycle", 1, ""),
					("CycleWithOffset", 2, ""),
					("Oscillate", 3, ""),
					("Linear", 4, ""),
				},
			},
		};
		private static readonly Dictionary<string, List<(string, long, string?)>> injectedValues = new()
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
			{ "UnityEngine.MeshTopology",
				new()
				{
					("TriangleStrip", 1, "Mesh is a triangle strip."),
				} },
			{ "UnityEngine.SpritePackingRotation",
				new()
				{
					("Rotate90", 4, "Might not exist. It was included in legacy code."),
					("Any_Old", 5, "Might not exist. It was included in legacy code."),
				} },
			{ "UnityEditor.Animations.AnimatorConditionMode",
				new()
				{
					("ExitTime", 5, "The condition is true when the source state has stepped over the exit time value."),
				} },
		};
		private static readonly Dictionary<string, List<(string, string)>> injectedDocumentation = new()
		{
			{ "UnityEditor.TextureUsageMode",
				new()
				{
					("Default", "Not a lightmap"),
					("LightmapDoubleLDR", "Range [0;2] packed to [0;1] with loss of precision"),
					("BakedLightmapDoubleLDR", "Range [0;2] packed to [0;1] with loss of precision"),
					("LightmapRGBM", "Range [0;kLightmapRGBMMax] packed to [0;1] with multiplier stored in the alpha channel"),
					("BakedLightmapRGBM", "Range [0;kLightmapRGBMMax] packed to [0;1] with multiplier stored in the alpha channel"),
					("NormalmapDXT5nm", "Compressed DXT5 normal map"),
					("NormalmapPlain", "Plain RGB normal map"),
					("AlwaysPadded", "Texture is always padded if NPOT and on low-end hardware"),
					("BakedLightmapFullHDR", "Baked lightmap without any encoding"),
				} },
		};

		public static void DoPass()
		{
			Dictionary<string, EnumHistory> dictionary = SharedState.Instance.HistoryFile.Enums;
			foreach (InjectedTypeData injectedTypeData in injectedTypes)
			{
				EnumHistory history = new();
				history.Name = injectedTypeData.Name;
				history.FullName = $"Injected.{injectedTypeData.Name}";
				history.IsFlagsEnum = injectedTypeData.IsFlags;
				history.ElementType.Add(UnityVersion.MinVersion, injectedTypeData.ElementType);
				history.NativeName.Add(UnityVersion.MinVersion, null);
				history.DocumentationString.Add(UnityVersion.MinVersion, injectedTypeData.Summary);
				history.ObsoleteMessage.Add(UnityVersion.MinVersion, null);
				history.Exists.Add(UnityVersion.MinVersion, true);
				foreach ((string fieldName, long value, string? description) in injectedTypeData.Members)
				{
					EnumMemberHistory member = new();
					member.Name = fieldName;
					member.NativeName.Add(UnityVersion.MinVersion, null);
					member.Value.Add(UnityVersion.MinVersion, value);
					member.DocumentationString.Add(UnityVersion.MinVersion, string.IsNullOrEmpty(description) ? null : description);
					member.ObsoleteMessage.Add(UnityVersion.MinVersion, null);
					member.Exists.Add(UnityVersion.MinVersion, true);
					history.Members.Add(fieldName, member);
				}
				dictionary.Add(history.FullName, history);
			}
			foreach ((string fullName, List<(string, long, string?)> list) in injectedValues)
			{
				EnumHistory history = dictionary[fullName];
				foreach ((string fieldName, long value, string? description) in list)
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
			foreach ((string fullName, List<(string, string)> list) in injectedDocumentation)
			{
				EnumHistory history = dictionary[fullName];
				foreach ((string fieldName, string description) in list)
				{
					EnumMemberHistory member = history.Members[fieldName];
					for (int i = 0; i < member.DocumentationString.Count; i++)
					{
						(UnityVersion version, string? oldDocumentation) = member.DocumentationString[i];
						string newDocumentation = string.IsNullOrEmpty(oldDocumentation) ? description : $"{description}<br/>\n{oldDocumentation}";
						member.DocumentationString[i] = new KeyValuePair<UnityVersion, string?>(version, newDocumentation);
					}
				}
			}
		}

		private record class InjectedTypeData
		{
			public required string Name { get; init; }
			public string? Summary { get; init; }
			public bool IsFlags { get; init; } = false;
			public ElementType ElementType { get; init; } = ElementType.I4;
			public required List<(string, long, string?)> Members { get; init; }
		}
	}
}
