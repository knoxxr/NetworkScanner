using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkScanner
{
    /// <summary>
    /// MainNetworkScanner.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class MainNetworkScanner : Window, IScanConfigProvider
    {
        public static string ProgramName = "NetworkScanner";

        UCSetting ucSetting = new UCSetting();
        UCIPList ucIPList = new UCIPList();
        UCRefPortList ucReservedPortInfo = new UCRefPortList();
        UCUserGuide ucUserGuide = new UCUserGuide();

        DispatcherTimer _Timer = new DispatcherTimer();

        public MainNetworkScanner()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EventLogger.WriteEventLogEntry("프로그램 시작", EventLogEntryType.Information);
            InitializeApp();
        }

        private void InitializeApp()
        {
            ucIPList.HorizontalAlignment = HorizontalAlignment.Stretch;
            ucIPList.VerticalAlignment = VerticalAlignment.Stretch;
            ucSetting.HorizontalAlignment = HorizontalAlignment.Stretch;
            ucSetting.VerticalAlignment = VerticalAlignment.Stretch;
            ucReservedPortInfo.HorizontalAlignment = HorizontalAlignment.Stretch;
            ucReservedPortInfo.VerticalAlignment = VerticalAlignment.Stretch;

            ucSetting.Initialized += UcSetting_Initialized;
            ucSetting.Loaded += UcSetting_Loaded;
            ucIPList.Initialized += UcIPList_Initialized;
            ucIPList.Loaded += UcIPList_Loaded;

            BdContent.Child = ucIPList;
            SetActiveNav(BtnIPList);

            // 저장해 둔 컬럼 너비 복원(종료 시 Window_Closing에서 저장).
            AppSettingsData layout = AppSettingsStore.LoadSettings();
            ucIPList.ApplyColumnWidths(layout.IpListColumnWidths);
            ucSetting.ApplyColumnWidths(layout.IpRangeColumnWidths);

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            tbVersion.Text = "ver. " + (version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "unknown");

            _Timer.Interval = TimeSpan.FromMilliseconds(1000*60);
            _Timer.Tick += _Timer_Tick;
            _Timer.Start();
        }

        private void UcIPList_Loaded(object sender, RoutedEventArgs e)
        {
            if (GetLoadLastestFile() == true)
                ucIPList.GetLastestFilePath(GetSystemName());
        }

        private void UcSetting_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UcSetting_Initialized(object? sender, EventArgs e)
        {
            
        }

        private void UcIPList_Initialized(object? sender, EventArgs e)
        {
        }

        private DateTime _lastMonitorScan = DateTime.MinValue;

        private void _Timer_Tick(object? sender, EventArgs e)
        {
            // 연속 모니터링: 지정한 분 간격마다 자동 재스캔한다(스케줄링과 독립적으로 동작).
            if (ucSetting.GetContinuousMonitoring())
            {
                int interval = ucSetting.GetMonitorIntervalMinutes();
                if ((DateTime.Now - _lastMonitorScan).TotalMinutes >= interval && ucIPList.IsScanning() == false)
                {
                    _lastMonitorScan = DateTime.Now;
                    ucIPList.StartScan();
                }
            }

            bool? useSchd = false;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                useSchd = ucSetting.ChkScheduling.IsChecked;
            }));

            if (useSchd != true) return;

            int curHour = int.Parse(DateTime.Now.ToString("HH"));
            int curmin = DateTime.Now.Minute;
            if (curmin != 0) return;

            bool? onTime = false;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                onTime = ucSetting.IsInScheduleHour(curHour);
            }));

            if (ucIPList.IsScanning() == true) return;

            if (onTime == true)
            {
                ucIPList.SchedulingScan();
            }
        }

        public bool? GetUseFTP()
        {
            return ucSetting.ChkUseFTP.IsChecked;
        }

        public string GetPortList()
        {
            return ucSetting.tbPortsList.Text;
        }

        public IPAddress GetFTPIP()
        {
            if (ucSetting.TbFTPIP.Text == "") return    null;
            return IPAddress.Parse(ucSetting.TbFTPIP.Text);
        }

        public string GetFTPID()
        {
            return ucSetting.TbFTPID.Text;
        }

        public string GetFTPPW()
        {
            return ucSetting.TbFTPPW.Password;
        }

        public int GetFTPPort()
        {
            if (ucSetting.TbFTPPort.Text == "") return 0;
            return Int32.Parse(ucSetting.TbFTPPort.Text);
        }

        public string GetSystemName()
        {
            return ucSetting.GetSystemName();
        }

        public bool? GetUsePortChecking()
        {
            return ucSetting.ChkCheckPort.IsChecked;
        }

        public bool? GetLoadLastestFile()
        {
            return ucSetting.chkLoadLastestFileWhenStartup.IsChecked;
        }

        public List<RefPortInfo> GetReservedPortList()
        {
            List<RefPortInfo> result = new List<RefPortInfo>();

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                result = ucReservedPortInfo.ReservedPortList;
            }
            ));

            return result;
        }

        public List<RefPortInfo> GetProhibitPortList()
        {
            List<RefPortInfo> result = new List<RefPortInfo>();

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                result = ucReservedPortInfo.ProhibitPortList;
            }
            ));

            return result;
        }

        public ScanRangeList GetScanRanges()
        {
            return ucSetting.ScanRanges;
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            string path = ScanEngine.GetEnvDirectory();
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di.Create();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                ucIPList.LoadIPInfo(openFileDialog.FileName);
            }
            BdContent.Child = ucIPList;
        }

        private void BtnNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("리스트를 모두 삭제할까요?", "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ucIPList.ClearItems();
                BdContent.Child = ucIPList;
            }
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucSetting;
            SetActiveNav(BtnSetting);
        }

        private void BtnPortInfo_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucReservedPortInfo;
            SetActiveNav(BtnInfo);
        }

        private void BtnGuide_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucUserGuide;
            SetActiveNav(BtnGuide);
        }

        private void BtnIPList_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucIPList;
            SetActiveNav(BtnIPList);
        }

        // 사이드바에서 지금 어느 화면을 보고 있는지 좌측 강조선 + 배경 틴트로 표시한다.
        private void SetActiveNav(Button active)
        {
            var accent = (Brush)Resources["AccentBrush"];
            var activeBg = (Brush)Resources["SideActiveBackgroundBrush"];
            var inactiveBg = (Brush)Resources["SideBackgroundBrush"];

            foreach (Button nav in new[] { BtnIPList, BtnSetting, BtnInfo, BtnGuide })
            {
                bool isActive = ReferenceEquals(nav, active);
                nav.Background = isActive ? activeBg : inactiveBg;
                nav.BorderBrush = isActive ? accent : Brushes.Transparent;
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            //ucIPList.SchedulingScan();
#endif
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppSettingsStore.SaveColumnLayout(ucIPList.GetColumnWidths(), ucSetting.GetColumnWidths());
            EventLogger.WriteEventLogEntry("프로그램 종료", EventLogEntryType.Information);
        }
    }
}
