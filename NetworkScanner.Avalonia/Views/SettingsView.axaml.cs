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
                var checkBox = new CheckBox { Content = $"{label:D2}시", Margin = new global::Avalonia.Thickness(8, 2) };
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
            };

            for (int i = 0; i < 24; i++) data.HourEnabled[i] = _hourCheckboxes[i].IsChecked == true;

            return data;
        }

        public bool IsInScheduleHour(int hour) => CollectSettingsFromControls().IsInScheduleHour(hour);

        public string GetSystemName() => TbCurSystemName.Text ?? "";

        // MainWindow가 IScanConfigProvider를 구현하기 위해 화면에 반영된 현재 설정값을 읽어올 때 사용한다.
        public AppSettingsData GetCurrentSettings() => CollectSettingsFromControls();

        private void SaveScanRanges()
        {
            AppSettingsStore.SaveScanRanges(ScanRanges);
            TbMsg.Text = "IP 검색 대역 파일을 저장하였습니다.";
        }

        private void SaveSettings()
        {
            AppSettingsStore.SaveSettings(CollectSettingsFromControls());
            TbMsg.Text = "설정 파일을 저장하였습니다.";
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

            if (!IsValidIP(TbStartIP.Text ?? ""))
            {
                await SimpleDialogs.ShowMessageAsync(owner, "시작 IP 값 이상");
                return;
            }
            if (!IsValidIP(TbEndIP.Text ?? ""))
            {
                await SimpleDialogs.ShowMessageAsync(owner, "종료 IP 값 이상");
                return;
            }

            bool added = ScanRanges.AddItem(new ScanRangeInfo
            {
                Index = 0,
                StartIP = TbStartIP.Text ?? "",
                EndIP = TbEndIP.Text ?? "",
                Description = TbDescription.Text ?? "",
            });

            if (!added)
            {
                await SimpleDialogs.ShowMessageAsync(owner, "이미 등록된 대역입니다.");
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
            bool confirmed = await SimpleDialogs.ShowConfirmAsync(owner, $"{item.StartIP} ~ {item.EndIP} 대역을 삭제하시겠습니까?", "삭제");
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
