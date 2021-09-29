using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Passes
{
	public static class Pass75_FillWriteMethods
	{
		private static MethodReference WriteMethod;

		public static void DoPass()
		{
			Logger.Info("Pass 75: Filling write methods");
			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeReadMethod = type.Methods.First(m => m.Name == "WriteEditor");
				var releaseModeReadMethod = type.Methods.First(m => m.Name == "WriteRelease");

				var editorModeBody = editorModeReadMethod.Body = new(editorModeReadMethod);
				var releaseModeBody = releaseModeReadMethod.Body = new(releaseModeReadMethod);

				var editorModeProcessor = editorModeBody.GetILProcessor();
				var releaseModeProcessor = releaseModeBody.GetILProcessor();

				var fields = GetAllFieldsInTypeAndBase(type);

				//Logger.Info($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (var unityNode in klass.EditorRootNode.SubNodes)
					{
						AddWriteToProcessor(unityNode, editorModeProcessor, fields);
					}
				}

				//Logger.Info($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode != null)
				{
					foreach (var unityNode in klass.ReleaseRootNode.SubNodes)
					{
						AddWriteToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}

				editorModeProcessor.Emit(OpCodes.Ret);
				releaseModeProcessor.Emit(OpCodes.Ret);

				editorModeBody.Optimize();
				releaseModeBody.Optimize();
			}
		}

		private static List<FieldDefinition> GetAllFieldsInTypeAndBase(TypeDefinition type)
		{
			if (type == null)
				return new();

			var ret = type.Fields.ToList();

			ret.AddRange(GetAllFieldsInTypeAndBase(type.BaseType?.Resolve()));

			return ret;
		}

		private static void AddWriteToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields)
		{
			//Get field
			var field = fields.FirstOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			if (WriteMethod == null)
				WriteMethod = processor.Body.Method.Module.ImportReference(CommonTypeGetter.AssetWriterDefinition.Resolve().Methods.First(m => m.Name == nameof(AssetWriter.WriteGeneric)));

			processor.Emit(OpCodes.Ldarg_1); //Load writer
			processor.Emit(OpCodes.Ldarg_0); //Load this
			processor.Emit(OpCodes.Ldfld, field); //Load field

			var genericMethod = new GenericInstanceMethod(WriteMethod);
			genericMethod.GenericArguments.Add(field.FieldType);
			processor.Emit(OpCodes.Call, genericMethod);

			MaybeAlignBytes(node, processor);
		}

		private static void MaybeAlignBytes(UnityNode node, ILProcessor processor)
		{
			if (((TransferMetaFlags)node.MetaFlag).IsAlignBytes())
			{
				//Load reader
				processor.Emit(OpCodes.Ldarg_1);

				//Get ReadAsset
				var alignMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "AlignStream");

				//Call it
				processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(alignMethod));
			}
		}
	}
}