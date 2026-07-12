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
    private readonly DispatcherTimer _timer = new();

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

        Closing += (_, _) => AppLogger.LogInfo("NetworkScanner", "프로그램 종료");

        if (_settingsView.GetCurrentSettings().LoadLatestFileOnStartup)
        {
            _ipListView.GetLatestFilePath(GetSystemName());
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

    // 사이드바에서 지금 어느 화면을 보고 있는지 좌측 강조선 + 배경 틴트로 표시한다.
    private void SetActiveNav(Button active)
    {
        var accent = (IBrush)Resources["AccentBrush"]!;
        var activeBg = (IBrush)Resources["SideActiveBackgroundBrush"]!;
        var inactiveBg = (IBrush)Resources["SideBackgroundBrush"]!;

        foreach (Button nav in new[] { BtnIPList, BtnSetting, BtnPortInfo })
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
