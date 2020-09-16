using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using WiFiAutoReconnectLib;
using WiFiAutoReconnectSvc;

namespace WiFiReconnect_Svc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new WiFiAutoReconnect_Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch(Exception exc)
            {
                using (Logger _logFile = Logger.CreateLogger())
                {
                    _logFile?.LogWithTimestamp(exc.ToString(), Logger.LogLevel.ERROR);
                    throw exc;
                }
            }
        }
    }
}
