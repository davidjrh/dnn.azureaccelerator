using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using System.Xml;

namespace DNNShared
{
    public class RoleStartupUtils
    {

        /// <summary>
        /// Mounts the VHD as a local drive
        /// </summary>
        /// <returns>A string with the path of the local mounted drive</returns>
        public static CloudDrive MountCloudDrive(string StorageConnectionString, string driveContainerName, string driveName, string driveSize)
        {
            
            CloudDrive drive = null;
            // Mount the Cloud Drive - Lots of tracing in this part

            Trace.TraceInformation("Mounting cloud drive - Begin");
            Trace.TraceInformation("Mounting cloud drive - Accesing acount info");
            CloudStorageAccount account = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            Trace.TraceInformation("Mounting cloud drive - Locating VHD container:" + driveContainerName);
            CloudBlobContainer driveContainer = blobClient.GetContainerReference(driveContainerName);

            Trace.TraceInformation("Mounting cloud drive - Creating VHD container if not exists");
            //driveContainer.CreateIfNotExist();

            Trace.TraceInformation("Mounting cloud drive - Local cache initialization");
            LocalResource localCache = RoleEnvironment.GetLocalResource("AzureDriveCache");
            CloudDrive.InitializeCache(localCache.RootPath, localCache.MaximumSizeInMegabytes);

            Trace.TraceInformation("Mounting cloud drive - Creating cloud drive");
            drive = new CloudDrive(driveContainer.GetBlobReference(driveName).Uri, account.Credentials);
            try
            {
                drive.Create(int.Parse(driveSize));
            }
            catch (CloudDriveException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }            

            Trace.TraceInformation("Mounting cloud drive - Mount drive");
            string driveLetter = drive.Mount(localCache.MaximumSizeInMegabytes, DriveMountOptions.None);

            return drive;
        }

        /// <summary>
        /// Modifies web.config to setup database connection string
        /// </summary>
        /// <param name="webConfigPath">Path to the web config file</param>
        /// <param name="DatabaseConnectionString">Database connection string</param>
        public static bool SetupDBConnectionString(string webConfigPath, string DatabaseConnectionString)
        {
            bool success = false;
            try
            {
                // Modifiy web.config settings: connection string, appsettings
                Trace.TraceInformation("Modifying web.config settings to set database connection string");
                ConfigXmlDocument webconfig = new ConfigXmlDocument();
                webconfig.Load(webConfigPath);

                XmlNode csNode = webconfig.SelectSingleNode("/configuration/connectionStrings/add[@name='SiteSqlServer']");
                csNode.Attributes["connectionString"].Value = DatabaseConnectionString;

                XmlNode bcNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='SiteSqlServer']");
                bcNode.Attributes["value"].Value = DatabaseConnectionString;

                webconfig.Save(webConfigPath);
                Trace.TraceInformation("Web.config modified successfully");
                success = true;
            }
            catch (Exception ex)
            {
                // Log and continue - Perhaps the website content is not uploaded yet (first time VHD access, paste contents through RDP, for example)
                Trace.TraceWarning("Can not setup database connection string. Error: " + ex.Message);
            }
            return success;
        }

