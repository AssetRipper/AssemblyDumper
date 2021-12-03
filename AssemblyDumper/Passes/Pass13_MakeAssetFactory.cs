using AssemblyDumper.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyDumper.Passes
{
	public static class Pass13_MakeAssetFactory
	{
		const TypeAttributes SealedClassAttributes = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public | TypeAttributes.Sealed;
		const MethodAttributes InterfaceOverrideAttributes = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual;

		public static TypeDefinition FactoryDefinition { get; private set; }

		public static void DoPass()
		{
			System.Console.WriteLine("Pass 13: Make Asset Factory");
			FactoryDefinition = CreateFactoryDefinition();
			FactoryDefinition.AddCreateEngineAsset();
			FactoryDefinition.AddCreateAsset();
		}

		private static TypeDefinition CreateFactoryDefinition()
		{
			var result = new TypeDefinition(SharedState.RootNamespace, "AssetFactory", SealedClassAttributes, SystemTypeGetter.Object);
			SharedState.Module.Types.Add(result);
			result.Interfaces.Add(new InterfaceImplementation(SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.IAssetFactory>()));
			ConstructorUtils.AddDefaultConstructor(result);
			return result;
		}

		private static void AddCreateEngineAsset(this TypeDefinition factoryDefinition)
		{
			TypeReference iassetType = SharedState.Module.ImportCommonType<AssetRipper.Core.IO.Asset.IAsset>();
			MethodDefinition createEngineAsset = new MethodDefinition("CreateEngineAsset", InterfaceOverrideAttributes, iassetType);
			createEngineAsset.Parameters.Add(new ParameterDefinition("name", ParameterAttributes.None, SystemTypeGetter.String));
			createEngineAsset.Body.GetILProcessor().EmitNotSupportedException();
			factoryDefinition.Methods.Add(createEngineAsset);
		}

		private static void AddCreateAsset(this TypeDefinition factoryDefinition)
		{
			TypeReference iunityObjectBase = SharedState.Module.ImportCommonType<AssetRipper.Core.Interfaces.IUnityObjectBase>();
			TypeReference assetInfoType = SharedState.Module.ImportCommonType<AssetRipper.Core.Parser.Asset.AssetInfo>();
			MethodDefinition createAsset = new MethodDefinition("CreateAsset", InterfaceOverrideAttributes, iunityObjectBase);
			var parameter = new ParameterDefinition("assetInfo", ParameterAttributes.None, assetInfoType);
			createAsset.Parameters.Add(parameter);
			createAsset.Body.InitLocals = true;
			createAsset.Body.GetILProcessor().EmitSwitchStatement(parameter);
			factoryDefinition.Methods.Add(createAsset);
		}

		private static void EmitSwitchStatement(this ILProcessor processor, ParameterDefinition parameter)
		{
			List<(int, MethodReference)> constructors = GetAllAssetInfoConstructors();
			int count = constructors.Count;

			var switchCondition = new VariableDefinition(SystemTypeGetter.Int32);
			processor.Body.Variables.Add(switchCondition);
			{
				processor.Emit(OpCodes.Ldarg, parameter);
				MethodReference propertyRef = SharedState.Module.ImportCommonMethod<AssetRipper.Core.Parser.Asset.AssetInfo>(m => m.Name == "get_ClassNumber");
				processor.Emit(OpCodes.Call, propertyRef);
			}
			processor.Emit(OpCodes.Stloc, switchCondition);

			Instruction[] nopInstructions = Enumerable.Range(0, count).Select(i => processor.Create(OpCodes.Nop)).ToArray();
			Instruction defaultNop = processor.Create(OpCodes.Nop);
			for(int i = 0; i < count; i++)
			{
				processor.Emit(OpCodes.Ldloc, switchCondition);
				processor.Emit(OpCodes.Ldc_I4, constructors[i].Item1);
				processor.Emit(OpCodes.Beq, nopInstructions[i]);
			}
			processor.Emit(OpCodes.Br, defaultNop);
			for (int i = 0; i < count; i++)
			{
				processor.Append(nopInstructions[i]);
				processor.Emit(OpCodes.Ldarg, parameter);
				processor.Emit(OpCodes.Newobj, constructors[i].Item2);
				processor.Emit(OpCodes.Ret);
			}
			processor.Append(defaultNop);
			processor.Emit(OpCodes.Ldnull);
			processor.Emit(OpCodes.Ret);
		}

		private static List<(int, MethodReference)> GetAllAssetInfoConstructors()
		{
			var result = new List<(int, MethodReference)>();
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
