using System;
using System.Diagnostics;
using System.IO;

namespace NetworkScanner
{
    // Windows에서는 이벤트 로그를, 그 외 플랫폼에서는 실행 파일 옆의 로그 파일을 사용하는
    // 크로스플랫폼 로거. WPF 앱은 기존 EventLogger를 그대로 쓰고, Avalonia 앱은 이것을 사용한다.
    public static class AppLogger
    {
        private const string FallbackLogFileName = "networkscanner.log";

        public static void LogError(string source, string message)
        {
            Log(source, message, EventLogEntryType.Error);
        }

        public static void LogInfo(string source, string message)
        {
            Log(source, message, EventLogEntryType.Information);
        }

        public static void Log(string source, string message, EventLogEntryType entryType)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (!EventLog.SourceExists(source))
                    {
                        EventLog.CreateEventSource(source, "Application");
                    }

                    using EventLog eventLog = new EventLog { Source = source };
                    eventLog.WriteEntry(message, entryType);
                    return;
                }
            }
            catch
            {
                // 이벤트 로그 기록에 실패하면 아래 파일 폴백으로 넘어간다.
            }

            WriteFallbackLogEntry(message, entryType);
        }

        private static void WriteFallbackLogEntry(string message, EventLogEntryType entryType)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FallbackLogFileName);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{entryType}] {message}{Environment.NewLine}";
                File.AppendAllText(path, line);
            }
            catch
            {
                // 로깅 실패가 애플리케이션을 중단시켜서는 안 된다.
            }
        }
    }
}
