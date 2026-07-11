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
            });
            _engine.ProgressChanged += val => Dispatcher.UIThread.Post(() => PbProgress.Value = val);
            _engine.ResultsSummaryChanged += (alive, dead, total) => Dispatcher.UIThread.Post(() =>
                TbResult.Text = $"정상:{alive},끊김{dead}/전체{total}");
            _engine.ItemsRefreshNeeded += () => Dispatcher.UIThread.Post(RefreshGrid);

            _engine.InitFromConfig();
        }

        private void RefreshGrid()
        {
            DgIPList.ItemsSource = null;
            DgIPList.ItemsSource = _items;
        }

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
