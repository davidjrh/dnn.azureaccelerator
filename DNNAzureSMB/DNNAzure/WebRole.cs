using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.Web.Administration;
using DNNShared;

namespace DNNAzure
{
    public class WebRole : RoleEntryPoint
    {
        private const string WebSiteName = "DotNetNuke";
        private const string OfflineSiteName = "Offline";
        private const string AppPoolName = "DotNetNukeApp";
        private const string SitesRoot = "root";

        private static string _drivePath;
        private static CloudDrive _drive;
        private static bool _driveMounted;

        private volatile bool _busy = true;
        private bool Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                Trace.TraceInformation(_busy ? "Role instance {0} is going Busy" : "Role instance {0} is going Ready",
                                       RoleEnvironment.CurrentRoleInstance.Id);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is SMB server.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is SMB server; otherwise, <c>false</c>.
        /// </value>
        private static bool IsSMBServer 
        { 
            get
            {
                try
                {
                    // Role "SMBServer" does not exist, so the webrole will act as a SMB Server
                    return RoleEnvironment.Roles.All(x => x.Key != "SMBServer");  
                }
                catch { return true; }                
            }
        }

        private static string LocalPath
        {
            get
            {
                // To avoid the "Path too long" issue, we are going to use the folder short path name. This gives us an additional 56 characters.
                // Thiis is in combination with the SetupSiteRoot.cmd startup task
                return Path.Combine(@"C:\Resources\Directory\Sites", SitesRoot);
            }
        }

        private PluginsManager _plugins;
        private PluginsManager Plugins
        {
            get
            {
                if (_plugins == null)
                    _plugins = new PluginsManager(Utils.GetSetting("Plugins.Url"));
                return _plugins;
            }
        }


        /// <summary>
        /// Called by Windows Azure to initialize the role instance.
        /// </summary>
        /// <returns>
        /// True if initialization succeeds, False if it fails. The default implementation returns True.
        /// </returns>
        /// <remarks>
        ///   <para>
        /// Override the OnStart method to run initialization code for your role.
        ///   </para>
        ///   <para>
        /// Before the OnStart method returns, the instance's status is set to Busy and the instance is not available
        /// for requests via the load balancer.
        ///   </para>
        ///   <para>
        /// If the OnStart method returns false, the instance is immediately stopped. If the method
        /// returns true, then Windows Azure starts the role by calling the <see cref="M:Microsoft.WindowsAzure.ServiceRuntime.RoleEntryPoint.Run" /> method.
        ///   </para>
        ///   <para>
        /// A web role can include initialization code in the ASP.NET Application_Start method instead of the OnStart method.
        /// Application_Start is called after the OnStart method.
        ///   </para>
        ///   <para>
        /// Any exception that occurs within the OnStart method is an unhandled exception.
        ///   </para>
        /// </remarks>
        public override bool OnStart()
        {

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = int.Parse(Utils.GetSetting("DefaultConnectionLimit", "1000"));

            // Setup OnStatuscheck event
            RoleEnvironment.StatusCheck += RoleEnvironmentOnStatusCheck;

            // Setup OnChanging event
            RoleEnvironment.Changing += RoleEnvironmentOnChanging;

            // Setup OnChanged event
            RoleEnvironment.Changed += RoleEnvironmentOnChanged;


            // Inits the Diagnostic Monitor
            Utils.ConfigureDiagnosticMonitor();


            if (IsSMBServer)
            {
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Server");

                // Mount the drive and publish on the same server
                try
                {
                    // Create windows user accounts for shareing the drive and other FTP related
                    Utils.CreateUserAccounts();

                    // Enable SMB traffic through the firewall
                    EnableSMBFirewallTraffic();

                    // Setup the drive object
                    _drive = Utils.InitializeCloudDrive(Utils.GetSetting("AcceleratorConnectionString"),
                                                            Utils.GetSetting("driveContainer"),
                                                            Utils.GetSetting("driveName"),
                                                            Utils.GetSetting("driveSize"));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Fatal error on the OnStart event: {0}", ex);
                    throw;
                }
            }
            else
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Client");

            Plugins.OnStart();

            return base.OnStart();
        }

