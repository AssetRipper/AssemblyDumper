using System.Diagnostics;

namespace AssetRipper.AssemblyDumper
{
	internal sealed class TimingCookie : IDisposable
	{
		private readonly Stopwatch stopWatch = new();

		public TimingCookie(string message)
		{
			Console.WriteLine(message);
			stopWatch.Start();
		}

		public void Dispose()
		{
			stopWatch.Stop();
			Console.WriteLine($"\tFinished in {stopWatch.ElapsedMilliseconds} ms");
		}
	}
}
