using System.Threading;
using System.Threading.Tasks;
using NetworkScanner;
using NetworkScanner.Tests.Fixtures;

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

        [Fact]
        public async Task DoScanAllRange_NoConfiguredRanges_ReportsGuidanceInsteadOfSilentlyFinishing()
        {
            // 검색 대역이 하나도 없으면(예: 대역 추가가 실패했는데 사용자가 눈치채지 못한 경우)
            // 진행률 없이 즉시 끝나버려 "스캔이 진행 중인지 알 수 없다"는 문제로 이어진다.
            // 대신 이유를 알려주는 메시지를 띄워야 한다.
            var config = new FakeScanConfigProvider();
            var engine = new ScanEngine(new IPInfoList(), new OUIInfo(), config);

            string? lastMessage = null;
            engine.Message += msg => lastMessage = msg;

            await engine.DoScanAllRange(scheduling: false, systemName: "test", CancellationToken.None);

            Assert.Contains("대역", lastMessage);
        }
    }
}
