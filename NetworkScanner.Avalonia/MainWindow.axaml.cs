using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        TbVersion.Text = "ver. " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown");

        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        Closing += (_, _) => AppLogger.LogInfo("NetworkScanner", "프로그램 종료");

        if (_settingsView.GetCurrentSettings().LoadLatestFileOnStartup)
        {
            _ipListView.GetLatestFilePath(GetSystemName());
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_settingsView.IsInScheduleHour(DateTime.Now.Hour)) return;
        if (DateTime.Now.Minute != 0) return;
        if (_ipListView.IsScanning()) return;

        _ipListView.SchedulingScan();
    }

    private void BtnIPList_Click(object? sender, RoutedEventArgs e) => ContentArea.Content = _ipListView;
    private void BtnSetting_Click(object? sender, RoutedEventArgs e) => ContentArea.Content = _settingsView;
    private void BtnPortInfo_Click(object? sender, RoutedEventArgs e) => ContentArea.Content = _refPortListView;

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
