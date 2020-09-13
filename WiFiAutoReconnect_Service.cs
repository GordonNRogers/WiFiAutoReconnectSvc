﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WiFiAutoReconnectLib;

namespace WiFiAutoReconnectSvc
{
    public partial class WiFiAutoReconnect_Service : ServiceBase
    {
        private WiFi_Connector connector = null;

        public WiFiAutoReconnect_Service()
        {
            try
            {
                InitializeComponent();
                this.ServiceName = Constants.ServiceName;

                Utils.InstallEventLog();
            }
            catch (Exception exc)
            {
                Utils.LogEvent(Constants.ServiceName + " Error: " + exc.ToString(), EventLogEntryType.Error);
                throw exc;
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
#if DEBUG
                connector = new WiFi_Connector(LogFile.LogLevel.DIAGNOSTIC);
#else
                connector = new WiFi_Connector(LogFile.LogLevel.INFO);
#endif
                connector.Start();
                Utils.LogEvent(Constants.ServiceName + " started.");
            }
            catch (Exception exc)
            {
                Utils.LogEvent(Constants.ServiceName + " Error: " + exc.ToString(), EventLogEntryType.Error);
                throw exc;
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (connector != null)
                {
                    connector.Stop();
                    connector.Dispose();
                    connector = null;
                }
                Utils.LogEvent(Constants.ServiceName + " stopped.");
            }
            catch (Exception exc)
            {
                Utils.LogEvent(Constants.ServiceName + " Error: " + exc.ToString(), EventLogEntryType.Error);
                throw exc;
            }
        }

    }
}
