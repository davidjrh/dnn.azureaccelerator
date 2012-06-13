using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using DNNShared;


namespace SMBServer
{
    public class WorkerRole : RoleEntryPoint
    {
        public static string DriveLetter;
        public static CloudDrive Drive;

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
            RoleStartupUtils.ConfigureDiagnosticMonitor();            
            
            try
            {
                // Mount the drive
                Drive = RoleStartupUtils.MountCloudDrive(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
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
                string rdpUserName = "";
                try { rdpUserName = RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"); }
                catch
                {
                    Trace.TraceWarning("No RDP user name was specified. Consider enabling RDP for better maintenance options.");
                }
                // The cloud drive can't be shared if it is running on Windows Azure Compute Emulator. 
                if (!RoleEnvironment.IsEmulated)
                    RoleStartupUtils.ShareLocalFolder(RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"),
                                                    rdpUserName, Drive.LocalPath,
                                                    RoleEnvironment.GetConfigurationSettingValue("shareName"));

                // Check for the database existence
                Trace.TraceInformation("Checking for database existence...");
                if (!RoleStartupUtils.SetupDatabase(RoleEnvironment.GetConfigurationSettingValue("DBAdminUser"),
                                            RoleEnvironment.GetConfigurationSettingValue("DBAdminPassword"),
                                            RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString")))
                    Trace.TraceError("Error while setting up the database. Check previous messages.");

                // Check for the creation of the Website contents from Azure storage
                Trace.TraceInformation("Check for website content...");
                if (!RoleStartupUtils.SetupWebSiteContents(Drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder"),
                                                        RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
                                                        RoleEnvironment.GetConfigurationSettingValue("packageContainer"),
                                                        RoleEnvironment.GetConfigurationSettingValue("package"),
                                                        RoleEnvironment.GetConfigurationSettingValue("packageUrl")))
                    Trace.TraceError("Website content could not be prepared. Check previous messages.");


                // Setup Database Connection string
                RoleStartupUtils.SetupWebConfig(Drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder") + "\\web.config",
                                                RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString"),
                                                RoleEnvironment.GetConfigurationSettingValue("InstallationDate"));

                // Setup DotNetNuke.install.config
                RoleStartupUtils.SetupInstallConfig(
                                    Path.Combine(new[]
                                                         {
                                                             Drive.LocalPath, RoleEnvironment.GetConfigurationSettingValue("dnnFolder"),
                                                             "Install\\DotNetNuke.install.config"
                                                         }),
                                    RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
                                    RoleEnvironment.GetConfigurationSettingValue("packageContainer"),
                                    RoleEnvironment.GetConfigurationSettingValue("packageInstallConfiguration"));

                // Setup post install addons (always overwrite)
                RoleStartupUtils.InstallAddons(RoleEnvironment.GetConfigurationSettingValue("AddonsUrl"),
                                                Drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder"));

                
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
            if (Drive != null)
                Drive.Unmount();
            base.OnStop();
        }
    }
}