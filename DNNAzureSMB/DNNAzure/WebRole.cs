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
using Microsoft.Web.Administration;


namespace DNNAzure
{
    public class WebRole : RoleEntryPoint
    {
        public const int tenSecondsAsMS = 10000;

        public override bool OnStart()
        {

            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);
            Trace.WriteLine("DNNAzure Diagnostics Setup complete", "Information");

            // The code here mounts the drive shared out by the server worker role
            // The mounted drive contains DotNetNuke contents and published through the
            // service definition file

            Trace.WriteLine("DNNAzure initialization", "Information");
            string localPath = RoleEnvironment.GetConfigurationSettingValue("localPath");
            string dnnFolder = RoleEnvironment.GetConfigurationSettingValue("dnnFolder");
            string shareName = RoleEnvironment.GetConfigurationSettingValue("shareName");
            string userName = RoleEnvironment.GetConfigurationSettingValue("fileshareUserName");
            string password = RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword");
            string hostHeaders = RoleEnvironment.GetConfigurationSettingValue("hostHeaders");
            string managedRuntimeVersion = RoleEnvironment.GetConfigurationSettingValue("managedRuntimeVersion");
            string managedPipelineMode = RoleEnvironment.GetConfigurationSettingValue("managedPipelineMode");


            // Map the network drive
            try
            {
                if (!MapNetworkDrive(localPath, shareName, userName, password))
                    Trace.WriteLine("Failed to map network drive " + shareName, "Error");
                //TODO: Create a thread for checking that the NetworkDrive has not been disconnected (SMB Server Fails) for trying to reconnecting to the new instance                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error");
                throw;
            }
            
            
            //Create the user account    
            Trace.WriteLine("Creating user account for sharing", "Information");
            string error, output = "";
            int exitCode = ExecuteCommand("net.exe", "user " + userName + " " + password + " /add", out output, out error, 10000);
            if (exitCode != 0)
            {
                //Log error and continue since the user account may already exist
                Trace.WriteLine("Error creating user account, error msg:" + error, "Warning");
            }

            // Create the DNN Web site
            try
            {
                if (!CreateDNNWebSite(hostHeaders, localPath + "\\" + dnnFolder, userName, password, managedRuntimeVersion, managedPipelineMode))
                    Trace.WriteLine("Failed to create the DNNWebSite", "Error");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error");
                throw;
            }

