using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using NetworkScanner.Avalonia.Helpers;

namespace NetworkScanner.Avalonia.Views
{
    /// <summary>
    /// 실제 설정 파일 읽기/쓰기는 NetworkScanner.Core의 AppSettingsStore에 있다.
    /// 이 클래스는 Avalonia 컨트롤과 AppSettingsData를 서로 매핑하는 얇은 어댑터 역할만 한다.
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public ScanRangeList ScanRanges { get; } = new ScanRangeList();
        private readonly CheckBox[] _hourCheckboxes = new CheckBox[24];

        public SettingsView()
        {
            InitializeComponent();
            BuildHourCheckboxes();
            LoadFromStore();
        }

        private void BuildHourCheckboxes()
        {
            for (int label = 1; label <= 24; label++)
            {
                var checkBox = new CheckBox { Content = Localization.IsKorean ? $"{label:D2}시" : $"{label:D2}h", Margin = new global::Avalonia.Thickness(8, 2) };
                _hourCheckboxes[label - 1] = checkBox;
                HourCheckboxPanel.Children.Add(checkBox);
            }
        }

        private void LoadFromStore()
        {
            foreach (var range in AppSettingsStore.LoadScanRanges())
            {
                ScanRanges.AddItem(range);
            }
            DgIPRange.ItemsSource = ScanRanges;

            ApplySettingsToControls(AppSettingsStore.LoadSettings());
        }

        private void ApplySettingsToControls(AppSettingsData data)
        {
            ChkScheduling.IsChecked = data.UseScheduling;
            for (int i = 0; i < 24; i++) _hourCheckboxes[i].IsChecked = data.HourEnabled[i];
            ChkUseFTP.IsChecked = data.UseFTP;
            TbFTPIP.Text = data.FtpIp;
            TbFTPID.Text = data.FtpId;
            TbFTPPW.Text = data.FtpPassword;
            TbFTPPort.Text = data.FtpPort;
            TbCurSystemName.Text = data.SystemName;
            TbPortsList.Text = data.PortList;
            ChkCheckPort.IsChecked = data.UsePortChecking;
            ChkLoadLatestFileWhenStartup.IsChecked = data.LoadLatestFileOnStartup;
            ChkContinuousMonitoring.IsChecked = data.ContinuousMonitoring;
            TbMonitorInterval.Text = data.MonitorIntervalMinutes.ToString();
            CmbLanguage.SelectedIndex = data.Language == Localization.Korean ? 1 : 0;
        }

        private AppSettingsData CollectSettingsFromControls()
        {
            var data = new AppSettingsData
            {
                UseScheduling = ChkScheduling.IsChecked == true,
                UseFTP = ChkUseFTP.IsChecked == true,
                FtpIp = TbFTPIP.Text ?? "",
                FtpId = TbFTPID.Text ?? "",
                FtpPassword = TbFTPPW.Text ?? "",
                FtpPort = TbFTPPort.Text ?? "",
                SystemName = TbCurSystemName.Text ?? "",
                PortList = TbPortsList.Text ?? "",
                UsePortChecking = ChkCheckPort.IsChecked == true,
                LoadLatestFileOnStartup = ChkLoadLatestFileWhenStartup.IsChecked == true,
                ContinuousMonitoring = ChkContinuousMonitoring.IsChecked == true,
                MonitorIntervalMinutes = int.TryParse(TbMonitorInterval.Text, out int m) && m > 0 ? m : 10,
                Language = CmbLanguage.SelectedIndex == 1 ? Localization.Korean : Localization.English,
            };

            for (int i = 0; i < 24; i++) data.HourEnabled[i] = _hourCheckboxes[i].IsChecked == true;

            return data;
        }

        public string GetColumnWidths() => ColumnLayout.Serialize(DgIPRange);
        public void ApplyColumnWidths(string csv) => ColumnLayout.Apply(DgIPRange, csv);

        public bool IsInScheduleHour(int hour) => CollectSettingsFromControls().IsInScheduleHour(hour);

        public string GetSystemName() => TbCurSystemName.Text ?? "";

        // MainWindow가 IScanConfigProvider를 구현하기 위해 화면에 반영된 현재 설정값을 읽어올 때 사용한다.
        public AppSettingsData GetCurrentSettings() => CollectSettingsFromControls();

        private void SaveScanRanges()
        {
            AppSettingsStore.SaveScanRanges(ScanRanges);
            TbMsg.Text = Localization.T("msg.range.saved");
        }

        private void SaveSettings()
        {
            AppSettingsStore.SaveSettings(CollectSettingsFromControls());
            TbMsg.Text = Localization.T("msg.settings.saved");
        }

        private static bool IsValidIP(string val)
        {
            return System.Net.IPAddress.TryParse(val, out var parsed)
                && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        private void RefreshGrid()
        {
            DgIPRange.ItemsSource = null;
            DgIPRange.ItemsSource = ScanRanges;
        }

        private async void BtnAddRange_Click(object? sender, RoutedEventArgs e)
        {
            var owner = (Window)TopLevel.GetTopLevel(this)!;

            string startText = TbStartIP.Text ?? "";
            string endText = TbEndIP.Text ?? "";

            // 시작 IP 칸에 "192.168.1.0/24" 처럼 CIDR을 입력하면 시작/종료 IP로 자동 변환한다.
            if (startText.Contains('/'))
            {
                if (!IPRangeUtil.TryParseCidr(startText, out string cs, out string ce))
                {
                    await SimpleDialogs.ShowMessageAsync(owner, Localization.T("msg.cidr.invalid"));
                    return;
                }
                startText = cs;
                endText = ce;
            }

            if (!IsValidIP(startText))
            {
                await SimpleDialogs.ShowMessageAsync(owner, Localization.T("msg.startip.invalid"));
                return;
            }
            if (!IsValidIP(endText))
            {
                await SimpleDialogs.ShowMessageAsync(owner, Localization.T("msg.endip.invalid"));
                return;
            }

            bool added = ScanRanges.AddItem(new ScanRangeInfo
            {
                Index = 0,
                StartIP = startText,
                EndIP = endText,
                Description = TbDescription.Text ?? "",
            });

            if (!added)
            {
                await SimpleDialogs.ShowMessageAsync(owner, Localization.T("msg.range.duplicate"));
                return;
            }

            TbStartIP.Text = "";
            TbEndIP.Text = "";
            TbDescription.Text = "";

            SaveScanRanges();
            RefreshGrid();
        }

        private async void BtnRemoveRange_Click(object? sender, RoutedEventArgs e)
        {
            if (DgIPRange.SelectedItem is not ScanRangeInfo item) return;

            var owner = (Window)TopLevel.GetTopLevel(this)!;
            bool confirmed = await SimpleDialogs.ShowConfirmAsync(owner, $"{item.StartIP} ~ {item.EndIP}", Localization.T("settings.removerange"));
            if (confirmed)
            {
                ScanRanges.DelItem(item.StartIP, item.EndIP);
                SaveScanRanges();
                RefreshGrid();
            }
        }

        private void BtnSaveFile_Click(object? sender, RoutedEventArgs e)
        {
            SaveScanRanges();
            SaveSettings();
        }
    }
}
