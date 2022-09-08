using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using System.Collections;
using System.Text;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass410_SetValuesMethods
	{
		public static void DoPass()
		{
			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				if (group.InterfaceProperties.All(prop => prop.Signature?.ReturnType is SzArrayTypeSignature or CorLibTypeSignature) && group.InterfaceProperties.Count > 0)
				{
					group.ImplementSetValuesMethod();
					group.ImplementCopyValuesMethod();
				}
			}
		}

		private static void ImplementCopyValuesMethod(this SubclassGroup group)
		{
			group.Interface.AddMethod("CopyValues", InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void)
				.AddParameter(group.Interface.ToTypeSignature(), "source");

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod("CopyValues", InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
				method.AddParameter(group.Interface.ToTypeSignature(), "source");
				CilInstructionCollection processor = method.GetProcessor();
				foreach ((PropertyDefinition interfaceProperty, PropertyDefinition property) in instance.InterfacePropertiesToInstanceProperties)
				{
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldarg_1);
					processor.Add(CilOpCodes.Callvirt, interfaceProperty.GetMethod ?? throw new Exception("Interface get method can't be null"));
					processor.Add(CilOpCodes.Call, property.SetMethod ?? throw new Exception("Set method can't be null"));
				}
				processor.Add(CilOpCodes.Ret);
				processor.OptimizeMacros();
			}
		}

		private static void ImplementSetValuesMethod(this SubclassGroup group)
		{
			MethodDefinition interfaceMethod = group.Interface.AddMethod("SetValues", InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
			foreach (PropertyDefinition property in group.GetInterfacePropertiesInOrder())
			{
				interfaceMethod.AddParameter(property.Signature!.ReturnType, GetParameterName(property.Name));
			}

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod("SetValues", InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
				CilInstructionCollection processor = method.GetProcessor();
				IEnumerable<PropertyDefinition> properties = group.IsVector4()
					? new Vector4PropertyEnumerable_Instance(instance)
					: group.IsColorRGBAf()
						? new ColorPropertyEnumerable_Instance(instance)
						: instance.InterfacePropertiesToInstanceProperties.Values;
				foreach (PropertyDefinition property in properties)
				{
					Parameter parameter = method.AddParameter(property.Signature!.ReturnType, GetParameterName(property.Name));
					processor.Add(CilOpCodes.Ldarg_0);
					processor.Add(CilOpCodes.Ldarg, parameter);
					processor.Add(CilOpCodes.Call, property.SetMethod ?? throw new Exception("Set method can't be null"));
				}
				processor.Add(CilOpCodes.Ret);
				processor.OptimizeMacros();
			}
		}

		private static IEnumerable<PropertyDefinition> GetInterfacePropertiesInOrder(this SubclassGroup group)
		{
			return group.IsVector4()
				? new Vector4PropertyEnumerable_Group(group)
				: group.IsColorRGBAf()
					? new ColorPropertyEnumerable_Group(group)
					: group.InterfaceProperties;
		}

		private static string GetParameterName(string? propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				throw new Exception("Property name cannot be null or empty");
			}

			StringBuilder sb = new StringBuilder(propertyName.Length);
			sb.Append(char.ToLowerInvariant(propertyName[0]));
			if (propertyName.Length > 1)
			{
				sb.Append(propertyName.AsSpan(1));
			}
			return sb.ToString();
		}

		private static bool IsVector4(this SubclassGroup group) => group.Name is "Vector4f" or "Vector4Float" or "Quaternionf";
		private abstract class Vector4PropertyEnumerableBase : IEnumerable<PropertyDefinition>
		{
			public IEnumerator<PropertyDefinition> GetEnumerator()
			{
				yield return GetProperty("X_");
				yield return GetProperty("Y_");
				yield return GetProperty("Z_");
				yield return GetProperty("W_");
			}
			protected abstract PropertyDefinition GetProperty(string propertyName);
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
		private sealed class Vector4PropertyEnumerable_Instance : Vector4PropertyEnumerableBase
		{
			private readonly GeneratedClassInstance instance;
			public Vector4PropertyEnumerable_Instance(GeneratedClassInstance instance) => this.instance = instance;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return instance.InterfacePropertiesToInstanceProperties.Single(pair => pair.Value.Name == propertyName).Value;
			}
		}
		private sealed class Vector4PropertyEnumerable_Group : Vector4PropertyEnumerableBase
		{
			private readonly SubclassGroup group;
			public Vector4PropertyEnumerable_Group(SubclassGroup group) => this.group = group;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return group.InterfaceProperties.Single(property => property.Name == propertyName);
			}
		}

		private static bool IsColorRGBAf(this SubclassGroup group) => group.Name is "ColorRGBAf";
		private abstract class ColorPropertyEnumerableBase : IEnumerable<PropertyDefinition>
		{
			public IEnumerator<PropertyDefinition> GetEnumerator()
			{
				yield return GetProperty("R");
				yield return GetProperty("G");
				yield return GetProperty("B");
				yield return GetProperty("A");
			}
			protected abstract PropertyDefinition GetProperty(string propertyName);
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
		private sealed class ColorPropertyEnumerable_Instance : ColorPropertyEnumerableBase
		{
			private readonly GeneratedClassInstance instance;
			public ColorPropertyEnumerable_Instance(GeneratedClassInstance instance) => this.instance = instance;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return instance.InterfacePropertiesToInstanceProperties.Single(pair => pair.Value.Name == propertyName).Value;
			}
		}
		private sealed class ColorPropertyEnumerable_Group : ColorPropertyEnumerableBase
		{
			private readonly SubclassGroup group;
			public ColorPropertyEnumerable_Group(SubclassGroup group) => this.group = group;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return group.InterfaceProperties.Single(property => property.Name == propertyName);
			}
		}
	}
}
