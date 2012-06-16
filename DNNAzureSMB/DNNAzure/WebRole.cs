using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.Web.Administration;
using DNNShared;


namespace DNNAzure
{
    public class WebRole : RoleEntryPoint
    {
        public static string DriveLetter;
        public static CloudDrive Drive;


        public bool SMBMode;
        
        // Act as a SMB server - The Instance "0" is the instance that will mount the drive
        public bool IsSMBServer 
        { 
            get { return (!SMBMode && RoleEnvironment.CurrentRoleInstance.Id.EndsWith("_0"));}
        }

        public override bool OnStart()
        {
            
            // Inits the Diagnostic Monitor
            RoleStartupUtils.ConfigureDiagnosticMonitor();            

            Trace.TraceInformation("DNNAzure initialization");

            try
            {
                SMBMode = bool.Parse(RoleEnvironment.GetConfigurationSettingValue("SMBMode"));
            }
            catch {SMBMode = true;}


            if (IsSMBServer)
            {
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Server");

                // Mount the drive and publish on the same server
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
                    if (RoleStartupUtils.EnableSMBFirewallTraffic()!=0)
                        goto Exit;

                    // Share it using SMB
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
                    Trace.TraceInformation("Checking for website content...");
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
                                          RoleEnvironment.GetConfigurationSettingValue("managedPipelineMode"),
                                          RoleEnvironment.GetConfigurationSettingValue("appPool.IdleTimeout").ToLower() == "infinite"?
                                                                  TimeSpan.Zero:
                                                                  new TimeSpan(0, int.Parse(RoleEnvironment.GetConfigurationSettingValue("appPool.IdleTimeout")), 0),
                                        new TimeSpan(0, 0, int.Parse(RoleEnvironment.GetConfigurationSettingValue("appPool.StartupTimeLimit"))),
                                        new TimeSpan(0, 0, int.Parse(RoleEnvironment.GetConfigurationSettingValue("appPool.PingResponseTime"))),
                                        RoleEnvironment.GetConfigurationSettingValue("SSL.CertificateThumbprint"),
                                        RoleEnvironment.GetConfigurationSettingValue("SSL.HostHeader"),
                                        RoleEnvironment.GetConfigurationSettingValue("SSL.Port")
                                      ))
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
        /// <param name="homePath">Home path of the DNN portal (on the SMB share)</param>
        /// <param name="userName">User name for the application pool identity</param>
        /// <param name="password">Password for the application pool identity </param>
        /// <param name="managedRuntimeVersion">Runtime version for the application pool</param>
        /// <param name="managedPipelineMode">Pipeline mode for the application pool</param>
        /// <param name="appPoolIdleTimeout">Amount of time (in minutes) a worker process will remain idle before it shuts down </param>
        /// <param name="appPoolStartupTimeLimit">Specifies the time (in seconds) that IIS waits for an application pool to start. </param>
        /// <param name="appPoolPingResponseTime">Specifies the time (in seconds) that a worker process is given to respond to a health-monitoring ping. After the time limit is exceeded, the WWW service terminates the worker process. </param>
        /// <param name="sslHostHeader">Host header for SSL binding</param>
        /// <param name="sslPort">Port for SSL binding</param>
        /// <param name="sslThumbprint">Certificate thumbprint of the SSL binding</param>
        /// <returns></returns>
        public bool CreateDNNWebSite(string hostHeaders, string homePath, string userName, string password, 
                                        string managedRuntimeVersion, string managedPipelineMode,
                                        TimeSpan appPoolIdleTimeout, TimeSpan appPoolStartupTimeLimit,
                                        TimeSpan appPoolPingResponseTime,
                                        string sslThumbprint, string sslHostHeader, string sslPort)
        {
            // Create the user account for the Application Pool. 
            Trace.TraceInformation("Creating user account for the Application Pool");
            RoleStartupUtils.CreateUserAccount(userName, password);

            

            // Build bindings based on HostHeaders
            Trace.TraceInformation("Creating DNNWebSite with hostHeaders '" + hostHeaders + "'...");
            string systemDrive = Environment.SystemDirectory.Substring(0, 2);
            string originalwebSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
            string webSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_DotNetNuke";
            string[] headers = hostHeaders.Split(';');
            string protocol = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].Protocol;
            string port = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].IPEndpoint.Port.ToString();

