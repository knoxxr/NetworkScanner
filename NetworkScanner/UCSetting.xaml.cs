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
    public partial class UCSetting : UserControl
    {
        public const string IPRangeFileName = "iprange.ini";
        public const string SettingFileName = "setting.ini";
        public static ScanRangeList _ScanRangeList = new ScanRangeList();
        private const string StrUseSCHEDULING = "usescheuling";
        private const string StrUseHR01= "hr01";
        private const string StrUseHR02= "hr02";
        private const string StrUseHR03= "hr03";
        private const string StrUseHR04= "hr04";
        private const string StrUseHR05= "hr05";
        private const string StrUseHR06= "hr06";
        private const string StrUseHR07= "hr07";
        private const string StrUseHR08= "hr08";
        private const string StrUseHR09= "hr09";
        private const string StrUseHR10= "hr10";
        private const string StrUseHR11= "hr11";
        private const string StrUseHR12= "hr12";
        private const string StrUseHR13= "hr13";
        private const string StrUseHR14= "hr14";
        private const string StrUseHR15= "hr15";
        private const string StrUseHR16= "hr16";
        private const string StrUseHR17= "hr17";
        private const string StrUseHR18= "hr18";
        private const string StrUseHR19= "hr19";
        private const string StrUseHR20= "hr20";
        private const string StrUseHR21= "hr21";
        private const string StrUseHR22= "hr22";
        private const string StrUseHR23= "hr23";
        private const string StrUseHR24= "hr24";
        private const string StrUseFTP = "useftp";
        private const string StrFTPIP = "ftpip";
        private const string StrFTPID = "ftpid";
        private const string StrFTPPW = "ftppw";
        private const string StrFTPPORT = "ftpport";
        private const string StrSYSTEMNAME = "systemname";

        public bool? UseFTP
        {
            get
            {
                return ChkUseFTP.IsChecked;
            }
        }

        public IPAddress FTPIP
        {
            get
            {
                return IPAddress.Parse(TbFTPIP.Text);
            }
        }

        public string FTPID
        {
            get
            {
                return TbFTPID.Text;
            }
        }

        public string FTPPW
        {
            get
            {
                return TbFTPPW.Text;
            }
        }

        public int FTPPort
        {
            get
            {
                return int.Parse(TbFTPPort.Text);
            }
        }

        public string SystemName
        {
            get
            {
                return tbCurSystemName.Text;
            }
        }

        public bool? UseScheduling
        {
            get
            {
                return ChkScheduling.IsChecked;
            }
        }

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

        public UCSetting()
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
            LoadSettingFile();
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

        private void LoadSettingFile()
        {
            string filename = Directory.GetCurrentDirectory() + @"\" + SettingFileName;
            FileInfo fi = new FileInfo(filename);

            if (fi.Exists)
            {
                string[] lines = System.IO.File.ReadAllLines(filename);
                ParsingSetting(lines);
            }
            else
            {
                using (File.Create(filename))
                {
                    DisplayMsg("setting.ini 파일 생성");
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
        private void ParsingSetting(string[] raw)
        {
            foreach (string line in raw)
            {
                string[] token = line.Split("=");

                if (token.Length > 0)
                {
                    switch(token[0])
                    {
                        case StrUseSCHEDULING:
                            ChkScheduling.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR01:
                            Chk01.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR02:
                            Chk02.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR03:
                            Chk03.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR04:
                            Chk04.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR05:
                            Chk05.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR06:
                            Chk06.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR07:
                            Chk07.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR08:
                            Chk08.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR09:
                            Chk09.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR10:
                            Chk10.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR11:
                            Chk11.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR12:
                            Chk12.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR13:
                            Chk13.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR14:
                            Chk14.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR15:
                            Chk15.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR16:
                            Chk16.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR17:
                            Chk17.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR18:
                            Chk18.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR19:
                            Chk19.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR20:
                            Chk20.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR21:
                            Chk21.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR22:
                            Chk22.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR23:
                            Chk23.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseHR24:
                            Chk24.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrUseFTP:
                            ChkUseFTP.IsChecked = bool.Parse(token[1]);
                            break;
                        case StrFTPIP:
                            TbFTPIP.Text= token[1];
                            break;
                        case StrFTPID:
                            TbFTPID.Text = token[1];
                            break;
                        case StrFTPPW:
                            TbFTPPW.Text= token[1];
                            break;
                        case StrFTPPORT:
                            TbFTPPort.Text= token[1];
                            break;
                        case StrSYSTEMNAME:
                            tbCurSystemName.Text= token[1];
                            break;
                    }
                }
            }
        }

        public bool IsInScheduleHour(int hour)
        {
            if (Chk01.IsChecked == true && hour==1) return true;
            if (Chk02.IsChecked == true && hour==2) return true;
            if (Chk03.IsChecked == true && hour==3) return true;
            if (Chk04.IsChecked == true && hour==4) return true;
            if (Chk05.IsChecked == true && hour==5) return true;
            if (Chk06.IsChecked == true && hour==6) return true;
            if (Chk07.IsChecked == true && hour==7) return true;
            if (Chk08.IsChecked == true && hour==8) return true;
            if (Chk09.IsChecked == true && hour==9) return true;
            if (Chk10.IsChecked == true && hour==10) return true;
            if (Chk11.IsChecked == true && hour==11) return true;
            if (Chk12.IsChecked == true && hour==12) return true;
            if (Chk13.IsChecked == true && hour==13) return true;
            if (Chk14.IsChecked == true && hour==14) return true;
            if (Chk15.IsChecked == true && hour==15) return true;
            if (Chk16.IsChecked == true && hour==16) return true;
            if (Chk17.IsChecked == true && hour==17) return true;
            if (Chk18.IsChecked == true && hour==18) return true;
            if (Chk19.IsChecked == true && hour==19) return true;
            if (Chk20.IsChecked == true && hour==20) return true;
            if (Chk21.IsChecked == true && hour==21) return true;
            if (Chk22.IsChecked == true && hour==22) return true;
            if (Chk23.IsChecked == true && hour==23) return true;
            if (Chk24.IsChecked == true && hour ==0) return true;

            return false;
        }

        public async void WriteScanRangeInfo()
        {
            List<string> lines = new List<string>();

            foreach (ScanRangeInfo info in _ScanRangeList)
            {
                string line = string.Format("{0},{1},{2},{3}", info.Index, info.StartIP, info.EndIP, info.Description);
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(IPRangeFileName, lines, Encoding.UTF8);

            DisplayMsg(string.Format("IP 검색 대역 파일을 저장하였습니다."));
        }

        public async void WriteSettingInfo()
        {
            List<string> lines = new List<string>();

            lines.Add(string.Format("{0}={1}", StrUseSCHEDULING, ChkScheduling.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR01, Chk01.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR02, Chk02.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR03, Chk03.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR04, Chk04.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR05, Chk05.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR06, Chk06.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR07, Chk07.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR08, Chk08.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR09, Chk09.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR10, Chk10.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR11, Chk11.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR12, Chk12.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR13, Chk13.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR14, Chk14.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR15, Chk15.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR16, Chk16.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR17, Chk17.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR18, Chk18.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR19, Chk19.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR20, Chk20.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR21, Chk21.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR22, Chk22.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR23, Chk23.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseHR24, Chk24.IsChecked));
            lines.Add(string.Format("{0}={1}", StrUseFTP, ChkUseFTP.IsChecked));
            lines.Add(string.Format("{0}={1}", StrFTPIP, TbFTPIP.Text));
            lines.Add(string.Format("{0}={1}", StrFTPID, TbFTPID.Text));
            lines.Add(string.Format("{0}={1}", StrFTPPW, TbFTPPW.Text));
            lines.Add(string.Format("{0}={1}", StrFTPPORT, TbFTPPort.Text));
            lines.Add(string.Format("{0}={1}", StrSYSTEMNAME, tbCurSystemName.Text));

            await File.WriteAllLinesAsync(SettingFileName, lines, Encoding.UTF8);

            DisplayMsg(string.Format("설정 파일을 저장하였습니다."));
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

            ScanRangeInfo newinfo = new ScanRangeInfo();
            newinfo.Index = 0;
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

            if (MessageBox.Show(String.Format("{0} ~ {1} 대역을 삭제하시겠습니까?", item.StartIP, item.EndIP), "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _ScanRangeList.DelItem(item.StartIP, item.EndIP);
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

        public string GetSystemName()
        {
            string result = "";
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                result  = tbCurSystemName.Text;
            }));

            return result;
        }
        private void Btn_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            WriteScanRangeInfo();
            WriteSettingInfo();

        }
    }
}
