using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AssemblyDumper.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass90_MakeAssetFactory
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes InterfaceOverrideAttributes = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual;

		public static TypeDefinition FactoryDefinition { get; private set; }

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 90: Make Asset Factory");
			FactoryDefinition = CreateFactoryDefinition();
			FactoryDefinition.AddCreateEngineAsset();
			FactoryDefinition.AddCreateAsset();
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			var result = new TypeDefinition(SharedState.RootNamespace, "AssetFactory", SealedClassAttributes, SystemTypeGetter.Object.ToTypeDefOrRef());
			SharedState.Module.TopLevelTypes.Add(result);
			result.Interfaces.Add(new InterfaceImplementation(SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.IAssetFactory>()));
			ConstructorUtils.AddDefaultConstructor(result);
			return result;
		}

		private static void AddCreateEngineAsset(this TypeDefinition factoryDefinition)
		{
			ITypeDefOrRef iassetType = SharedState.Module.ImportCommonType<AssetRipper.Core.IO.Asset.IAsset>();
			MethodDefinition createEngineAsset = factoryDefinition.AddMethod("CreateEngineAsset", InterfaceOverrideAttributes, iassetType);
			createEngineAsset.AddParameter("name", SystemTypeGetter.String);

			createEngineAsset.CilMethodBody.Instructions.AddNotSupportedException();
		}

		private static void AddCreateAsset(this TypeDefinition factoryDefinition)
		{
			ITypeDefOrRef iunityObjectBase = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			ITypeDefOrRef assetInfoType = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.AssetInfo>();
			MethodDefinition createAsset = factoryDefinition.AddMethod("CreateAsset", InterfaceOverrideAttributes, iunityObjectBase);
			Parameter parameter = createAsset.AddParameter("assetInfo", assetInfoType);
			
			createAsset.CilMethodBody.InitializeLocals = true;
			createAsset.CilMethodBody.Instructions.EmitSwitchStatement(parameter);
		}

		private static void EmitSwitchStatement(this CilInstructionCollection processor, Parameter parameter)
		{
			List<(int, IMethodDefOrRef)> constructors = GetAllAssetInfoConstructors();
			int count = constructors.Count;

			var switchCondition = new CilLocalVariable(SystemTypeGetter.Int32);
			processor.Owner.LocalVariables.Add(switchCondition);
			{
				processor.Add(CilOpCodes.Ldarg, parameter);
				IMethodDefOrRef propertyRef = SharedState.Module.ImportCommonMethod<AssetRipper.Core.Parser.Asset.AssetInfo>(m => m.Name == "get_ClassNumber");
				processor.Add(CilOpCodes.Call, propertyRef);
			}
			processor.Add(CilOpCodes.Stloc, switchCondition);

			CilInstructionLabel[] nopInstructions = Enumerable.Range(0, count).Select(i => new CilInstructionLabel()).ToArray();
			CilInstructionLabel defaultNop = new CilInstructionLabel();
			for(int i = 0; i < count; i++)
			{
				processor.Add(CilOpCodes.Ldloc, switchCondition);
				processor.Add(CilOpCodes.Ldc_I4, constructors[i].Item1);
				processor.Add(CilOpCodes.Beq, nopInstructions[i]);
			}
			processor.Add(CilOpCodes.Br, defaultNop);
			for (int i = 0; i < count; i++)
			{
				nopInstructions[i].Instruction = processor.Add(CilOpCodes.Nop);
				processor.Add(CilOpCodes.Ldarg, parameter);
				processor.Add(CilOpCodes.Newobj, constructors[i].Item2);
				processor.Add(CilOpCodes.Ret);
			}
			defaultNop.Instruction = processor.Add(CilOpCodes.Nop);
			processor.Add(CilOpCodes.Ldnull);
			processor.Add(CilOpCodes.Ret);
		}

		private static List<(int, IMethodDefOrRef)> GetAllAssetInfoConstructors()
		{
			var result = new List<(int, IMethodDefOrRef)>();
			foreach(var pair in SharedState.ClassDictionary)
			{
				if(pair.Value.TypeID >= 0 && !pair.Value.IsAbstract) //Is an object and not abstract
				{
					result.Add((pair.Value.TypeID, SharedState.TypeDictionary[pair.Key].GetAssetInfoConstructor()));
				}
			}
			return result;
		}

		private static MethodDefinition GetAssetInfoConstructor(this TypeDefinition typeDefinition)
		{
			return typeDefinition.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.Name == "AssetInfo").Single();
		}
	}
}
