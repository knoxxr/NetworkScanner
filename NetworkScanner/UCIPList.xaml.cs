using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetworkScanner
{
    /// <summary>
    /// UCIPList.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 실제 스캔/저장/불러오기 로직은 NetworkScanner.Core의 ScanEngine에 있다.
    /// 이 클래스는 WPF 컨트롤과 ScanEngine 이벤트를 연결하는 얇은 어댑터 역할만 한다.
    public partial class UCIPList : UserControl
    {
        private IPInfoList _IPInfoList;
        private readonly OUIInfo _oui = new OUIInfo();
        private ScanEngine _engine;

        // 스캔에 필요한 설정(포트 목록, FTP 계정 등)을 제공하는 대상. 기본값은 현재 MainWindow이지만,
        // 인터페이스에만 의존하므로 다른 구현으로 교체하거나 테스트 시 목(mock)으로 대체할 수 있다.
        public IScanConfigProvider Config { get; set; }

        public UCIPList()
        {
            InitializeComponent();
            _oui.LoadInfo();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControl();
        }

        private void InitializeControl()
        {
            _IPInfoList = Resources["IPInfoList"] as IPInfoList;
            Config ??= Application.Current.MainWindow as IScanConfigProvider;

            _engine = new ScanEngine(_IPInfoList, _oui, Config);

            // 병렬 스캔이 백그라운드 스레드에서 _IPInfoList를 수정해도 WPF가 안전하게 열람하도록,
            // 엔진의 쓰기 락과 동일한 객체로 컬렉션 동기화를 등록한다.
            BindingOperations.EnableCollectionSynchronization(_IPInfoList, _engine.ItemsSyncRoot);

            // 아래 핸들러들은 64개 워커 스레드에서 매우 잦게 호출되므로, 동기 Invoke(=워커 정지) 대신
            // 비차단 BeginInvoke를 쓴다. UI 갱신(Items.Refresh)은 별도로 병합(throttle)한다.
            _engine.Message += msg => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => TbMsg.Text = msg));
            _engine.ProgressMaxChanged += max => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                pbProgress.Maximum = max;
                pbProgress.Value = 0;
                UpdateProgressPercentText(0, max);
            }));
            _engine.ProgressChanged += val => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                pbProgress.Value = val;
                UpdateProgressPercentText(val, (int)pbProgress.Maximum);
            }));
            _engine.ResultsSummaryChanged += (alive, dead, total) => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                tbResult.Text = string.Format("정상:{0},끊김{1}/전체{2}", alive, dead, total)));
            _engine.ItemsRefreshNeeded += RequestGridRefresh;
            _engine.ScanStarted += () => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => SetScanningState(true)));
            _engine.ScanFinished += () => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                SetScanningState(false);
                LvIPList.Items.Refresh();
                TbProgressPercent.Text = "";
            }));
            _engine.ScanChangesDetected += changes => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => ReportChanges(changes)));

            CollectionViewSource.GetDefaultView(_IPInfoList).Filter = FilterItem;

            _engine.InitFromConfig();
        }

        // 병렬 스캔은 초당 수백 번 갱신을 요청할 수 있으므로, 이미 예약된 갱신이 있으면 무시해
        // UI 스레드가 Items.Refresh로 넘치지 않도록 병합한다.
        private volatile bool _refreshPending;
        private void RequestGridRefresh()
        {
            if (_refreshPending) return;
            _refreshPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _refreshPending = false;
                LvIPList.Items.Refresh();
            }));
        }

        // 진행률 바 위에 겹쳐 보여줄 "N% (진행/전체)" 텍스트를 계산한다. 전체 개수가 0이면(스캔 시작 전) 비워둔다.
        private void UpdateProgressPercentText(int value, int max)
        {
            TbProgressPercent.Text = max > 0 ? $"{value * 100 / max}% ({value}/{max})" : "";
        }

        // 직전 스캔 대비 변화를 이벤트 로그에 남기고, 보안상 중요한 변화(MAC 변경/위험 포트)는 팝업으로 경고한다.
        private void ReportChanges(System.Collections.Generic.IReadOnlyList<ScanChange> changes)
        {
            var security = new System.Collections.Generic.List<string>();
            foreach (ScanChange c in changes)
            {
                string line = ScanDiff.Describe(c);
                EventLogger.WriteEventLogEntry("스캔 변화: " + line,
                    ScanDiff.IsSecurityRelevant(c.Type) ? System.Diagnostics.EventLogEntryType.Warning : System.Diagnostics.EventLogEntryType.Information);
                if (ScanDiff.IsSecurityRelevant(c.Type)) security.Add(line);
            }

            TbMsg.Text = $"변화 {changes.Count}건 감지" + (security.Count > 0 ? $" (보안 경고 {security.Count}건)" : "");

            if (security.Count > 0)
            {
                MessageBox.Show(string.Join("\n", security), "보안 경고 - 스캔 변화 감지",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 스캔 진행 중에는 "스캔" 버튼을 비활성화하고 "취소" 버튼만 눌리도록 해 중복 스캔 시작을 막는다.
        private void SetScanningState(bool scanning)
        {
            BtnRefresh.IsEnabled = !scanning;
            BtnStop.IsEnabled = scanning;
            TbScanLabel.Text = scanning ? "스캔 중..." : "스캔";
        }

        private bool FilterItem(object obj)
        {
            if (string.IsNullOrWhiteSpace(TbSearch.Text)) return true;
            if (obj is not IPInfo info) return true;

            string keyword = TbSearch.Text.Trim();
            return (info.Ip?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.SystemName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Macaddr?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Vendor?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Ports?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (info.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_IPInfoList).Refresh();
        }

        public void ClearItems()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => _IPInfoList.Clear()));
            RefreshItems();
        }

        public IPInfoList GetItems() => _IPInfoList;

        public void RefreshItems()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LvIPList.Items.Refresh()));
        }

        public void LoadIPInfo(string filename) => _engine.LoadIPInfo(filename);

        public void SchedulingScan() => _engine.StartSchedulingScan(Config.GetSystemName());

        public void ScanningStop() => _engine.ScanningStop();

        public bool IsScanning() => _engine.IsScanning();

        public void GetLastestFilePath(string prefixname) => _engine.GetLatestFilePath(prefixname);

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _engine.StartRefreshAllRange(Config.GetSystemName());
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _engine.ScanningStop();
        }

        private void BtnNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("리스트를 모두 삭제할까요?", "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ClearItems();
            }
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            string path = ScanEngine.GetEnvDirectory();
            System.IO.Directory.CreateDirectory(path);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                _engine.LoadIPInfo(openFileDialog.FileName);
            }
        }

        private async void BtnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            await _engine.WriteIPInfo(false, Config.GetSystemName());
        }

        private void LvIPList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void MenuItemPing_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            _engine.PingOnce(selValue.Ip);
        }

        private void MenuItemCheckPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            _engine.StartCheckUserPortList(selValue.Ip);
        }
        private void MenuItemCheckReservedPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            _engine.StartCheckReservedPortList(selValue.Ip);
        }

        private void MenuItemCheckProhibitPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            _engine.StartCheckProhibitPortList(selValue.Ip);
        }

        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => _IPInfoList.DelItem(selValue.Ip)));
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }
    }
    // IPInfo.StatusKey("good"/"warn"/"bad")를 상태 배지 색상으로 변환한다.
    public class StatusColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush Good = new((Color)ColorConverter.ConvertFromString("#FF34C777"));
        private static readonly SolidColorBrush Warn = new((Color)ColorConverter.ConvertFromString("#FFF3A53E"));
        private static readonly SolidColorBrush Bad = new((Color)ColorConverter.ConvertFromString("#FFEF5875"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) switch
            {
                "good" => Good,
                "warn" => Warn,
                _ => Bad,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
