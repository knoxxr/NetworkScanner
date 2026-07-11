using NetworkScanner;

namespace NetworkScanner.Tests
{
    // OUIInfo.LoadInfo()는 프로세스 현재 디렉터리의 "ouiinfo.ini"를 읽는다.
    // 테스트 출력 디렉터리에 Fixtures/ouiinfo.ini가 같은 이름으로 복사되어 있다(csproj의 Link 설정 참고).
    public class OUIInfoTests
    {
        [Fact]
        public void GetVender_KnownMacPrefix_ReturnsOrganizationName()
        {
            var oui = new OUIInfo();
            oui.LoadInfo();

            string vendor = oui.GetVender("00-22-72-AA-BB-CC");

            Assert.Equal("American Micro-Fuel Device Corp.", vendor);
        }

        [Fact]
        public void GetVender_UnknownMacPrefix_ReturnsEmptyString()
        {
            var oui = new OUIInfo();
            oui.LoadInfo();

            string vendor = oui.GetVender("FF-FF-FF-AA-BB-CC");

            Assert.Equal("", vendor);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsValidMac_NullOrEmpty_ReturnsFalse(string? mac)
        {
            var oui = new OUIInfo();

            Assert.False(oui.IsValidMac(mac!));
        }

        [Fact]
        public void IsValidMac_NonEmptyString_ReturnsTrue()
        {
            var oui = new OUIInfo();

            Assert.True(oui.IsValidMac("00-22-72-AA-BB-CC"));
        }
    }
}
