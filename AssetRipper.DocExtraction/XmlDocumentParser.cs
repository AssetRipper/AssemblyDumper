﻿using System.IO;
using System.Xml;

namespace AssetRipper.DocExtraction;

public static class XmlDocumentParser
{
	public static void ExtractDocumentationFromXml(
		string path,
		Dictionary<string, string> typeSummaries,
		Dictionary<string, string> fieldSummaries,
		Dictionary<string, string> propertySummaries)
	{
		if (!File.Exists(path))
		{
			return;
		}

		XmlDocument doc = new();
		doc.Load(path);
		XmlNode membersNode = doc.ChildNodes[1]! //doc
			.ChildNodes[0]!; //members

		foreach (XmlNode memberNode in membersNode.ChildNodes)
		{
			if (memberNode.Name != "member")
			{
				if (memberNode.Name == "assembly")
				{
					continue;
				}
				throw new Exception("Child was not member");
			}

			string memberName = memberNode.Attributes!["name"]!.InnerText;

			if (memberName.StartsWith("T:", StringComparison.Ordinal))
			{
				typeSummaries.TryAdd(memberName.Substring(2), GetSummary(memberNode));
			}
			else if (memberName.StartsWith("F:", StringComparison.Ordinal))
			{
				fieldSummaries.TryAdd(memberName.Substring(2), GetSummary(memberNode));
			}
			else if (memberName.StartsWith("P:", StringComparison.Ordinal))
			{
				propertySummaries.TryAdd(memberName.Substring(2), GetSummary(memberNode));
			}
		}
	}

	private static string GetSummary(XmlNode memberNode)
	{
		XmlNode summaryNode = memberNode.ChildNodes[0]!;
		return summaryNode.Name == "summary" ? summaryNode.InnerText : throw new Exception("Child was not summary");
	}
}