using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace NetworkScanner.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // NetworkScanner.Core는 플랫폼 종속적인 로깅에 의존하지 않으므로,
        // 실제 로깅 대상(AppLogger: Windows 이벤트 로그/폴백 파일)은 호스트 앱이 여기서 연결한다.
        const string LogSource = "NetworkScanner";
        OUIInfo.OnError = message => AppLogger.LogError(LogSource, message);
        ArpResolver.OnError = message => AppLogger.LogError(LogSource, message);
        PingTester.OnError = message => AppLogger.LogError(LogSource, message);
        FTPService.OnError = message => AppLogger.LogError(LogSource, message);
        PortReferenceLoader.OnError = message => AppLogger.LogError(LogSource, message);
        AppSettingsStore.OnError = message => AppLogger.LogError(LogSource, message);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}