        /// <summary>
        /// Maps a Network Drive (SMB Server Share)
        /// </summary>
        /// <param name="localPath">Drive name</param>
        /// <param name="shareName">Share name on the SMB server</param>
        /// <param name="userName">Username for mapping the network drive</param>
        /// <param name="password">Password for mapping the network drive</param>
        /// <returns>True if the mapping successfull</returns>
        public static bool MapNetworkDrive(bool SMBMode, string localPath, string shareName, string userName, string password)
        {
            int exitCode = 1;

            // The code here mounts the drive shared out by the server worker role
            // The mounted drive contains DotNetNuke contents and published through the
            // service definition file

            string machineIP = null;
            while (exitCode != 0)
            {
                int i = 0;
                string error, output;

                Trace.TraceInformation("Mapping network drive...");
                RoleInstance server;
                if (SMBMode)
                    server = RoleEnvironment.Roles["SMBServer"].Instances[0];
                else
                    server = (from r in RoleEnvironment.Roles["DNNAzure"].Instances
                              where r.Id.EndsWith("_0")
                              select r).FirstOrDefault();

                Trace.TraceInformation("Trying to connect the drive to SMB role instance " + server.Id + ")");

                machineIP = server.InstanceEndpoints["SMB"].IPEndpoint.Address.ToString();
                machineIP = "\\\\" + machineIP + "\\";
                exitCode = ExecuteCommand("net.exe", " use " + localPath + " " + machineIP + shareName + " " + password + " /user:"
                    + userName, out output, out error, 20000);

                if (exitCode != 0)
                {
                    Trace.TraceWarning("Error mapping network drive, retrying in 10 seconds error msg:" + error);
                    // clean up stale mounts and retry 
                    Trace.TraceInformation("DNNAzure - Cleaning up stale mounts...");
                    ExecuteCommand("net.exe", " use " + localPath + "  /delete", out output, out error, 20000);
                    System.Threading.Thread.Sleep(10000);
                    i++;
                    if (i > 100) break;
                }
            }

            if (exitCode == 0)
            {
                Trace.TraceInformation("Success: mapped network drive" + machineIP + shareName);
                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Shares a local folder
        /// </summary>
        /// <param name="userName">Username to share the drive with full access permissions</param>
        /// <param name="RDPuserName">(Optional) Second username to grant full access permissions</param>
        /// <param name="path">Path to share</param>
        /// <param name="shareName">Share name</param>
        /// <returns>Returns 0 if success</returns>
        public static int ShareLocalFolder(string userName, string RDPuserName, string path, string shareName)
        {
            int exitCode;
            string error;
            Trace.TraceInformation("Sharing local folder " + path);
            string GrantRDPUserName = "";
            if (RDPuserName != "")
                GrantRDPUserName = " /Grant:" + RDPuserName + ",full";
            exitCode = ExecuteCommand("net.exe", " share " + shareName + "=" + path + " /Grant:" + userName + ",full" + GrantRDPUserName, out error, 10000);

            if (exitCode != 0)
                //Log error and continue since the drive may already be shared
                Trace.TraceWarning("Error creating fileshare, error msg:" + error, "Warning");
            return exitCode;
        }

        /// <summary>
        /// Enables SMB traffic through the firewall
        /// </summary>
        /// <returns>Returns 0 if success</returns>
        public static int EnableSMBFirewallTraffic()
        {
            int exitCode;
            string error;
            //Enable SMB traffic through the firewall
            Trace.TraceInformation("Enable SMB traffic through the firewall");
            exitCode = ExecuteCommand("netsh.exe", "firewall set service type=fileandprint mode=enable scope=all", out error, 10000);
            if (exitCode != 0)            
                Trace.TraceError("Error setting up firewall, error msg:" + error);
            return exitCode;
        }

        /// <summary>
        /// Creates a user account on the local machine
        /// </summary>
        /// <param name="userName">Name for the user account</param>
        /// <param name="password">Password for the user account</param>
        /// <returns>Returns 0 if success</returns>
        public static int CreateUserAccount(string userName, string password)
        {
            int exitCode;
            string error;

            //Create the user account    
            Trace.TraceInformation("Creating user account for sharing");
            exitCode = ExecuteCommand("net.exe", "user " + userName + " " + password + " /add", out error, 10000);
            if (exitCode != 0)
            {
                //Log error and continue since the user account may already exist
                Trace.TraceWarning("Error creating user account, error msg:" + error);
            }
            return exitCode;
        }


        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="output">Output of the command</param>
        /// <param name="error">Contents of the error results if fails</param>
        /// <param name="timeout">Timeout for executing the command in milliseconds</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string output, out string error, int timeout)
        {
            Process p = new Process();
            int exitCode;
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            error = p.StandardError.ReadToEnd();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeout);
            exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="error">Contents of the error results if fails</param>
        /// <param name="timeout">Timeout for executing the command in milliseconds</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string error, int timeout)
        {
            string output;
            return ExecuteCommand(exe, arguments, out output, out error, timeout);
        }


        /// <summary>
        /// Initializes the DiagnosticMonitor
        /// </summary>
        public static void InitializeDiagnosticMonitor()
        {
            // See for more info http://blog.bareweb.eu/2011/01/implementing-azure-diagnostics-with-sdk-v1-3/

            // Initialize configuration
            string wadConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(wadConnectionString));

            RoleInstanceDiagnosticManager roleInstanceDiagnosticManager = storageAccount.CreateRoleInstanceDiagnosticManager(RoleEnvironment.DeploymentId, RoleEnvironment.CurrentRoleInstance.Role.Name, RoleEnvironment.CurrentRoleInstance.Id);
            DiagnosticMonitorConfiguration config = roleInstanceDiagnosticManager.GetCurrentConfiguration();

            // Setup Windows Azure Logs (Table)
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            // IIS 7.0 Logs (Blob)
            config.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);

            // Windows Infraestructure Diagnostic Infraestructure Logs (Table)
            config.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogLevel.Warning;
            config.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);

            // Failed Request Logs (Blob) ==> See web.config settings and http://www.iis.net/ConfigReference/system.webServer/tracing/traceFailedRequests
            //config.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);

            // Windows Events Logs (Table)
            config.WindowsEventLog.DataSources.Add("System!*"); config.WindowsEventLog.DataSources.Add("Application!*");
            config.WindowsEventLog.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);

            // Performance Counters (Table)
            //PerformanceCounterConfiguration procTimeConfig = new PerformanceCounterConfiguration();procTimeConfig.CounterSpecifier = @"\Processor(*)\% Processor Time";
            //procTimeConfig.SampleRate = System.TimeSpan.FromSeconds(1.0);
            //config.PerformanceCounters.DataSources.Add(procTimeConfig);

            // Crash Dumps (Blob)
            //CrashDumps.EnableCollection(true);

            // Custom error Logs (Table)
            /*LocalResource localResource = RoleEnvironment.GetLocalResource("LogPath");
            config.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(1.0);
            DirectoryConfiguration directoryConfiguration = new DirectoryConfiguration();
            directoryConfiguration.Container = "wad-custom-log-container";
            directoryConfiguration.DirectoryQuotaInMB = localResource.MaximumSizeInMegabytes;
            directoryConfiguration.Path = localResource.RootPath;
            config.Directories.DataSources.Add(directoryConfiguration);*/

            roleInstanceDiagnosticManager.SetCurrentConfiguration(config);

            Trace.TraceInformation("Diagnostics Setup complete");
        }

    }
}
