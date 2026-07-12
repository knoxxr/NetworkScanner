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
            DgIPList.ItemsSource = _items;
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
            _engine.ItemsRefreshNeeded += () => Dispatcher.UIThread.Post(RefreshGrid);
            _engine.ScanStarted += () => Dispatcher.UIThread.Post(() => SetScanningState(true));
            _engine.ScanFinished += () => Dispatcher.UIThread.Post(() =>
            {
                SetScanningState(false);
                TbProgressPercent.Text = "";
            });

            _engine.InitFromConfig();
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

        private IEnumerable<IPInfo> FilterItems()
        {
            string keyword = TbSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(keyword)) return _items;
            return _items.Where(i => Matches(i, keyword)).ToList();
        }

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
