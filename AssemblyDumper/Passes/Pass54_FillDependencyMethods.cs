using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyDumper.Passes
{
	public static class Pass54_FillDependencyMethods
	{
		public static void DoPass()
		{
			Console.WriteLine("Pass 54: Fill Fetch Dependency Methods");
			foreach(TypeDefinition type in SharedState.TypeDictionary.Values)
			{
				type.GetDependencyMethod().Body.GetILProcessor().EmitNotSupportedException();
			}
		}

		private static MethodDefinition GetDependencyMethod(this TypeDefinition type)
		{
			return type.Methods.Single(x => x.Name == "FetchDependencies");
		}
	}
}
