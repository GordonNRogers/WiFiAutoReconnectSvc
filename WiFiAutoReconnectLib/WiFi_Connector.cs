﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Data;

namespace WiFiAutoReconnectLib
{
    public class WiFi_Connector : IDisposable
    {
        // "Device Name" in "Control Panel\Network and Internet\Network Connections"
        // Matches up to NetworkInterface.Description
        private string[] wifiAdapterNames = {};
        private string[] ethernetAdapterNames = { };
        private string ssid = "Rogers2";
        private string baseLogFileName = "WiFiConnect";
        private Thread runThread = null;
        private AutoResetEvent shutdownRequest = new AutoResetEvent(false);
        private readonly TimeSpan checkInterval;
        private int numSecondsBetweenChecks = (60 * 5);
        private bool connectWiFiWhenEthernetActive = false;
        private int daysToKeepLogs = 5;
        private LogFile _logFile = null;
        private LogFile.LogLevel fileLogLevel = LogFile.LogLevel.INFO;
        private LogFile.LogLevel eventLogLevel = LogFile.LogLevel.INFO;

        public LogFile LogFile { get { return _logFile; } }
        public EventHandler OnComplete = onCompleteDefault;

        public WiFi_Connector(EventLog eventLog=null)
        {
            checkInterval = new TimeSpan(0, 0, numSecondsBetweenChecks); // read-only, so must be assigned in the constructor
            initialize();
        }

        ~WiFi_Connector()
        {
            this.Dispose();
        }

        private void initialize()
        {
            try
            {
                // load settings from app.config
                string errors = "";
                try
                {
                    baseLogFileName = ConfigurationManager.AppSettings["LogFileName"];
                    daysToKeepLogs = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToKeepLogs"]);

                    string sFileLogLevel = ConfigurationManager.AppSettings["FileLogLevel"];
                    string sEventlogLevel = ConfigurationManager.AppSettings["EventLogLevel"];
                    if (!Enum.TryParse(sFileLogLevel, true, out fileLogLevel))
                        errors += "Config file error: invalid FileLogLevel: " + sFileLogLevel;
                    if(!Enum.TryParse(sEventlogLevel, true, out eventLogLevel))
                        errors += "Config file error: invalid EventLogLevel: " + sEventlogLevel;

                    // CONSIDER:  file logging, event logging
                    //  Should these be split up? (seperation of responsibilities)
                    //  Is this a good place to use dependancy injection?
                    _logFile = new LogFile(baseLogFileName, daysToKeepLogs, fileLogLevel, eventLogLevel);
                }
                catch (Exception exc)
                {
                    errors += exc.ToString();
                    baseLogFileName = "WiFi_Connector_Error";
                    daysToKeepLogs = 3;
                    _logFile = new LogFile(baseLogFileName, daysToKeepLogs);
                }

                if(errors.Length>0)
                {
                    _logFile?.LogWithTimestamp(errors, LogFile.LogLevel.ERROR);
                }

                ssid = ConfigurationManager.AppSettings["SSID"];
                numSecondsBetweenChecks = Convert.ToInt32(ConfigurationManager.AppSettings["NumSecondsBetweenChecks"]);
                connectWiFiWhenEthernetActive = Convert.ToBoolean(ConfigurationManager.AppSettings["ConnectWiFiWhenEthernetActive"]);

                // https://stackoverflow.com/questions/1779117/how-to-get-a-liststring-collection-of-values-from-app-config-in-wpf
                // type has to implement System.Configuration.IConfigurationSectionHandler
                AdapterList wfiAdapters = ConfigurationManager.GetSection("WiFiAdapters") as AdapterList;
                wifiAdapterNames = wfiAdapters.Adapters;

                AdapterList ethernetAdapters = ConfigurationManager.GetSection("EthernetAdapters") as AdapterList;
                ethernetAdapterNames = ethernetAdapters.Adapters;

            }
            catch (Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
            }


        }

        public void Dispose()
        {
            _logFile?.Dispose();
        }

        public void Start()
        {
            try
            {
                // start a thread that will periodically invoke DoConnect()
                ParameterizedThreadStart pts = new ParameterizedThreadStart(threadFunc);
                runThread = new Thread(pts);
                runThread.IsBackground = true;
                runThread.Start(this);
            }
            catch(Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
            }
        }

        private static void threadFunc(object obj)
        {
            WiFi_Connector wc = obj as WiFi_Connector;
            Debug.Assert(wc != null);

            do
            {
                wc.DoConnect();
            } while (!wc.shutdownRequest.WaitOne(wc.checkInterval));
            wc._logFile?.LogWithTimestamp("End of threadFunc.", LogFile.LogLevel.DIAGNOSTIC);
        }