        /// <summary>
        /// Roles the environment on changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoleEnvironmentChangedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RoleEnvironmentOnChanged(object sender, RoleEnvironmentChangedEventArgs e)
        {
            try
            {
                // Implements the changes after restarting the role instance
                Trace.TraceInformation("Configurations settings changed...");

                Plugins.RoleEnvironmentOnChanged(sender, e);

                foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentConfigurationSettingChange))
                {
                    if (settingChange.ConfigurationSettingName == "AppOffline.Enabled")
                    {
                        var appOfflineEnabled = bool.Parse(Utils.GetSetting("AppOffline.Enabled"));
                        Trace.TraceInformation("AppOffline.Enabled has changed to '{0}'. Swapping the sites...", appOfflineEnabled);
                        // Setup the offline site settings
                        Utils.SetupOfflineSiteSettings(LocalPath);

                        // Ensure that the portal aliases for the offline site have been created (only needed if the AppOffline.Enabled == "true")
                        if (appOfflineEnabled)
                        {
                            Utils.SetupOfflineSitePortalAliases(LocalPath + "\\" + Utils.GetSetting("dnnFolder") + "\\web.config",
                                                                           RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpInOffline"].IPEndpoint.Port,
                                                                           RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpsInOffline"].IPEndpoint.Port);
                        }
                        // Setup IIS sites
                        SetupIISSites();
                    }

                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while processing the new settings: {0}", ex);
                throw;
            }
        }


