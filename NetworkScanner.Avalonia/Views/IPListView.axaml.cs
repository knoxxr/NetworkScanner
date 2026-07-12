using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NetworkScanner.Avalonia.Helpers;

namespace NetworkScanner.Avalonia.Views
{
    /// <summary>
    /// 실제 스캔/저장/불러오기 로직은 NetworkScanner.Core의 ScanEngine에 있다.
    /// 이 클래스는 Avalonia 컨트롤과 ScanEngine 이벤트를 연결하는 얇은 어댑터 역할만 한다.
    /// </summary>
    public partial class IPListView : UserControl
    {
        private readonly IPInfoList _items = new IPInfoList();
        private readonly OUIInfo _oui = new OUIInfo();
        private ScanEngine? _engine;

        public IScanConfigProvider? Config { get; set; }

        public IPListView()
        {
            InitializeComponent();
            _oui.LoadInfo();
            // DataGrid는 절대 라이브 컬렉션(_items)에 직접 바인딩하지 않는다. 병렬 스캔이 백그라운드
            // 스레드에서 _items를 수정하면 Avalonia가 UI 스레드가 아닌 곳에서 CollectionChanged를
            // 처리하려다 교착/예외에 빠지기 때문이다. 항상 락으로 뜬 스냅샷(복사본)만 바인딩한다.
            RefreshGrid();
        }

        public void Initialize(IScanConfigProvider config)
        {
            Config = config;
            _engine = new ScanEngine(_items, _oui, Config);
            _engine.Message += msg => Dispatcher.UIThread.Post(() => TbMsg.Text = msg);
            _engine.ProgressMaxChanged += max => Dispatcher.UIThread.Post(() =>
            {
                PbProgress.Maximum = max;
                PbProgress.Value = 0;
                UpdateProgressPercentText(0, max);
            });
            _engine.ProgressChanged += val => Dispatcher.UIThread.Post(() =>
            {
                PbProgress.Value = val;
                UpdateProgressPercentText(val, (int)PbProgress.Maximum);
            });
            _engine.ResultsSummaryChanged += (alive, dead, total) => Dispatcher.UIThread.Post(() =>
                TbResult.Text = $"정상:{alive},끊김{dead}/전체{total}");
            _engine.ItemsRefreshNeeded += RequestGridRefresh;
            _engine.ScanStarted += () => Dispatcher.UIThread.Post(() => SetScanningState(true));
            _engine.ScanFinished += () => Dispatcher.UIThread.Post(() =>
            {
                SetScanningState(false);
                RefreshGrid();
                TbProgressPercent.Text = "";
            });
            _engine.ScanChangesDetected += changes => Dispatcher.UIThread.Post(() => ReportChanges(changes));

            _engine.InitFromConfig();
        }

        // 직전 스캔 대비 변화를 로그에 남기고, 보안상 중요한 변화(MAC 변경/위험 포트)는 팝업으로 경고한다.
        private async void ReportChanges(System.Collections.Generic.IReadOnlyList<ScanChange> changes)
        {
            var security = new System.Collections.Generic.List<string>();
            foreach (ScanChange c in changes)
            {
                string line = ScanDiff.Describe(c);
                if (ScanDiff.IsSecurityRelevant(c.Type)) { AppLogger.LogError("NetworkScanner", "스캔 변화: " + line); security.Add(line); }
                else AppLogger.LogInfo("NetworkScanner", "스캔 변화: " + line);
            }

            TbMsg.Text = $"변화 {changes.Count}건 감지" + (security.Count > 0 ? $" (보안 경고 {security.Count}건)" : "");

            if (security.Count > 0 && TopLevel.GetTopLevel(this) is Window owner)
            {
                await SimpleDialogs.ShowMessageAsync(owner, string.Join("\n", security), "보안 경고 - 스캔 변화 감지");
            }
        }

