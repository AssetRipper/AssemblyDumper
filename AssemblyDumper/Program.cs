using AssemblyDumper.Passes;
using System;

namespace AssemblyDumper
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Logger.Info("Making a new dll");
			try
			{
				Pass00_Initialize.DoPass();

				Pass06_AddTypeDefinitions.DoPass();
				//Pass07_ApplyInheritance.DoPass();
				//Pass08_AddDefaultConstructors.DoPass();
				
				Pass99_SaveAssembly.DoPass();
				Logger.Info("Done!");
			}
			catch (Exception ex)
			{
				Logger.Info(ex.ToString());
			}
		}
	}
}
