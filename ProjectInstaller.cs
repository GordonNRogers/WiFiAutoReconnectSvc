using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using WiFiAutoReconnectLib;

namespace WiFiAutoReconnectSvc
{
    // http://www.herongyang.com/Windows/Service-Controller-Command-Line-Tool-sc-exe.html

    // https://docs.microsoft.com/en-us/dotnet/api/system.configuration.install.installer?view=netframework-4.7.2&f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.Configuration.Install.Installer);k(TargetFrameworkMoniker-.NETFramework,Version%253Dv4.7.2);k(DevLang-csharp)%26rd%3Dtrue
    // https://docs.microsoft.com/en-us/dotnet/framework/windows-services/how-to-add-installers-to-your-service-application
    // https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer



    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        // The event log is installed by the service so no need to do it here, but the uninstall method needs to remove it.
        private LogFile _logFile;

        public ProjectInstaller()
        {
            try
            {
                _logFile = new LogFile("ProjectInstaller", 1, LogFile.LogLevel.DIAGNOSTIC);
                InitializeComponent();

                // set string values from Constants so they're all in one place
                serviceInstaller.Description = Constants.Description;
                serviceInstaller.DisplayName = Constants.DisplayName;
                serviceInstaller.ServiceName = Constants.ServiceName;
            }
            catch (Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
                throw exc;
            }
        }
        ~ProjectInstaller()
        {
            _logFile?.Dispose();
        }

        public override void Install(IDictionary stateSaver)
        {
            try
            {
                _logFile?.LogWithTimestamp("+ProjectInstaller.Install()", LogFile.LogLevel.DIAGNOSTIC);
                Utils.UninstallEventLog();
                Utils.InstallEventLog();
                base.Install(stateSaver);
            }
            catch (Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
                throw exc;
            }
            finally
            {
                Utils.CheckPermissionsAndLog(_logFile);
                _logFile?.LogWithTimestamp("-ProjectInstaller.Install()", LogFile.LogLevel.DIAGNOSTIC);
            }
        }


        public override void Uninstall(IDictionary savedState)
        {
            _logFile?.LogWithTimestamp("+ProjectInstaller.Uninstall()", LogFile.LogLevel.DIAGNOSTIC);
            try
            {
                base.Uninstall(savedState);
                Utils.UninstallEventLog();
            }
            catch (Exception exc)
            {
                _logFile?.LogWithTimestamp(exc.ToString(), LogFile.LogLevel.ERROR);
                throw exc;
            }
            finally
            {
                Utils.CheckPermissionsAndLog(_logFile);
                _logFile?.LogWithTimestamp("-ProjectInstaller.Uninstall()", LogFile.LogLevel.DIAGNOSTIC);
            }
        }

    }
}
