using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WiFiAutoReconnectLib
{
    public class LogFile : IDisposable
    {
        public enum LogLevel { DIAGNOSTIC, INFO, WARNING, ERROR };  // from most important to least important

        private TextWriter logWriter = null;
        LogLevel maxFileLogLevel = LogLevel.INFO;
        LogLevel maxEventLogLevel = LogLevel.INFO;

        public LogFile(string baseName, int daysToKeep, LogLevel MaxFileLogLevel = LogLevel.INFO, LogLevel MaxEventLogLevel = LogLevel.INFO)
        {
            maxFileLogLevel = MaxFileLogLevel;
            maxEventLogLevel = MaxEventLogLevel;

            try
            {
                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                DirectoryInfo cdi = new DirectoryInfo(path);
                // build the date into the file name
                string dateStamp = DateTime.Now.ToString("MM_dd_yyyy");
                string logFileName = string.Format("{0}\\{1}_{2}.log", path, baseName, dateStamp);
                Console.WriteLine("Createing log file: " + logFileName);
                logWriter = new StreamWriter(logFileName, true, Encoding.UTF8) as TextWriter;

                // select all files matching the pattern and delete any that are older than max keep days
                string pattern = string.Format("{0}_*.log", baseName);
                var today = DateTime.Now.Date;
                var files = cdi.GetFiles(pattern).Where(f => today.Subtract(f.LastWriteTime.Date).Days > daysToKeep);
                foreach (FileInfo fi in files)
                {
                    fi.Delete();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }


        }

        public void Dispose()
        {
            if (logWriter != null)
            { 
                try
                {
                    logWriter.Close();
                }
                catch (Exception exc)
                {
#if DEBUG
                    Debug.WriteLine(exc.ToString());
#endif
                }
                finally
                {
                    logWriter = null;
                }
            }
        }

        public void LogWithTimestamp(string text, LogLevel lvl)
        {
            // log the current time
            string logType = Enum.GetName(typeof(LogLevel), lvl);
            string timeStamp = DateTime.Now.ToString("[MM/dd/yyyy HH:mm:ss");
            string sLogMessage = string.Format("{0} [{1}]: {2}", timeStamp, logType, text).Trim(); ;
#if DEBUG
            Debug.WriteLine(sLogMessage);
#endif
            if (lvl >= maxFileLogLevel)
            {
                if (logWriter != null)
                {
                    lock (logWriter)
                    {
                        logWriter.WriteLine(sLogMessage);
                        logWriter.Flush();
                    }
                }
                else
                {
                    Console.WriteLine(sLogMessage);
                }
            }

            if (lvl >= maxEventLogLevel)
            {
                switch (lvl)
                {
                    case LogLevel.DIAGNOSTIC:
                        Utils.LogEvent(text, EventLogEntryType.Information);
                        break;
                    case LogLevel.INFO:
                        Utils.LogEvent(text, EventLogEntryType.Information);
                        break;
                    case LogLevel.ERROR:
                        Utils.LogEvent(text, EventLogEntryType.Error);
                        break;
                    case LogLevel.WARNING:
                        Utils.LogEvent(text, EventLogEntryType.Warning);
                        break;
                    default:
                        string errorMsg = "Unhandled event type: " + lvl.ToString();
                        Console.WriteLine(sLogMessage);
                        Utils.LogEvent(errorMsg, EventLogEntryType.Information);
                        Utils.LogEvent(text, EventLogEntryType.Information);
#if DEBUG
                        if (Debugger.IsAttached) 
                            Debugger.Break();
#endif
                        break;
                }
            }
        }

    }
}
