using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NetworkScanner
{
    // FTP 비밀번호 등을 저장 전 보호한다.
    // - Windows: DPAPI(사용자 계정 범위) — 접두어 없는 Base64로 저장(기존 WPF 앱과 완전히 동일한 형식).
    // - macOS: `security` 명령으로 로그인 Keychain에 저장하고, ini 파일에는 "KEYCHAIN" 마커만 남긴다.
    // - Linux: `secret-tool`(libsecret) 명령으로 Secret Service(GNOME Keyring 등)에 저장한다.
    // - 위 도구가 없거나 실패하면 "B64:" 접두어를 붙인 단순 Base64로 폴백한다 — 이는 암호화가 아니라
    //   난독화이며, 폴백이 발생했음을 OnError로 알린다.
    public static class CredentialProtector
    {
        public static Action<string>? OnError { get; set; }

        private const string UnixMarker = "B64:";
        private const string KeychainMarker = "KEYCHAIN";
        private const string ServiceName = "NetworkScanner";
        private const string AccountName = "ftp-password";

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            if (OperatingSystem.IsWindows())
            {
                byte[] cipherBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }

            if (OperatingSystem.IsMacOS() && TryMacKeychainStore(plainText))
            {
                return KeychainMarker;
            }

            if (OperatingSystem.IsLinux() && TryLinuxSecretStore(plainText))
            {
                return KeychainMarker;
            }

            OnError?.Invoke("OS 자격 증명 저장소(Keychain/libsecret)를 사용할 수 없어 비밀번호를 Base64로만 난독화합니다. " +
                            "macOS는 'security', Linux는 'secret-tool'(libsecret-tools 패키지)이 필요합니다.");
            return UnixMarker + Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        public static string Unprotect(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return "";

            if (storedValue == KeychainMarker)
            {
                string? secret = OperatingSystem.IsMacOS() ? TryMacKeychainRetrieve()
                    : OperatingSystem.IsLinux() ? TryLinuxSecretRetrieve()
                    : null;

                if (secret == null)
                {
                    OnError?.Invoke("OS 자격 증명 저장소에서 FTP 비밀번호를 읽지 못했습니다.");
                }
                return secret ?? "";
            }

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

        // 저장된 비밀번호를 OS 자격 증명 저장소에서 제거한다. Windows는 별도 저장소가 없으므로(값이
        // ini 파일 자체에 DPAPI로 암호화되어 들어있음) 아무 동작도 하지 않는다.
        public static void ClearStoredSecret()
        {
            if (OperatingSystem.IsMacOS())
            {
                RunProcess("security", new[] { "delete-generic-password", "-a", AccountName, "-s", ServiceName }, input: null);
            }
            else if (OperatingSystem.IsLinux())
            {
                RunProcess("secret-tool", new[] { "clear", "service", ServiceName, "account", AccountName }, input: null);
            }
        }

        // macOS: `security` 도구는 비밀번호를 표준입력이 아닌 인자(-w)로만 받기 때문에,
        // 실행 중인 잠깐 동안 다른 프로세스가 인자 목록을 볼 수 있는 것은 이 도구 자체의 한계다.
        private static bool TryMacKeychainStore(string password)
        {
            int exitCode = RunProcess("security",
                new[] { "add-generic-password", "-a", AccountName, "-s", ServiceName, "-w", password, "-U" },
                input: null);
            return exitCode == 0;
        }

        private static string? TryMacKeychainRetrieve()
        {
            var (exitCode, output) = RunProcessCaptureOutput("security",
                new[] { "find-generic-password", "-a", AccountName, "-s", ServiceName, "-w" });
            return exitCode == 0 ? output.Trim('\n', '\r') : null;
        }

        // Linux: secret-tool은 비밀번호를 표준입력으로 받으므로 인자 노출 문제가 없다.
        private static bool TryLinuxSecretStore(string password)
        {
            int exitCode = RunProcess("secret-tool",
                new[] { "store", "--label=NetworkScanner FTP Password", "service", ServiceName, "account", AccountName },
                input: password);
            return exitCode == 0;
        }

        private static string? TryLinuxSecretRetrieve()
        {
            var (exitCode, output) = RunProcessCaptureOutput("secret-tool",
                new[] { "lookup", "service", ServiceName, "account", AccountName });
            return exitCode == 0 && !string.IsNullOrEmpty(output) ? output.Trim('\n', '\r') : null;
        }

        private static int RunProcess(string fileName, string[] arguments, string? input)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName)
                {
                    RedirectStandardInput = input != null,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                foreach (var arg in arguments) psi.ArgumentList.Add(arg);

                using Process? process = Process.Start(psi);
                if (process == null) return -1;

                if (input != null)
                {
                    process.StandardInput.Write(input);
                    process.StandardInput.Close();
                }

                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                    return -1;
                }
                return process.ExitCode;
            }
            catch
            {
                // 도구가 설치되어 있지 않은 경우 등 — 호출자가 폴백 처리한다.
                return -1;
            }
        }

        private static (int ExitCode, string Output) RunProcessCaptureOutput(string fileName, string[] arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                foreach (var arg in arguments) psi.ArgumentList.Add(arg);

                using Process? process = Process.Start(psi);
                if (process == null) return (-1, "");

                string output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                    return (-1, "");
                }
                return (process.ExitCode, output);
            }
            catch
            {
                return (-1, "");
            }
        }
    }
}
