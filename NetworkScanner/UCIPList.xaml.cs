using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        public UCIPList()
        {
            InitializeComponent();
        }
        public void LoadIPInfo(string filename)
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControl();
        }

        private void InitializeControl()
        {
            _IPInfoList  = Resources["IPInfoList"] as IPInfoList;
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
        public async void WriteIPInfo()
        {
            if (_IPInfoList.Count == 0)
            {
                DisplayMsg(string.Format("저장할 아이템이 없습니다 "));
                return;
            }

            List<string> lines = new List<string>();

            foreach (IPInfo info in _IPInfoList)
            {
                string line = string.Format("{0},{1},{2},{3},{4}", info.Ip, info.Port, info.SystemName, info.Description, info.CommitDate.ToString("yyyy/MM/dd HH:mm:ss"));
                lines.Add(line);
            }

            string path = Directory.GetCurrentDirectory() + @"\env\";
            string filename = string.Format("{0}.csv", DateTime.Now.ToString("yyyyMMdd HHmmss"));
            await File.WriteAllLinesAsync(path + filename, lines);

            DisplayMsg(string.Format("파일을 저장했습니다.  File Name : {0}", filename));
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (rbRefreshAllRange.IsChecked == true)
            {
                Task k = DoasyncScanAllRange();
            }
            else if(rbRefreshOnlyOnList.IsChecked == true)
            {
                Task k = DoasyncRefreshIPList();
            }
        }


        public async Task DoasyncRefreshIPList()
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
                    RefreshItems();
                }
                DisplayMsg("스캐닝을 완료했습니다.");
            });
        }

        public async Task DoasyncScanAllRange()
        {
            await Task.Run(() =>
        {
            foreach (ScanRangeInfo item in UCIPRange._ScanRangeList)
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
                            info.RountTime = reply.RoundtripTime;
                            info.Alive = true;
                            RefreshItems();
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

                            AddNewItem(newIpInfo);
                        }
                    }
                    else
                    {
                        if (_IPInfoList.GetItem(newip) != null)
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
            DisplayMsg("스캐닝을 완료했습니다.");
        });
        }
        private void DisplayMsg(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TbMsg.Text = msg;
            }));
        }

        private void AddNewItem(IPInfo item)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                _IPInfoList.Add(item);
            }));
            RefreshItems();
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
            WriteIPInfo();

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
