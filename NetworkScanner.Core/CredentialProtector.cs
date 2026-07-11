using System;
using System.Security.Cryptography;
using System.Text;

namespace NetworkScanner
{
    // FTP 비밀번호 등을 저장 전 보호한다. Windows에서는 DPAPI(사용자 계정 범위)를 사용해 기존 WPF 앱과
    // 완전히 동일한 형식(접두어 없는 Base64)으로 저장한다. DPAPI가 없는 macOS/Linux에서는 "B64:" 접두어를
    // 붙인 단순 Base64로 저장한다 — 이는 암호화가 아니라 난독화이며, 향후 OS 키체인(Keychain/libsecret)
    // 연동 전까지의 임시 방편임을 명확히 한다.
    public static class CredentialProtector
    {
        private const string UnixMarker = "B64:";

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            if (OperatingSystem.IsWindows())
            {
                byte[] cipherBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }

            return UnixMarker + Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        public static string Unprotect(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return "";

            if (storedValue.StartsWith(UnixMarker, StringComparison.Ordinal))
            {
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(storedValue.Substring(UnixMarker.Length)));
                }
                catch
                {
                    return storedValue;
                }
            }

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    byte[] cipherBytes = Convert.FromBase64String(storedValue);
                    byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plainBytes);
                }
                catch
                {
                    // 이전 버전에서 평문으로 저장된 값과의 호환을 위한 폴백.
                    return storedValue;
                }
            }

            // Windows에서 DPAPI로 암호화된 값을 다른 OS에서 복호화할 수는 없다.
            // 평문으로 저장되었던 값이라고 가정하고 그대로 반환한다.
            return storedValue;
        }
    }
}
