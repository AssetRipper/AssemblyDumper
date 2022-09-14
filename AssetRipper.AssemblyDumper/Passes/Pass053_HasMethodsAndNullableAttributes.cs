using AssetRipper.AssemblyCreationTools.Attributes;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.DocExtraction.Extensions;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass053_HasMethodsAndNullableAttributes
	{
		public static void DoPass()
		{
			foreach (ClassGroupBase group in SharedState.Instance.AllGroups)
			{
				RecalculateInterfacePropertyRanges(group);
				ApplyNullableAttributesToTypes(group);
				AddHasMethodsAndApplyNullableAttributesToProperties(group);
				AddMemberNotNullAttributesToHasMethods(group);
			}
		}

		private static void RecalculateInterfacePropertyRanges(ClassGroupBase group)
		{
			foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
			{
				interfaceProperty.RecalculateRanges();
			}
		}

		private static void ApplyNullableAttributesToTypes(ClassGroupBase group)
		{
			group.Interface.AddNullableContextAttribute(NullableAnnotation.NotNull);
			group.Interface.AddNullableAttribute(NullableAnnotation.Oblivious);
			foreach (TypeDefinition instanceType in group.Types)
			{
				instanceType.AddNullableContextAttribute(NullableAnnotation.NotNull);
				instanceType.AddNullableAttribute(NullableAnnotation.Oblivious);
			}
		}

		private static void AddHasMethodsAndApplyNullableAttributesToProperties(ClassGroupBase group)
		{
			foreach (InterfaceProperty interfaceProperty in group.InterfaceProperties)
			{
				if (interfaceProperty.AbsentRange.IsEmpty())
				{
					continue;
				}

				bool isValueType = interfaceProperty.Definition.IsValueType();
				string propertyName = interfaceProperty.Definition.Name!;
				interfaceProperty.HasMethod = group.Interface.AddHasMethodDeclaration(propertyName);

				if (!isValueType)
				{
					interfaceProperty.Definition.AddNullableAttributesForMaybeNull();
				}

				foreach (ClassProperty classProperty in interfaceProperty.Implementations)
				{
					classProperty.HasMethod = classProperty.Class.Type.AddHasMethodImplementation(classProperty.Definition.Name!, classProperty.BackingField is not null);

					if (!isValueType)
					{
						classProperty.Definition.AddNullableAttributesForMaybeNull();
						if (classProperty.BackingField is not null)
						{
							classProperty.Definition.GetMethod!.AddNotNullAttribute();
						}
					}
				}
			}
		}

		private static void AddMemberNotNullAttributesToHasMethods(ClassGroupBase group)
		{
			foreach (InterfaceProperty property in group.InterfaceProperties)
			{
				if (property.HasMethod is null)
				{
					continue;
				}

				foreach (InterfaceProperty otherProperty in group.InterfaceProperties)
				{
					if (otherProperty.HasMethod is null || otherProperty.Definition.IsValueType())
					{
						continue;
					}
					//other is always present when this is present
					else if (otherProperty.PresentRange.Contains(property.PresentRange))
					{
						property.HasMethod.AddMemberNotNullAttribute(SharedState.Instance.Importer, true, otherProperty.Definition.Name!);
					}
					//other is always present when this is absent
					else if (otherProperty.PresentRange.Contains(property.AbsentRange))
					{
						property.HasMethod.AddMemberNotNullAttribute(SharedState.Instance.Importer, false, otherProperty.Definition.Name!);
					}
				}
			}
		}

		private static MethodDefinition AddHasMethodDeclaration(this TypeDefinition @interface, string propertyName)
		{
			return @interface.AddMethod(
				GeneratedInterfaceUtils.GetHasMethodName(propertyName),
				InterfaceUtils.InterfaceMethodDeclaration,
				SharedState.Instance.Importer.Boolean);
		}

		private static MethodDefinition AddHasMethodImplementation(this TypeDefinition declaringType, string propertyName, bool isType)
		{
			MethodDefinition method = declaringType.AddMethod(
				GeneratedInterfaceUtils.GetHasMethodName(propertyName),
				InterfaceUtils.InterfaceMethodImplementation,
				SharedState.Instance.Importer.Boolean);
			method.CilMethodBody!.Instructions.FillWithSimpleBooleanReturn(isType);
			return method;
		}

	}
}
