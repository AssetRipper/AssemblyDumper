using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using AssetRipper.AssemblyDumper.Documentation;

namespace AssetRipper.AssemblyDumper.Passes;

internal static class Pass301_SourcePrefabProperty
{
	private const string PropertyName = "SourcePrefabP";

	public static void DoPass()
	{
		ClassGroup group = SharedState.Instance.ClassGroups[1001];

		//Interface Property
		{
			PropertyDefinition property = group.Interface.AddFullProperty(PropertyName, InterfaceUtils.InterfacePropertyDeclaration, group.Interface.ToTypeSignature());
			property.AddNullableAttributesForMaybeNull();
			DocumentationHandler.AddPropertyDefinitionLine(property, $"Injected in {nameof(Pass301_SourcePrefabProperty)}");
			DocumentationHandler.AddPropertyDefinitionLine(property, "It is the source/parent prefab of this prefab instance.");
		}

		foreach (GeneratedClassInstance instance in group.Instances)
		{
			PropertyDefinition existingProperty = instance.Properties.Single(p => p.BackingField?.Name == "m_SourcePrefab").SpecialDefinition!;

			PropertyDefinition property = instance.Type.AddFullProperty(PropertyName, InterfaceUtils.InterfacePropertyImplementation, group.Interface.ToTypeSignature());

			//Get method
			{
				MethodDefinition method = property.GetMethod!;
				CilInstructionCollection processor = method.GetInstructions();
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Call, existingProperty.GetMethod!);
				processor.Add(CilOpCodes.Ret);
			}

			//Set method
			{
				MethodDefinition method = property.SetMethod!;
				CilInstructionCollection processor = method.GetInstructions();
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Ldarg_1);
				processor.Add(CilOpCodes.Call, existingProperty.SetMethod!);
				processor.Add(CilOpCodes.Ret);
			}

			property.AddNullableAttributesForMaybeNull();
		}
	}
}
