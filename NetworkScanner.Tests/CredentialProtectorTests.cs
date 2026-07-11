using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class CredentialProtectorTests
    {
        [Fact]
        public void RoundTrip_ProtectThenUnprotect_ReturnsOriginalPassword()
        {
            string original = "s3cr3t-P@ss";

            string protectedValue = CredentialProtector.Protect(original);
            string unprotected = CredentialProtector.Unprotect(protectedValue);

            Assert.Equal(original, unprotected);
            Assert.NotEqual(original, protectedValue); // 저장값이 평문 그대로여서는 안 된다.
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
