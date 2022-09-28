using AssetRipper.AssemblyCreationTools.Fields;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.DataStructures;

namespace AssetRipper.AssemblyDumper.Documentation
{
	internal static class EnumTypeDocumenter
	{
		public static void AddEnumTypeDocumentation(TypeDefinition type, EnumHistory history)
		{
			DocumentationHandler.AddTypeDefinitionLine(type, $"Full Name: \"{XmlUtils.EscapeXmlInvalidCharacters(history.FullName)}\"");
			VersionedListDocumenter.AddSet(type, history.DocumentationString, "Summary: ");
			VersionedListDocumenter.AddList(type, history.ObsoleteMessage, "Obsolete Message: ");

			foreach ((string memberName, EnumMemberHistory memberHistory) in history.Members)
			{
				if (memberHistory.Value.Count == 1)
				{
					FieldDefinition field = type.GetFieldByName(memberName);
					VersionedListDocumenter.AddSet(field, memberHistory.DocumentationString, "Summary: ");
					VersionedListDocumenter.AddList(field, memberHistory.ObsoleteMessage, "Obsolete Message: ");
					DocumentationHandler.AddFieldDefinitionLine(field, memberHistory.GetVersionRange().GetUnionedRanges().GetString());
				}
				else
				{
					HashSet<long> values = new();
					foreach (long value in memberHistory.Value.Values)
					{
						if (values.Add(value))
						{
							string fieldName = GetEnumFieldName(memberName, value);
							FieldDefinition field = type.GetFieldByName(fieldName);
							VersionedListDocumenter.AddSet(field, memberHistory.DocumentationString, "Summary: ");
							VersionedListDocumenter.AddList(field, memberHistory.ObsoleteMessage, "Obsolete Message: ");
							DocumentationHandler.AddFieldDefinitionLine(field,
								GetVersionRange(memberHistory.Exists, memberHistory.Value, value).GetUnionedRanges().GetString());
						}
					}
				}
			}

			DocumentationHandler.AddTypeDefinitionLine(type, history.GetVersionRange().GetUnionedRanges().GetString());
		}

		private static string GetEnumFieldName(string name, long value)
		{
			return value < 0 ? $"{name}_N{-value}" : $"{name}_{value}";
		}

		private static IEnumerable<UnityVersionRange> GetVersionRange(VersionedList<bool> existence, VersionedList<long> values, long value)
		{
			int existenceIndex = 0;
			int valuesIndex = 0;
			while (existenceIndex < existence.Count && valuesIndex < values.Count)
			{
				if (!existence[existenceIndex].Value) //Field does not exist in this range.
				{
					existenceIndex++;
					continue;
				}
				if (values[valuesIndex].Value != value) //Value does not match in this range.
				{
					valuesIndex++;
					continue;
				}

				UnityVersionRange valuesRange = values.GetRange(valuesIndex);
				UnityVersionRange existenceRange = existence.GetRange(existenceIndex);
				if (valuesRange.End <= existenceRange.Start) //Value range is lagging behind existence range.
				{
					valuesIndex++;
					continue;
				}
				if (existenceRange.End <= valuesRange.Start) //Existence range is lagging behind value range.
				{
					existenceIndex++;
					continue;
				}

				//At this point, the value range intersects the existence range, the field exists, and the value matches.

				UnityVersionRange intersection = valuesRange.MakeIntersection(existenceRange);
				yield return intersection;

				if (valuesRange.End <= existenceRange.End) //Existence range may still have space to intersect with the next value range.
				{
					valuesIndex++;
				}
				else // Vice versa.
				{
					existenceIndex++;
				}
			}
		}
	}
}
