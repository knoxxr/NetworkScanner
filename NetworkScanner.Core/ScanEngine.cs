using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkScanner
{
    // UCIPList(WPF)의 스캔/저장/불러오기 로직을 UI 프레임워크에 의존하지 않는 형태로 추출한 것.
    // WPF와 Avalonia UI가 이 클래스를 공유하며, 진행 상태/메시지/오류는 이벤트로 UI에 알린다.
    // UI 스레드 마샬링(Dispatcher.Invoke 등)은 호스트 UI가 이벤트 핸들러 안에서 직접 처리한다.
    public class ScanEngine
    {
        private readonly IPInfoList _items;
        private readonly OUIInfo _oui;
        private readonly FTPService _ftp = new FTPService();
        private readonly object _scanLock = new object();
        private CancellationTokenSource? _scanCts;

        public IScanConfigProvider Config { get; set; }
        public Task? Scanning { get; private set; }
        public IPInfoList Items => _items;

        public event Action<string>? Message;
        public event Action<int>? ProgressMaxChanged;
        public event Action<int>? ProgressChanged;
        public event Action<int, int, int>? ResultsSummaryChanged; // alive, dead, total
        public event Action? ItemsRefreshNeeded;
        public event Action? ScanStarted;
        public event Action? ScanFinished;

        public ScanEngine(IPInfoList items, OUIInfo oui, IScanConfigProvider config)
        {
            _items = items;
            _oui = oui;
            Config = config;
        }

        public void InitFromConfig()
        {
            _ftp.HostIP = Config.GetFTPIP();
            _ftp.ID = Config.GetFTPID();
            _ftp.PW = Config.GetFTPPW();
            _ftp.Port = Config.GetFTPPort();

            PingTester._PortList.Clear();
            foreach (string port in Config.GetPortList().Split('/'))
            {
                if (int.TryParse(port, out int castingPort) && castingPort > 0)
                {
                    PingTester._PortList.Add(castingPort);
                }
            }
        }

        public void LoadIPInfo(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                ParsingIP(lines);
                RaiseResultsSummary();
                ItemsRefreshNeeded?.Invoke();
            }
            catch (Exception ex)
            {
                Message?.Invoke("파일을 불러오지 못했습니다: " + ex.Message);
            }
        }

        private void ParsingIP(string[] raw)
        {
            _items.Clear();
            int lineCount = 0;
            foreach (string line in raw)
            {
                if (lineCount++ == 0) continue;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    string[] token = line.Split(",");
                    if (token.Length < 5) continue;

                    IPInfo ip = new IPInfo
                    {
                        Ip = token[0],
                        Ports = token[1],
                        SystemName = token[2],
                        Description = token[3],
                        CommitDate = token[4],
                    };
                    if (token.Length >= 6 && bool.TryParse(token[5], out bool alive))
                        ip.Alive = alive;
                    if (token.Length >= 7)
                        ip.Macaddr = token[6];
                    if (token.Length >= 8)
                        ip.Vendor = token[7];
                    _items.Add(ip);
                }
                catch (Exception ex)
                {
                    AppLogger.LogError("NetworkScanner", "IP 목록 파싱 실패(줄 건너뜀): " + ex.Message);
                }
            }
        }

        public static string SanitizeFileNameComponent(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            string result = name.Replace("..", "");
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c, '_');
            }
            return result;
        }

        public static string GetEnvDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "env");
        }

        public async Task WriteIPInfo(bool autosave, string systemName)
        {
            if (_items.Count == 0)
            {
                Message?.Invoke("저장할 아이템이 없습니다");
                return;
            }

            try
            {
                List<string> lines = new List<string>
                {
                    "IPAddress,Port,SystemName,Description,Commitdate,Alive,MacAddress,Vendor,RoundTime"
                };

                foreach (IPInfo info in _items)
                {
                    lines.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                        info.Ip, info.Ports, info.SystemName, info.Description, info.CommitDate,
                        info.Alive, info.Macaddr, info.Vendor, info.RountTime));
                }

                string path = GetEnvDirectory();
                Directory.CreateDirectory(path);

                string autosaveTag = autosave ? "_(SCHEDULING)" : "";
                string safeSystemName = SanitizeFileNameComponent(systemName);
                string filename = $"{safeSystemName}{DateTime.Now:_yyyyMMdd_HHmmss}{autosaveTag}.csv";
                await File.WriteAllLinesAsync(Path.Combine(path, filename), lines, Encoding.UTF8);

                if (Config.GetUseFTP() == true)
                {
                    _ftp.UploadFileList(path + Path.DirectorySeparatorChar, filename);
                }

                Message?.Invoke($"파일을 저장했습니다.  File Name : {filename}");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("NetworkScanner", "IP 목록 저장 실패: " + ex.Message);
                Message?.Invoke("파일 저장 중 오류가 발생했습니다: " + ex.Message);
            }
        }

        public void GetLatestFilePath(string prefixName)
        {
            if (string.IsNullOrEmpty(prefixName)) return;

            string path = GetEnvDirectory();
            if (!Directory.Exists(path)) return;

            FileInfo? file = new DirectoryInfo(path).GetFiles(prefixName + "*.csv")
                .OrderByDescending(fi => fi.LastWriteTime)
                .FirstOrDefault();

            if (file != null)
            {
                LoadIPInfo(file.FullName);
            }
        }

        // 이미 스캐닝 중인지 확인하고, 아니라면 새 CancellationTokenSource를 발급해 스캔을 시작할 수 있게 한다.
        // 취소 여부 확인과 Scanning 필드 대입을 한 lock 안에서 처리해 두 스캔이 동시에 시작되는 경쟁 상태를 막는다.
        private bool TryBeginScan(out CancellationToken token)
        {
            lock (_scanLock)
            {
                if (IsScanning())
                {
                    Message?.Invoke("이미 스캐닝 중입니다.");
                    token = default;
                    return false;
                }

                _scanCts?.Dispose();
                _scanCts = new CancellationTokenSource();
                token = _scanCts.Token;
                return true;
            }
        }

        public void ScanningStop()
        {
            lock (_scanLock)
            {
                _scanCts?.Cancel();
            }
            Message?.Invoke("스캔 취소를 요청했습니다.");
        }

        public bool IsScanning()
        {
            return Scanning != null && Scanning.Status == TaskStatus.Running && !Scanning.IsCompleted;
        }

        public bool StartSchedulingScan(string systemName)
        {
            if (!TryBeginScan(out CancellationToken token)) return false;
            Scanning = RunTracked(DoScanAllRange(true, systemName, token));
            return true;
        }

        public bool StartRefreshAllRange(string systemName)
        {
            if (!TryBeginScan(out CancellationToken token)) return false;
            Scanning = RunTracked(DoScanAllRange(false, systemName, token));
            return true;
        }

        public bool StartCheckUserPortList(string ip)
        {
            if (!TryBeginScan(out CancellationToken token)) return false;
            Scanning = RunTracked(DoCheckUserPortList(ip, token));
            return true;
        }

        public bool StartCheckReservedPortList(string ip)
        {
            if (!TryBeginScan(out CancellationToken token)) return false;
            Scanning = RunTracked(DoCheckReservedPortList(ip, token));
            return true;
        }

        public bool StartCheckProhibitPortList(string ip)
        {
            if (!TryBeginScan(out CancellationToken token)) return false;
            Scanning = RunTracked(DoCheckProhibitPortList(ip, token));
            return true;
        }

        // Start* 메서드가 공통으로 거치는 래퍼: UI가 버튼 상태(예: "스캔 중…" 비활성화)를
        // 이벤트로만 구독해도 알 수 있도록 스캔 시작/종료를 알린다.
        private async Task RunTracked(Task task)
        {
            ScanStarted?.Invoke();
            try
            {
                await task;
            }
            finally
            {
                ScanFinished?.Invoke();
            }
        }

        public void PingOnce(string ip)
        {
            var reply = PingTester.SendPing(ip);
            var openPorts = PingTester.CheckPortsOpen(ip);
            RefreshIPInfo(reply, ip, openPorts);

            string status = reply != null ? reply.Status.ToString() : "실패(권한 등 오류, 로그 확인)";
            Message?.Invoke($"수동으로 {ip}으로 Ping을 보냈습니다. 결과 : {status}");
        }

        public async Task DoCheckReservedPortList(string ip, CancellationToken token)
        {
            int idx = 0;
            var portList = Config.GetReservedPortList();
            int maxCnt = portList.Count;
            IPInfo ipInfo = _items.GetItem(ip);
            ProgressMaxChanged?.Invoke(maxCnt);
            Message?.Invoke($" {ipInfo.Ip}으로 예약된 포트 전부 검색을 시작합니다.");
            string reservedPorts = "";

            await Task.Run(() =>
            {
                foreach (RefPortInfo port in portList)
                {
                    if (token.IsCancellationRequested) break;

                    if (PingTester.CheckReservedPortsOpen(ipInfo.Ip, port.PortNo))
                    {
                        reservedPorts += port.PortNo + "/";
                    }
                    ProgressChanged?.Invoke(idx++);
                    Message?.Invoke($"({idx}/{maxCnt})IP: {ip}, 검색 Port: {port.PortNo}");
                }

                ipInfo.Ports = reservedPorts;
                ItemsRefreshNeeded?.Invoke();
            });

            ProgressChanged?.Invoke(0);
            Message?.Invoke($" {ipInfo.Ip}으로 예약된 포트 전부 검색 했습니다. 결과 : {reservedPorts}");
        }

        public async Task DoCheckProhibitPortList(string ip, CancellationToken token)
        {
            int idx = 0;
            var portList = Config.GetProhibitPortList();
            int maxCnt = portList.Count;
            IPInfo ipInfo = _items.GetItem(ip);
            ProgressMaxChanged?.Invoke(maxCnt);
            Message?.Invoke($" {ipInfo.Ip}으로 금지된 포트 전부 검색을 시작합니다.");
            string prohibitPorts = "";

            await Task.Run(() =>
            {
                foreach (RefPortInfo port in portList)
                {
                    if (token.IsCancellationRequested) break;

                    if (PingTester.CheckReservedPortsOpen(ipInfo.Ip, port.PortNo))
                    {
                        prohibitPorts += port.PortNo + "/";
                    }
                    ProgressChanged?.Invoke(idx++);
                    Message?.Invoke($"({idx}/{maxCnt})IP: {ipInfo.Ip}, 검색 Port: {port.PortNo}");
                }

                ipInfo.Ports = prohibitPorts;
                ItemsRefreshNeeded?.Invoke();
            });

            ProgressChanged?.Invoke(0);
            Message?.Invoke($" {ipInfo.Ip}으로 금지된 포트 전부 검색 했습니다. 결과 : {prohibitPorts}");
        }

        public async Task DoCheckUserPortList(string ip, CancellationToken token)
        {
            int idx = 0;
            IPInfo ipInfo = _items.GetItem(ip);
            Message?.Invoke($" {ipInfo.Ip}으로 사용자 포트 전부 검색을 시작합니다.");

            var portList = Config.GetPortList().Split('/');
            int maxCnt = portList.Length;
            ProgressMaxChanged?.Invoke(maxCnt);
            string userPorts = "";

            await Task.Run(() =>
            {
                foreach (string port in portList)
                {
                    if (token.IsCancellationRequested) break;

                    if (int.TryParse(port, out int portNo) && PingTester.CheckReservedPortsOpen(ipInfo.Ip, portNo))
                    {
                        userPorts += port + "/";
                    }
                    ProgressChanged?.Invoke(idx++);
                    Message?.Invoke($"({idx}/{maxCnt})IP: {ipInfo.Ip}, 검색 Port: {port}");
                }

                ipInfo.Ports = userPorts;
                ItemsRefreshNeeded?.Invoke();
            });

            ProgressChanged?.Invoke(0);
            Message?.Invoke($" {ipInfo.Ip}으로 사용자 포트 전부 검색 했습니다. 결과 : {userPorts}");
        }

        public async Task DoScanAllRange(bool scheduling, string systemName, CancellationToken token)
        {
            ScanRangeList ranges = Config.GetScanRanges();
            int maxCnt = ComputeIPCount(ranges);
            ProgressMaxChanged?.Invoke(maxCnt);
            int idx = 0;
            Message?.Invoke($"전체 대역 스캔을 시작합니다. {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            bool? usePortChecking = Config.GetUsePortChecking();

            await Task.Run(() =>
            {
                foreach (ScanRangeInfo item in ranges)
                {
                    if (token.IsCancellationRequested) break;

                    uint startVal, endVal;
                    try
                    {
                        startVal = IPRangeUtil.ToUInt32(IPAddress.Parse(item.StartIP));
                        endVal = IPRangeUtil.ToUInt32(IPAddress.Parse(item.EndIP));
                    }
                    catch (FormatException)
                    {
                        Message?.Invoke($"잘못된 IP 대역을 건너뜁니다: {item.StartIP} ~ {item.EndIP}");
                        continue;
                    }

                    if (endVal < startVal) continue;

                    for (uint ipVal = startVal; ipVal <= endVal; ipVal++)
                    {
                        if (token.IsCancellationRequested) break;

                        IPAddress newIp = IPRangeUtil.FromUInt32(ipVal);
                        string strIp = newIp.ToString();
                        var reply = PingTester.SendPing(newIp);

                        string openPorts = "";
                        if (usePortChecking == true)
                        {
                            openPorts = PingTester.CheckPortsOpen(strIp);
                        }

                        RefreshIPInfo(reply, strIp, openPorts);
                        Message?.Invoke($"Send Ping to : {strIp}");
                        RaiseResultsSummary();
                        ProgressChanged?.Invoke(idx++);
                    }
                }
            });
            ProgressChanged?.Invoke(0);
            Message?.Invoke(token.IsCancellationRequested
                ? $"전체 대역 스캔이 취소되었습니다. {DateTime.Now:yyyy/MM/dd HH:mm:ss}"
                : $"전체 대역 스캔을 완료했습니다. {DateTime.Now:yyyy/MM/dd HH:mm:ss}");

            if (scheduling) await WriteIPInfo(true, systemName);
        }

        public static int ComputeIPCount(ScanRangeList ranges)
        {
            int cnt = 0;
            foreach (ScanRangeInfo info in ranges)
            {
                try
                {
                    uint startVal = IPRangeUtil.ToUInt32(IPAddress.Parse(info.StartIP));
                    uint endVal = IPRangeUtil.ToUInt32(IPAddress.Parse(info.EndIP));
                    if (endVal >= startVal)
                        cnt += (int)(endVal - startVal + 1);
                }
                catch (FormatException)
                {
                    // 잘못된 IP 형식의 대역은 카운트에서 제외한다.
                }
            }
            return cnt;
        }

        private void RefreshIPInfo(PingReply? reply, string targetIp, string openPorts)
        {
            bool success = reply != null && reply.Status == IPStatus.Success;

            IPInfo info = _items.GetItem(targetIp);
            if (info != null)
            {
                info.Ports = openPorts;
                info.RountTime = success ? reply!.RoundtripTime.ToString() : "Timeout";
                info.Alive = success;
                info.HasProhibitedPort = ContainsProhibitedPort(openPorts);
                info.Macaddr = _items.GetMACAddress(targetIp);
                info.Vendor = _oui.GetVender(info.Macaddr);

                if (info.SystemName == "")
                    info.SystemName = _items.GetHostName(IPAddress.Parse(targetIp));
                ItemsRefreshNeeded?.Invoke();
            }
            else if (success)
            {
                IPInfo newIpInfo = new()
                {
                    Ip = targetIp,
                    Ports = openPorts,
                    Description = "",
                    CommitDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    RountTime = reply!.RoundtripTime.ToString(),
                    Alive = true,
                    HasProhibitedPort = ContainsProhibitedPort(openPorts),
                };
                newIpInfo.Macaddr = _items.GetMACAddress(targetIp);
                newIpInfo.Vendor = _oui.GetVender(newIpInfo.Macaddr);
                newIpInfo.SystemName = _items.GetHostName(IPAddress.Parse(targetIp));
                _items.Add(newIpInfo);
                ItemsRefreshNeeded?.Invoke();
            }
        }

        // 스캔에서 발견된 열린 포트 중 위험(백도어) 포트 목록과 일치하는 것이 있는지 확인한다.
        private bool ContainsProhibitedPort(string portsField)
        {
            if (string.IsNullOrEmpty(portsField)) return false;

            List<RefPortInfo> prohibitList = Config.GetProhibitPortList();
            if (prohibitList == null || prohibitList.Count == 0) return false;

            var prohibitedNumbers = new HashSet<int>(prohibitList.Select(p => p.PortNo));
            foreach (string token in portsField.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out int port) && prohibitedNumbers.Contains(port))
                {
                    return true;
                }
            }
            return false;
        }

        private void RaiseResultsSummary()
        {
            int alive = 0, dead = 0;
            foreach (var info in _items)
            {
                if (info.Alive) alive++; else dead++;
            }
            ResultsSummaryChanged?.Invoke(alive, dead, _items.Count);
        }
    }
}
