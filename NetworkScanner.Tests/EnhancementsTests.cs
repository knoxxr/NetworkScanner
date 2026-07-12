using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class CidrTests
    {
        [Fact]
        public void TryParseCidr_Slash24_ReturnsUsableHostRange()
        {
            Assert.True(IPRangeUtil.TryParseCidr("192.168.1.0/24", out string start, out string end));
            Assert.Equal("192.168.1.1", start);
            Assert.Equal("192.168.1.254", end);
        }

        [Fact]
        public void TryParseCidr_Slash32_ReturnsSingleHost()
        {
            Assert.True(IPRangeUtil.TryParseCidr("10.0.0.5/32", out string start, out string end));
            Assert.Equal("10.0.0.5", start);
            Assert.Equal("10.0.0.5", end);
        }

        [Theory]
        [InlineData("not-a-cidr")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/33")]
        [InlineData("192.168.1.0/-1")]
        public void TryParseCidr_Invalid_ReturnsFalse(string input)
        {
            Assert.False(IPRangeUtil.TryParseCidr(input, out _, out _));
        }
    }

    public class DeviceClassifierTests
    {
        [Fact]
        public void Classify_ByVendorKeyword()
        {
            Assert.Equal("Apple 기기", DeviceClassifier.Classify("Apple, Inc.", ""));
            Assert.Equal("네트워크 장비", DeviceClassifier.Classify("Cisco Systems", ""));
        }

        [Fact]
        public void Classify_FallsBackToPortHeuristics_WhenVendorUnknown()
        {
            Assert.Equal("프린터", DeviceClassifier.Classify("", "9100/"));
            Assert.Equal("서버/리눅스", DeviceClassifier.Classify("Unknown Co", "22/"));
        }

        [Fact]
        public void Classify_ReturnsEmpty_WhenNothingMatches()
        {
            Assert.Equal("", DeviceClassifier.Classify("Unknown Co", ""));
        }
    }

    public class ReportGeneratorTests
    {
        [Fact]
        public void BuildHtml_IncludesHostsCountsAndFlagsProhibited()
        {
            var items = new IPInfoList
            {
                new IPInfo { Ip = "10.0.0.1", Alive = true, Ports = "80/", Vendor = "Cisco Systems" },
                new IPInfo { Ip = "10.0.0.2", Alive = true, Ports = "31337/", HasProhibitedPort = true },
                new IPInfo { Ip = "10.0.0.3", Alive = false },
            };

            string html = ReportGenerator.BuildHtml(items, "TestSys", "2026-07-12 10:00:00");

            Assert.Contains("10.0.0.1", html);
            Assert.Contains("10.0.0.2", html);
            Assert.Contains("prohib", html);      // 위험 포트 행이 강조 클래스로 표시됨
            Assert.Contains("TestSys", html);
            Assert.Contains("네트워크 장비", html); // Cisco -> 종류 분류가 리포트에 반영됨
        }
    }
}
