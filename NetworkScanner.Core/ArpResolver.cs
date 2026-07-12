using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NetworkScanner
{
    // Windows에서는 iphlpapi의 SendARP를, macOS/Linux에서는 시스템 arp 명령 출력을 파싱해
    // MAC 주소를 조회하는 크로스플랫폼 ARP 조회기. 실패 시 null을 반환한다("XX-XX-XX-XX-XX-XX" 형식).
    public static class ArpResolver
    {
        public static Action<string>? OnError { get; set; }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int destinationIPValue, int sourceIPValue, byte[] physicalAddressArray, ref uint physicalAddresArrayLength);

        public static string? GetMacAddress(string ip)
        {
            try
            {
                return OperatingSystem.IsWindows() ? GetMacAddressWindows(ip) : GetMacAddressUnix(ip);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                return null;
            }
        }

        private static string? GetMacAddressWindows(string ip)
        {
            IPAddress destinationIPAddress = IPAddress.Parse(ip);
            byte[] macBytes = new byte[6];
            uint macBytesLength = (uint)macBytes.Length;
            int destinationIPValue = BitConverter.ToInt32(destinationIPAddress.GetAddressBytes(), 0);

            int returnCode = SendARP(destinationIPValue, 0, macBytes, ref macBytesLength);
            if (returnCode != 0) return null;

            string[] parts = new string[(int)macBytesLength];
            for (int i = 0; i < macBytesLength; i++)
            {
                parts[i] = macBytes[i].ToString("X2");
            }
            return string.Join("-", parts);
        }

        private static string? GetMacAddressUnix(string ip)
        {
            // 사전에 ping 등으로 ARP 캐시가 채워져 있어야 조회된다(호출자가 ping 후 호출).
            string output = RunProcess("arp", $"-n {ip}");
            if (string.IsNullOrEmpty(output))
            {
                output = RunProcess("arp", ip);
            }

            Match match = Regex.Match(output, @"([0-9A-Fa-f]{1,2}[:-]){5}[0-9A-Fa-f]{1,2}");
            if (!match.Success) return null;

            return NormalizeMac(match.Value);
        }

        // macOS/Linux의 arp는 각 옥텟에서 앞자리 0을 생략해 "0:23:aa:..." 처럼 출력한다.
        // OUI 조회 키("00-23-AA")·표시 형식과 맞도록 옥텟마다 2자리로 0을 채우고 대문자·하이픈 형식으로 정규화한다.
        private static string NormalizeMac(string raw)
        {
            string[] octets = raw.Split(':', '-');
            for (int i = 0; i < octets.Length; i++)
            {
                octets[i] = octets[i].ToUpperInvariant().PadLeft(2, '0');
            }
            return string.Join("-", octets);
        }

        private static string RunProcess(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process? process = Process.Start(psi);
                if (process == null) return "";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);
                return output;
            }
            catch
            {
                return "";
            }
        }
    }
}
