using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using NetworkScanner.Avalonia.Views;

namespace NetworkScanner.Avalonia;

public partial class MainWindow : Window, IScanConfigProvider
{
    private readonly IPListView _ipListView = new();
    private readonly SettingsView _settingsView = new();
    private readonly RefPortListView _refPortListView = new();
    private readonly UserGuideView _userGuideView = new();
    private readonly DispatcherTimer _timer = new();
    private readonly UpdateService _updateService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        AppLogger.LogInfo("NetworkScanner", "프로그램 시작");

        _ipListView.Initialize(this);
        ContentArea.Content = _ipListView;
        SetActiveNav(BtnIPList);

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        TbVersion.Text = "ver. " + (version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "unknown");

        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // 저장해 둔 컬럼 너비 복원, 종료 시 현재 너비 저장.
        AppSettingsData startup = _settingsView.GetCurrentSettings();
        _ipListView.ApplyColumnWidths(startup.IpListColumnWidths);
        _settingsView.ApplyColumnWidths(startup.IpRangeColumnWidths);

        Closing += (_, _) =>
        {
            AppSettingsStore.SaveColumnLayout(_ipListView.GetColumnWidths(), _settingsView.GetColumnWidths());
            AppLogger.LogInfo("NetworkScanner", "프로그램 종료");
        };

        if (startup.LoadLatestFileOnStartup)
        {
            _ipListView.GetLatestFilePath(GetSystemName());
        }

        _ = CheckForUpdatesAsync();
    }

    // GitHub Releases에서 새 버전을 확인하고, 있으면 사이드바의 업데이트 버튼을 노출한다.
    // 설치본이 아닌 개발 실행(dotnet run)에서는 조용히 아무것도 하지 않는다.
    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            string? newVersion = await _updateService.CheckAsync();
            if (newVersion == null) return;

            TbUpdate.Text = $"{Localization.T("update.button")} v{newVersion}";
            ToolTip.SetTip(BtnUpdate, string.Format(Localization.T("update.available"), newVersion));
            BtnUpdate.IsVisible = true;
        }
        catch (Exception ex)
        {
            // 오프라인 등 확인 실패는 치명적이지 않다 - 로그만 남긴다.
            AppLogger.LogInfo("NetworkScanner", "업데이트 확인 실패: " + ex.Message);
        }
    }

    private async void BtnUpdate_Click(object? sender, RoutedEventArgs e)
    {
        BtnUpdate.IsEnabled = false;
        try
        {
            await _updateService.DownloadAndApplyAsync(percent =>
                Dispatcher.UIThread.Post(() =>
                    TbUpdate.Text = $"{Localization.T("update.downloading")} {percent}%"));
        }
        catch (Exception ex)
        {
            AppLogger.LogError("NetworkScanner", "업데이트 실패: " + ex.Message);
            TbUpdate.Text = Localization.T("update.failed");
            BtnUpdate.IsEnabled = true;
        }
    }

    private DateTime _lastMonitorScan = DateTime.MinValue;

    private void Timer_Tick(object? sender, EventArgs e)
    {
        AppSettingsData settings = _settingsView.GetCurrentSettings();

        // 연속 모니터링: 지정한 분 간격마다 자동 재스캔(스케줄링과 독립적으로 동작).
        if (settings.ContinuousMonitoring
            && (DateTime.Now - _lastMonitorScan).TotalMinutes >= settings.MonitorIntervalMinutes
            && !_ipListView.IsScanning())
        {
            _lastMonitorScan = DateTime.Now;
            _ipListView.StartScan();
            return;
        }

        if (!_settingsView.IsInScheduleHour(DateTime.Now.Hour)) return;
        if (DateTime.Now.Minute != 0) return;
        if (_ipListView.IsScanning()) return;

        _ipListView.SchedulingScan();
    }

    private void BtnIPList_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _ipListView;
        SetActiveNav(BtnIPList);
    }

    private void BtnSetting_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _settingsView;
        SetActiveNav(BtnSetting);
    }

    private void BtnPortInfo_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _refPortListView;
        SetActiveNav(BtnPortInfo);
    }

    private void BtnGuide_Click(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _userGuideView;
        SetActiveNav(BtnGuide);
    }

    // 사이드바에서 지금 어느 화면을 보고 있는지 좌측 강조선 + 배경 틴트로 표시한다.
    private void SetActiveNav(Button active)
    {
        var accent = (IBrush)Resources["AccentBrush"]!;
        var activeBg = (IBrush)Resources["SideActiveBackgroundBrush"]!;
        var inactiveBg = (IBrush)Resources["SideBackgroundBrush"]!;

        foreach (Button nav in new[] { BtnIPList, BtnSetting, BtnPortInfo, BtnGuide })
        {
            bool isActive = ReferenceEquals(nav, active);
            nav.Background = isActive ? activeBg : inactiveBg;
            nav.BorderBrush = isActive ? accent : Brushes.Transparent;
        }
    }

    // IScanConfigProvider 구현 - SettingsView/RefPortListView에 위임한다.
    public string GetPortList() => _settingsView.GetCurrentSettings().PortList;

    public IPAddress GetFTPIP()
    {
        string ip = _settingsView.GetCurrentSettings().FtpIp;
        return string.IsNullOrEmpty(ip) ? IPAddress.None : IPAddress.Parse(ip);
    }

    public string GetFTPID() => _settingsView.GetCurrentSettings().FtpId;
    public string GetFTPPW() => _settingsView.GetCurrentSettings().FtpPassword;

    public int GetFTPPort()
    {
        string port = _settingsView.GetCurrentSettings().FtpPort;
        return string.IsNullOrEmpty(port) ? 0 : int.Parse(port);
    }

    public bool? GetUseFTP() => _settingsView.GetCurrentSettings().UseFTP;
    public string GetSystemName() => _settingsView.GetSystemName();
    public bool? GetUsePortChecking() => _settingsView.GetCurrentSettings().UsePortChecking;
    public List<RefPortInfo> GetReservedPortList() => _refPortListView.ReservedPortList;
    public List<RefPortInfo> GetProhibitPortList() => _refPortListView.ProhibitPortList;
    public ScanRangeList GetScanRanges() => _settingsView.ScanRanges;
}
