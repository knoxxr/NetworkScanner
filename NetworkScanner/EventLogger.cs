using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    internal class EventLogger
    {
        public static void WriteEventLogEntry(string message, EventLogEntryType evttype, int eventid = 0)
        {
            System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();

            if (!System.Diagnostics.EventLog.SourceExists("NetworkScanner"))
            {
                System.Diagnostics.EventLog.CreateEventSource(MainNetworkScanner.ProgramName, "Application");
            }

            eventLog.Source = MainNetworkScanner.ProgramName;

            eventLog.WriteEntry(message, evttype,eventid);

            eventLog.Close();
        }
    }
}
