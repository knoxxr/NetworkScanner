using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetworkScanner
{
    // setting.ini/iprange.ini의 실제 읽기/쓰기를 담당한다. WPF/Avalonia UI가 공유하며,
    // UI는 AppSettingsData를 화면 컨트롤과 매핑하는 역할만 한다.
    public static class AppSettingsStore
    {
        public const string SettingFileName = "setting.ini";
        public const string IPRangeFileName = "iprange.ini";

        public static Action<string>? OnError { get; set; }

        private const string KeyUseScheduling = "usescheuling";
        private const string KeyUseFTP = "useftp";
        private const string KeyFTPIP = "ftpip";
        private const string KeyFTPID = "ftpid";
        private const string KeyFTPPW = "ftppw";
        private const string KeyFTPPORT = "ftpport";
        private const string KeySystemName = "systemname";
        private const string KeyPortList = "portlist";
        private const string KeyUsePortChecking = "useportchecking";
        private const string KeyLoadLatestFile = "loadlastestfile";

        private static string HourKey(int label) => string.Format("hr{0:D2}", label);

        public static AppSettingsData LoadSettings()
        {
            var data = new AppSettingsData();
            string filename = Path.Combine(Directory.GetCurrentDirectory(), SettingFileName);

            if (!File.Exists(filename))
            {
                try { using (File.Create(filename)) { } }
                catch (Exception ex) { OnError?.Invoke("setting.ini 생성 실패: " + ex.Message); }
                return data;
            }

            foreach (string rawLine in File.ReadAllLines(filename))
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;

                string[] token = rawLine.Split('=');
                if (token.Length < 2) continue;

                try
                {
                    bool matchedHour = false;
                    for (int label = 1; label <= 24; label++)
                    {
                        if (token[0] == HourKey(label))
                        {
                            data.HourEnabled[label - 1] = bool.Parse(token[1]);
                            matchedHour = true;
                            break;
                        }
                    }
                    if (matchedHour) continue;

                    switch (token[0])
                    {
                        case KeyUseScheduling: data.UseScheduling = bool.Parse(token[1]); break;
                        case KeyUseFTP: data.UseFTP = bool.Parse(token[1]); break;
                        case KeyFTPIP: data.FtpIp = token[1]; break;
                        case KeyFTPID: data.FtpId = token[1]; break;
                        case KeyFTPPW: data.FtpPassword = CredentialProtector.Unprotect(token[1]); break;
                        case KeyFTPPORT: data.FtpPort = token[1]; break;
                        case KeySystemName: data.SystemName = token[1]; break;
                        case KeyPortList: data.PortList = token[1]; break;
                        case KeyUsePortChecking: data.UsePortChecking = bool.Parse(token[1]); break;
                        case KeyLoadLatestFile: data.LoadLatestFileOnStartup = bool.Parse(token[1]); break;
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke("setting.ini 파싱 실패(줄 건너뜀): " + ex.Message);
                }
            }

            return data;
        }

        public static void SaveSettings(AppSettingsData data)
        {
            try
            {
                var lines = new List<string> { $"{KeyUseScheduling}={data.UseScheduling}" };

                for (int label = 1; label <= 24; label++)
                {
                    lines.Add($"{HourKey(label)}={data.HourEnabled[label - 1]}");
                }

                lines.Add($"{KeyUseFTP}={data.UseFTP}");
                lines.Add($"{KeyFTPIP}={data.FtpIp}");
                lines.Add($"{KeyFTPID}={data.FtpId}");
                lines.Add($"{KeyFTPPW}={CredentialProtector.Protect(data.FtpPassword)}");
                lines.Add($"{KeyFTPPORT}={data.FtpPort}");
                lines.Add($"{KeySystemName}={data.SystemName}");
                lines.Add($"{KeyPortList}={data.PortList}");
                lines.Add($"{KeyUsePortChecking}={data.UsePortChecking}");
                lines.Add($"{KeyLoadLatestFile}={data.LoadLatestFileOnStartup}");

                File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), SettingFileName), lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("setting.ini 저장 실패: " + ex.Message);
            }
        }

        public static ScanRangeList LoadScanRanges()
        {
            var ranges = new ScanRangeList();
            string filename = Path.Combine(Directory.GetCurrentDirectory(), IPRangeFileName);

            if (!File.Exists(filename))
            {
                // 최초 실행이라 설정된 대역이 없으면, 이 PC가 속한 로컬 서브넷을 기본값으로 제안한다.
                ScanRangeInfo? localRange = LocalNetworkInfo.GetLocalSubnetRange();
                if (localRange != null)
                {
                    ranges.AddItem(localRange);
                }

                SaveScanRanges(ranges);
                return ranges;
            }

            foreach (string line in File.ReadAllLines(filename))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    string[] token = line.Split(',');
                    if (token.Length < 4) continue;

                    ranges.AddItem(new ScanRangeInfo
                    {
                        Index = int.Parse(token[0]),
                        StartIP = token[1],
                        EndIP = token[2],
                        Description = token[3],
                    });
                }
                catch (Exception ex)
                {
                    OnError?.Invoke("iprange.ini 파싱 실패(줄 건너뜀): " + ex.Message);
                }
            }

            return ranges;
        }

        public static void SaveScanRanges(ScanRangeList ranges)
        {
            try
            {
                var lines = new List<string>();
                foreach (var info in ranges)
                {
                    lines.Add($"{info.Index},{info.StartIP},{info.EndIP},{info.Description}");
                }
                File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), IPRangeFileName), lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("iprange.ini 저장 실패: " + ex.Message);
            }
        }
    }
}
