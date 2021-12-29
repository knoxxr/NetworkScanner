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
using static NetworkScanner.IPInfo;
using static NetworkScanner.IPRange;

namespace NetworkScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string IPRangeFileName = "iprange.ini";
        public MainWindow()
        {
            InitializeComponent();

            InitializeApp();
        }

        private void InitializeApp()
        {
            LvIPList.ItemsSource = IPInfo.GetInstance();
            LvIPRange.ItemsSource = IPRangeInfo.GetInstance();
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
            IPRangeInfo.GetInstance().Clear();
            foreach (string line in raw)
            {
                string[] token = line.Split(",");

                if (token.Length > 0)
                {
                    IPRangeInfo info = new IPRangeInfo();
                    info.Index = int.Parse(token[0]);
                    info.StartIP = IPAddress.Parse(token[1]);
                    info.EndIP = IPAddress.Parse(token[2]);
                    info.Description = token[3];
                    IPRangeInfo.GetInstance().Add(info);
                }
            }
        }

        private void LoadIPInformation(string filename)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);

            ParsingIP(lines);

            LvIPList.Items.Refresh();
        }

        private void ParsingIP(string[] raw)
        {
            IPInfo.GetInstance().Clear();
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
                    IPInfo.GetInstance().Add(ip);
                }
            }
        }

        public async Task DoasyncRefreshIPRange()
        {
            await Task.Run(() =>
            {
                foreach (IPInfo item in IPInfo.GetInstance())
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
                    Thread.Sleep(300);
                    DisplayMsg("Scanning is Completed.");
                    RefreshIPList();
                }
            });
        }

        public async Task DoasyncScanIPRange(IPRangeInfo info)
        {
            await Task.Run(() =>
            {
                foreach (IPRangeInfo item in IPRangeInfo.GetInstance())
                {
                    int startip = Int32.Parse(item.StartIP.ToString().Split('.')[3]);
                    int endip = Int32.Parse(item.EndIP.ToString().Split('.')[3]);

                    for (int i = startip; i <= endip; i++)
                    {
                        string[] parseIP = item.StartIP.ToString().Split('.');
                        string ipbyte4 = (Int32.Parse(item.StartIP.ToString().Split('.')[3]) + 1).ToString();
                        parseIP[3] = ipbyte4;

                        IPAddress newip = IPAddress.Parse(string.Format("{0}.{1}.{2}.{3}", parseIP[0], parseIP[1], parseIP[2], parseIP[3]));

                        var reply = PingTester.SendPing(newip);

                        IPInfo info = GetIPList(newip);

                        if (info != null)
                        {
                            info.RountTime = reply.RoundtripTime;
                            info.Alive = reply.RoundtripTime == 9999 ? false : true;
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
                            newIpInfo.Alive = reply.RoundtripTime == 9999?false:true;

                            IPInfo.GetInstance().Add(newIpInfo);
                        }

                        DisplayMsg(reply.Address.ToString());
                        Thread.Sleep(300);

                        RefreshIPList();
                    }
                }
                DisplayMsg("Scanning is Completed.");
            });
        }

        private IPInfo? GetIPList(IPAddress ip)
        {
            return IPInfo.GetInstance().Find(x => x.Ip == ip);
        }

        private bool IsExistOnIPList(IPAddress ip)
        {
            if (IPInfo.GetInstance().FindIndex(x => x.Ip == ip) > 0)
                return true;
            else
                return false;
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
            Task k = DoasyncRefreshIPRange();
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
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                LoadIPInformation(openFileDialog.FileName);
            }

        }

        private void BtnAddIPRange_Click(object sender, RoutedEventArgs e)
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

            IPRangeInfo newinfo = new IPRangeInfo();
            newinfo.Index = IPRangeInfo.GetInstance().Count + 1;
            newinfo.StartIP = IPAddress.Parse(tbStartIP.Text);
            newinfo.EndIP = IPAddress.Parse(tbEndIP.Text);
            newinfo.Description = TbRangeDescription.Text;

            IPRangeInfo.GetInstance().Add(newinfo);

            WriteIPRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void BtnDelIPRange_Click(object sender, RoutedEventArgs e)
        {
            if (LvIPRange.SelectedItems.Count == 0) return;
            IPRangeInfo item = (IPRangeInfo)LvIPRange.SelectedItems[0];

            if (MessageBox.Show(String.Format("{0} 대역을 삭제하시겠습니까?", item.Index), "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DelelteIPRange(item.Index);
            }

            WriteIPRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void DelelteIPRange(int index)
        {
            int iprangeIndex = IPRangeInfo.GetInstance().FindIndex(x => x.Index == index);

            if (iprangeIndex != -1)
            {
                IPRangeInfo.GetInstance().RemoveAt(iprangeIndex);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            WriteIPInfo();
        }

        public async void WriteIPInfo()
        {
            List<string> lines = new List<string>();

            foreach (IPInfo info in IPInfo.GetInstance())
            {
                string line = string.Format("{0},{1},{2},{3},{4}", info.Ip, info.Port, info.SystemName, info.Description, info.CommitDate);
                lines.Add(line);
            }

            string filename = string.Format("{0}.csv", DateTime.Now.ToString("yyyyMMdd HHmmss"));

            await File.WriteAllLinesAsync(filename, lines);

            DisplayMsg(string.Format("Write File. File Name : {0}.csv", filename));
        }

        public async void WriteIPRangeInfo()
        {
            List<string> lines = new List<string>();

            foreach (IPRangeInfo info in IPRangeInfo.GetInstance())
            {
                string line = string.Format("{0},{1},{2},{3}", info.Index, info.StartIP, info.EndIP, info.Description);
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(IPRangeFileName, lines);

            DisplayMsg(string.Format("Write IP Range File."));
        }

        private void btnScanIPRange_Click(object sender, RoutedEventArgs e)
        {
            foreach (IPRangeInfo info in IPRangeInfo.GetInstance())
            {
                Task k = DoasyncScanIPRange(info);
            }
        }
    }

    public class AliveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool overTen = (bool)value;
            if (overTen)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
