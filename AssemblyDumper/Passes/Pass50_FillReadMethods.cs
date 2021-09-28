using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyDumper.Unity;
using AssetRipper.Core.Parser.Files.SerializedFiles.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyDumper.Passes
{
	public static class Pass50_FillReadMethods
	{
		public static void DoPass()
		{
			Logger.Info("Pass 50: Filling read methods");
			foreach (var (name, klass) in SharedState.ClassDictionary)
			{
				if (!SharedState.TypeDictionary.ContainsKey(name))
					//Skip primitive types
					continue;

				var type = SharedState.TypeDictionary[name];

				var editorModeReadMethod = type.Methods.First(m => m.Name == "ReadEditor");
				var releaseModeReadMethod = type.Methods.First(m => m.Name == "ReadRelease");

				var editorModeBody = editorModeReadMethod.Body = new(editorModeReadMethod);
				var releaseModeBody = releaseModeReadMethod.Body = new(releaseModeReadMethod);

				var editorModeProcessor = editorModeBody.GetILProcessor();
				var releaseModeProcessor = releaseModeBody.GetILProcessor();

				var fields = GetAllFieldsInTypeAndBase(type);

				if (klass.EditorRootNode != null)
				{
					foreach (var unityNode in klass.EditorRootNode.SubNodes)
					{
						AddLoadToProcessor(unityNode, editorModeProcessor, fields);
					}
				}

				if (klass.ReleaseRootNode != null)
				{
					foreach (var unityNode in klass.ReleaseRootNode.SubNodes)
					{
						AddLoadToProcessor(unityNode, releaseModeProcessor, fields);
					}
				}

				editorModeProcessor.Emit(OpCodes.Ret);
				releaseModeProcessor.Emit(OpCodes.Ret);
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

		private static void AddLoadToProcessor(UnityNode node, ILProcessor processor, List<FieldDefinition> fields)
		{
			//Get field
			var field = fields.FirstOrDefault(f => f.Name == node.Name);

			if (field == null)
				throw new Exception($"Field {node.Name} cannot be found in {processor.Body.Method.DeclaringType} (fields are {string.Join(", ", fields.Select(f => f.Name))})");

			if (SharedState.TypeDictionary.TryGetValue(node.TypeName, out var fieldType))
			{
				//Complex field type, IAssetReadable, call read

				//Load "this" for field store later
				processor.Emit(OpCodes.Ldarg_0);

				//Load reader
				processor.Emit(OpCodes.Ldarg_1);

				//Get ReadAsset
				var readMethod = CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAsset");

				//Make generic ReadAsset<T>
				var genericInst = new GenericInstanceMethod(readMethod);
				genericInst.GenericArguments.Add(processor.Body.Method.Module.ImportReference(fieldType));

				//Call it
				processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(genericInst));

				//Store result in field
				processor.Emit(OpCodes.Stfld, field);

				//Maybe Align Bytes
				MaybeAlignBytes(node, processor);

				return;
			}

			switch (node.TypeName)
			{
				//TODO
				case "vector":
				case "set":
				case "staticvector":
					return;
				case "map":
					return;
				case "pair":
					return;
				case "TypelessData": //byte array
					{
						//Load "this" for field store later
						processor.Emit(OpCodes.Ldarg_0);

						//Load reader
						processor.Emit(OpCodes.Ldarg_1);

						//Get ReadAsset
						var readMethod = CommonTypeGetter.BinaryReaderExtensionsDefinition.Resolve().Methods.First(m => m.Name == "ReadUInt8Array");

						//Call it
						processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(readMethod));

						//Store result in field
						processor.Emit(OpCodes.Stfld, field);

						//Maybe Align Bytes
						MaybeAlignBytes(node, processor);

						return;
					}
				case "Array":
					return;
			}

			//Primitives
			var csPrimitiveTypeName = SystemTypeGetter.CppPrimitivesToCSharpPrimitives[node.TypeName];
			var csPrimitiveType = processor.Body.Method.DeclaringType.Module.GetPrimitiveType(node.TypeName);

			//Read
			var primitiveReadMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}");

			primitiveReadMethod ??= SystemTypeGetter.BinaryReader.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}");

			if (primitiveReadMethod == null)
				throw new Exception($"Missing a read method for {csPrimitiveTypeName} in {processor.Body.Method.DeclaringType}");
			
			//Load this
			processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);
			
			//Call read method
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(primitiveReadMethod));

			//Store result in field
			processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
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