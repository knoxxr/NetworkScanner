using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkScanner
{
    /// <summary>
    /// UCIPList.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UCIPList : UserControl
    {
        private IPInfoList _IPInfoList;
        private int _SleepTime = 10;
        FTPService FTP = new FTPService();

        public Task Scanning;

        public UCIPList()
        {
            InitializeComponent();
        }
        public void LoadIPInfo(string filename)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(filename);

                ParsingIP(lines);

                LvIPList.Items.Refresh();
            }
            catch(System.IO.IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ParsingIP(string[] raw)
        {
            _IPInfoList.Clear();
            int lintcnt = 0;
            foreach (string line in raw)
            {
                if (lintcnt++ == 0) continue;

                string[] token = line.Split(",");

                if (token.Length > 0)
                {
                    IPInfo ip = new IPInfo();
                    ip.Ip = token[0];
                    ip.Port = Int32.Parse(token[1]);
                    ip.SystemName = token[2];
                    ip.Description = token[3];
                    ip.CommitDate = token[4];
                    if(token.Length>=6)
                        ip.Alive = bool.Parse(token[5]);
                    _IPInfoList.Add(ip);
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControl();
        }

        private void InitializeControl()
        {
            _IPInfoList  = Resources["IPInfoList"] as IPInfoList;
            InitFTP();
        }

        private void InitFTP()
        {
            FTP.HostIP = ((MainNetworkScanner)Application.Current.MainWindow).GetFTPIP();
            FTP.ID = ((MainNetworkScanner)Application.Current.MainWindow).GetFTPID();
            FTP.PW = ((MainNetworkScanner)Application.Current.MainWindow).GetFTPPW();
            FTP.Port = ((MainNetworkScanner)Application.Current.MainWindow).GetFTPPort();
        }

        public void ClearItems()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                _IPInfoList.Clear();
            }));

            RefreshItems();
        }
        public IPInfoList GetItems()
        {
            return _IPInfoList;
        }

        public void RefreshItems()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                LvIPList.Items.Refresh();
            }));
        }
        public async void WriteIPInfo(bool autosave, string systemName)
        {
            if (_IPInfoList.Count == 0)
            {
                DisplayMsg(string.Format("저장할 아이템이 없습니다 "));
                return;
            }

            List<string> lines = new List<string>();

            string title = string.Format("IPAddress,Port,SystemName,Description,Commitdate,Alive");
            lines.Add(title);

            foreach (IPInfo info in _IPInfoList)
            {
                string line = string.Format("{0},{1},{2},{3},{4},{5}", info.Ip, info.Port, info.SystemName, info.Description, info.CommitDate, info.Alive);
                lines.Add(line);
            }

            string path = Directory.GetCurrentDirectory() + @"\env\";
            if(Directory.Exists(path)== false)
            { 
                Directory.CreateDirectory(path);    
            }
            string autosavetag = autosave==true ? "(SCHEDULING)" : "";
            string filename = autosavetag + string.Format("{0}.csv", systemName + DateTime.Now.ToString(String.Format("_yyyyMMdd_HHmmss")));
            await File.WriteAllLinesAsync(path + filename, lines, Encoding.UTF8);

            if(((MainNetworkScanner)Application.Current.MainWindow).GetUseFTP() == true)
            {
                FTP.UploadFileList(path, filename);
            }

            DisplayMsg(string.Format("파일을 저장했습니다.  File Name : {0}", filename));
        }

        CancellationTokenSource ts = new CancellationTokenSource();
        public void SchedulingScan()
        {
            Scanning = DoasyncScanAllRange(true, ((MainNetworkScanner)Application.Current.MainWindow).GetSystemName());
            //Scanning.Wait();

        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (rbRefreshAllRange.IsChecked == true)
            {
                Scanning = DoasyncScanAllRange(false, ((MainNetworkScanner)Application.Current.MainWindow).GetSystemName());
                //Scanning.Wait();
            }
            else if(rbRefreshOnlyOnList.IsChecked == true)
            {
                Scanning = DoasyncRefreshIPList();
                //Scanning.Wait();
            }
        }

        public void ScanningStop()
        {
            if (Scanning == null) return;

            if(Scanning.Status == TaskStatus.Running)
            {
                //Scanning.
            }

        }

        public bool IsScanning()
        {
            if (Scanning == null) return false;
            if(Scanning.Status == TaskStatus.Running && !Scanning.IsCompleted)
                return true;
            else
                return false;
        }

        public async Task DoasyncRefreshIPList()
        {
            InitProgress(_IPInfoList.Count);
            int idx = 0;
            await Task.Run(() =>
            {
                foreach (IPInfo item in _IPInfoList)
                {
                    var reply = PingTester.SendPing(IPAddress.Parse(item.Ip));
                    if (reply.Status == IPStatus.Success)
                    {
                        item.RountTime = reply.RoundtripTime;
                        item.Alive = true;
                    }
                    else
                    {
                        item.RountTime = 9999;
                        item.Alive = false;
                    }

                    if(item.SystemName=="")
                        item.SystemName = GetHostName(IPAddress.Parse(item.Ip));

                    DisplayMsg(reply.Address.ToString());
                    SetProgress(idx++);
                    Thread.Sleep(_SleepTime);
                    RefreshItems();
                }
                DisplayMsg("스캐닝을 완료했습니다.");
            });
            InitProgress(0);
        }

        public async Task DoasyncScanAllRange(bool scheduling, string systemname)
        {
            InitProgress(UCSetting.IPCount);
            int idx = 0;
            DisplayMsg(string.Format("스캐줄링 스캔을 시작합니다. {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));

            await Task.Run(() =>
            {
                foreach (ScanRangeInfo item in UCSetting._ScanRangeList)
                {
                    string[] parseStartIP = item.StartIP.Split('.');
                    string[] parseEndIP = item.EndIP.Split('.');
                    int startip = Int32.Parse(parseStartIP[3]);
                    int endip = Int32.Parse(parseEndIP[3]);

                    for (int i = startip; i <= endip; i++)
                    {
                        string strIP = string.Format("{0}.{1}.{2}.{3}", parseStartIP[0], parseStartIP[1], parseStartIP[2], parseStartIP[3]);
                        IPAddress newIp = IPAddress.Parse(strIP);
                        var reply = PingTester.SendPing(newIp);

                        RefreshIPInfo(reply, strIP);
                        DisplayMsg(string.Format("Send Ping to : {0}",reply.Address.ToString()));
                        string ipbyte4 = (Int32.Parse(parseStartIP[3]) + 1).ToString();
                        parseStartIP[3] = ipbyte4;
                        SetProgress(idx++);

                        Thread.Sleep(_SleepTime);
                    }
                }
            });
            InitProgress(0);
            DisplayMsg(string.Format("스캐줄링 스캔을 완료했습니다. {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));

            if (scheduling) WriteIPInfo(true, systemname);
        }

        private void RefreshIPInfo(PingReply reply, string targetip)
        {
            IPInfo info = _IPInfoList.GetItem(targetip);
            if (info != null)
            {
                IPInfo newIpInfo = new IPInfo();
                newIpInfo.Ip = targetip;
                newIpInfo.Port = 0;
                newIpInfo.Description = "";
                newIpInfo.CommitDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                newIpInfo.RountTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : 9999;
                newIpInfo.Alive = reply.Status == IPStatus.Success ? true : false;
                newIpInfo.SystemName = GetHostName(IPAddress.Parse(targetip));
                RefreshItems();
            }
            else
            {
                if (reply.Status == IPStatus.Success)
                {
                    IPInfo newIpInfo = new IPInfo();
                    newIpInfo.Ip = targetip;
                    newIpInfo.Port = 0;
                    newIpInfo.Description = "";
                    newIpInfo.CommitDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    newIpInfo.RountTime = reply.RoundtripTime;
                    newIpInfo.Alive = true;
                    newIpInfo.SystemName = GetHostName(targetip);
                    AddNewItem(newIpInfo);
                    RefreshItems();
                }
            }
        }

        private string GetHostName(IPAddress hostip)
        {
            string result = "";
            try
            {
                IPHostEntry host = Dns.GetHostByAddress(hostip);
                result = host.HostName;
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                return result;
            }

            return result;
        }

        private string GetHostName(string hostip)
        {
            IPAddress ip = IPAddress.Parse(hostip);
            return GetHostName(ip); 
        }
        private void DisplayMsg(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TbMsg.Text = msg;
            }));
        }

        private void DeleteItem(string ip)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                _IPInfoList.DelItem(ip);
            }));
        }

        private void AddNewItem(IPInfo item)
        {
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                  {
                      _IPInfoList.Add(item);
                  }));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString()); 
            }
            RefreshItems();
        }
        private void SetProgress(int value)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                pbProgress.Value = value;
            }));
        }
        private void InitProgress(int maxvalue)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                pbProgress.Maximum = maxvalue;
                pbProgress.Value = 0;
            }));
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
                LoadIPInfo(openFileDialog.FileName);
            }
        }

        private void BtnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            WriteIPInfo(false, ((MainNetworkScanner)Application.Current.MainWindow).GetSystemName());
        }

        private void LvIPList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void MenuItemPing_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            var reply = PingTester.SendPing(selValue.Ip);
            RefreshIPInfo(reply, selValue.Ip);
            DisplayMsg(String.Format("수동으로 {0}으로 Ping을 보냈습니다. 결과 : {1}", selValue.Ip, reply.Status));
        }

        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            this.DeleteItem(selValue.Ip);
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
