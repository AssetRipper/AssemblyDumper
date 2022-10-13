using AsmResolver.DotNet.Cloning;
using AssetRipper.AssemblyCreationTools.Methods;
using AssetRipper.AssemblyCreationTools.Types;
using System.Collections;
using System.Text;

namespace AssetRipper.AssemblyDumper.Passes
{
	internal static class Pass410_SetValuesMethods
	{
		private const string SetValuesName = "SetValues";
		private const string CopyValuesName = "CopyValues";
		private const string DeepCloneName = "DeepClone";
		private static readonly HashSet<string> processedClasses = new();
		private static readonly HashSet<string> skippedClasses = new();
		private static MethodDefinition? duplicateArrayMethod;

		public static void DoPass()
		{
			duplicateArrayMethod = InjectHelper().GetMethodByName(nameof(ArrayDuplicationHelper.DuplicateArray));

			foreach (SubclassGroup group in SharedState.Instance.SubclassGroups.Values)
			{
				ProcessGroup(group);
			}
			processedClasses.Clear();
			skippedClasses.Clear();
		}

		private static bool ProcessGroup(SubclassGroup group)
		{
			if (skippedClasses.Contains(group.Name))
			{
				return false;
			}
			else if (processedClasses.Contains(group.Name))
			{
				return true;
			}
			else if (group.InterfaceProperties.Count == 0)
			{
				group.ImplementCopyValuesMethod();
				group.ImplementDeepCloneMethod();
				processedClasses.Add(group.Name);
				return true;
			}
			else if (group.InterfaceProperties.Select(i => i.Definition).All(prop => prop.IsArrayOrPrimitive()))
			{
				group.ImplementSetValuesMethod();
				group.ImplementCopyValuesMethod();
				group.ImplementDeepCloneMethod();
				processedClasses.Add(group.Name);
				return true;
			}
			else if (group.InterfaceProperties.Select(i => i.Definition).All(prop => prop.IsArrayOrPrimitiveOrProcessedType()))
			{
				group.ImplementCopyValuesMethod();
				group.ImplementDeepCloneMethod();
				processedClasses.Add(group.Name);
				return true;
			}
			else
			{
				skippedClasses.Add(group.Name);
				return false;
			}
		}

		private static bool IsArrayOrPrimitive(this PropertyDefinition property)
		{
			return property.Signature?.ReturnType is SzArrayTypeSignature or CorLibTypeSignature;
		}

		private static bool IsArrayOrPrimitiveOrProcessedType(this PropertyDefinition property)
		{
			return property.Signature?.ReturnType switch
			{
				SzArrayTypeSignature or CorLibTypeSignature => true,
				TypeDefOrRefSignature typeSignature => typeSignature.ToTypeDefOrRef() is TypeDefinition type
					&& ProcessGroup((SubclassGroup)SharedState.Instance.TypesToGroups[type]),
				_ => false
			};
		}

