using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class ScanRangeListTests
    {
        [Fact]
        public void AddItem_NewRange_IsAdded()
        {
            var list = new ScanRangeList();

            list.AddItem(new ScanRangeInfo { Index = 1, StartIP = "10.0.0.1", EndIP = "10.0.0.10", Description = "test" });

            Assert.Single(list);
        }

        [Fact]
        public void AddItem_DuplicateStartOrEndIP_IsIgnored()
        {
            var list = new ScanRangeList();
            list.AddItem(new ScanRangeInfo { Index = 1, StartIP = "10.0.0.1", EndIP = "10.0.0.10", Description = "first" });

            list.AddItem(new ScanRangeInfo { Index = 2, StartIP = "10.0.0.1", EndIP = "10.0.0.99", Description = "duplicate start ip" });

            Assert.Single(list);
        }

        [Fact]
        public void DelItem_ExistingRange_IsRemoved()
        {
            var list = new ScanRangeList();
            list.AddItem(new ScanRangeInfo { Index = 1, StartIP = "10.0.0.1", EndIP = "10.0.0.10", Description = "test" });

            list.DelItem("10.0.0.1", "10.0.0.10");

            Assert.Empty(list);
        }

        [Fact]
        public void IsExist_ByIndex_ReturnsExpectedResult()
        {
            var list = new ScanRangeList();
            list.AddItem(new ScanRangeInfo { Index = 5, StartIP = "10.0.0.1", EndIP = "10.0.0.10", Description = "test" });

            Assert.True(list.IsExist(5));
            Assert.False(list.IsExist(6));
        }
    }
}
