﻿using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.DocExtraction.MetaData;
using AssetRipper.VersionUtilities;

namespace AssetRipper.DocExtraction.DataStructures;
public sealed class EnumHistory : TypeHistory<EnumMemberHistory, EnumMemberDocumentation>
{
	public VersionedList<ElementType> ElementType { get; set; } = new();
	public bool IsFlagsEnum { get; set; }

	public static EnumHistory From(UnityVersion version, EnumDocumentation @enum)
	{
		EnumHistory? history = new();
		history.Initialize(version, @enum);
		return history;
	}

	public override void Initialize(UnityVersion version, DocumentationBase first)
	{
		base.Initialize(version, first);
		EnumDocumentation @enum = (EnumDocumentation)first;
		ElementType.Add(version, @enum.ElementType);
		IsFlagsEnum = @enum.IsFlagsEnum;
	}

	protected override void AddNotNull(UnityVersion version, DocumentationBase next)
	{
		base.AddNotNull(version, next);
		EnumDocumentation @enum = (EnumDocumentation)next;
		AddIfNotEqual(ElementType, version, @enum.ElementType);
		IsFlagsEnum |= @enum.IsFlagsEnum;
	}
}