        // 병렬 스캔은 초당 수백 번 갱신을 요청할 수 있으므로, 이미 예약된 갱신이 있으면 무시해 병합한다.
        private volatile bool _refreshPending;
        private void RequestGridRefresh()
        {
            if (_refreshPending) return;
            _refreshPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _refreshPending = false;
                RefreshGrid();
            }, DispatcherPriority.Background);
        }

        // 스캔 진행 중에는 "스캔" 버튼을 비활성화하고 "취소" 버튼만 눌리도록 해 중복 스캔 시작을 막는다.
        private void SetScanningState(bool scanning)
        {
            BtnRefresh.IsEnabled = !scanning;
            BtnStop.IsEnabled = scanning;
            TbScanLabel.Text = scanning ? "스캔 중..." : "스캔";
        }

        // 진행률 바 위에 겹쳐 보여줄 "N% (진행/전체)" 텍스트를 계산한다. 전체 개수가 0이면(스캔 시작 전) 비워둔다.
        private void UpdateProgressPercentText(int value, int max)
        {
            TbProgressPercent.Text = max > 0 ? $"{value * 100 / max}% ({value}/{max})" : "";
        }

        private void RefreshGrid()
        {
            DgIPList.ItemsSource = null;
            DgIPList.ItemsSource = FilterItems();
        }

        // 병렬 스캔 중 워커 스레드가 _items를 수정할 수 있으므로, 엔진의 쓰기 락을 잡고 스냅샷(복사본)을
        // 만들어 반환한다. DataGrid는 이 고정된 복사본만 열람하므로 열람 중 컬렉션 변경 예외가 나지 않는다.
        private List<IPInfo> FilterItems()
        {
            object sync = _engine?.ItemsSyncRoot ?? _items;
            string keyword = TbSearch.Text?.Trim() ?? "";
            bool aliveOnly = ChkAliveOnly.IsChecked == true;
            lock (sync)
            {
                return _items.Where(i =>
                        (!aliveOnly || i.Alive)
                        && (string.IsNullOrEmpty(keyword) || Matches(i, keyword)))
                    .ToList();
            }
        }

        private void ChkAliveOnly_Changed(object? sender, RoutedEventArgs e) => RefreshGrid();

        private static bool Matches(IPInfo info, string keyword)
        {
            return (info.Ip?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.SystemName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Macaddr?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Vendor?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Ports?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private void TbSearch_TextChanged(object? sender, TextChangedEventArgs e) => RefreshGrid();

        public void ClearItems()
        {
            _items.Clear();
            RefreshGrid();
        }

        public void LoadIPInfo(string filename)
        {
            _engine?.LoadIPInfo(filename);
            RefreshGrid();
        }

        public void SchedulingScan() => _engine?.StartSchedulingScan(Config?.GetSystemName() ?? "");

        // 연속 모니터링용: 자동저장 없이 전체 대역을 재스캔한다(변화 감지 알림은 그대로 동작).
        public void StartScan() => _engine?.StartRefreshAllRange(Config?.GetSystemName() ?? "");

        public void ScanningStop() => _engine?.ScanningStop();

        public bool IsScanning() => _engine?.IsScanning() ?? false;

        public void GetLatestFilePath(string prefixName) => _engine?.GetLatestFilePath(prefixName);

        private void BtnRefresh_Click(object? sender, RoutedEventArgs e)
        {
            _engine?.StartRefreshAllRange(Config?.GetSystemName() ?? "");
        }

        private void BtnStop_Click(object? sender, RoutedEventArgs e)
        {
            _engine?.ScanningStop();
        }

        private async void BtnNewFile_Click(object? sender, RoutedEventArgs e)
        {
            var owner = (Window)TopLevel.GetTopLevel(this)!;
            if (await SimpleDialogs.ShowConfirmAsync(owner, "리스트를 모두 삭제할까요?", "삭제"))
            {
                ClearItems();
            }
        }

        private async void BtnLoadFile_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            string envDir = ScanEngine.GetEnvDirectory();
            Directory.CreateDirectory(envDir);

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "IP 목록 CSV 열기",
                AllowMultiple = false,
                SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(envDir),
                FileTypeFilter = new[] { new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } } },
            });

            var file = files.FirstOrDefault();
            if (file != null)
            {
                LoadIPInfo(file.Path.LocalPath);
            }
        }

        private async void BtnSaveFile_Click(object? sender, RoutedEventArgs e)
        {
            if (_engine == null) return;
            await _engine.WriteIPInfo(false, Config?.GetSystemName() ?? "");
        }

        private async void BtnReport_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null || _engine == null) return;

            List<IPInfo> snapshot;
            lock (_engine.ItemsSyncRoot) snapshot = _items.ToList();
            if (snapshot.Count == 0) { TbMsg.Text = "리포트로 저장할 결과가 없습니다."; return; }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "리포트 저장 (HTML/JSON)",
                SuggestedFileName = $"NetworkScanner_{System.DateTime.Now:yyyyMMdd_HHmmss}.html",
                DefaultExtension = "html",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("HTML") { Patterns = new[] { "*.html" } },
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                },
            });
            if (file == null) return;

            string ts = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            bool json = file.Name.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);
            string content = json
                ? ReportGenerator.BuildJson(snapshot, Config?.GetSystemName() ?? "", ts)
                : ReportGenerator.BuildHtml(snapshot, Config?.GetSystemName() ?? "", ts);

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
            TbMsg.Text = json ? "JSON 리포트를 저장했습니다." : "HTML 리포트를 저장했습니다.";
        }

        private async void MenuItemWakeOnLan_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;
            if (string.IsNullOrWhiteSpace(SelectedItem.Macaddr))
            {
                TbMsg.Text = "MAC 주소가 없어 Wake-on-LAN을 보낼 수 없습니다.";
                return;
            }

            bool ok = WakeOnLan.Send(SelectedItem.Macaddr);
            TbMsg.Text = ok
                ? $"Wake-on-LAN 매직 패킷을 보냈습니다: {SelectedItem.Macaddr}"
                : "Wake-on-LAN 전송에 실패했습니다.";

            if (!ok && TopLevel.GetTopLevel(this) is Window owner)
                await SimpleDialogs.ShowMessageAsync(owner, "Wake-on-LAN 전송에 실패했습니다. MAC 주소를 확인하세요.");
        }

        private IPInfo? SelectedItem => DgIPList.SelectedItem as IPInfo;

        private void MenuItemPing_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null || _engine == null) return;
            _engine.PingOnce(SelectedItem.Ip);
        }

        private void MenuItemCheckPort_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;
            _engine?.StartCheckUserPortList(SelectedItem.Ip);
        }

        private void MenuItemCheckReservedPort_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;
            _engine?.StartCheckReservedPortList(SelectedItem.Ip);
        }

        private void MenuItemCheckProhibitPort_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;
            _engine?.StartCheckProhibitPortList(SelectedItem.Ip);
        }

        private void MenuItemRemove_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem == null) return;
            _items.DelItem(SelectedItem.Ip);
            RefreshGrid();
        }
    }
}
