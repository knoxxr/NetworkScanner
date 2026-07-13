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
        private const string KeyContinuousMonitoring = "continuousmonitoring";
        private const string KeyMonitorInterval = "monitorintervalminutes";
        private const string KeyLanguage = "language";
        private const string KeyIpListColWidths = "iplistcolwidths";
        private const string KeyIpRangeColWidths = "iprangecolwidths";

        private static string HourKey(int label) => string.Format("hr{0:D2}", label);

        public static AppSettingsData LoadSettings()
        {
            var data = new AppSettingsData();
            string filename = UserDataPaths.Resolve(SettingFileName);

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
                        case KeyContinuousMonitoring: data.ContinuousMonitoring = bool.Parse(token[1]); break;
                        case KeyMonitorInterval: if (int.TryParse(token[1], out int mins)) data.MonitorIntervalMinutes = mins; break;
                        case KeyLanguage: data.Language = token[1]; break;
                        case KeyIpListColWidths: data.IpListColumnWidths = token[1]; break;
                        case KeyIpRangeColWidths: data.IpRangeColumnWidths = token[1]; break;
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke("setting.ini 파싱 실패(줄 건너뜀): " + ex.Message);
                }
            }

            return data;
        }

        // 컬럼 너비만 갱신해 저장한다. 디스크의 기존 설정을 읽어 이 두 필드만 바꾸므로,
        // 사용자가 설정 화면에서 저장하지 않은 편집 내용을 덮어쓰지 않는다(창 종료 시 호출).
        public static void SaveColumnLayout(string ipListWidths, string ipRangeWidths)
        {
            AppSettingsData data = LoadSettings();
            data.IpListColumnWidths = ipListWidths;
            data.IpRangeColumnWidths = ipRangeWidths;
            SaveSettings(data);
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
                lines.Add($"{KeyContinuousMonitoring}={data.ContinuousMonitoring}");
                lines.Add($"{KeyMonitorInterval}={data.MonitorIntervalMinutes}");
                lines.Add($"{KeyLanguage}={data.Language}");
                lines.Add($"{KeyIpListColWidths}={data.IpListColumnWidths}");
                lines.Add($"{KeyIpRangeColWidths}={data.IpRangeColumnWidths}");

                File.WriteAllLines(UserDataPaths.Resolve(SettingFileName), lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("setting.ini 저장 실패: " + ex.Message);
            }
        }

        public static ScanRangeList LoadScanRanges()
        {
            var ranges = new ScanRangeList();
            string filename = UserDataPaths.Resolve(IPRangeFileName);

            if (File.Exists(filename))
            {
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
            }

            // 설정된 대역이 하나도 없으면(최초 실행이거나, 파일은 있지만 전부 지워진 경우) 이 PC가
            // 속한 로컬 서브넷을 기본값으로 채워 넣어, 스캔 화면을 처음 열어도 바로 사용할 수 있게 한다.
            if (ranges.Count == 0)
            {
                ScanRangeInfo? localRange = LocalNetworkInfo.GetLocalSubnetRange();
                if (localRange != null)
                {
                    ranges.AddItem(localRange);
                }

                SaveScanRanges(ranges);
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
                File.WriteAllLines(UserDataPaths.Resolve(IPRangeFileName), lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("iprange.ini 저장 실패: " + ex.Message);
            }
        }
    }
}
