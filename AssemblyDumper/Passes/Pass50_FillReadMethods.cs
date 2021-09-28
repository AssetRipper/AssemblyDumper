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

				//Logger.Info($"Generating the editor read method for {name}");
				if (klass.EditorRootNode != null)
				{
					foreach (var unityNode in klass.EditorRootNode.SubNodes)
					{
						AddLoadToProcessor(unityNode, editorModeProcessor, fields);
					}
				}

				//Logger.Info($"Generating the release read method for {name}");
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
				ReadAssetType(node, processor, field, fieldType, 0);
				return;
			}

			switch (node.TypeName)
			{
				//TODO
				case "vector":
				case "set":
				case "staticvector":
					ReadVector(node, processor, field, 1);
					return;
				case "map":
					return;
				case "pair":
					return;
				case "TypelessData": //byte array
					ReadByteArray(node, processor, field);
					return;
				case "Array":
					ReadArray(node, processor, field, 1);
					return;
			}

			ReadPrimitiveType(node, processor, field, 0);
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

		private static void ReadPrimitiveType(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			//Primitives
			var csPrimitiveTypeName = SystemTypeGetter.CppPrimitivesToCSharpPrimitives[node.TypeName];
			var csPrimitiveType = processor.Body.Method.DeclaringType.Module.GetPrimitiveType(node.TypeName);

			//Read
			MethodDefinition primitiveReadMethod = arrayDepth switch
			{
				0 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}") //String
					?? CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}")
					?? SystemTypeGetter.BinaryReader.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}"),//Byte, SByte, and Boolean
				1 => CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}Array"),
				2 => CommonTypeGetter.EndianReaderExtensionsDefinition.Resolve().Methods.FirstOrDefault(m => m.Name == $"Read{csPrimitiveTypeName}ArrayArray"),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadPrimitiveType does not support array depth '{arrayDepth}'"),
			};

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
			//Note: string has its own alignment built-in. That's why this doesn't appear to align strings
			MaybeAlignBytes(node, processor);
		}

		/// <summary>
		/// Complex field type, IAssetReadable, call read
		/// </summary>
		private static void ReadAssetType(UnityNode node, ILProcessor processor, FieldDefinition field, TypeDefinition fieldType, int arrayDepth)
		{
			//Load "this" for field store later
			processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);

			//Get ReadAsset
			MethodDefinition readMethod = arrayDepth switch
			{
				0 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAsset"),
				1 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArray"),
				2 => CommonTypeGetter.AssetReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadAssetArrayArray"),
				_ => throw new ArgumentOutOfRangeException(nameof(arrayDepth), $"ReadAssetType does not support array depth '{arrayDepth}'"),
			};

			//Make generic ReadAsset<T>
			var genericInst = new GenericInstanceMethod(readMethod);
			genericInst.GenericArguments.Add(processor.Body.Method.Module.ImportReference(fieldType));

			//Call it
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(genericInst));

			//Store result in field
			processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
			MaybeAlignBytes(node, processor);
		}

		private static void ReadByteArray(UnityNode node, ILProcessor processor, FieldDefinition field)
		{
			//Load "this" for field store later
			processor.Emit(OpCodes.Ldarg_0);

			//Load reader
			processor.Emit(OpCodes.Ldarg_1);

			//Get ReadAsset
			var readMethod = CommonTypeGetter.EndianReaderDefinition.Resolve().Methods.First(m => m.Name == "ReadByteArray");

			//Call it
			processor.Emit(OpCodes.Call, processor.Body.Method.Module.ImportReference(readMethod));

			//Store result in field
			processor.Emit(OpCodes.Stfld, field);

			//Maybe Align Bytes
			//warning: this will generate incorrect reads
			//there will be a double alignment from the endian reader aligning itself
			MaybeAlignBytes(node, processor);
		}

		private static void ReadVector(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			var listTypeNode = node.SubNodes[0];
			if (listTypeNode.TypeName is "Array")
			{
				ReadArray(listTypeNode, processor, field, arrayDepth);
			}
			else
			{
				throw new ArgumentException($"Invalid subnode for {node.TypeName}", nameof(node));
			}

			//warning: this will generate incorrect reads
			//there will be a double alignment from the endian reader aligning itself
			MaybeAlignBytes(node, processor);
		}

		private static void ReadArray(UnityNode node, ILProcessor processor, FieldDefinition field, int arrayDepth)
		{
			var listTypeNode = node.SubNodes[1];
			if (SharedState.TypeDictionary.TryGetValue(listTypeNode.TypeName, out var fieldType))
			{
				ReadAssetType(listTypeNode, processor, field, fieldType, arrayDepth);
			}
			else if (listTypeNode.TypeName is "vector" or "set" or "staticvector")
			{
				ReadVector(listTypeNode, processor, field, arrayDepth + 1);
			}
			else if (listTypeNode.TypeName is "Array")
			{
				ReadArray(listTypeNode, processor, field, arrayDepth + 1);
			}
			else if (listTypeNode.TypeName is "map" or "pair")
			{
				//TODO
			}
			else
			{
				ReadPrimitiveType(listTypeNode, processor, field, arrayDepth);
			}

			MaybeAlignBytes(node, processor);
		}
	}
}