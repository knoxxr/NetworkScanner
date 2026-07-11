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
            _engine.Message += msg => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => TbMsg.Text = msg));
            _engine.ProgressMaxChanged += max => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                pbProgress.Maximum = max;
                pbProgress.Value = 0;
            }));
            _engine.ProgressChanged += val => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => pbProgress.Value = val));
            _engine.ResultsSummaryChanged += (alive, dead, total) => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                tbResult.Text = string.Format("정상:{0},끊김{1}/전체{2}", alive, dead, total)));
            _engine.ItemsRefreshNeeded += () => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LvIPList.Items.Refresh()));

            _engine.InitFromConfig();
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
    public class AliveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool alive = (bool)value;
            if (alive)
            {
                return new SolidColorBrush(Colors.Blue); ;
            }
            else
            {
                return new SolidColorBrush(Colors.Red);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