        /// <summary>
        /// Called by Windows Azure after the role instance has been initialized. This method serves as the
        /// main thread of execution for your role.
        /// </summary>
        /// <remarks>
        ///   <para>
        /// Override the Run method to implement your own code to manage the role's execution. The Run method should implement
        /// a long-running thread that carries out operations for the role. The default implementation sleeps for an infinite
        /// period, blocking return indefinitely.
        ///   </para>
        ///   <para>
        /// The role recycles when the Run method returns.
        ///   </para>
        ///   <para>
        /// Any exception that occurs within the Run method is an unhandled exception.
        ///   </para>
        /// </remarks>
        public override void Run()
        {
            // The code here mounts the drive shared out by the server worker role
            // Each client role instance writes to a log file named after the role instance in the logfile directory

            Trace.TraceInformation("DNNAzure entry point called (Role {0})...", RoleEnvironment.CurrentRoleInstance.Id);

            try
            {
                // Create another thread to continuosly check for the mapped network drive
                ThreadPool.QueueUserWorkItem(o => SetupNetworkDriveAndWebsite());

                if (IsSMBServer)
                {
                    // If acts as a SMB server, compete for the drive lease
                    CompeteForMount();
                }
                else
                {
                    base.Run();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fatal error on Run: {0}", ex);
            }
        }

        #region Private functions

        private void SetupNetworkDriveAndWebsite()
        {
            Trace.TraceInformation("Setting up network drive and website...");
            string shareName = Utils.GetSetting("shareName");
            var userName = Utils.GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername");
            var password =
                Utils.DecryptPassword(
                    Utils.GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword"));

            string logDir = Path.Combine(LocalPath, "logs");
            string fileName = RoleEnvironment.CurrentRoleInstance.Id + ".txt";
            string logFilePath = Path.Combine(logDir, fileName);

            Trace.TraceInformation("Impersonating with user {0}...", userName);
            var impersonationContext = Utils.ImpersonateValidUser(userName, "", password);
            if (impersonationContext == null)
            {
                Trace.TraceError("Fatal error: could not impersonate user {0}.", userName);
                return;
            }

            while (true)
            {
                // Change the instance status to Busy
                Busy = true;
                try
                {
                    if (Utils.CreateSymbolicLink(LocalPath, shareName, userName, password, (IsSMBServer?"DNNAzure":"SMBServer")))
                    {
                        // Setup IIS - Website and FTP site
                        SetupIISSites();

                        // Change the status to Ready
                        Busy = false;

                        // Inform the plugins that the site is up and running
                        Plugins.OnSiteReady();

                        while (true)
                        {
                            // write to the log file. Do some retries in the case of failure to avoid false positives
                            Utils.AppendLogEntryWithRetries(logFilePath, 5);

                            // If the file/share becomes inaccessible, AppendAllText will throw an exception and
                            // the worker role will exit, and then get restarted, and then it fill find the new share
                            Thread.Sleep(Utils.SleepTimeAfterSuccessfulPolling);
                        }
                    }
                    Trace.TraceError("Failed to mount {0} on role instance {1}", shareName, RoleEnvironment.CurrentRoleInstance.Id);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Starting remapping process because of error on role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    Thread.Sleep(Utils.SleepTimeBeforeStartToRemap);
                }
            }

// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        /// <summary>
        /// Setups the IIS sites.
        /// </summary>
        private static void SetupIISSites()
        {
            Trace.TraceInformation("Setting up IIS configuration...");
            // Create the DNN Web site
            try
            {
                if (CreateDNNWebSite(Utils.GetSetting("hostHeaders"),
                                          Path.Combine(LocalPath, Utils.GetSetting("dnnFolder")),
                                          Path.Combine(LocalPath, Utils.GetSetting("AppOffline.Folder")),
                                          Utils.GetSetting("fileshareUserName"),
                                          Utils.GetSetting("fileshareUserPassword"),
                                        Utils.GetSetting("SSL.CertificateThumbprint"),
                                        Utils.GetSetting("SSL.HostHeader")
                                      ))
                {                    
                    // Setup FTP
                    if (bool.Parse(Utils.GetSetting("FTP.Enabled", "False")))
                    {
                        // Create the FTP site
                        var externalIP =
                            Utils.GetExternalIP(
                                Utils.GetSetting("FTP.ExternalIpProvider.Url", @"http://checkip.dyndns.org/"),
                                Utils.GetSetting("FTP.ExternalIpProvider.RegexPattern", @"[^\d\.]*"));

                        CreateDNNFTPSite(Utils.GetSetting("hostHeaders"),
                                         Utils.GetSetting("FTP.Root.Username"),
                                         Utils.GetSetting("FTP.Portals.Username"),
                                         Path.Combine(LocalPath, Utils.GetSetting("dnnFolder")),
                                         Path.Combine(Path.Combine(LocalPath, Utils.GetSetting("dnnFolder")), "Portals"),
                                         WebSiteName,
                                         externalIP,
                                         RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPDataPassive"].IPEndpoint.Port,
                                         RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPDataPassive"].IPEndpoint.Port);
                        if (!string.IsNullOrEmpty(externalIP))
                        {
                            Utils.RestartService("FTPSVC");
                        }
                    }

                    // Ensure the website is started
                    StartWebsite(WebSiteName);
                    StartWebsite(OfflineSiteName);
                }
                else
                {
                    Trace.TraceError("Failed to create the DNNWebSite");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to setup IIS: {0}", ex);
            }
            
        }

        /// <summary>
        /// Competes for the cloud drive lease.
        /// </summary>
        private static void CompeteForMount()
        {

            for (; ; )
            {
                _driveMounted = false;
                _drivePath = "";
                try
                {
                    Trace.TraceInformation("Competing for mount {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                    Utils.MountCloudDrive(_drive);
                    _driveMounted = true;
                    Trace.TraceInformation("{0} Successfully mounted the drive!", RoleEnvironment.CurrentRoleInstance.Id);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Equals("ERROR_LEASE_LOCKED"))
                    {
                        //Trace.TraceInformation("{0} could not mount the drive. The lease is locked. Will retry in 5 seconds.",
                        //                   RoleEnvironment.CurrentRoleInstance.Id);
                    }
                    else
                    {
                        Trace.TraceWarning("{0} could not mount the drive, Will retry in 5 seconds. Reason: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    }
                }

                if (!_driveMounted)
                {
                    Thread.Sleep(5000);
                    continue;   // Compete again for the lease
                }

                // Shares the drive
                _drivePath = Utils.ShareDrive(_drive);

                // Setup the website settings
                Utils.SetupWebSiteSettings(_drive);

                // Setup the offline site settings
                Utils.SetupOfflineSiteSettings(_drive.LocalPath);

                // Now, spin checking if the drive is still accessible.
                Utils.WaitForMoutingFailure(_drive);

                // Drive is not accessible. Remove the share
                Utils.DeleteShare(Utils.GetSetting("shareName"));
                try
                {
                    Trace.TraceInformation("Unmounting cloud drive on role {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                    _drive.Unmount();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error while unmounting the cloud drive on role {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);                    
                }
            }
            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns


        /// <summary>
        /// Enables the SMB firewall traffic.
        /// </summary>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">Could not setup the firewall rules. See previous errors</exception>
        private static void EnableSMBFirewallTraffic()
        {
            if (Utils.EnableSMBFirewallTraffic() != 0)
            {
                throw new ConfigurationErrorsException("Could not setup the firewall rules. See previous errors");
            }

            if (bool.Parse(Utils.GetSetting("FTP.Enabled", "False")))
            {
                // Enable FTP traffic through the firewall
                if (Utils.EnableFTPFirewallTraffic() != 0)
                {
                    Trace.TraceWarning("Coud not setup the FTP firewall rules. See previous errors");
                }
            }
        }

        /// <summary>
        /// Creates the DNNFTP site.
        /// </summary>
        /// <param name="hostHeaders">The host headers.</param>
        /// <param name="rootUsername">The root username.</param>
        /// <param name="portalsAdminUsername">The portals admin username.</param>
        /// <param name="siteRoot">The site root.</param>
        /// <param name="portalsRoot">The portals root.</param>
        /// <param name="webSiteName">Name of the web site.</param>
        /// <param name="externalIP">The external IP.</param>
        /// <param name="lowDataChannelPort">The low data channel port.</param>
        /// <param name="highDataChannelPort">The high data channel port.</param>
        /// <returns></returns>
        private static bool CreateDNNFTPSite(string hostHeaders, string rootUsername, string portalsAdminUsername, string siteRoot, string portalsRoot, string webSiteName, string externalIP, int lowDataChannelPort, int highDataChannelPort)
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
                        var localIpAddress = Utils.GetFirstIPv4LocalNetworkAddress();
                        if (localIpAddress == "")
                            localIpAddress = "*";
                        var binding = localIpAddress + ":" + port + ":";
                        Trace.TraceInformation(
                            "Creating FTP (SiteName={0}; Protocol={1}; Bindings={2}; RootPath={3}; PortalsPath={4}",
                            ftpSiteName, "ftp", binding, siteRoot, portalsRoot);
                        site = serverManager.Sites.Add(ftpSiteName, protocol, localIpAddress + ":" + port + ":", ftproot);

                        for (int i = 1; i < headers.Length; i++)
                            site.Bindings.Add(localIpAddress + ":" + port + ":" + headers[i], protocol);
                    }
                    else
                    {
                        // TODO Rebuild the site permissions below and don't exit here
                        Trace.TraceInformation("FTP site was already configured");
                        return true;
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
        /// <param name="sslHostHeader">Host header for SSL binding</param>
        /// <param name="sslThumbprint">Certificate thumbprint of the SSL binding</param>
        /// <returns></returns>
        private static bool CreateDNNWebSite(string hostHeaders, string homePath, string offlinePath, string userName, string password, string sslThumbprint, string sslHostHeader)
        {
            // Create the user account for the Application Pool. 
            Trace.TraceInformation("Creating user account for the Application Pool");
            Utils.CreateUserAccount(userName, password);
            

            // Build bindings based on HostHeaders
            Trace.TraceInformation("Creating website...");
            string systemDrive = Environment.SystemDirectory.Substring(0, 2);
            string originalwebSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
            string[] headers = hostHeaders.Split(';');
            const string protocol = "http";
            string port = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpIn"].IPEndpoint.Port.ToString();
            string sslPort = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpsIn"].IPEndpoint.Port.ToString();
            string offlinePort = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpInOffline"].IPEndpoint.Port.ToString();
            string offlineSslPort = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpsInOffline"].IPEndpoint.Port.ToString();
            var isOfflineEnabled = bool.Parse(Utils.GetSetting("AppOffline.Enabled", "false"));

            string bindings = protocol + "://" + string.Join(":" + (isOfflineEnabled?offlinePort:port) + "," + protocol + "://", headers) + ":" + (isOfflineEnabled?offlinePort:port);
            var localIpAddress = Utils.GetFirstIPv4LocalNetworkAddress();
            if (localIpAddress == "")
                localIpAddress = "*"; 
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

                        serverManager.CommitChanges();
                    }                    
                }

                using (var serverManager = new ServerManager())
                {
                    
                    // Create website "DotNetNuke"
                    var site = CreateSite(serverManager, WebSiteName, homePath, protocol, localIpAddress, isOfflineEnabled, offlinePort, port, headers);

                    // Create website "Offline"
                    var offlineSite = CreateSite(serverManager, OfflineSiteName, offlinePath, protocol, localIpAddress, !isOfflineEnabled, offlinePort, port, headers);

                    // Add SSL binding
                    if (!string.IsNullOrEmpty(sslThumbprint))
                    {
                        Trace.TraceInformation("Adding SSL binding using certificate '{0}' on port '{1}'...", sslThumbprint, sslPort);
                        var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                        store.Open(OpenFlags.OpenExistingOnly);
                        var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, sslThumbprint, true);

                        if (certificate != null && certificate.Count > 0)
                        {
                            site.Bindings.Add(localIpAddress + ":" + sslPort + ":" + sslHostHeader, certificate[0].GetCertHash(), "My");
                            offlineSite.Bindings.Add(localIpAddress + ":" + offlineSslPort + ":" + sslHostHeader, certificate[0].GetCertHash(), "My");
                        }
                        else
                        {
                            Trace.TraceError("Can't add SSL binding. The certificate thumbprint '{0}' does not exist in the local machine store", sslThumbprint);
                        }
                    }

                    // Creates an application pool with the identity of the user that connects to the SMB Server                                        
                    CreateApplicationPool(serverManager, userName, password);

                    // Sets the application pool
                    Trace.TraceInformation("Setting application pool to the website...");
                    site.ApplicationDefaults.ApplicationPoolName = AppPoolName;
                    offlineSite.ApplicationDefaults.ApplicationPoolName = AppPoolName;

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

        private static void CreateApplicationPool(ServerManager serverManager, string userName, string password)
        {
            var appPool = serverManager.ApplicationPools[AppPoolName];
            if (appPool == null)
            {
                Trace.TraceInformation("Creating application pool...");
                appPool = serverManager.ApplicationPools.Add(AppPoolName);
            }
            else
            {
                Trace.TraceInformation("Updating application pool...");
            }

            appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
            appPool.ProcessModel.UserName = "localhost\\" + userName;
            appPool.ProcessModel.Password = password;

            // Setup limits
            appPool.ProcessModel.IdleTimeout = Utils.GetSetting("appPool.IdleTimeout").ToLower() == "infinite" ?
                                                                  TimeSpan.Zero :
                                                                  new TimeSpan(0, int.Parse(Utils.GetSetting("appPool.IdleTimeout")), 0);
            appPool.ProcessModel.StartupTimeLimit = new TimeSpan(0, 0, int.Parse(Utils.GetSetting("appPool.StartupTimeLimit")));
            appPool.ProcessModel.PingResponseTime = new TimeSpan(0, 0, int.Parse(Utils.GetSetting("appPool.PingResponseTime")));


            // Enable 32bit modules on Win64
            if (bool.Parse(Utils.GetSetting("appPool.Enable32bitApps", "false")))
            {
                appPool.Enable32BitAppOnWin64 = true;    
            }            

            // Set start mode to Always running (IIS 8) - see http://blogs.msdn.com/b/vijaysk/archive/2012/10/11/iis-8-what-s-new-website-settings.aspx
            if (appPool.Attributes.Any(x => x.Name == "startMode"))
            {
                appPool.SetAttributeValue("startMode", 1); // Always running
            }

            appPool.ManagedRuntimeVersion = Utils.GetSetting("managedRuntimeVersion");
            appPool.ManagedPipelineMode = Utils.GetSetting("managedPipelineMode").ToLower() == "integrated" ? ManagedPipelineMode.Integrated : ManagedPipelineMode.Classic;            
        }

        private static Site CreateSite(ServerManager serverManager, string webSiteName, string homePath, string protocol, string localIpAddress,
                                       bool isOffline, string offlinePort, string port, string[] headers)
        {
            // Creates or updates the DotNetNuke site
            var site = serverManager.Sites[webSiteName];
            if (site == null)
            {
                Trace.TraceInformation("Creating website " + webSiteName + "...");
                site = serverManager.Sites.Add(webSiteName, protocol,
                                               localIpAddress + ":" + (isOffline ? offlinePort : port) + ":" + headers[0],
                                               homePath);
            }
            else
            {
                Trace.TraceInformation("Updating website " + webSiteName + "...");
                site.Bindings.Clear();
                site.Bindings.Add(localIpAddress + ":" + (isOffline ? offlinePort : port) + ":", protocol);
            }

            // Setup header bindings
            for (var i = 1; i < headers.Length; i++)
                site.Bindings.Add(localIpAddress + ":" + (isOffline ? offlinePort : port) + ":" + headers[i], protocol);

            // Add preload support (IIS 8) - see http://blogs.msdn.com/b/vijaysk/archive/2012/10/11/iis-8-what-s-new-website-settings.aspx
            if (site.ApplicationDefaults.Attributes.Any(x => x.Name == "preloadEnabled"))
            {
                site.ApplicationDefaults.SetAttributeValue("preloadEnabled", true);
            }
            return site;
        }

        /// <summary>
        /// Starts the website.
        /// </summary>
        public static void StartWebsite(string siteName)
        {
            try
            {
                // FIX - In some cases, the site bindings end in a Event ID 1007 when using Windows Server 2012
                // See http://technet.microsoft.com/en-us/library/dd316029(v=ws.10).aspx for more info. As workaround, 
                // we will always start the site
                using (var serverManager = new ServerManager())
                {
                    var site = serverManager.Sites[siteName];
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

        /// <summary>
        /// Called by Windows Azure when the role instance is to be stopped.
        /// </summary>
        /// <remarks>
        ///   <para>
        /// Override the OnStop method to implement any code your role requires to shut down in an orderly fashion.
        ///   </para>
        ///   <para>
        /// This method must return within certain period of time. If it does not, Windows Azure
        /// will stop the role instance.
        ///   </para>
        ///   <para>
        /// A web role can include shutdown sequence code in the ASP.NET Application_End method instead of the OnStop method.
        /// Application_End is called before the Stopping event is raised or the OnStop method is called.
        ///   </para>
        ///   <para>
        /// Any exception that occurs within the OnStop method is an unhandled exception.
        ///   </para>
        /// </remarks>
        public override void OnStop()
        {
            try
            {
                Trace.TraceInformation("Stopping worker role instance {0}...", RoleEnvironment.CurrentRoleInstance.Id);

                Plugins.OnStop();

                // Change the status to Busy
                Busy = true;

                // clean up directory link
                Utils.DeleteSymbolicLink(LocalPath);

                // Remove the share
                if (!string.IsNullOrEmpty(_drivePath))
                {
                    Utils.DeleteShare(Utils.GetSetting("shareName"));
                }

                if (_driveMounted && _drive != null)
                {
                    try
                    {
                        Trace.TraceInformation("Unmounting cloud drive on role {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                        _drive.Unmount();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error while unmounting the cloud drive on role {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    }                    
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while stopping the role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
            }
            base.OnStop();
        }

        /// <summary>
        /// This event is called after configuration changes have been submited to Windows Azure but before they have been applied in this instance
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoleEnvironmentChangingEventArgs" /> instance containing the event data.</param>
        private void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            
            Plugins.RoleEnvironmentOnChanging(sender, e);

            // Implements the changes after restarting the role instance
            foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentConfigurationSettingChange))
            {
                Trace.TraceInformation("Configurations are changing...");
                switch (settingChange.ConfigurationSettingName)
                {
                    case "AcceleratorConnectionString":
                    case "driveName":
                    case "driveSize":
                    case "fileshareUserName":
                    case "fileshareUserPassword":
                    case "shareName":
                    case "driveContainer":
                        if (IsSMBServer)    
                        {
                            Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "Startup.ExternalTasks":
                        Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
                        e.Cancel = true;
                        return;
                }                
            }
        }

        /// <summary>
        /// Roles the environment on status check.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="roleInstanceStatusCheckEventArgs">The <see cref="RoleInstanceStatusCheckEventArgs"/> instance containing the event data.</param>
        private void RoleEnvironmentOnStatusCheck(object sender, RoleInstanceStatusCheckEventArgs roleInstanceStatusCheckEventArgs)
        {
            Plugins.RoleEnvironmentOnStatusCheck(sender, roleInstanceStatusCheckEventArgs);

            if (_busy)
            {
                roleInstanceStatusCheckEventArgs.SetBusy();
            }
        }
    }
}
