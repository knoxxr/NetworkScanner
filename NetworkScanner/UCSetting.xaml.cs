using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetworkScanner
{
    /// <summary>
    /// UCIPRange.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 실제 설정 파일 읽기/쓰기는 NetworkScanner.Core의 AppSettingsStore에 있다.
    /// 이 클래스는 WPF 컨트롤과 AppSettingsData를 서로 매핑하는 얇은 어댑터 역할만 한다.
    public partial class UCSetting : UserControl
    {
        public ScanRangeList ScanRanges { get; private set; } = new ScanRangeList();

        // 시간대 체크박스(Chk01~Chk24)를 라벨(1~24, ini 키 "hr01".."hr24")에 매핑한다.
        // Chk24는 자정(clock hour 0)을 의미하는 기존 표기 방식을 그대로 따른다.
        private Dictionary<int, CheckBox> HourCheckboxMap => new Dictionary<int, CheckBox>
        {
            [1] = Chk01, [2] = Chk02, [3] = Chk03, [4] = Chk04, [5] = Chk05, [6] = Chk06,
            [7] = Chk07, [8] = Chk08, [9] = Chk09, [10] = Chk10, [11] = Chk11, [12] = Chk12,
            [13] = Chk13, [14] = Chk14, [15] = Chk15, [16] = Chk16, [17] = Chk17, [18] = Chk18,
            [19] = Chk19, [20] = Chk20, [21] = Chk21, [22] = Chk22, [23] = Chk23, [24] = Chk24,
        };

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
            ScanRanges = Resources["ScanRangeList"] as ScanRangeList;

            foreach (var range in AppSettingsStore.LoadScanRanges())
            {
                ScanRanges.AddItem(range);
            }

            ApplySettingsToControls(AppSettingsStore.LoadSettings());
        }

        private void ApplySettingsToControls(AppSettingsData data)
        {
            ChkScheduling.IsChecked = data.UseScheduling;
            foreach (var entry in HourCheckboxMap)
            {
                entry.Value.IsChecked = data.HourEnabled[entry.Key - 1];
                // 시간대 라벨은 언어에 맞춰 코드에서 설정한다("01시" / "01h").
                entry.Value.Content = Localization.IsKorean ? $"{entry.Key:D2}시" : $"{entry.Key:D2}h";
            }
            CmbLanguage.SelectedIndex = data.Language == Localization.Korean ? 1 : 0;
            ChkUseFTP.IsChecked = data.UseFTP;
            TbFTPIP.Text = data.FtpIp;
            TbFTPID.Text = data.FtpId;
            TbFTPPW.Password = data.FtpPassword;
            TbFTPPort.Text = data.FtpPort;
            tbCurSystemName.Text = data.SystemName;
            tbPortsList.Text = data.PortList;
            ChkCheckPort.IsChecked = data.UsePortChecking;
            chkLoadLastestFileWhenStartup.IsChecked = data.LoadLatestFileOnStartup;
            ChkContinuousMonitoring.IsChecked = data.ContinuousMonitoring;
            tbMonitorInterval.Text = data.MonitorIntervalMinutes.ToString();
        }

        public string GetColumnWidths() => ColumnLayout.Serialize(LvIPRange);
        public void ApplyColumnWidths(string csv) => ColumnLayout.Apply(LvIPRange, csv);

        public bool GetContinuousMonitoring() => ChkContinuousMonitoring.IsChecked == true;

        public int GetMonitorIntervalMinutes()
            => int.TryParse(tbMonitorInterval.Text, out int m) && m > 0 ? m : 10;

        private AppSettingsData CollectSettingsFromControls()
        {
            var data = new AppSettingsData
            {
                UseScheduling = ChkScheduling.IsChecked == true,
                UseFTP = ChkUseFTP.IsChecked == true,
                FtpIp = TbFTPIP.Text,
                FtpId = TbFTPID.Text,
                FtpPassword = TbFTPPW.Password,
                FtpPort = TbFTPPort.Text,
                SystemName = tbCurSystemName.Text,
                PortList = tbPortsList.Text,
                UsePortChecking = ChkCheckPort.IsChecked == true,
                LoadLatestFileOnStartup = chkLoadLastestFileWhenStartup.IsChecked == true,
                ContinuousMonitoring = ChkContinuousMonitoring.IsChecked == true,
                MonitorIntervalMinutes = GetMonitorIntervalMinutes(),
                Language = CmbLanguage.SelectedIndex == 1 ? Localization.Korean : Localization.English,
            };

            foreach (var entry in HourCheckboxMap)
            {
                data.HourEnabled[entry.Key - 1] = entry.Value.IsChecked == true;
            }

            return data;
        }

        public bool IsInScheduleHour(int hour) => CollectSettingsFromControls().IsInScheduleHour(hour);

        public async Task WriteScanRangeInfo()
        {
            await Task.Run(() => AppSettingsStore.SaveScanRanges(ScanRanges));
            DisplayMsg(Localization.T("msg.range.saved"));
        }

        public async Task WriteSettingInfo()
        {
            await Task.Run(() => AppSettingsStore.SaveSettings(CollectSettingsFromControls()));
            DisplayMsg(Localization.T("msg.settings.saved"));
        }

        private async void BtnAddRange_Click(object sender, RoutedEventArgs e)
        {
            string startText = tbStartIP.Text;
            string endText = tbEndIP.Text;

            // 시작 IP 칸에 "192.168.1.0/24" 처럼 CIDR을 입력하면 시작/종료 IP로 자동 변환한다.
            if (startText.Contains('/'))
            {
                if (!IPRangeUtil.TryParseCidr(startText, out string cs, out string ce))
                {
                    MessageBox.Show(Localization.T("msg.cidr.invalid"));
                    return;
                }
                startText = cs;
                endText = ce;
            }

            if (!IsValidIP(startText))
            {
                MessageBox.Show(Localization.T("msg.startip.invalid"));
                return;
            }

            if (!IsValidIP(endText))
            {
                MessageBox.Show(Localization.T("msg.endip.invalid"));
                return;
            }

            ScanRangeInfo newinfo = new ScanRangeInfo();
            newinfo.Index = 0;
            newinfo.StartIP = startText;
            newinfo.EndIP = endText;
            newinfo.Description = tbDescription.Text;

            if (!ScanRanges.AddItem(newinfo))
            {
                MessageBox.Show(Localization.T("msg.range.duplicate"));
                return;
            }

            tbStartIP.Text = "";
            tbEndIP.Text = "";
            tbDescription.Text = "";

            await WriteScanRangeInfo();
            LvIPRange.Items.Refresh();
        }

        private async void BtnRemoveRange_Click(object sender, RoutedEventArgs e)
        {
            if (LvIPRange.SelectedItems.Count == 0) return;
            ScanRangeInfo item = (ScanRangeInfo)LvIPRange.SelectedItems[0];

            if (MessageBox.Show($"{item.StartIP} ~ {item.EndIP}", Localization.T("settings.removerange"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ScanRanges.DelItem(item.StartIP, item.EndIP);
            }

            await WriteScanRangeInfo();
            LvIPRange.Items.Refresh();
        }
        private bool IsValidIP(string val)
        {
            return IPAddress.TryParse(val, out IPAddress parsed)
                && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
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
        private async void Btn_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            await WriteScanRangeInfo();
            await WriteSettingInfo();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Update?", Localization.T("dlg.confirm"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ProgramUpdate.CheckCurVersion();
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }
    }
}
