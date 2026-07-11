using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class CredentialProtectorTests
    {
        [Fact]
        public void RoundTrip_ProtectThenUnprotect_ReturnsOriginalPassword()
        {
            // macOS/Linux에서는 실제 OS 자격 증명 저장소(Keychain/libsecret)에 기록될 수 있으므로,
            // 테스트가 개발 머신의 실제 상태를 남기지 않도록 반드시 정리한다.
            string original = "s3cr3t-P@ss";
            try
            {
                string protectedValue = CredentialProtector.Protect(original);
                string unprotected = CredentialProtector.Unprotect(protectedValue);

                Assert.Equal(original, unprotected);
                Assert.NotEqual(original, protectedValue); // 저장값이 평문 그대로여서는 안 된다.
            }
            finally
            {
                CredentialProtector.ClearStoredSecret();
            }
        }

        [Fact]
        public void Protect_EmptyString_ReturnsEmptyString()
        {
            Assert.Equal("", CredentialProtector.Protect(""));
        }

        [Fact]
        public void Unprotect_LegacyPlaintextValue_ReturnsAsIs()
        {
            // 이전 버전에서 평문으로 저장된 setting.ini와의 호환성.
            string legacyPlaintext = "old-plaintext-password";

            Assert.Equal(legacyPlaintext, CredentialProtector.Unprotect(legacyPlaintext));
        }

        [Fact]
        public void Unprotect_B64MarkedValue_DecodesCorrectly()
        {
            // OS 자격 증명 저장소를 사용할 수 없을 때의 폴백 형식.
            string encoded = "B64:" + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fallback-pw"));

            Assert.Equal("fallback-pw", CredentialProtector.Unprotect(encoded));
        }

        [Fact]
        public void Unprotect_UnresolvableKeychainMarker_ReturnsEmptyString()
        {
            // 이 값이 저장된 적 없는 경우(테스트 환경 등) 조회에 실패하면 빈 문자열을 반환해야 한다.
            CredentialProtector.ClearStoredSecret();

            Assert.Equal("", CredentialProtector.Unprotect("KEYCHAIN"));
        }
    }

    public class AppSettingsDataTests
    {
        [Fact]
        public void IsInScheduleHour_ClockHourZero_MapsToLabel24()
        {
            var data = new AppSettingsData();
            data.HourEnabled[23] = true; // label 24

            Assert.True(data.IsInScheduleHour(0));
        }

        [Fact]
        public void IsInScheduleHour_MatchingHour_ReturnsTrue()
        {
            var data = new AppSettingsData();
            data.HourEnabled[8] = true; // label 9 => 09시

            Assert.True(data.IsInScheduleHour(9));
            Assert.False(data.IsInScheduleHour(10));
        }
    }
}
