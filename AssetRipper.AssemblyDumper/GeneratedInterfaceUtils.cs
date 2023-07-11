namespace AssetRipper.AssemblyDumper
{
	internal static class GeneratedInterfaceUtils
	{
		private static string[] blackListedNames = new string[] { "Name" };

		public static string GetPropertyNameFromFieldName(string fieldName, int id)
		{
			if (blackListedNames.Contains(fieldName))
			{
				throw new Exception($"Field uses a blacklisted name");
			}

			string result = fieldName;
			if (result.StartsWith("m_", StringComparison.Ordinal))
			{
				result = result.Substring(2);
			}

			if (result.StartsWith('_'))
			{
				result = $"P{result}";
			}
			else if (char.IsDigit(result[0]))
			{
				result = $"P_{result}";
			}
			else if (char.IsLower(result[0]))
			{
				result = $"{char.ToUpperInvariant(result[0])}{result.Substring(1)}";
			}

			if (id >= 0)
			{
				result = $"{result}_C{id}";
			}

			if (blackListedNames.Contains(result))
			{
				result = $"{result}_R";
			}
			else if (result == fieldName)
			{
				result = $"P_{result}";
			}

			return result;
		}

		public static string GetHasMethodName(string propertyNameWithTypeSuffix)
		{
			return $"Has_{propertyNameWithTypeSuffix}";
		}

		public static string GetReleaseOnlyMethodName(string propertyNameWithTypeSuffix)
		{
			return $"IsReleaseOnly_{propertyNameWithTypeSuffix}";
		}

		public static string GetEditorOnlyMethodName(string propertyNameWithTypeSuffix)
		{
			return $"IsEditorOnly_{propertyNameWithTypeSuffix}";
		}

		public static void FillWithSimpleBooleanReturn(this CilInstructionCollection processor, bool returnTrue)
		{
			if (returnTrue)
			{
				processor.Add(CilOpCodes.Ldc_I4_1);
			}
			else
			{
				processor.Add(CilOpCodes.Ldc_I4_0);
			}

			processor.Add(CilOpCodes.Ret);
		}
	}
}
