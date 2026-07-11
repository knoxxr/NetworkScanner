using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkScanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // NetworkScanner.Core는 플랫폼 종속적인 로깅에 의존하지 않으므로,
            // 실제 로깅 대상(Windows 이벤트 로그/폴백 파일)은 호스트 앱이 여기서 연결한다.
            OUIInfo.OnError = message => EventLogger.WriteEventLogEntry(message, EventLogEntryType.Error);
            ArpResolver.OnError = message => EventLogger.WriteEventLogEntry(message, EventLogEntryType.Error);
            PingTester.OnError = message => EventLogger.WriteEventLogEntry(message, EventLogEntryType.Error);
            FTPService.OnError = message => EventLogger.WriteEventLogEntry(message, EventLogEntryType.Error);
            PortReferenceLoader.OnError = message => EventLogger.WriteEventLogEntry(message, EventLogEntryType.Error);
        }
    }
}
