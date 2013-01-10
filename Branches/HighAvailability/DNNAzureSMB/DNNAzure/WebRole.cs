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
        const string WebSiteName = "DotNetNuke";
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
                SMBMode = bool.Parse(RoleStartupUtils.GetSetting("SMBMode"));
            }
            catch {SMBMode = true;}


            if (IsSMBServer)
            {
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Server");

                // Mount the drive and publish on the same server
                try
                {
                    // Mount the drive
                    Drive = RoleStartupUtils.MountCloudDrive(RoleStartupUtils.GetSetting("AcceleratorConnectionString"), 
                                                            RoleStartupUtils.GetSetting("driveContainer"), 
                                                            RoleStartupUtils.GetSetting("driveName"), 
                                                            RoleStartupUtils.GetSetting("driveSize"));

                    // Create a local account for sharing the drive
                    RoleStartupUtils.CreateUserAccount(RoleStartupUtils.GetSetting("fileshareUserName"),
                                                       RoleStartupUtils.GetSetting("fileshareUserPassword"));

                    // Setup FTP user accounts
                    if (bool.Parse(RoleStartupUtils.GetSetting("FTP.Enabled", "False")))
                    {
                        // Create a local account for the FTP root user
                        RoleStartupUtils.CreateUserAccount(
                            RoleStartupUtils.GetSetting("FTP.Root.Username"),
                            RoleStartupUtils.DecryptPassword(RoleStartupUtils.GetSetting("FTP.Root.EncryptedPassword")));

                        if (!string.IsNullOrEmpty(RoleStartupUtils.GetSetting("FTP.Portals.Username")))
                        {
                            // Optionally create a local account for the FTP portals user
                            RoleStartupUtils.CreateUserAccount(
                                RoleStartupUtils.GetSetting("FTP.Portals.Username"),
                                RoleStartupUtils.DecryptPassword(
                                    RoleStartupUtils.GetSetting("FTP.Portals.EncryptedPassword")));
                        }
                    }


                    // Enable SMB traffic through the firewall
                    if (RoleStartupUtils.EnableSMBFirewallTraffic()!=0)
                        goto Exit;

                    // Share it using SMB
                    string rdpUserName = "";
                    try { rdpUserName = RoleStartupUtils.GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"); }
                    catch
                    {
                        Trace.TraceWarning("No RDP user name was specified. Consider enabling RDP for better maintenance options.");
                    }                  
                    // The cloud drive can't be shared if it is running on Windows Azure Compute Emulator. 
                    if (!RoleEnvironment.IsEmulated)
                        RoleStartupUtils.ShareLocalFolder(new []
                                                              {
                                                                 RoleStartupUtils.GetSetting("fileshareUserName"),  
                                                                 rdpUserName,
                                                                 RoleStartupUtils.GetSetting("FTP.Root.Username"),  
                                                                 RoleStartupUtils.GetSetting("FTP.Portals.Username")
                                                              },  
                                                        Drive.LocalPath,
                                                        RoleStartupUtils.GetSetting("shareName"));

                    // Check for the database existence
                    Trace.TraceInformation("Checking for database existence...");
                    if (!RoleStartupUtils.SetupDatabase(RoleStartupUtils.GetSetting("DBAdminUser"),
                                                RoleStartupUtils.GetSetting("DBAdminPassword"),
                                                RoleStartupUtils.GetSetting("DatabaseConnectionString")))
                        Trace.TraceError("Error while setting up the database. Check previous messages.");

                    // Check for the creation of the Website contents from Azure storage
                    Trace.TraceInformation("Checking for website content...");
                    if (!RoleStartupUtils.SetupWebSiteContents(Drive.LocalPath + "\\" + RoleStartupUtils.GetSetting("dnnFolder"),
                                                            RoleStartupUtils.GetSetting("AcceleratorConnectionString"),
                                                            RoleStartupUtils.GetSetting("packageContainer"),
                                                            RoleStartupUtils.GetSetting("package"),
                                                            RoleStartupUtils.GetSetting("packageUrl")))
                        Trace.TraceError("Website content could not be prepared. Check previous messages.");


                    // Setup Database Connection string
                    RoleStartupUtils.SetupWebConfig(Drive.LocalPath + "\\" + RoleStartupUtils.GetSetting("dnnFolder") + "\\web.config",
                                                    RoleStartupUtils.GetSetting("DatabaseConnectionString"),
                                                    RoleStartupUtils.GetSetting("InstallationDate"),
                                                    RoleStartupUtils.GetSetting("UpdateService.Source"));

                    // Setup DotNetNuke.install.config
                    RoleStartupUtils.SetupInstallConfig(
                                        Path.Combine(new[]
                                                         {
                                                             Drive.LocalPath, RoleStartupUtils.GetSetting("dnnFolder"),
                                                             "Install\\DotNetNuke.install.config"
                                                         }),
                                        RoleStartupUtils.GetSetting("AcceleratorConnectionString"),
                                        RoleStartupUtils.GetSetting("packageContainer"),
                                        RoleStartupUtils.GetSetting("packageInstallConfiguration"));

                    // Setup post install addons (always overwrite)
                    RoleStartupUtils.InstallAddons(RoleStartupUtils.GetSetting("AddonsUrl"),
                                                    Drive.LocalPath + "\\" + RoleStartupUtils.GetSetting("dnnFolder"));

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
                if (!RoleStartupUtils.MapNetworkDrive(SMBMode, RoleStartupUtils.GetSetting("localPath"),
                                    RoleStartupUtils.GetSetting("shareName"),
                                    RoleStartupUtils.GetSetting("fileshareUserName"),
                                    RoleStartupUtils.GetSetting("fileshareUserPassword")))
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
                if (CreateDNNWebSite(RoleStartupUtils.GetSetting("hostHeaders"),
                                          RoleStartupUtils.GetSetting("localPath") + "\\" + RoleStartupUtils.GetSetting("dnnFolder"),
                                          RoleStartupUtils.GetSetting("fileshareUserName"),
                                          RoleStartupUtils.GetSetting("fileshareUserPassword"), 
                                          RoleStartupUtils.GetSetting("managedRuntimeVersion"), 
                                          RoleStartupUtils.GetSetting("managedPipelineMode"),
                                          RoleStartupUtils.GetSetting("appPool.IdleTimeout").ToLower() == "infinite"?
                                                                  TimeSpan.Zero:
                                                                  new TimeSpan(0, int.Parse(RoleStartupUtils.GetSetting("appPool.IdleTimeout")), 0),
                                        new TimeSpan(0, 0, int.Parse(RoleStartupUtils.GetSetting("appPool.StartupTimeLimit"))),
                                        new TimeSpan(0, 0, int.Parse(RoleStartupUtils.GetSetting("appPool.PingResponseTime"))),
                                        RoleStartupUtils.GetSetting("SSL.CertificateThumbprint"),
                                        RoleStartupUtils.GetSetting("SSL.HostHeader"),
                                        RoleStartupUtils.GetSetting("SSL.Port")
                                      ))
                {
                    // Setup FTP
                    if (bool.Parse(RoleStartupUtils.GetSetting("FTP.Enabled", "False")))
                    {
                        // Enable FTP traffic through the firewall
                        if (RoleStartupUtils.EnableFTPFirewallTraffic() != 0)
                            goto Exit;                        

                        // Create the FTP site
                        var externalIP =
                            RoleStartupUtils.GetExternalIP(
                                RoleStartupUtils.GetSetting("FTP.ExternalIpProvider.Url", @"http://checkip.dyndns.org/"),
                                RoleStartupUtils.GetSetting("FTP.ExternalIpProvider.RegexPattern", @"[^\d\.]*"));

                        CreateDNNFTPSite(RoleStartupUtils.GetSetting("hostHeaders"),
                                         RoleStartupUtils.GetSetting("FTP.Root.Username"),
                                         RoleStartupUtils.GetSetting("FTP.Portals.Username"),
                                         RoleStartupUtils.GetSetting("localPath") + "\\" + RoleStartupUtils.GetSetting("dnnFolder"),
                                         Path.Combine(RoleStartupUtils.GetSetting("localPath") + "\\" + RoleStartupUtils.GetSetting("dnnFolder"), "Portals"), 
                                         WebSiteName,
                                         externalIP,
                                         RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPDataPassive"].IPEndpoint.Port,
                                         RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPDataPassive"].IPEndpoint.Port);
                        if (!string.IsNullOrEmpty(externalIP))
                        {
                            RoleStartupUtils.RestartService("FTPSVC");
                        }
                    }

                    // Ensure the website is started
                    StartWebsite();
                }
                else
                {
                    Trace.TraceError("Failed to create the DNNWebSite");
                }
                    

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

        private bool CreateDNNFTPSite(string hostHeaders, string rootUsername, string portalsAdminUsername, string siteRoot, string portalsRoot, string webSiteName, string externalIP, int lowDataChannelPort, int highDataChannelPort)
        {
            Trace.TraceInformation("Creating FTP site...");
            try
            {
                const string protocol = "ftp";
                var ftproot = Environment.SystemDirectory.Substring(0, 2) + @"\inetpub\ftproot";
                var port = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPCmd"].IPEndpoint.Port.ToString();                
                var ftpSiteName = string.Format("{0}_FTP", webSiteName);
                string[] headers = hostHeaders.Split(';');

                using (var serverManager = new ServerManager())
                {
                    var site = serverManager.Sites[ftpSiteName];
                    if (site == null)
                    {                        
                        var localIpAddress = RoleStartupUtils.GetFirstIPv4LocalNetworkAddress();
                        if (localIpAddress == "")
                            localIpAddress = "*";
                        var binding = localIpAddress + ":" + port + ":";
                        Trace.TraceInformation("Creating FTP (SiteName={0}; Protocol={1}; Bindings={2}; RootPath={3}; PortalsPath={4}", ftpSiteName, "ftp", binding, siteRoot, portalsRoot);
                        site = serverManager.Sites.Add(ftpSiteName, protocol, localIpAddress + ":" + port + ":", ftproot);

                        for (int i = 1; i < headers.Length; i++)
                            site.Bindings.Add(localIpAddress + ":" + port + ":" + headers[i], protocol);
                    }

                    // Enable basic authentication
                    Trace.TraceInformation("Enabling basic authentication on the FTP site...");
                    site.GetChildElement("ftpServer").GetChildElement("security").GetChildElement("authentication").GetChildElement("basicAuthentication")["enabled"] = true;

                    var config = serverManager.GetApplicationHostConfiguration();

                    // Enable passive mode
                    if (!string.IsNullOrEmpty(externalIP))
                    {
                        Trace.TraceInformation("Enabling passive mode on the FTP site...");
                        var firewallSupportSection = config.GetSection("system.ftpServer/firewallSupport");
                        site.GetChildElement("ftpServer").GetChildElement("firewallSupport")["externalIp4Address"] = externalIP;
                        firewallSupportSection["lowDataChannelPort"] = lowDataChannelPort;
                        firewallSupportSection["highDataChannelPort"] = highDataChannelPort;
                    }
                    else
                    {
                        Trace.TraceWarning("Cannot enable FTP passive mode");
                    }

                    // Enable authorization for users in the root folder (read only)
                    Trace.TraceInformation("Setting upt authorization for users in the FTP root folder (read only)...");
                    var authorizationSection = config.GetSection("system.ftpServer/security/authorization", ftpSiteName);
                    var authorizationCollection = authorizationSection.GetCollection();
                    var authElement = authorizationCollection.CreateElement("add");
                    authElement["accessType"] = @"Allow";
                    authElement["users"] = rootUsername;
                    authElement["permissions"] = @"Read";
                    authorizationCollection.Add(authElement);
                    if (!string.IsNullOrEmpty(portalsAdminUsername))
                    {
                        authElement = authorizationCollection.CreateElement("add");
                        authElement["accessType"] = @"Allow";
                        authElement["users"] = portalsAdminUsername;
                        authElement["permissions"] = @"Read";
                        authorizationCollection.Add(authElement);                        
                    }

                    // Remove SSL requirement
                    Trace.TraceInformation("Removing SSL requirement for FTP...");
                    site.GetChildElement("ftpServer").GetChildElement("security").GetChildElement("ssl")["controlChannelPolicy"] = 0;
                    site.GetChildElement("ftpServer").GetChildElement("security").GetChildElement("ssl")["dataChannelPolicy"] = 0;

                    // Add two virtual directories (one for each user)
                    Trace.TraceInformation("Creating FTP virtual directories...");
                    site.Applications[0].VirtualDirectories.Add("/" + rootUsername, siteRoot);
                    if (!string.IsNullOrEmpty(portalsAdminUsername))
                    {
                        site.Applications[0].VirtualDirectories.Add("/" + portalsAdminUsername, portalsRoot);
                    }

                    // Add read/write permissions for each user
                    Trace.TraceInformation("Setting up FTP permissions...");
                    //   Root
                    authorizationSection = config.GetSection("system.ftpServer/security/authorization", string.Format("{0}/{1}", ftpSiteName, rootUsername));
                    authorizationCollection = authorizationSection.GetCollection();
                    authorizationCollection.Clear();
                    authElement = authorizationCollection.CreateElement("add");
                    authElement["accessType"] = @"Allow";
                    authElement["users"] = rootUsername;
                    authElement["permissions"] = @"Read, Write";
                    authorizationCollection.Add(authElement);
                    if (!string.IsNullOrEmpty(portalsAdminUsername))
                    {
                        //   PortalsAdmin
                        authorizationSection = config.GetSection("system.ftpServer/security/authorization", string.Format("{0}/{1}", ftpSiteName, portalsAdminUsername));
                        authorizationCollection = authorizationSection.GetCollection();
                        authorizationCollection.Clear();
                        authElement = authorizationCollection.CreateElement("add");
                        authElement["accessType"] = @"Allow";
                        authElement["users"] = portalsAdminUsername;
                        authElement["permissions"] = @"Read, Write";
                        authorizationCollection.Add(authElement);
                    }

                    // Enable user isolation to "User name directory"
                    Trace.TraceInformation("Enabling user isolation to 'User name directory'...");
                    site.GetChildElement("ftpServer").GetChildElement("userIsolation")["mode"] = "StartInUsersDirectory";

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while creating the FTP site: " + ex.Message);
                return false;
            }
            return true;
        }


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
                    
                    var site = serverManager.Sites[WebSiteName];
                    if (site == null)
                    {
                        Trace.TraceInformation("Creating DNNWebSite (SiteName=" + WebSiteName + ";protocol=" + protocol + ";Bindings=" + "*:" + port + ":" + headers[0] + ";HomePath=" + homePath);
                        var localIPAddress = RoleStartupUtils.GetFirstIPv4LocalNetworkAddress();
                        if (localIPAddress == "")
                            localIPAddress = "*"; 
                        site = serverManager.Sites.Add(WebSiteName, protocol, localIPAddress + ":" + port + ":" + headers[0], homePath);

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
                    serverManager.Sites[WebSiteName].ApplicationDefaults.ApplicationPoolName = appPoolName;

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

        public static void StartWebsite()
        {
            try
            {
                // FIX - In some cases, the site bindings end in a Event ID 1007 when using Windows Server 2012
                // See http://technet.microsoft.com/en-us/library/dd316029(v=ws.10).aspx for more info. As workaround, 
                // we will always start the site
                using (var serverManager = new ServerManager())
                {
                    var site = serverManager.Sites[WebSiteName];
                    if (site != null)
                    {
                        site.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while restarting the DNNWebSite: " + ex.Message);
            }
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
