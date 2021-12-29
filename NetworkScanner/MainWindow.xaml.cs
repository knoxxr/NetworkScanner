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
        public MainWindow()
        {
            InitializeComponent();

            InitializeApp();
        }

        private void InitializeApp()
        {
            LvIPList.ItemsSource = IPinformation.GetInstance();
            LvIPRange.ItemsSource = IPRangeInfo.GetInstance();
            LoadIPRange();
        }

        private void LoadIPRange()
        {
            string filename = Directory.GetCurrentDirectory() + @"\" + IPRangeFileName;
            string[] lines = System.IO.File.ReadAllLines(filename);

            ParsingIPRange(lines);
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

        private void LoadInformation(string filename)
        {
            string[] lines = System.IO.File.ReadAllLines(filename);

            ParsingIP(lines);

            LvIPList.Items.Refresh();
        }

        private void ParsingIP(string[] raw)
        {
            IPinformation.GetInstance().Clear();
            foreach (string line in raw)
            {
                string[] token = line.Split(",");

                if (token.Length > 0)
                {
                    IPinformation ip = new IPinformation();
                    ip.Ip = IPAddress.Parse(token[0]);
                    ip.Port = Int32.Parse(token[1]);
                    ip.SystemName = token[2];
                    ip.Description = token[3];
                    ip.CommitDate = DateTime.Parse(token[4]);
                    IPinformation.GetInstance().Add(ip);
                }
            }
        }

        public async Task Doasync(IPAddress start, IPAddress end)
        {
            await Task.Run(() =>
            {
                foreach (IPinformation item in IPinformation.GetInstance())
                {
                    var reply = PingTester.SendPing(item.Ip);

                    item.RountTime = reply.RoundtripTime;
                    DisplayMsg(reply.Address.ToString());
                    Thread.Sleep(1000);
                    Console.WriteLine("Doasync");
                }
                DisplayMsg("Scanning is Completed.");
            });
        }

        private void DisplayMsg(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                TbMsg.Text = msg;
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
            //ping
            Task k = Doasync(IPAddress.Parse("10.100.113.1"), IPAddress.Parse("10.100.113.100"));
            //refresh list
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
                LoadInformation(openFileDialog.FileName);
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

            if (MessageBox.Show(String.Format("{0} 대역을 삭제하시겠습니까?", item.Index),"삭제",MessageBoxButton.YesNo)== MessageBoxResult.Yes)
            {
                DelelteIPRange(item.Index);
            }

            WriteIPRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void DelelteIPRange(int index)
        {
            int iprangeIndex = IPRangeInfo.GetInstance().FindIndex(x => x.Index == index);

            if(iprangeIndex != -1)
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

            foreach (IPinformation info in IPinformation.GetInstance())
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
    }

    public class PingTester
    {
        public static PingReply SendPing(IPAddress targetIP)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "a";

            byte[] buffer = Encoding.ASCII.GetBytes(data);

            int timeout = 120;

            PingReply reply = pingSender.Send(targetIP, timeout, buffer, options);

            return reply;
        }
    }

    public class IPInformationListViewModel
    {
        private readonly IPInformationList items;

        public IPInformationListViewModel()
        {
            this.items = new IPInformationList();
        }

        public IPInformationList Items
        {
            get { return this.items; }
        }
    }

    public class IPInformationList : ObservableCollection<IPinformation>
    {

    }

    public class IPinformation : INotifyPropertyChanged
    {
        private IPAddress ip;
        private int port;
        private string systemName;
        private long rountTime;
        private bool alive;
        private DateTime commitDate;
        private string description;

        public IPAddress Ip { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }
        public string SystemName { get => systemName; set => systemName = value; }
        public long RountTime { get => rountTime; set => rountTime = value; }
        public bool Alive { get => alive; set => alive = value; }
        public DateTime CommitDate { get => commitDate; set => commitDate = value; }
        public string Description { get => description; set => description = value; }

        private static List<IPinformation> instance;

        public static List<IPinformation> GetInstance()
        {
            if (instance == null)
                instance = new List<IPinformation>();

            return instance;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            if (!String.IsNullOrEmpty(name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

    public class IPRangeInfo : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public IPAddress StartIP { get; set; }
        public IPAddress EndIP { get; set; }
        public string Description { get; set; }

        private static List<IPRangeInfo> instance;

        public static List<IPRangeInfo> GetInstance()
        {
            if (instance == null)
                instance = new List<IPRangeInfo>();

            return instance;
        }     

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            if (!String.IsNullOrEmpty(name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
