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
            // Create an instance of EventLog
            System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();

            // Check if the event source exists. If not create it.
            if (!System.Diagnostics.EventLog.SourceExists("NetworkScanner"))
            {
                System.Diagnostics.EventLog.CreateEventSource("NetworkScanner", "Application");
            }

            // Set the source name for writing log entries.
            eventLog.Source = "NetworkScanner";

            // Write an entry to the event log.
            eventLog.WriteEntry(message, evttype,eventid);

            // Close the Event Log
            eventLog.Close();
        }
    }
}
