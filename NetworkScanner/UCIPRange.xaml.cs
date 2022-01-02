using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkScanner
{
    /// <summary>
    /// UCIPRange.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UCIPRange : UserControl
    {
        public const string IPRangeFileName = "iprange.ini";
        public static ScanRangeList _ScanRangeList = new ScanRangeList();

        public static int IPCount
        {
            get
            {
                int cnt = 0;
                foreach (ScanRangeInfo info in _ScanRangeList)
                {
                    string[] startip = info.StartIP.Split('.');
                    string[] endip = info.EndIP.Split('.');

                    int startNo = Int32.Parse(startip[3]);
                    int endNo = Int32.Parse(endip[3]);

                    cnt += endNo - startNo + 1;

                }
                return cnt;
            }
        }

        public UCIPRange()
        {
            InitializeComponent();
            InitializeControl();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void InitializeControl()
        {
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
                    info.StartIP = token[1];
                    info.EndIP = token[2];
                    info.Description = token[3];
                    _ScanRangeList.AddItem(info);
                }
            }
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

            DisplayMsg(string.Format("설장파일을 저장하였습니다."));
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

        private void BtnAddRange_Click(object sender, RoutedEventArgs e)
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
            newinfo.Description = tbDescription.Text;

            _ScanRangeList.AddItem(newinfo);

            WriteScanRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private void BtnRemoveRange_Click(object sender, RoutedEventArgs e)
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
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
           
        }


        private bool IsValidIP(string val)
        {
            string[] token = val.Split(".");
            if (token.Length != 4)
                return false;

            return true;
        }

        private void DisplayMsg(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                tbMsg.Text = msg;
            }));
        }

        private void Btn_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            WriteScanRangeInfo();
        }
    }
}