            string bindings = protocol + "://" + string.Join(":" + port + "," + protocol + "://", headers) + ":" + port;
            Trace.TraceInformation("Calculated bindings are: " + bindings);
            
            
            // Creates the DNN WebSite 
            try
            {
                using (var serverManager = new ServerManager())
                {
                    // Change the default web site binding to allow DNN website to be the default site
                    var webroleSite = serverManager.Sites[originalwebSiteName];
                    if (webroleSite != null)
                    {
                        webroleSite.Bindings.Clear();
                        webroleSite.Bindings.Add(string.Format("*:{0}:admin.dnndev.me", port), protocol);
                    }
                    serverManager.CommitChanges();
                }

                using (var serverManager = new ServerManager())
                {
                    
                    var site = serverManager.Sites[webSiteName];
                    if (site == null)
                    {
                        Trace.TraceInformation("Creating DNNWebSite (SiteName=" + webSiteName + ";protocol=" + protocol + ";Bindings=" + "*:" + port + ":" + headers[0] + ";HomePath=" + homePath);
                        var localIPAddress = RoleStartupUtils.GetFirstIPv4LocalNetworkAddress();
                        if (localIPAddress == "")
                            localIPAddress = "*"; 
                        site = serverManager.Sites.Add(webSiteName, protocol, localIPAddress + ":" + port + ":" + headers[0], homePath);

                        for (int i = 1; i < headers.Length; i++)
                            site.Bindings.Add(localIPAddress + ":" + port + ":" + headers[i], protocol);

                        // Add SSL binding
                        if (!string.IsNullOrEmpty(sslThumbprint))
                        {
                            Trace.TraceInformation("Adding SSL binding using certificate '{0}' on port '{1}'...", sslThumbprint, sslPort);
                            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                            store.Open(OpenFlags.OpenExistingOnly);
                            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, sslThumbprint, true);

                            if (certificate != null && certificate.Count > 0)
                            {
                                site.Bindings.Add(localIPAddress + ":" + sslPort + ":" + sslHostHeader, certificate[0].GetCertHash(),
                                                  "My");
                            }
                            else
                            {
                                Trace.TraceError("Can't add SSL binding. The certificate thumbprint '{0}' does not exist in the local machine store", sslThumbprint);
                            }
                        }
                    }

                    // Creates an application pool with the identity of the user that connects to the SMB Server                    
                    string appPoolName = "DotNetNukeApp";

                    var appPool = serverManager.ApplicationPools[appPoolName];
                    if (appPool == null)
                    {
                        Trace.TraceInformation("Creating application pool...");
                        appPool = serverManager.ApplicationPools.Add(appPoolName);
                    }
                    else
                    {
                        Trace.TraceInformation("Updating application pool...");
                    }

                    appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    appPool.ProcessModel.UserName = "localhost\\" + userName;
                    appPool.ProcessModel.Password = password;

                    // Setup limits
                    appPool.ProcessModel.IdleTimeout = appPoolIdleTimeout;
                    appPool.ProcessModel.StartupTimeLimit = appPoolStartupTimeLimit;
                    appPool.ProcessModel.PingResponseTime = appPoolPingResponseTime;

                    // Enable 32bit modules on Win64
                    appPool.Enable32BitAppOnWin64 = true;

                    appPool.ManagedRuntimeVersion = managedRuntimeVersion;
                    appPool.ManagedPipelineMode = managedPipelineMode.ToLower() == "integrated" ? ManagedPipelineMode.Integrated : ManagedPipelineMode.Classic;

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
            if (Drive != null)
                Drive.Unmount();
                //TODO Tell to other instance to mount the drive and reconnect the drive an all instances?
            base.OnStop();
        }
    }
}
