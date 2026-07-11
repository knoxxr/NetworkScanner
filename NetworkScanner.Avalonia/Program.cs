using Avalonia;
using System;
using Velopack;

namespace NetworkScanner.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack이 패키지(설치/제거/업데이트 등 OS별 라이프사이클 이벤트)를 올바르게 처리하려면
        // 다른 초기화보다 먼저 호출되어야 한다. 자동 업데이트 기능을 아직 사용하지 않더라도,
        // vpk로 패키징된 실행 파일이 정상 동작하려면(특히 Windows 설치 시) 반드시 필요하다.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
