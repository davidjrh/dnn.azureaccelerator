using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using System.Xml;
using DNNShared;


namespace SMBServer
{
    public class WorkerRole : RoleEntryPoint
    {
        public static string driveLetter = null;
        public static CloudDrive drive = null;

        public override void Run()
        {
            Trace.TraceInformation("SMBServer entry point called");

            while (true)
            {
                Thread.Sleep(10000);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Inits the Diagnostic Monitor
            RoleStartupUtils.InitializeDiagnosticMonitor();            
            
            try
            {
                // Mount the drive
                drive = RoleStartupUtils.MountCloudDrive(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
                                                        RoleEnvironment.GetConfigurationSettingValue("driveContainer"),
                                                        RoleEnvironment.GetConfigurationSettingValue("driveName"),
                                                        RoleEnvironment.GetConfigurationSettingValue("driveSize"));

                // Create a local account for sharing the drive
                RoleStartupUtils.CreateUserAccount(RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"),
                                                   RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword"));

                // Enable SMB traffic through the firewall
                if (RoleStartupUtils.EnableSMBFirewallTraffic() != 0)
                    goto Exit;

                // Share it using SMB (add permissions for RDP user if it's configured)
                string RDPuserName = "";
                try { RDPuserName = RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"); }
                catch { };
                RoleStartupUtils.ShareLocalFolder(RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"),
                                                RDPuserName, drive.LocalPath,
                                                RoleEnvironment.GetConfigurationSettingValue("shareName"));

                // Setup Database Connection string
                RoleStartupUtils.SetupDBConnectionString(drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder") + "\\web.config",
                                                RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString"));
                
                Trace.TraceInformation("Exiting SMB Server OnStart");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Trace.TraceInformation("Exiting");
                //throw;
            }

        Exit:
            return base.OnStart();
        }

        public override void OnStop()
        {
            if (drive != null)
            {
                drive.Unmount();
            }
            base.OnStop();
        }
    }
}