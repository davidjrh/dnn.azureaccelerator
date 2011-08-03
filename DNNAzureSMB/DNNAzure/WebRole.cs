using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.Web.Administration;
using System.Configuration;
using System.Xml;
using DNNShared;


namespace DNNAzure
{
    public class WebRole : RoleEntryPoint
    {
        public static string driveLetter = null;
        public static CloudDrive drive = null;


        public bool SMBMode = false;
        
        // Act as a SMB server - The Instance "0" is the instance that will mount the drive
        public bool IsSMBServer 
        { 
            get { return (!SMBMode && RoleEnvironment.CurrentRoleInstance.Id.EndsWith("_0"));}
        }

        public override bool OnStart()
        {
            
            // Inits the Diagnostic Monitor
            RoleStartupUtils.InitializeDiagnosticMonitor();            


            Trace.TraceInformation("DNNAzure initialization");

            try
            {
                SMBMode = bool.Parse(RoleEnvironment.GetConfigurationSettingValue("SMBMode"));
            }
            catch {SMBMode = true;}


            if (this.IsSMBServer)
            {
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Server");

                // Mount the drive and publish on the same server
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
                    if (RoleStartupUtils.EnableSMBFirewallTraffic()!=0)
                        goto Exit;

                    // Share it using SMB
                    string RDPuserName = "";
                    try { RDPuserName = RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"); }
                    catch { };                    
                    RoleStartupUtils.ShareLocalFolder(RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"), 
                                                    RDPuserName, drive.LocalPath,
                                                    RoleEnvironment.GetConfigurationSettingValue("shareName"));

                    // Check for the creation of the Website contents from Azure storage
                    Trace.TraceInformation("Check for website content...");
                    if (!RoleStartupUtils.SetupWebSiteContents(drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder"),
                                                            RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
                                                            RoleEnvironment.GetConfigurationSettingValue("packageContainer"),
                                                            RoleEnvironment.GetConfigurationSettingValue("package")))
                        Trace.TraceError("Website content could not be prepared. Check previous messages.");


                    // Setup Database Connection string
                    RoleStartupUtils.SetupDBConnectionString(drive.LocalPath + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder") + "\\web.config",
                                                    RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString"));

                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    goto Exit;
                }
            }
            else
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Client");

            // Map the network drive
            try
            {
                if (!RoleStartupUtils.MapNetworkDrive(SMBMode, RoleEnvironment.GetConfigurationSettingValue("localPath"),
                                    RoleEnvironment.GetConfigurationSettingValue("shareName"),
                                    RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"),
                                    RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword")))
                    Trace.TraceError("Failed to map network drive");
                //TODO: Create a thread for checking that the NetworkDrive has not been disconnected (SMB Server Fails) for trying to reconnecting to the new instance                
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString(), "Error");
                goto Exit;
            }


           
            // Create the DNN Web site
            try
            {
                if (!CreateDNNWebSite(RoleEnvironment.GetConfigurationSettingValue("hostHeaders"), 
                                        RoleEnvironment.GetConfigurationSettingValue("localPath") + "\\" + RoleEnvironment.GetConfigurationSettingValue("dnnFolder"),
                                      RoleEnvironment.GetConfigurationSettingValue("fileshareUserName"),
                                      RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword"), 
                                      RoleEnvironment.GetConfigurationSettingValue("managedRuntimeVersion"), 
                                      RoleEnvironment.GetConfigurationSettingValue("managedPipelineMode")))
                    Trace.TraceError("Failed to create the DNNWebSite");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                goto Exit;
            }

            Exit:
            return base.OnStart();
        }

#region Private functions


        /// <summary>
        /// Creates a DNNWebSite listening for hostHeaders
        /// </summary>
        /// <param name="hostHeaders">Host headers for bindings</param>
        /// <param name="HomePath">Home path of the DNN portal (on the SMB share)</param>
        /// <returns></returns>
        public bool CreateDNNWebSite(string hostHeaders, string HomePath, string userName, string password, string managedRuntimeVersion, string managedPipelineMode)
        {
            // Create the user account for the Application Pool. 
            Trace.TraceInformation("Creating user account for the Application Pool");
            RoleStartupUtils.CreateUserAccount(userName, password);

            

            // Build bindings based on HostHeaders
            Trace.TraceInformation("Creating DNNWebSite with hostHeaders '" + hostHeaders + "'...");
            string systemDrive = Environment.SystemDirectory.Substring(0, 2);
            string originalwebSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
            string webSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_DotNetNuke";
            string[] Headers = hostHeaders.Split(';');
            string protocol = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].Protocol;
            string port = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].IPEndpoint.Port.ToString();

            string bindings = protocol + "://" + string.Join(":" + port + "," + protocol + "://", Headers) + ":" + port;
            Trace.TraceInformation("Calculated bindings are: " + bindings);
            
            
            // Creates the DNN WebSite 
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var site = serverManager.Sites[webSiteName];
                    if (site == null)
                    {
                        Trace.TraceInformation("Creating DNNWebSite (SiteName=" + webSiteName + ";protocol=" + protocol + ";Bindings=" + "*:" + port + ":" + Headers[0] + ";HomePath=" + HomePath);
                        site = serverManager.Sites.Add(webSiteName, protocol, "*:" + port + ":" + Headers[0], HomePath);
                        for (int i = 1; i < Headers.Length; i++)
                            site.Bindings.Add("*:" + port + ":" + Headers[i], protocol);
                    }

                    // Creates an application pool with the identity of the user that connects to the SMB Server                    
                    string appPoolName = "DotNetNukeApp";

                    var appPool = serverManager.ApplicationPools[appPoolName];
                    if (appPool == null)
                    {
                        Trace.TraceInformation("Creating application pool...");
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

                    // Sets the application pool
                    Trace.TraceInformation("Setting application pool to the website...");
                    serverManager.Sites[webSiteName].ApplicationDefaults.ApplicationPoolName = appPoolName;

                    // Commit all changes
                    serverManager.CommitChanges();
                }
                Trace.WriteLine("Successfully created the DNNWebSite", "Information");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while creating the DNNWebSite: " + ex.Message);
                return false;
            }
            return true;
        }
#endregion

        public override void OnStop()
        {
            if (drive != null)
            {
                drive.Unmount();
                //TODO Tell to other instance to mount the drive and reconnect the drive an all instances?
            }
            base.OnStop();
        }
    }
}
