using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass301_HasHideFlagsInterface
	{
		public static void DoPass()
		{
			TypeSignature hideFlagsSignature = Pass040_AddEnums.EnumDictionary["UnityEngine.HideFlags"].Item1.ToTypeSignature();
			TypeDefinition hasHideFlagsInterface = MakeHasHideFlagsInterface(hideFlagsSignature);
			foreach (ClassGroup group in SharedState.Instance.ClassGroups.Values)
			{
				DoPassOnGroup(group, hasHideFlagsInterface, hideFlagsSignature);
			}
		}

		private static TypeDefinition MakeHasHideFlagsInterface(TypeSignature hideFlagsSignature)
		{
			TypeDefinition @interface = InterfaceCreator.CreateEmptyInterface(SharedState.Instance.Module, SharedState.InterfacesNamespace, "IHasHideFlags");
			@interface.AddFullProperty("ObjectHideFlags", InterfaceUtils.InterfacePropertyDeclaration, hideFlagsSignature);
			return @interface;
		}

		private static void DoPassOnGroup(ClassGroup group, TypeDefinition hasHideFlagsInterface, TypeSignature hideFlagsSignature)
		{
			if (group.Types.All(t => t.TryGetObjectHideFlagsField(out var _)))
			{
				group.Interface.AddInterfaceImplementation(hasHideFlagsInterface);

				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetObjectHideFlagsField(out FieldDefinition? field))
					{
						if (type.Properties.Any(p => p.Name == "ObjectHideFlags"))
						{
							throw new Exception("Already had an ObjectHideFlags Property");
						}
						else
						{
							type.ImplementFullProperty("ObjectHideFlags", InterfaceUtils.InterfacePropertyImplementation, hideFlagsSignature, field);
						}
					}
					else
					{
						throw new Exception("Should never happen");
					}
				}
			}
			else
			{
				foreach (TypeDefinition type in group.Types)
				{
					if (type.TryGetObjectHideFlagsField(out FieldDefinition? field))
					{
						type.AddInterfaceImplementation(hasHideFlagsInterface);
						if (type.Properties.Any(p => p.Name == "ObjectHideFlags"))
						{
							throw new Exception("Already had an ObjectHideFlags Property");
						}
						else
						{
							type.ImplementFullProperty("ObjectHideFlags", InterfaceUtils.InterfacePropertyImplementation, hideFlagsSignature, field);
						}
					}
				}
			}
		}

		private static bool TryGetObjectHideFlagsField(this TypeDefinition type, [NotNullWhen(true)] out FieldDefinition? field)
		{
			field = type.TryGetFieldByName("m_HideFlags");
			return field?.Signature?.FieldType.Name == "UInt32";
		}
	}
}
