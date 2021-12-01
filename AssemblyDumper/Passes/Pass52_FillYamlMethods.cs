using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass52_FillYamlMethods
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 52: Filling yaml methods");
			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeYamlMethod = type.Methods.First(m => m.Name == "ExportYAMLEditor");
				var releaseModeYamlMethod = type.Methods.First(m => m.Name == "ExportYAMLRelease");

				var editorModeBody = editorModeYamlMethod.Body = new(editorModeYamlMethod);
				var releaseModeBody = releaseModeYamlMethod.Body = new(releaseModeYamlMethod);

				var editorModeProcessor = editorModeBody.GetILProcessor();
				var releaseModeProcessor = releaseModeBody.GetILProcessor();

				var fields = FieldUtils.GetAllFieldsInTypeAndBase(type);

				editorModeBody.InitLocals = true;
				VariableDefinition editorResult = new VariableDefinition(CommonTypeGetter.YAMLMappingNodeDefinition);
				editorModeBody.Variables.Add(editorResult);
				editorModeProcessor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLMappingNodeConstructor);
				editorModeProcessor.Emit(OpCodes.Stloc, editorResult);

				releaseModeBody.InitLocals = true;
				VariableDefinition releaseResult = new VariableDefinition(CommonTypeGetter.YAMLMappingNodeDefinition);
				releaseModeBody.Variables.Add(releaseResult);
				releaseModeProcessor.Emit(OpCodes.Newobj, CommonTypeGetter.YAMLMappingNodeConstructor);
				releaseModeProcessor.Emit(OpCodes.Stloc, releaseResult);

				//Console.WriteLine($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (var unityNode in klass.EditorRootNode.SubNodes)
					{
						AddExportToProcessor(unityNode, editorModeProcessor, fields);
					}
				}

				//Console.WriteLine($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode != null)
				{
					foreach (var unityNode in klass.ReleaseRootNode.SubNodes)
					{
						AddExportToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}

				editorModeProcessor.Emit(OpCodes.Ldloc, editorResult);
				editorModeProcessor.Emit(OpCodes.Ret);
				releaseModeProcessor.Emit(OpCodes.Ldloc, releaseResult);
				releaseModeProcessor.Emit(OpCodes.Ret);

				editorModeBody.Optimize();
				releaseModeBody.Optimize();
			}
		}

		private static void AddExportToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields)
		{
			//Get field
			var field = fields.FirstOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			ExportFieldContent(node, processor, field);
		}

		private static void ExportFieldContent(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			if (SharedState.TypeDictionary.TryGetValue(node.TypeName, out var fieldType))
			{
				//ReadAssetType(node, processor, field, fieldType, 0);
				return;
			}

			switch (node.TypeName)
			{
				case "vector":
				case "set":
				case "staticvector":
					//ReadVector(node, processor, field, 1);
					return;
				case "map":
					//ReadDictionary(node, processor, field);
					return;
				case "pair":
					//ReadPair(node, processor, field);
					return;
				case "TypelessData": //byte array
					//ReadByteArray(node, processor, field);
					return;
				case "Array":
					//ReadArray(node, processor, field, 1);
					return;
			}

			//ReadPrimitiveType(node, processor, field, 0);
		}
	}
}