            return base.OnStart();
        }

#region Private functions
        /// <summary>
        /// Maps a Network Drive (SMB Server Share)
        /// </summary>
        /// <param name="localPath">Drive name</param>
        /// <param name="shareName">Share name on the SMB server</param>
        /// <param name="userName">Username for mapping the network drive</param>
        /// <param name="password">Password for mapping the network drive</param>
        /// <returns>True if the mapping successfull</returns>
        public static bool MapNetworkDrive(string localPath, string shareName, string userName, string password)
        {
            int exitCode = 1;

            string machineIP = null;
            while (exitCode != 0)
            {
                int i = 0;
                string error, output;

                Trace.WriteLine("DNNAzure - Mapping network drive...", "Information");
                var server = RoleEnvironment.Roles["SMBServer"].Instances[0];
                machineIP = server.InstanceEndpoints["SMB"].IPEndpoint.Address.ToString();
                machineIP = "\\\\" + machineIP + "\\";
                exitCode = ExecuteCommand("net.exe", " use " + localPath + " " + machineIP + shareName + " " + password + " /user:"
                    + userName, out output, out error, 20000);

                if (exitCode != 0)
                {
                    Trace.WriteLine("Error mapping network drive, retrying in 10 seconds error msg:" + error, "Warning");
                    // clean up stale mounts and retry 
                    Trace.WriteLine("DNNAzure - Cleaning up stale mounts...", "Information");
                    ExecuteCommand("net.exe", " use " + localPath + "  /delete", out output, out error, 20000);
                    Thread.Sleep(10000);
                    i++;
                    if (i > 100) break;
                }
            }

            if (exitCode == 0)
            {
                Trace.WriteLine("DNNAzure - Success: mapped network drive" + machineIP + shareName, "Information");
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Creates a DNNWebSite listening for hostHeaders
        /// </summary>
        /// <param name="hostHeaders">Host headers for bindings</param>
        /// <param name="HomePath">Home path of the DNN portal (on the SMB share)</param>
        /// <returns></returns>
        public static bool CreateDNNWebSite(string hostHeaders, string HomePath, string userName, string password, string managedRuntimeVersion, string managedPipelineMode)
        {
            //int exitCode = 1;
            string systemDrive = Environment.SystemDirectory.Substring(0, 2);

            Trace.WriteLine("Creating DNNWebSite with hostHeaders '" + hostHeaders + "'...", "Information");

            string originalwebSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
            string webSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_DotNetNuke";
            string[] Headers = hostHeaders.Split(';');
            string protocol = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].Protocol;
            string port = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].IPEndpoint.Port.ToString();

            string bindings = protocol + "://" + string.Join(":" + port + "," + protocol + "://", Headers) + ":" + port;

            Trace.WriteLine("Calculated bindings are: " + bindings, "Information");
            //string error, output;
            
            
            // Creates the DNN WebSite 
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var site = serverManager.Sites[webSiteName];
                    if (site == null)
                    {
                        site = serverManager.Sites.Add(webSiteName, protocol, "*:" + port + ":" + Headers[0], HomePath);
                        for (int i = 1; i < Headers.Length; i++)
                            site.Bindings.Add("*:" + port + ":" + Headers[i], protocol);
                    }
                    serverManager.CommitChanges();
                }
                Trace.WriteLine("Successfully created the DNNWebSite", "Information");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error while creating the DNNWebSite: " + ex.Message, "Error");
                return false;
            }
            // Same code as above using a process call
           /* exitCode = ExecuteCommand(systemDrive + "\\windows\\system32\\inetsrv\\appcmd.exe", 
                " add site /name:\"" + webSiteName + "\" /bindings:" + bindings + " /physicalpath:\"" + HomePath + "\"", out output, out error, 20000);
            if (exitCode == 0)
                Trace.WriteLine("Successfully created the DNNWebSite", "Information");
            else
            {
                Trace.WriteLine("Error while creating the DNNWebSite", "Error");
                return false;
            }*/

            // Creates an application pool with the identity of the user that connects to the SMB Server
            string appPoolName = "DotNetNukeApp";
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var appPool = serverManager.ApplicationPools[appPoolName];
                    if (appPool == null)
                    {
                        appPool = serverManager.ApplicationPools.Add(appPoolName);

                        appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                        appPool.ProcessModel.UserName = "localhost\\" + userName;
                        appPool.ProcessModel.Password = password;
                        appPool.ManagedRuntimeVersion = managedRuntimeVersion;
                        if (managedPipelineMode.ToLower() == "integrated") 
                            appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                        else
                            appPool.ManagedPipelineMode = ManagedPipelineMode.Classic;                        
                    }
                    serverManager.CommitChanges();
                }
                Trace.WriteLine("Successfully created the Application Pool", "Information");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error while creating the Application Pool: " + ex.Message, "Error");
                return false;
            }
            // Same code as above using a process call
           /* exitCode = ExecuteCommand(systemDrive + "\\windows\\system32\\inetsrv\\appcmd.exe",
                " add apppool /name:\"DotNetNukeApp\" -processModel.identityType:\"SpecificUser\" -processModel.userName:\"localhost\\" + userName + "\"" +
                " -processModel.password:\"" + password + "\" -managedRuntimeVersion:\"" + managedRuntimeVersion + "\" -managedPipelineMode:\"" + managedPipelineMode + "\"", out output, out error, 20000);
            if (exitCode == 0)
                Trace.WriteLine("Successfully created the Application Pool", "Information");
            else
            {
                Trace.WriteLine("Error while creating the Application Pool: " + error, "Error");
                return false;
            }*/
            


            // Sets the application pool 
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    serverManager.Sites[webSiteName].ApplicationDefaults.ApplicationPoolName = appPoolName;                    
                    serverManager.CommitChanges();
                }
                Trace.WriteLine("uccessfully changed the Poolname", "Information");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error while changing the AppPool: " + ex.Message, "Error");
                return false;
            }

            return true;
            // Same code as above using a process call
/*            exitCode = ExecuteCommand(systemDrive + "\\windows\\system32\\inetsrv\\appcmd.exe",
                " set app /app.name:\"" + webSiteName + "/\" /applicationPool:\"" + appPoolName + "\"", out output, out error, 20000);
            if (exitCode == 0)
            {
                Trace.WriteLine("Successfully changed the Poolname", "Information");
                return true;
            }
            else
            {
                Trace.WriteLine("Error while changing the Poolname: " + error, "Error");
                return false;
            }*/
        }

        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
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
#endregion

    }
}
