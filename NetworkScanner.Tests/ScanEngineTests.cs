using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class ScanEngineTests
    {
        [Fact]
        public void ComputeIPCount_SingleRangeWithinOneOctet_CountsInclusiveRange()
        {
            var ranges = new ScanRangeList();
            ranges.AddItem(new ScanRangeInfo { Index = 1, StartIP = "10.0.0.1", EndIP = "10.0.0.10", Description = "" });

            Assert.Equal(10, ScanEngine.ComputeIPCount(ranges));
        }

        [Fact]
        public void ComputeIPCount_RangeCrossingSubnetBoundary_CountsCorrectly()
        {
            // Phase 2에서 수정한 서브넷 경계 버그의 회귀 테스트.
            var ranges = new ScanRangeList();
            ranges.AddItem(new ScanRangeInfo { Index = 1, StartIP = "10.0.1.250", EndIP = "10.0.2.10", Description = "" });

            Assert.Equal(17, ScanEngine.ComputeIPCount(ranges));
        }

        [Fact]
        public void ComputeIPCount_InvalidRange_IsExcluded()
        {
            var ranges = new ScanRangeList();
            ranges.AddItem(new ScanRangeInfo { Index = 1, StartIP = "not-an-ip", EndIP = "10.0.0.10", Description = "" });

            Assert.Equal(0, ScanEngine.ComputeIPCount(ranges));
        }

        [Fact]
        public void SanitizeFileNameComponent_RemovesPathTraversalAndSeparators()
        {
            // ".."와 경로 구분자를 제거해 상위 디렉터리 접근을 막는지 확인한다.
            string result = ScanEngine.SanitizeFileNameComponent("../secret/name.csv");

            Assert.DoesNotContain("..", result);
            Assert.DoesNotContain("/", result);
        }
    }
}
