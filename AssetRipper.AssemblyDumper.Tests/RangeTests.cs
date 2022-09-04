using AssetRipper.AssemblyDumper.Utils;

namespace AssetRipper.AssemblyDumper.Tests
{
	public class RangeTests
	{
		private static readonly Range<int> ZeroToTen = new Range<int>(0, 10);
		private static readonly Range<int> OneToTen = new Range<int>(1, 10);
		private static readonly Range<int> OneToEleven = new Range<int>(1, 11);
		private static readonly Range<int> ZeroToEleven = new Range<int>(0, 11);
		private static readonly Range<int> TenToTwenty = new Range<int>(10, 20);
		private static readonly Range<int> ZeroToTwenty = new Range<int>(0, 20);

		[Test]
		public void IntersectionTest1()
		{
			Assert.IsTrue(OneToTen.Equals(ZeroToTen.MakeIntersection(OneToEleven)));
		}

		[Test]
		public void IntersectingUnionTest()
		{
			Assert.IsTrue(ZeroToEleven.Equals(ZeroToTen.MakeUnion(OneToEleven)));
		}

		[Test]
		public void NonintersectingUnionTest()
		{
			Assert.IsTrue(ZeroToTwenty.Equals(ZeroToTen.MakeUnion(TenToTwenty)));
		}

		[Test]
		public void ContainsItself()
		{
			Assert.IsTrue(ZeroToTen.Contains(ZeroToTen));
		}

		[Test]
		public void ContainsStart()
		{
			Assert.IsTrue(ZeroToTen.Contains(0));
		}

		[Test]
		public void ContainsMiddle()
		{
			Assert.IsTrue(ZeroToTen.Contains(5));
		}

		[Test]
		public void DoesNotContainEnd()
		{
			Assert.IsFalse(ZeroToTen.Contains(10));
		}

		[Test]
		public void DoesNotContainLess()
		{
			Assert.IsFalse(ZeroToTen.Contains(-10));
		}

		[Test]
		public void DoesNotContainMore()
		{
			Assert.IsFalse(ZeroToTen.Contains(100));
		}
	}
}