        public void Stop()
        {
            TimeSpan timeout = new TimeSpan(0, 0, 30);
            // signal the thread to stop...if it times out, abort it
            try
            {
                _logFile?.LogWithTimestamp("Stop() entered.", LogFile.LogLevel.DIAGNOSTIC);

                shutdownRequest.Set();

                _logFile?.LogWithTimestamp("Joining thread.", LogFile.LogLevel.DIAGNOSTIC);
                if (runThread.Join(timeout))
                {
                    _logFile?.LogWithTimestamp("Thread exited.", LogFile.LogLevel.DIAGNOSTIC);
                }
                else
                {
                    _logFile?.LogWithTimestamp("Aborting thread.", LogFile.LogLevel.WARNING);

                    // _= tells the compiler the result is disposable
                    _ = Task.Run(() =>
                      {
                          _logFile?.LogWithTimestamp("Abort task started.", LogFile.LogLevel.DIAGNOSTIC);
                          runThread?.Abort();
                          _logFile?.LogWithTimestamp("Thread aborted.", LogFile.LogLevel.WARNING);
                      }).ConfigureAwait(false);

                    _logFile?.LogWithTimestamp("Thread abandoned.", LogFile.LogLevel.DIAGNOSTIC);
                }
            }
            catch (Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
            }
        }

        private static void onCompleteDefault(object obj, EventArgs e)
        {
            // just a placeholder so the event can be invoked without checking for null
        }


        private void DoConnect()
        {
            _logFile?.LogWithTimestamp("Enter DoConnect()", LogFile.LogLevel.INFO);

            lock (this)
            {
                List<NetworkInterface> apaptersToConnect = new List<NetworkInterface>(); // list of adapters to be connected
                bool ethernetConnected = false;

                try
                {
                    NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface ni in networkInterfaces)
                    {
                        // if an ethernet adapter is connected, don't connect to wifi
                        if (!connectWiFiWhenEthernetActive
                            && ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                            && ethernetAdapterNames.Contains(ni.Description))
                        {
                            _logFile?.LogWithTimestamp(string.Format("Ethernet Controller:  {0} ({1}): {2}",
                                ni.Description,
                                ni.Name,
                                ni.OperationalStatus.ToString()), 
                                LogFile.LogLevel.INFO);

                            if (ni.OperationalStatus == OperationalStatus.Up)
                            {
                                ethernetConnected = true;
                                _logFile?.LogWithTimestamp("Eternet adapter connected, not connecting to wifi.", LogFile.LogLevel.INFO);
                                break;
                            }
                        }

                        // otherwise, if the adapeter is on the list to check and not connected, add it to the apaptersToConnect list.
                        if (wifiAdapterNames.Contains(ni.Description))
                        {
                            _logFile?.LogWithTimestamp(string.Format("WiFi Adapter Status - {0} ({1}): {2}",
                                ni.Description,
                                ni.Name,
                                ni.OperationalStatus.ToString()),
                                LogFile.LogLevel.INFO);

                            if (ni.OperationalStatus == OperationalStatus.Down)
                            {
                                apaptersToConnect.Add(ni);
                            }
                        }
                    }

                    if (!ethernetConnected)
                    {
                        foreach (NetworkInterface ni in apaptersToConnect)
                        {
                            // https://www.windowscentral.com/how-connect-wi-fi-network-windows-10#connect_wifi_cmd
                            // netsh wlan connect name="Rogers2" interface="Wi-Fi"
                            //string cmd = string.Format("netsh wlan connect name=\"{0}\" interface=\"{1}\"", profile, ni.Name);
                            //System.Diagnostics.Debugger.Break();

                            _logFile?.LogWithTimestamp(string.Format("Enabling controller: {0} ({1})",
                                ni.Description,
                                ni.Name),
                                LogFile.LogLevel.INFO);

                            string procParams = string.Format("wlan connect name=\"{0}\" interface=\"{1}\"", ssid, ni.Name);
                            ProcessStartInfo psi = new ProcessStartInfo("netsh.exe", procParams);
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = false;
                            psi.RedirectStandardOutput = true;
                            Process p = Process.Start(psi);
                            p.WaitForExit();
                            string result = p.StandardOutput.ReadToEnd();
                            _logFile?.LogWithTimestamp(result, LogFile.LogLevel.INFO);
                        }

                    }
                }
                catch (Exception exc)
                {
                    _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
                }
                finally
                {
                    try
                    {
                        // fire the OnComplete event
                        _logFile?.LogWithTimestamp("Before OnComplete()", LogFile.LogLevel.DIAGNOSTIC);
                        OnComplete(this, new EventArgs());
                        GC.Collect();  // force a garbage collection to keep the footprint as small as possible
                        _logFile?.LogWithTimestamp("After OnComplete()", LogFile.LogLevel.DIAGNOSTIC);
                    }
                    catch (Exception exc)
                    {
                        _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
                    }
                }
            } // lock(this)

            _logFile?.LogWithTimestamp("Exit DoConnect()", LogFile.LogLevel.DIAGNOSTIC);
        }

    }
}
