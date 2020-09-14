using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiFiAutoReconnectLib;
using System.Security.Principal;
using System.Diagnostics;

namespace WiFiAutoReconnectLib
{
    public static class Utils
    {
        public static void CheckPermissionsAndLog(LogFile _logFile)
        {
            if (!IsElevated)
            {
                _logFile?.LogWithTimestamp("******   Needs to run as admin.  Aborting.   ******", LogFile.LogLevel.ERROR);
            }
        }
        private static bool IsElevated
        {
            get
            {
                // https://stackoverflow.com/questions/9564420/the-source-was-not-found-but-some-or-all-event-logs-could-not-be-searched
                // https://www.codeproject.com/Articles/105506/Getting-Elevated-Privileges-on-Demand-using-C
                // Note:  even if the user has admin priveledges, they're not active unless you specifcally launch the process as admin.
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void InstallEventLog()
        {
            if (!EventLog.SourceExists(Constants.EventSource))
            {
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.eventlog.createeventsource?view=netframework-4.7.2#System_Diagnostics_EventLog_CreateEventSource_System_String_System_String_
                EventSourceCreationData ecsd = new EventSourceCreationData(Constants.EventSource, Constants.EventLogName)
                {
                    MachineName = Constants.MachineName
                };
                EventLog.CreateEventSource(ecsd);
                LogEvent("Event log created.");
            }
        }

        public static void LogEvent(string eventText, EventLogEntryType type=EventLogEntryType.Information)
        {
            if (EventLog.SourceExists(Constants.EventSource))
            {
                using (EventLog eventLog1 = new EventLog(Constants.EventLogName, Constants.MachineName, Constants.EventSource))
                {
                    eventLog1.WriteEntry(eventText, type);
                }
            }
        }

        public static void UninstallEventLog()
        {
            if (EventLog.SourceExists(Constants.EventSource))
            {
                EventLog.DeleteEventSource(Constants.EventSource);
            }
        }
    }
}
