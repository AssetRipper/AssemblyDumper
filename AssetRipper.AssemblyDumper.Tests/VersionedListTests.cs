using AssetRipper.AssemblyDumper.Utils;
using AssetRipper.VersionUtilities;

namespace AssetRipper.AssemblyDumper.Tests
{
	internal class VersionedListTests
	{
		[Test]
		public void DivisionTest()
		{
			VersionedList<Int> integers = new()
			{
				{ new UnityVersion(3), 3 },
				{ new UnityVersion(4), 4 },
				{ new UnityVersion(5), 5 },
				{ new UnityVersion(2017), 7 },
			};
			Assert.That(integers.Count, Is.EqualTo(4));
			Assert.That(integers[2].Key, Is.EqualTo(new UnityVersion(5)));
			Assert.That(integers[3].Key, Is.EqualTo(new UnityVersion(2017)));

			integers.Divide(new UnityVersion(5));
			Assert.That(integers.Count, Is.EqualTo(4));
			Assert.That(integers[2].Key, Is.EqualTo(new UnityVersion(5)));
			Assert.That(integers[3].Key, Is.EqualTo(new UnityVersion(2017)));

			integers.Divide(new UnityVersion(2017));
			Assert.That(integers.Count, Is.EqualTo(4));
			Assert.That(integers[2].Key, Is.EqualTo(new UnityVersion(5)));
			Assert.That(integers[3].Key, Is.EqualTo(new UnityVersion(2017)));

			integers.Divide(new UnityVersion(6));
			Assert.That(integers.Count, Is.EqualTo(5));
			Assert.That(integers[2].Key, Is.EqualTo(new UnityVersion(5)));
			Assert.That(integers[3].Key, Is.EqualTo(new UnityVersion(6)));
			Assert.That(integers[4].Key, Is.EqualTo(new UnityVersion(2017)));
		}

		private readonly record struct Int(int Value) : IDeepCloneable<Int>
		{
			public Int DeepClone() => this;
			public static implicit operator Int(int value) => new Int(value);
			public static implicit operator int(Int value) => value.Value;
		}
	}
}
