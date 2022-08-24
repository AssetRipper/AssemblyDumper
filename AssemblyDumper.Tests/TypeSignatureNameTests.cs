﻿using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using System.Collections.Generic;
using System;
using System.Text;

namespace AssetRipper.AssemblyDumper.Tests
{
	internal static class TypeSignatureNameTests
	{
		private static readonly ModuleDefinition module = new ModuleDefinition("test", KnownCorLibs.SystemPrivateCoreLib_v5_0_0_0);
		private static readonly ReferenceImporter importer = new ReferenceImporter(module);
		
		[Test]
		public static void AsmByteArrayName()
		{
			TypeSignature type = module.CorLibTypeFactory.Byte.MakeSzArrayType();
			Assert.AreEqual("Byte[]", type.Name);
		}

		[Test]
		public static void AsmListTest()
		{
			TypeSignature list = importer.ImportTypeSignature(typeof(List<>));
			GenericInstanceTypeSignature stringList = list.MakeGenericInstanceType(module.CorLibTypeFactory.String);
			Assert.AreEqual("List`1<System.String>", stringList.Name);
			Assert.AreEqual("List`1", list.Name);
		}

		[Test]
		public static void ByteArrayTest()
		{
			TypeSignature type = module.CorLibTypeFactory.Byte.MakeSzArrayType();
			Assert.AreEqual("Byte_Array", GetName(type));
		}

		[Test]
		public static void DictionaryTest()
		{
			TypeSignature dictionary = importer.ImportTypeSignature(typeof(Dictionary<,>));
			Assert.AreEqual("Dictionary", GetName(dictionary));
		}

		[Test]
		public static void DictionaryInstanceTest()
		{
			TypeSignature dictionary = importer.ImportTypeSignature(typeof(Dictionary<,>));
			GenericInstanceTypeSignature intStringDictionary = dictionary.MakeGenericInstanceType(module.CorLibTypeFactory.Int32, module.CorLibTypeFactory.String);
			Assert.AreEqual("Dictionary_Int32_String", GetName(intStringDictionary));
		}

		[Test]
		public static void ListInstanceTest()
		{
			TypeSignature list = importer.ImportTypeSignature(typeof(List<>));
			GenericInstanceTypeSignature stringList = list.MakeGenericInstanceType(module.CorLibTypeFactory.String);
			Assert.AreEqual("List_String", GetName(stringList));
		}

		[Test]
		public static void ListListInstanceTest()
		{
			TypeSignature list = importer.ImportTypeSignature(typeof(List<>));
			GenericInstanceTypeSignature stringList = list.MakeGenericInstanceType(module.CorLibTypeFactory.String);
			GenericInstanceTypeSignature stringListList = list.MakeGenericInstanceType(stringList);
			Assert.AreEqual("List_List_String", GetName(stringListList));
		}

		private static string GetName(TypeSignature type)
		{
			if (type is CorLibTypeSignature)
			{
				return type.Name ?? "";
			}
			else if (type is TypeDefOrRefSignature normalType)
			{
				string asmName = normalType.Name;
				int index = asmName.IndexOf('`');
				return index > -1 ? asmName.Substring(0, index) : asmName;
			}
			else if (type is SzArrayTypeSignature arrayType)
			{
				return $"{GetName(arrayType.BaseType)}_Array";
			}
			else if (type is GenericInstanceTypeSignature genericInstanceType)
			{
				string baseTypeName = GetName(genericInstanceType.GenericType.ToTypeSignature());
				StringBuilder sb = new StringBuilder();
				sb.Append(baseTypeName);
				foreach(TypeSignature typeArgument in genericInstanceType.TypeArguments)
				{
					sb.Append('_');
					sb.Append(GetName(typeArgument));
				}
				return sb.ToString();
			}
			else
			{
				throw new NotSupportedException($"GetName not support for {type.FullName} of type {type.GetType()}");
			}
		}
	}
}
