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
        private int _SleepTime = 1;
        private FTPService FTP = new FTPService();
        private OUIInfo OUI = new OUIInfo();

        public Task Scanning;

        public UCIPList()
        {
            InitializeComponent();
            OUI.LoadInfo();
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
                    ip.Ports = token[1];
                    ip.SystemName = token[2];
                    ip.Description = token[3];
                    ip.CommitDate = token[4];
                    if(token.Length>=6)
                        ip.Alive = bool.Parse(token[5]);
                    if(token.Length>=7)
                        ip.Macaddr = token[6];
                    if (token.Length >= 8)
                        ip.Vendor = token[7];
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
            _IPInfoList = Resources["IPInfoList"] as IPInfoList;
            InitFTP();
            InitPortList();
        }

        private void InitPortList()
        {
            PingTester._PortList.Clear();
            var ports = ((MainNetworkScanner)Application.Current.MainWindow).GetPortList().Split('/');

            foreach (string port in ports)
            {
                int castingport=0;
                Int32.TryParse(port, out castingport);
                if (castingport > 0)
                {
                    PingTester._PortList.Add(castingport);
                }
            }
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

            string title = string.Format("IPAddress,Port,SystemName,Description,Commitdate,Alive,MacAddress,Vendor");
            lines.Add(title);

            foreach (IPInfo info in _IPInfoList)
            {
                string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", info.Ip, info.Ports, info.SystemName, info.Description, info.CommitDate, info.Alive, info.Macaddr, info.Vendor, info.RoundTime);
                lines.Add(line);
            }

            string path = Directory.GetCurrentDirectory() + @"\env\";
            if(Directory.Exists(path)== false)
            { 
                Directory.CreateDirectory(path);    
            }
            string autosavetag = autosave==true ? "_(SCHEDULING)" : "";
            string filename = string.Format("{0}.csv", systemName + DateTime.Now.ToString(String.Format("_yyyyMMdd_HHmmss"))+ autosavetag);
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
            if (Scanning!= null && Scanning.Status == TaskStatus.Running)
            {
                DisplayMsg("이미 스캐닝 중입니다.");
                return;
            }
            string systemname = ((MainNetworkScanner)Application.Current.MainWindow).GetSystemName();
            Scanning = DoasyncScanAllRange(true, systemname);
            //Scanning.Wait();

        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (Scanning!= null && Scanning.Status == TaskStatus.Running)
            {
                DisplayMsg("이미 스캐닝 중입니다.");
                return;
            }

            if (rbRefreshAllRange.IsChecked == true)
            {
                string systemname = ((MainNetworkScanner)Application.Current.MainWindow).GetSystemName();
                Scanning = DoasyncScanAllRange(false, systemname);
                //Scanning.Wait();
            }
            else if (rbRefreshOnlyOnList.IsChecked == true)
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

        public async Task DoasyncCheckReservedPortList(string ip)
        {
            int idx = 0;
            int maxcnt = ((MainNetworkScanner)Application.Current.MainWindow).GetReservedPortList().Count;
            IPInfo ipinfo = _IPInfoList.GetItem(ip);
            InitProgress(maxcnt);
            DisplayMsg(String.Format(" {0}으로 예약된 포트 전부 검색을 시작합니다.", ipinfo.Ip));
            var portlist = ((MainNetworkScanner)Application.Current.MainWindow).GetReservedPortList();
            string reservedports = "";
            await Task.Run(() =>
            {
                foreach (RefPortInfo port in portlist)
                {
                    if (PingTester.CheckReservedPortsOpen(ipinfo.Ip, port.PortNo))
                    {
                        reservedports += port.PortNo + "/";
                    }
                    SetProgress(idx++);
                    DisplayMsg(string.Format("({0}/{1})IP: {2}, 검색 Port: {3}",idx, maxcnt, ip, port.PortNo));
                    //Thread.Sleep(_SleepTime);
                }

                ipinfo.Ports = reservedports;
                RefreshItems();
            });

            SetProgress(0);
            DisplayMsg(String.Format(" {0}으로 예약된 포트 전부 검색 했습니다. 결과 : {1}", ipinfo.Ip, reservedports));
        }

        public async Task DoasyncCheckProhibitPortList(string ip)
        {
            int idx = 0;
            int maxcnt = ((MainNetworkScanner)Application.Current.MainWindow).GetProhibitPortList().Count;
            IPInfo ipinfo = _IPInfoList.GetItem(ip);
            InitProgress(maxcnt);
            DisplayMsg(String.Format(" {0}으로 금지된 포트 전부 검색을 시작합니다.", ipinfo.Ip));

            var portlist = ((MainNetworkScanner)Application.Current.MainWindow).GetProhibitPortList();
            string prohibitPorts = "";
            await Task.Run(() =>
            {
                foreach (RefPortInfo port in portlist)
                {
                    if (PingTester.CheckReservedPortsOpen(ipinfo.Ip, port.PortNo))
                    {
                        prohibitPorts += port.PortNo + "/";
                    }
                    SetProgress(idx++);
                    DisplayMsg(string.Format("({0}/{1})IP: {2}, 검색 Port: {3}", idx, maxcnt, ipinfo.Ip, port.PortNo));
                    //Thread.Sleep(_SleepTime);
                }

                ipinfo.Ports = prohibitPorts;
                RefreshItems();
            });

            SetProgress(0);
            DisplayMsg(String.Format(" {0}으로 금지된 포트 전부 검색 했습니다. 결과 : {1}", ipinfo.Ip, prohibitPorts));
        }

        public async Task DoasyncCheckUserPortList(string ip)
        {
            int idx = 0;
            IPInfo ipinfo = _IPInfoList.GetItem(ip);
            DisplayMsg(String.Format(" {0}으로 사용자 포트 전부 검색을 시작합니다.", ipinfo.Ip));

            var portlist = ((MainNetworkScanner)Application.Current.MainWindow).GetPortList().Split('/');
            int maxcnt = portlist.Length;
            InitProgress(maxcnt);

            string userports = "";
            await Task.Run(() =>
            {
                foreach (string port in portlist)
                {
                    if (PingTester.CheckReservedPortsOpen(ipinfo.Ip, Int32.Parse(port)))
                    {
                        userports += port + "/";
                    }
                    SetProgress(idx++);
                    DisplayMsg(string.Format("({0}/{1})IP: {2}, 검색 Port: {3}", idx,maxcnt, ipinfo.Ip, port));
                    //Thread.Sleep(_SleepTime);
                }

                ipinfo.Ports = userports;
                RefreshItems();
            });

            SetProgress(0);
            DisplayMsg(String.Format(" {0}으로 사용자 포트 전부 검색 했습니다. 결과 : {1}", ipinfo.Ip, userports));
        }


        public async Task DoasyncRefreshIPList()
        {
            int maxcnt = _IPInfoList.Count;
            InitProgress(maxcnt);
            int idx = 0;
            DisplayMsg(string.Format("스캔을 시작합니다. {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));

            bool? useportchecking = ((MainNetworkScanner)Application.Current.MainWindow).GetUsePortChecking();
            await Task.Run(() =>
            {
                foreach (IPInfo item in _IPInfoList)
                {
                    var reply = PingTester.SendPing(IPAddress.Parse(item.Ip));
                    if (reply.Status == IPStatus.Success)
                    {
                        item.RountTime = reply.RoundtripTime.ToString();
                        item.Alive = true;

                        if (useportchecking == true)
                            item.Ports = PingTester.CheckPortsOpen(item.Ip);

                        if (item.Macaddr == "")
                        {
                            string mac = _IPInfoList.GetMACAddress(item.Ip);
                            item.Macaddr = mac;
                            item.Vendor = OUI.GetVender(mac);
                        }
                    }
                    else
                    {
                        item.RountTime = "Timeout";
                        item.Alive = false;
                    }

                    if (item.SystemName == "")
                        item.SystemName = _IPInfoList.GetHostName(IPAddress.Parse(item.Ip));

                    DisplayMsg(string.Format("({0}/{1}) IP : {2}", idx, maxcnt,  reply.Address.ToString()));
                    SetProgress(idx++);
                    RefreshItems();
                }
                DisplayMsg("스캔을 완료했습니다.");
            });
            SetProgress(0);
        }

        public async Task DoasyncScanAllRange(bool scheduling, string systemname)
        {
            int maxcnt = UCSetting.IPCount;
            InitProgress(maxcnt);
            int idx = 0;
            DisplayMsg(string.Format("전체 대역 스캔을 시작합니다. {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
            bool? useportchecking = ((MainNetworkScanner)Application.Current.MainWindow).GetUsePortChecking();

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

                        string openports="";
                        if (useportchecking == true)
                            openports = PingTester.CheckPortsOpen(strIP);

                        RefreshIPInfo(reply, strIP, openports);
                        DisplayMsg(string.Format("Send Ping to : {0}",reply.Address.ToString()));
                        string ipbyte4 = (Int32.Parse(parseStartIP[3]) + 1).ToString();
                        parseStartIP[3] = ipbyte4;
                        SetProgress(idx++);

                        //Thread.Sleep(_SleepTime);
                    }
                }
            });
            SetProgress(0);
            DisplayMsg(string.Format("전체 대역 스캔을 완료했습니다. {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));

            if (scheduling) WriteIPInfo(true, systemname);
        }

        private void RefreshPortInfo(string targetip, string openports)
        {
            IPInfo info = _IPInfoList.GetItem(targetip);
            info.Ports = openports;
            RefreshItems();
        }

        private void RefreshIPInfo(PingReply reply, string targetip, string openports)
        {
            IPInfo info = _IPInfoList.GetItem(targetip);
            if (info != null)
            {
                info.Ports = openports;
                info.RountTime = reply.Status == IPStatus.Success ? reply.RoundtripTime.ToString() : "Timeout";
                info.Alive = reply.Status == IPStatus.Success ? true : false;
                info.Macaddr = _IPInfoList.GetMACAddress(targetip);
                info.Vendor = OUI.GetVender(info.Macaddr); 

                if (info.SystemName == "")
                    info.SystemName = _IPInfoList.GetHostName(IPAddress.Parse(targetip));
                RefreshItems();
            }
            else
            {
                if (reply.Status == IPStatus.Success)
                {
                    IPInfo newIpInfo = new();
                    newIpInfo.Ip = targetip;
                    newIpInfo.Ports = openports;
                    newIpInfo.Description = "";
                    newIpInfo.CommitDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    newIpInfo.RountTime = reply.RoundtripTime.ToString();
                    newIpInfo.Alive = true;
                    newIpInfo.Macaddr = _IPInfoList.GetMACAddress(targetip);
                    newIpInfo.Vendor = OUI.GetVender(newIpInfo.Macaddr);
                    newIpInfo.SystemName = _IPInfoList.GetHostName(IPAddress.Parse(targetip));
                    AddNewItem(newIpInfo);
                    RefreshItems();
                }
            }
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
            var openports = PingTester.CheckPortsOpen(selValue.Ip);
            RefreshIPInfo(reply, selValue.Ip, openports);

            DisplayMsg(String.Format("수동으로 {0}으로 Ping을 보냈습니다. 결과 : {1}", selValue.Ip, reply.Status));
        }

        private void MenuItemCheckPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            Scanning = DoasyncCheckUserPortList(selValue.Ip);
        }
        private void MenuItemCheckReservedPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            Scanning = DoasyncCheckReservedPortList(selValue.Ip);
        }

        private void MenuItemCheckProhibitPort_Click(object sender, RoutedEventArgs e)
        {
            var selValue = (IPInfo)LvIPList.SelectedValue;
            if (selValue == null) return;

            Scanning = DoasyncCheckProhibitPortList(selValue.Ip);
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
