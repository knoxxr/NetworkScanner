using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    internal class EventLogger
    {
        private const string FallbackLogFileName = "networkscanner.log";

        public static void WriteEventLogEntry(string message, EventLogEntryType evttype, int eventid = 0)
        {
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists(MainNetworkScanner.ProgramName))
                {
                    System.Diagnostics.EventLog.CreateEventSource(MainNetworkScanner.ProgramName, "Application");
                }

                using System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();
                eventLog.Source = MainNetworkScanner.ProgramName;
                eventLog.WriteEntry(message, evttype, eventid);
            }
            catch
            {
                // 관리자 권한이 없어 이벤트 소스를 생성/기록할 수 없는 경우 파일로 대체 기록한다.
                WriteFallbackLogEntry(message, evttype);
            }
        }

        private static void WriteFallbackLogEntry(string message, EventLogEntryType evttype)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FallbackLogFileName);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{evttype}] {message}{Environment.NewLine}";
                File.AppendAllText(path, line);
            }
            catch
            {
                // 로깅 실패가 애플리케이션을 중단시켜서는 안 된다.
            }
        }
    }
}
