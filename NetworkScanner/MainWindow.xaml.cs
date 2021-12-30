using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string IPRangeFileName = "iprange.ini";
        ScanRangeList _ScanRangeList = new ScanRangeList();
        IPInfoList _IPInfoList = new IPInfoList();

        public MainWindow()
        {
            InitializeComponent();

            InitializeApp();
        }

        private void InitializeApp()
        {

            /*LvIPList.ItemsSource = ipInfoList;*/
            _IPInfoList = Resources["IPInfoList"] as IPInfoList;
            _ScanRangeList = Resources["ScanRangeList"] as ScanRangeList;
            LoadIPRange();
        }

        private void LoadIPRange()
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
                    info.StartIP = token[1] ;
                    info.EndIP = token[2] ;
                    info.Description = token[3];
                    _ScanRangeList.AddItem(info);
                }
            }
        }

        private void LoadIPInfo(string filename)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);

            ParsingIP(lines);

            LvIPList.Items.Refresh();
        }

        private void ParsingIP(string[] raw)
        {
            _IPInfoList.Clear();
            foreach (string line in raw)
            {
                string[] token = line.Split(",");

                if (token.Length > 0)
                {
                    IPInfo ip = new IPInfo();
                    ip.Ip = IPAddress.Parse(token[0]);
                    ip.Port = Int32.Parse(token[1]);
                    ip.SystemName = token[2];
                    ip.Description = token[3];
                    ip.CommitDate = DateTime.Parse(token[4]);
                    _IPInfoList.Add(ip);
                }
            }
        }

        public async Task DoasyncRefreshScanRange()
        {
            await Task.Run(() =>
            {
                foreach (IPInfo item in _IPInfoList)
                {
                    var reply = PingTester.SendPing(item.Ip);
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
                    DisplayMsg(reply.Address.ToString());
                    Thread.Sleep(500);
                    RefreshIPList();
                }
                    DisplayMsg("Scanning is Completed.");
            });
        }

        public async Task DoasyncScanRange()
        {
            await Task.Run(() =>
            {
                foreach (ScanRangeInfo item in _ScanRangeList)
                {
                    string[] parseStartIP = item.StartIP.Split('.');
                    string[] parseEndIP = item.EndIP.Split('.');
                    int startip = Int32.Parse(parseStartIP[3]);
                    int endip = Int32.Parse(parseEndIP[3]);

                    for (int i = startip; i <= endip; i++)
                    {
                        IPAddress newip = IPAddress.Parse(string.Format("{0}.{1}.{2}.{3}", parseStartIP[0], parseStartIP[1], parseStartIP[2], parseStartIP[3]));

                        var reply = PingTester.SendPing(newip);

                        if (reply.Status == IPStatus.Success)
                        {

                            IPInfo info = _IPInfoList.GetItem(newip);

                            if (info != null)
                            {
                                info.RountTime = reply.RoundtripTime ;
                                info.Alive = true;
                                RefreshIPList();
                            }
                            else
                            {
                                IPInfo newIpInfo = new IPInfo();
                                newIpInfo.Ip = newip;
                                newIpInfo.Port = 0;
                                newIpInfo.SystemName = "";
                                newIpInfo.Description = "";
                                newIpInfo.CommitDate = DateTime.Now;
                                newIpInfo.RountTime = reply.RoundtripTime;
                                newIpInfo.Alive = true;

                                //ipInfoList.Add(newIpInfo);
                                AddIPInfo(newIpInfo);
                                RefreshIPList();
                            }
                        }
                        else
                        {
                            if(_IPInfoList.GetItem(newip) != null)
                            {
                                _IPInfoList.DelItem(newip);
                            }
                        }

                        DisplayMsg(reply.Address.ToString());
                        string ipbyte4 = (Int32.Parse(parseStartIP[3]) + 1).ToString();
                        parseStartIP[3] = ipbyte4;

                        Thread.Sleep(200);

                    }
                }
                DisplayMsg("Scanning is Completed.");
            });
        }
        private void AddIPInfo(IPInfo info)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                _IPInfoList.AddItem(info);
            }));
        }
        private void DisplayMsg(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TbMsg.Text = msg;
            }));
        }

        private void RefreshIPList()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                LvIPList.Items.Refresh();
            }));
        }

        private void ClearIPList()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                LvIPList.Items.Clear();
            }));
        }


        private void BtnStartPing_Click(object sender, RoutedEventArgs e)
        {
            /*
            IPAddress startIP = IPAddress.Parse(((ComboBoxItem)CmbTargetIPFrom.SelectedItem).Content.ToString());
            IPAddress endIP = IPAddress.Parse(((ComboBoxItem)CmbTargetIPTo.SelectedItem).Content.ToString());

            Task k = Doasync(startIP, endIP);*/
        }

        private bool IsValidIP(string val)
        {
            string[] token = val.Split(".");
            if (token.Length != 4)
                return false;

            return true;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Task k = DoasyncRefreshScanRange();
        }

        private void BtnStopPing_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            string path = Directory.GetCurrentDirectory() + "\\env";
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di.Create();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path+@"\env";
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                LoadIPInfo(openFileDialog.FileName);
            }

        }

        private void BtnAddScanRange_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValidIP(tbStartIP.Text))
            {
                MessageBox.Show("시작 IP 값 이상");
                return;
            }

            if (!IsValidIP(tbEndIP.Text))
            {
                MessageBox.Show("종료 IP 값 이상");
                return;
            }

            IPAddress.Parse(tbEndIP.Text);

            ScanRangeInfo newinfo = new ScanRangeInfo();
            newinfo.Index = _ScanRangeList.Count + 1;
            newinfo.StartIP = tbStartIP.Text;
            newinfo.EndIP = tbEndIP.Text;
            newinfo.Description = TbRangeDescription.Text;

            _ScanRangeList.AddItem(newinfo);

            WriteScanRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void BtnDelScanRange_Click(object sender, RoutedEventArgs e)
        {
            if (LvIPRange.SelectedItems.Count == 0) return;
            ScanRangeInfo item = (ScanRangeInfo)LvIPRange.SelectedItems[0];

            if (MessageBox.Show(String.Format("{0} 대역을 삭제하시겠습니까?", item.Index), "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _ScanRangeList.DelItem(item.Index);
            }

            WriteScanRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            WriteIPInfo();
        }

        public async void WriteIPInfo()
        {
            if (_IPInfoList.Count == 0) return;

            List<string> lines = new List<string>();

            foreach (IPInfo info in _IPInfoList)
            {
                string line = string.Format("{0},{1},{2},{3},{4}", info.Ip, info.Port, info.SystemName, info.Description, info.CommitDate.ToString("yyyy/MM/dd HH:mm:ss"));
                lines.Add(line);
            }

            string path = Directory.GetCurrentDirectory() + @"\env\";
            string filename = string.Format("{0}.csv", DateTime.Now.ToString("yyyyMMdd HHmmss"));
            await File.WriteAllLinesAsync(path+filename, lines);

            DisplayMsg(string.Format("Write File. File Name : {0}", filename));
        }

        public async void WriteScanRangeInfo()
        {
            List<string> lines = new List<string>();

            foreach (ScanRangeInfo info in _ScanRangeList)
            {
                string line = string.Format("{0},{1},{2},{3}", info.Index, info.StartIP, info.EndIP, info.Description);
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(IPRangeFileName, lines);

            DisplayMsg(string.Format("Write IP Range File."));
        }

        private void btnScanAllRange_Click(object sender, RoutedEventArgs e)
        {
            _IPInfoList.Clear();
            Task task = DoasyncScanRange();
        }

        private void StartIP_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            WriteScanRangeInfo();
        }

        private void EndIP_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            WriteScanRangeInfo();
        }

        private void Desc_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            WriteScanRangeInfo();
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
