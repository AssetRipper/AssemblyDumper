using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;
using AssemblyDumper.Utils;
using AssetRipper.Core.IO.Asset;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AssemblyDumper.Passes
{
	public static class Pass51_FillWriteMethods
	{
		private static MethodReference WriteMethod;
		private static bool throwNotSupported = true;

		public static void DoPass()
		{
			Console.WriteLine("Pass 51: Filling write methods");
			foreach ((string name, UnityClass klass) in SharedState.ClassDictionary)
			{
				TypeDefinition type = SharedState.TypeDictionary[name];
				List<FieldDefinition> fields = FieldUtils.GetAllFieldsInTypeAndBase(type);
				type.FillEditorWriteMethod(klass, fields);
				type.FillReleaseWriteMethod(klass, fields);
			}
		}

		private static void FillEditorWriteMethod(this TypeDefinition type, UnityClass klass, List<FieldDefinition> fields)
		{
			MethodDefinition editorModeReadMethod = type.Methods.First(m => m.Name == "WriteEditor");
			MethodBody editorModeBody = editorModeReadMethod.Body = new(editorModeReadMethod);
			ILProcessor editorModeProcessor = editorModeBody.GetILProcessor();

			if (throwNotSupported)
			{
				editorModeProcessor.EmitNotSupportedException();
			}
			else
			{
				//Console.WriteLine($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (UnityNode unityNode in klass.EditorRootNode.SubNodes)
					{
						AddWriteToProcessor(unityNode, editorModeProcessor, fields);
					}
				}
				editorModeProcessor.Emit(OpCodes.Ret);
			}
			editorModeBody.Optimize();
		}

		private static void FillReleaseWriteMethod(this TypeDefinition type, UnityClass klass, List<FieldDefinition> fields)
		{
			MethodDefinition releaseModeReadMethod = type.Methods.First(m => m.Name == "WriteRelease");
			MethodBody releaseModeBody = releaseModeReadMethod.Body = new(releaseModeReadMethod);
			ILProcessor releaseModeProcessor = releaseModeBody.GetILProcessor();

			if (throwNotSupported)
			{
				releaseModeProcessor.EmitNotSupportedException();
			}
			else
			{
				//Console.WriteLine($"Generating the release read method for {name}");
				if (klass.ReleaseRootNode != null)
				{
					foreach (UnityNode unityNode in klass.ReleaseRootNode.SubNodes)
					{
						AddWriteToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}
				releaseModeProcessor.Emit(OpCodes.Ret);
			}
			releaseModeBody.Optimize();
		}

		private static void AddWriteToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields)
		{
			//Get field
			FieldDefinition field = fields.SingleOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			if (WriteMethod == null)
				WriteMethod = processor.Body.Method.Module.ImportReference(CommonTypeGetter.AssetWriterDefinition.Resolve().Methods.First(m => m.Name == nameof(AssetWriter.WriteGeneric)));

			processor.Emit(OpCodes.Ldarg_1); //Load writer
			processor.Emit(OpCodes.Ldarg_0); //Load this
			processor.Emit(OpCodes.Ldfld, field); //Load field

			GenericInstanceMethod genericMethod = new GenericInstanceMethod(WriteMethod);
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
				MethodDefinition alignMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "AlignStream");

				//Call it
				processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(alignMethod));
			}
		}
	}
}