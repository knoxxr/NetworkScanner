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
        // 대역 스캔을 여러 IP 동시에 수행하므로, 결과 목록(_items)에 대한 읽기/추가/수정은 이 락으로 직렬화한다.
        private readonly object _itemsLock = new object();
        private CancellationTokenSource? _scanCts;

        // 사용자가 지정한 이름/비고(IP 기준). 스캔 시작 시 파일에서 읽어와 새 호스트에 자동 병합한다.
        private Dictionary<string, AnnotationStore.Annotation> _annotations = new();

        // 대역 스캔 시 동시에 검사할 IP 개수. 대부분의 시간이 Ping 응답 대기(I/O)라 CPU 코어 수보다 크게 잡는다.
        private const int ScanParallelism = 64;

        // ICMP에 응답하지 않는 호스트의 생존 여부를 재확인할 때 두드려 볼 대표 TCP 포트.
        private static readonly int[] LivenessProbePorts = { 80, 443, 22, 445, 3389, 8080 };

        public IScanConfigProvider Config { get; set; }
        public Task? Scanning { get; private set; }
        public IPInfoList Items => _items;

        // 병렬 스캔 중 결과 목록(_items)을 UI가 안전하게 열람할 수 있도록, 엔진이 쓰기에 사용하는 락 객체를 노출한다.
        // WPF는 BindingOperations.EnableCollectionSynchronization에, Avalonia는 스냅샷 생성 시 lock에 사용한다.
        public object ItemsSyncRoot => _itemsLock;

        public event Action<string>? Message;
        public event Action<int>? ProgressMaxChanged;
        public event Action<int>? ProgressChanged;
        public event Action<int, int, int>? ResultsSummaryChanged; // alive, dead, total
        public event Action? ItemsRefreshNeeded;
        public event Action? ScanStarted;
        public event Action? ScanFinished;
        // 직전 스캔 대비 변화(신규/오프라인/ MAC 변경/새 포트/위험 포트)를 스캔 완료 시 한 번에 알린다.
        public event Action<IReadOnlyList<ScanChange>>? ScanChangesDetected;

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

                // 사용자가 지정한 이름/비고를 IP 기준으로 보관해 다음 스캔/재시작 후에도 유지되게 한다.
                AnnotationStore.Save(_items);

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
                    Message?.Invoke(Localization.T("scan.alreadyscanning"));
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
            Message?.Invoke(Localization.T("scan.cancelrequested"));
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
            RefreshIPInfo(reply, ip, openPorts, GetProhibitedPortSet());

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

        // 스캔을 2단계로 나눈다.
        //  1단계: 대역 전체를 병렬 Ping으로 훑어 살아있는 호스트를 빠르게(수 초) 찾아 목록에 표시한다.
        //  2단계: 살아있는 호스트에 대해서만 느린 부가 정보(MAC/제조사/호스트명/열린 포트)를 백그라운드로
        //         조회하며 각 행을 점진적으로 채운다.
        // 이렇게 하면 대부분의 대기 시간이 걸리는 부가 조회를 소수의 살아있는 호스트로 한정하고, 사용자는
        // 1단계 결과를 곧바로 보게 되어 전체를 기다리지 않아도 된다.
        public async Task DoScanAllRange(bool scheduling, string systemName, CancellationToken token)
        {
            ScanRangeList ranges = Config.GetScanRanges();
            List<string> targets = BuildTargetIPList(ranges);
            int maxCnt = targets.Count;
            ProgressMaxChanged?.Invoke(maxCnt);

            if (maxCnt == 0)
            {
                Message?.Invoke(Localization.T("scan.norange"));
                return;
            }

            // 스캔 중에는 UI(설정)를 건드리지 않으므로 위험포트/포트검사 설정을 루프 밖에서 한 번만 읽는다.
            bool usePortChecking = Config.GetUsePortChecking() == true;
            HashSet<int> prohibitedPorts = GetProhibitedPortSet();
            var options = new ParallelOptions { MaxDegreeOfParallelism = ScanParallelism, CancellationToken = token };

            // 사용자가 지정해 둔 이름/비고를 읽어와, 스캔 중 새로 발견되는 호스트에 자동 병합한다.
            _annotations = AnnotationStore.Load();

            // 변화 감지를 위해 스캔 시작 시점의 상태를 기준선으로 저장해 둔다.
            Dictionary<string, HostState> baseline = SnapshotStates();

            // ---- 1단계: 빠른 Ping 스윕(+ ICMP 무응답 시 TCP로 생존 재확인) ----
            Message?.Invoke($"{Localization.T("scan.phase1.start")} ({maxCnt}). {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            var aliveIps = new System.Collections.Concurrent.ConcurrentBag<string>();
            int pinged = 0;
            try
            {
                await Parallel.ForEachAsync(targets, options, async (strIp, ct) =>
                {
                    PingReply? reply = await PingTester.SendPingAsync(IPAddress.Parse(strIp));
                    bool alive = reply != null && reply.Status == IPStatus.Success;
                    long? roundtrip = alive ? reply!.RoundtripTime : null;
                    int ttl = alive && reply!.Options != null ? reply.Options.Ttl : 0;

                    // ICMP에 응답이 없으면 대표 포트로 TCP 생존 확인을 한 번 더 시도한다.
                    if (!alive)
                    {
                        alive = await PingTester.IsAliveByTcpAsync(LivenessProbePorts, strIp);
                    }

                    if (alive) aliveIps.Add(strIp);
                    ApplyPingResult(alive, roundtrip, ttl, strIp);

                    int cur = Interlocked.Increment(ref pinged);
                    ProgressChanged?.Invoke(cur);
                    Message?.Invoke($"{Localization.T("scan.phase1.progress")}: {strIp} ({cur}/{maxCnt})");
                    RaiseResultsSummary();
                });
            }
            catch (OperationCanceledException) { }

            // ---- 2단계: 살아있는 호스트 부가 정보 조회 ----
            List<string> aliveList = aliveIps.ToList();
            if (aliveList.Count > 0 && !token.IsCancellationRequested)
            {
                ProgressMaxChanged?.Invoke(aliveList.Count);
                Message?.Invoke($"{Localization.T("scan.phase2.start")} ({aliveList.Count})");
                int enriched = 0;
                try
                {
                    await Parallel.ForEachAsync(aliveList, options, async (strIp, ct) =>
                    {
                        await EnrichHostAsync(strIp, usePortChecking, prohibitedPorts);

                        int cur = Interlocked.Increment(ref enriched);
                        ProgressChanged?.Invoke(cur);
                        Message?.Invoke($"{Localization.T("scan.phase2.progress")}: {strIp} ({cur}/{aliveList.Count})");
                    });
                }
                catch (OperationCanceledException) { }
            }

            // ---- 변화 감지: 기준선과 이번 결과를 비교해 신규/오프라인/MAC 변경/새 포트/위험 포트를 알린다. ----
            if (!token.IsCancellationRequested)
            {
                List<ScanChange> changes = ScanDiff.ComputeChanges(baseline, SnapshotStates());
                if (changes.Count > 0) ScanChangesDetected?.Invoke(changes);
            }

            ProgressChanged?.Invoke(0);
            Message?.Invoke(token.IsCancellationRequested
                ? $"{Localization.T("scan.cancelled")} {DateTime.Now:yyyy/MM/dd HH:mm:ss}"
                : $"{Localization.T("scan.completed")} {Localization.T("scan.alivecount")}: {aliveList.Count}. {DateTime.Now:yyyy/MM/dd HH:mm:ss}");

            if (scheduling && !token.IsCancellationRequested)
            {
                await WriteIPInfo(true, systemName);
                await WriteAndUploadReport(systemName); // 무인 스케줄 스캔은 HTML 리포트도 함께 남기고, FTP 사용 시 업로드한다.
            }
        }

        // HTML 리포트를 env 폴더에 저장하고, FTP 사용 설정이면 함께 업로드한다.
        private async Task WriteAndUploadReport(string systemName)
        {
            try
            {
                List<IPInfo> snapshot;
                lock (_itemsLock) snapshot = new List<IPInfo>(_items);
                if (snapshot.Count == 0) return;

                string html = ReportGenerator.BuildHtml(snapshot, systemName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string path = GetEnvDirectory();
                Directory.CreateDirectory(path);
                string filename = $"{SanitizeFileNameComponent(systemName)}{DateTime.Now:_yyyyMMdd_HHmmss}_report.html";
                await File.WriteAllTextAsync(Path.Combine(path, filename), html, Encoding.UTF8);

                if (Config.GetUseFTP() == true)
                {
                    _ftp.UploadFileList(path + Path.DirectorySeparatorChar, filename);
                }
                Message?.Invoke($"리포트를 저장했습니다. File Name : {filename}");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("NetworkScanner", "자동 리포트 저장 실패: " + ex.Message);
            }
        }

        // 결과 목록의 현재 상태를 IP→상태 딕셔너리로 스냅샷한다(변화 감지 기준선/비교 대상으로 사용).
        private Dictionary<string, HostState> SnapshotStates()
        {
            lock (_itemsLock)
            {
                var map = new Dictionary<string, HostState>(_items.Count);
                foreach (IPInfo i in _items)
                {
                    map[i.Ip] = new HostState(i.Alive, i.Macaddr ?? "", i.Ports ?? "", i.HasProhibitedPort);
                }
                return map;
            }
        }

        // 스캔 대상 IP들을 미리 문자열 목록으로 펼친다. 잘못된 대역은 건너뛰며 안내 메시지를 남긴다.
        private List<string> BuildTargetIPList(ScanRangeList ranges)
        {
            var targets = new List<string>();
            foreach (ScanRangeInfo item in ranges)
            {
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
                    targets.Add(IPRangeUtil.FromUInt32(ipVal).ToString());
                }
            }
            return targets;
        }

        private HashSet<int> GetProhibitedPortSet()
        {
            List<RefPortInfo> prohibitList = Config.GetProhibitPortList();
            if (prohibitList == null) return new HashSet<int>();
            return new HashSet<int>(prohibitList.Select(p => p.PortNo));
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

        // 1단계 전용: 생존 여부/응답시간/TTL만 반영한다. 느린 부가 조회는 하지 않아 대역을 빠르게 훑는다.
        // roundtripMs가 null이면서 alive면 ICMP는 무응답이지만 TCP로 생존이 확인된 경우로, 응답시간을 "TCP"로 표기한다.
        private void ApplyPingResult(bool alive, long? roundtripMs, int ttl, string targetIp)
        {
            string roundTime = alive ? (roundtripMs.HasValue ? roundtripMs.Value.ToString() : "TCP") : "Timeout";
            bool changed = false;

            lock (_itemsLock)
            {
                IPInfo info = _items.GetItem(targetIp);
                if (info != null)
                {
                    info.RountTime = roundTime;
                    info.Alive = alive;
                    if (ttl > 0) info.Ttl = ttl;
                    changed = true;
                }
                else if (alive)
                {
                    IPInfo added = new()
                    {
                        Ip = targetIp,
                        Ports = "",
                        Description = "",
                        CommitDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        RountTime = roundTime,
                        Alive = true,
                        Macaddr = "",
                        Vendor = "",
                        SystemName = "",
                        Ttl = ttl,
                    };
                    // 사용자가 이전에 지정한 이름/비고가 있으면 새 호스트에 자동으로 채운다.
                    if (_annotations.TryGetValue(targetIp, out var note))
                    {
                        added.SystemName = note.Name;
                        added.Description = note.Description;
                    }
                    _items.Add(added);
                    changed = true;
                }
            }

            if (changed) ItemsRefreshNeeded?.Invoke();
        }

        // 2단계 전용: 살아있는 호스트 한 대의 MAC/제조사/호스트명/열린 포트를 조회해 해당 행을 채운다.
        // ARP·DNS·포트 검사는 모두 결과 목록 락 밖에서 수행하고, 마지막에 짧게 락을 잡아 값만 반영한다.
        // 서비스/배너를 수집할 만한 대표 포트(HTTP/SSH/FTP/SMTP 등). 열려 있으면 첫 번째 것의 배너를 읽는다.
        private static readonly int[] BannerPorts = { 80, 8080, 443, 22, 21, 25, 23, 3306 };

        private async Task EnrichHostAsync(string targetIp, bool usePortChecking, HashSet<int> prohibitedPorts)
        {
            string? mac = _items.GetMACAddress(targetIp);
            string vendor = string.IsNullOrEmpty(mac) ? "" : _oui.GetVender(mac);
            string? hostName = await LookupHostNameAsync(targetIp, 1500);
            string openPorts = usePortChecking ? await PingTester.CheckPortsOpenAsync(targetIp) : "";
            bool hasProhibited = ContainsProhibitedPort(openPorts, prohibitedPorts);

            // 열린 포트 중 배너를 읽을 만한 첫 포트에서 서비스 정보를 한 줄 수집한다.
            string service = usePortChecking ? await GrabServiceAsync(targetIp, openPorts) : "";

            bool changed = false;

            lock (_itemsLock)
            {
                IPInfo info = _items.GetItem(targetIp);
                if (info != null)
                {
                    if (!string.IsNullOrEmpty(mac)) info.Macaddr = mac;
                    if (!string.IsNullOrEmpty(vendor)) info.Vendor = vendor;
                    if (string.IsNullOrEmpty(info.SystemName) && !string.IsNullOrEmpty(hostName) && hostName != targetIp)
                        info.SystemName = hostName;
                    if (usePortChecking)
                    {
                        info.Ports = openPorts;
                        info.HasProhibitedPort = hasProhibited;
                        if (!string.IsNullOrEmpty(service)) info.Service = service;
                    }
                    changed = true;
                }
            }

            if (changed) ItemsRefreshNeeded?.Invoke();
        }

        private static async Task<string> GrabServiceAsync(string ip, string openPorts)
        {
            HashSet<int> open = new();
            foreach (string token in (openPorts ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(token, out int p)) open.Add(p);

            foreach (int port in BannerPorts)
            {
                if (!open.Contains(port)) continue;
                string banner = await PingTester.GrabBannerAsync(ip, port);
                if (!string.IsNullOrEmpty(banner)) return banner;
            }
            return "";
        }

        // 역방향 DNS 조회. PTR 레코드가 없는 LAN 호스트에서 오래 걸릴 수 있으므로 타임아웃을 강제한다.
        private static async Task<string?> LookupHostNameAsync(string ip, int timeoutMs)
        {
            try
            {
                IPHostEntry entry = await Dns.GetHostEntryAsync(ip).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
                return entry.HostName;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshIPInfo(PingReply? reply, string targetIp, string openPorts, HashSet<int> prohibitedPorts)
        {
            bool success = reply != null && reply.Status == IPStatus.Success;

            // MAC/제조사/호스트명 조회는 ARP·DNS라 느릴 수 있으므로 결과 목록 락 밖에서 미리 계산한다.
            // 살아있는 호스트에 대해서만 조회해(응답 없는 호스트는 어차피 목록에 추가되지 않음) 속도를 높인다.
            string? mac = null, vendor = null, hostName = null;
            if (success)
            {
                mac = _items.GetMACAddress(targetIp);
                vendor = _oui.GetVender(mac);
                hostName = _items.GetHostName(IPAddress.Parse(targetIp));
            }
            bool hasProhibited = ContainsProhibitedPort(openPorts, prohibitedPorts);
            bool changed = false;

            lock (_itemsLock)
            {
                IPInfo info = _items.GetItem(targetIp);
                if (info != null)
                {
                    info.Ports = openPorts;
                    info.RountTime = success ? reply!.RoundtripTime.ToString() : "Timeout";
                    info.Alive = success;
                    info.HasProhibitedPort = hasProhibited;
                    if (success)
                    {
                        info.Macaddr = mac;
                        info.Vendor = vendor;
                        if (string.IsNullOrEmpty(info.SystemName))
                            info.SystemName = hostName;
                    }
                    changed = true;
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
                        HasProhibitedPort = hasProhibited,
                        Macaddr = mac,
                        Vendor = vendor,
                        SystemName = hostName,
                    };
                    _items.Add(newIpInfo);
                    changed = true;
                }
            }

            // UI 갱신 알림은 반드시 락 밖에서 낸다. 락 안에서 동기 Dispatcher 호출을 하면
            // UI 스레드가 같은 락(EnableCollectionSynchronization)을 기다리며 교착에 빠질 수 있다.
            if (changed) ItemsRefreshNeeded?.Invoke();
        }

        // 열린 포트 중 위험(백도어) 포트 목록과 일치하는 것이 있는지 확인한다.
        private static bool ContainsProhibitedPort(string portsField, HashSet<int> prohibitedPorts)
        {
            if (string.IsNullOrEmpty(portsField) || prohibitedPorts.Count == 0) return false;

            foreach (string token in portsField.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out int port) && prohibitedPorts.Contains(port))
                {
                    return true;
                }
            }
            return false;
        }

        private void RaiseResultsSummary()
        {
            int alive = 0, dead = 0, total;
            lock (_itemsLock)
            {
                foreach (var info in _items)
                {
                    if (info.Alive) alive++; else dead++;
                }
                total = _items.Count;
            }
            ResultsSummaryChanged?.Invoke(alive, dead, total);
        }
    }
}
