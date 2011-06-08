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


namespace SMBServer
{
    public class WorkerRole : RoleEntryPoint
    {
        public static string driveLetter = null;
        public static CloudDrive drive = null;

        public override void Run()
        {
            Trace.WriteLine("SMBServer entry point called", "Information");

            while (true)
            {
                Thread.Sleep(10000);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Initialize logging and tracing
            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);
            Trace.WriteLine("Diagnostics Setup complete", "Information");
            
            try
            {
                // Mount the Cloud Drive - Lots of tracing in this part

                Trace.WriteLine("Mounting cloud drive - Begin", "Information");
                Trace.WriteLine("Mounting cloud drive - Accesing acount info", "Information");
                CloudStorageAccount account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"));                
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                Trace.WriteLine("Mounting cloud drive - Locating VHD container:" + RoleEnvironment.GetConfigurationSettingValue("driveContainer"), "Information");
                CloudBlobContainer driveContainer = blobClient.GetContainerReference(RoleEnvironment.GetConfigurationSettingValue("driveContainer"));
                Trace.WriteLine("Mounting cloud drive - Creating VHD container if not exists", "Information");
                //driveContainer.CreateIfNotExist();
                Trace.WriteLine("Mounting cloud drive - Get drive Name", "Information");
                String driveName = RoleEnvironment.GetConfigurationSettingValue("driveName");
                Trace.WriteLine("Mounting cloud drive - Local cache initialization", "Information");
                LocalResource localCache = RoleEnvironment.GetLocalResource("AzureDriveCache");
                CloudDrive.InitializeCache(localCache.RootPath, localCache.MaximumSizeInMegabytes);

                Trace.WriteLine("Mounting cloud drive - Creating cloud drive", "Information");
                drive = new CloudDrive(driveContainer.GetBlobReference(driveName).Uri, account.Credentials);
                try
                {
                    drive.Create(int.Parse(RoleEnvironment.GetConfigurationSettingValue("driveSize")));
                }
                catch (CloudDriveException ex)
                {
                    Trace.WriteLine(ex.ToString(), "Warning");
                }

                Trace.WriteLine("Mounting cloud drive - Mount drive", "Information");
                driveLetter = drive.Mount(localCache.MaximumSizeInMegabytes, DriveMountOptions.None);

                // Get share settings
                string userName = RoleEnvironment.GetConfigurationSettingValue("fileshareUserName");
                string password = RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword");
                string RDPuserName = "";
                try { RDPuserName = RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"); }
                catch { };
                

                // Modify path to share a specific directory on the drive
                string path = driveLetter;
                string shareName = RoleEnvironment.GetConfigurationSettingValue("shareName");
                int exitCode;
                string error;

                //Create the user account    
                Trace.WriteLine("Creating user account for sharing", "Information");
                exitCode = ExecuteCommand("net.exe", "user " + userName + " " + password + " /add", out error, 10000);
                if (exitCode != 0)
                {
                    //Log error and continue since the user account may already exist
                    Trace.WriteLine("Error creating user account, error msg:" + error, "Warning");
                }

                //Enable SMB traffic through the firewall
                Trace.WriteLine("Enable SMB traffic through the firewall", "Information");
                exitCode = ExecuteCommand("netsh.exe", "firewall set service type=fileandprint mode=enable scope=all", out error, 10000);
                if (exitCode != 0)
                {
                    Trace.WriteLine("Error setting up firewall, error msg:" + error, "Error");
                    goto Exit;
                }

#if DEBUG
 /*               // While in development mode, the cloud drive can't be shared, so be sure to 
                // store the VHD content on a development location                
                if (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString") == "UseDevelopmentStorage=true")
                    path = "C:\\development\\VHD.content";
  */
#endif 

                //Share the drive
                Trace.WriteLine("Share the drive", "Information");
                string GrantRDPUserName = "";
                if (RDPuserName != "")
                    GrantRDPUserName = " /Grant:" + RDPuserName + ",full";
                exitCode = ExecuteCommand("net.exe", " share " + shareName + "=" + path + " /Grant:"
                    + userName + ",full" + GrantRDPUserName, out error, 10000);

                if (exitCode != 0)
                {
                    //Log error and continue since the drive may already be shared
                    Trace.WriteLine("Error creating fileshare, error msg:" + error, "Warning");
                }

                // Modifiy web.config settings: connection string, appsettings
                Trace.WriteLine("Modify web.config settings", "Information");
                string DBConnectionString = RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString");
                ConfigXmlDocument webconfig = new ConfigXmlDocument();
                webconfig.Load(path + "DotNetNuke\\web.config");
                XmlNode csNode = webconfig.SelectSingleNode("/configuration/connectionStrings/add[@name='SiteSqlServer']");
                csNode.Attributes["connectionString"].Value = DBConnectionString;
                XmlNode bcNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='SiteSqlServer']");
                bcNode.Attributes["value"].Value = DBConnectionString;
                webconfig.Save(path + "DotNetNuke\\web.config");

                Trace.WriteLine("Exiting SMB Server OnStart", "Information");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error");
                Trace.WriteLine("Exiting", "Information");
                //throw;
            }

        Exit:
            return base.OnStart();
        }

        public static string GetValue(string[] PropCollection, string PropName)
        {
            var Value = from s in PropCollection
                        where s.Split('=')[0].ToLower() == PropName
                        select s;
            return Value.FirstOrDefault().ToString().Split('=')[1];
        }

        public static int ExecuteCommand(string exe, string arguments, out string error, int timeout)
        {            
            Process p = new Process();
            int exitCode;
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            error = p.StandardError.ReadToEnd();
            p.WaitForExit(timeout);
            exitCode = p.ExitCode;
            p.Close();

            return exitCode;
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