		private static void ImplementCopyValuesMethod(this SubclassGroup group)
		{
			group.Interface.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void)
				.AddParameter(group.Interface.ToTypeSignature(), "source");

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod(CopyValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
				method.AddParameter(group.Interface.ToTypeSignature(), "source");
				CilInstructionCollection processor = method.GetProcessor();
				foreach (ClassProperty classProperty in instance.Properties)
				{
					if (classProperty.IsAbsent)
					{
						continue;
					}

					MethodDefinition baseGetMethod = classProperty.Base.Definition.GetMethod ?? throw new Exception("Interface get method can't be null");
					switch (classProperty.Definition.Signature?.ReturnType)
					{
						case CorLibTypeSignature:
							{
								MethodDefinition setMethod = classProperty.Definition.SetMethod ?? throw new Exception("Set method can't be null");
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, baseGetMethod);
								processor.Add(CilOpCodes.Call, setMethod);
							}
							break;
						case SzArrayTypeSignature arrayType:
							{
								MethodDefinition setMethod = classProperty.Definition.SetMethod ?? throw new Exception("Set method can't be null");
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, baseGetMethod);
								processor.Add(CilOpCodes.Call, duplicateArrayMethod!.MakeGenericInstanceMethod(arrayType.BaseType));
								processor.Add(CilOpCodes.Call, setMethod);
							}
							break;
						case TypeDefOrRefSignature typeDefOrRefSignature:
							{
								MethodDefinition getMethod = classProperty.Definition.GetMethod ?? throw new Exception("Get method can't be null");
								MethodDefinition copyValuesMethod = ((TypeDefinition)typeDefOrRefSignature.ToTypeDefOrRef()).GetMethodByName(CopyValuesName);
								processor.Add(CilOpCodes.Ldarg_0);
								processor.Add(CilOpCodes.Call, getMethod);
								processor.Add(CilOpCodes.Ldarg_1);
								processor.Add(CilOpCodes.Callvirt, baseGetMethod);
								processor.Add(CilOpCodes.Callvirt, copyValuesMethod);
							}
							break;
						default:
							throw new NotSupportedException();
					}
				}
				processor.Add(CilOpCodes.Ret);
				processor.OptimizeMacros();
			}
		}

		private static void ImplementDeepCloneMethod(this SubclassGroup group)
		{
			group.Interface.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodDeclaration, group.Interface.ToTypeSignature());
			
			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod(DeepCloneName, InterfaceUtils.InterfaceMethodImplementation, group.Interface.ToTypeSignature());
				CilInstructionCollection processor = method.GetProcessor();
				processor.Add(CilOpCodes.Newobj, instance.Type.GetDefaultConstructor());
				processor.Add(CilOpCodes.Dup);
				processor.Add(CilOpCodes.Ldarg_0);
				processor.Add(CilOpCodes.Callvirt, instance.Type.GetMethodByName(CopyValuesName));
				processor.Add(CilOpCodes.Ret);
			}
		}

		private static void ImplementSetValuesMethod(this SubclassGroup group)
		{
			MethodDefinition interfaceMethod = group.Interface.AddMethod(SetValuesName, InterfaceUtils.InterfaceMethodDeclaration, SharedState.Instance.Importer.Void);
			foreach (PropertyDefinition property in group.GetInterfacePropertiesInOrder())
			{
				interfaceMethod.AddParameter(property.Signature!.ReturnType, GetParameterName(property.Name));
			}

			foreach (GeneratedClassInstance instance in group.Instances)
			{
				MethodDefinition method = instance.Type.AddMethod(SetValuesName, InterfaceUtils.InterfaceMethodImplementation, SharedState.Instance.Importer.Void);
				CilInstructionCollection processor = method.GetProcessor();
				IEnumerable<PropertyDefinition> properties = group.IsVector4()
					? new Vector4PropertyEnumerable_Instance(instance)
					: group.IsColorRGBAf()
						? new ColorPropertyEnumerable_Instance(instance)
						: instance.Properties.Select(c => c.Definition);
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
					: group.InterfaceProperties.Select(i => i.Definition);
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
				yield return GetProperty("X");
				yield return GetProperty("Y");
				yield return GetProperty("Z");
				yield return GetProperty("W");
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
				return instance.Properties.Select(c => c.Definition).Single(property => property.Name == propertyName);
			}
		}
		private sealed class Vector4PropertyEnumerable_Group : Vector4PropertyEnumerableBase
		{
			private readonly SubclassGroup group;
			public Vector4PropertyEnumerable_Group(SubclassGroup group) => this.group = group;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return group.InterfaceProperties.Select(i => i.Definition).Single(property => property.Name == propertyName);
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
				return instance.Properties.Select(c => c.Definition).Single(property => property.Name == propertyName);
			}
		}
		private sealed class ColorPropertyEnumerable_Group : ColorPropertyEnumerableBase
		{
			private readonly SubclassGroup group;
			public ColorPropertyEnumerable_Group(SubclassGroup group) => this.group = group;
			protected override PropertyDefinition GetProperty(string propertyName)
			{
				return group.InterfaceProperties.Select(i => i.Definition).Single(property => property.Name == propertyName);
			}
		}

		private static TypeDefinition InjectHelper()
		{
			MemberCloner cloner = new MemberCloner(SharedState.Instance.Module);
			cloner.Include(SharedState.Instance.Importer.LookupType(typeof(ArrayDuplicationHelper))!, true);
			MemberCloneResult result = cloner.Clone();
			foreach (TypeDefinition type in result.ClonedTopLevelTypes)
			{
				type.Namespace = SharedState.HelpersNamespace;
				SharedState.Instance.Module.TopLevelTypes.Add(type);
			}
			return result.ClonedTopLevelTypes.Single(t => t.Name == nameof(ArrayDuplicationHelper));
		}
	}
}
