using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Reflection;
using System.Security.Principal;

namespace NetworkScanner
{
    /// <summary>
    /// MainNetworkScanner.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class MainNetworkScanner : Window, IScanConfigProvider
    {
        public static string ProgramName = "NetworkScanner";
        public const string IPRangeFileName = "iprange.ini";
        ScanRangeList _ScanRangeList = new ScanRangeList();
        IPInfoList _IPInfoList = new IPInfoList();

        UCSetting ucSetting = new UCSetting();
        UCIPList ucIPList = new UCIPList();
        UCRefPortList ucReservedPortInfo = new UCRefPortList();

        DispatcherTimer _Timer = new DispatcherTimer();

        public MainNetworkScanner()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
 /*           if (!IsRunningAsAdministrator())
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase);
                {
                    var withBlock = processStartInfo;
                    withBlock.UseShellExecute = true;
                    withBlock.Verb = "runas";
                    Process.Start(processStartInfo);
                    Application.Exit();
                }
            }
            else
                this.Text += " " + "(Administrator)";*/

            EventLogger.WriteEventLogEntry("프로그램 시작", EventLogEntryType.Information);
            InitializeApp();
        }
        public bool IsRunningAsAdministrator()
        {
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void InitializeApp()
        {
            _IPInfoList = Resources["IPInfoList"] as IPInfoList;
            _ScanRangeList = Resources["ScanRangeList"] as ScanRangeList;

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

            tbVersion.Text = "ver. "+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

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

        private void _Timer_Tick(object? sender, EventArgs e)
        {
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

        private void _Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
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

            if(ucIPList.IsScanning() == true) return;

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

        /*private void LoadIPRange()
        {
            string filename = Directory.GetCurrentDirectory() + @"\" + IPRangeFileName;

            FileInfo fi = new FileInfo(filename);

            if (fi.Exists)
            {
                string[] lines = System.IO.File.ReadAllLines(filename);
                ParsingIPRange(lines);
            }
            else
            {
                using (File.Create(filename))
                {
                    DisplayMsg("iprange.ini 파일 생성");
                }
            }

        }*/

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
            return UCSetting._ScanRangeList;
        }

        private void ParsingIPRange(string[] raw)
        {
            _ScanRangeList.Clear();
            foreach (string line in raw)
            {
                string[] token = line.Split(",");

                if (token.Length > 0)
                {
                    ScanRangeInfo info = new ScanRangeInfo();
                    info.Index = int.Parse(token[0]);
                    info.StartIP = token[1];
                    info.EndIP = token[2];
                    info.Description = token[3];
                    _ScanRangeList.AddItem(info);
                }
            }
        }

        private void RefreshIPList()
        {
            ucIPList.RefreshItems();
        }

        private void ClearIPList()
        {
            ucIPList.ClearItems();
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            string path = Directory.GetCurrentDirectory() + "\\env";
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di.Create();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path + @"\env";
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
        }

        private void BtnPortInfo_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucReservedPortInfo;
        }

        private void BtnIPList_Click(object sender, RoutedEventArgs e)
        {
            BdContent.Child = ucIPList;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            //ucIPList.SchedulingScan();
#endif
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EventLogger.WriteEventLogEntry("프로그램 종료", EventLogEntryType.Information);
        }
    }
}
