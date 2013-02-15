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

        private static string _drivePath;
        private static CloudDrive _drive;

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
            ServicePointManager.DefaultConnectionLimit = 12;

            // Setup OnChanging event
            RoleEnvironment.Changing += RoleEnvironmentOnChanging;

            // Inits the Diagnostic Monitor
            RoleStartupUtils.ConfigureDiagnosticMonitor();


            if (IsSMBServer)
            {
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Server");

                // Mount the drive and publish on the same server
                try
                {
                    // Create windows user accounts for shareing the drive and other FTP related
                    RoleStartupUtils.CreateUserAccounts();

                    // Enable SMB traffic through the firewall
                    EnableSMBFirewallTraffic();

                    // Setup the drive object
                    _drive = RoleStartupUtils.InitializeCloudDrive(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"),
                                                            RoleEnvironment.GetConfigurationSettingValue("driveContainer"),
                                                            RoleEnvironment.GetConfigurationSettingValue("driveName"),
                                                            RoleEnvironment.GetConfigurationSettingValue("driveSize"));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Fatal error on the OnStart event: {0}", ex);
                    throw;
                }
            }
            else
                Trace.TraceInformation("Creating DNNAzure instance as a SMB Client");

            return base.OnStart();
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
            string localPath = RoleEnvironment.GetConfigurationSettingValue("localPath");
            string shareName = RoleEnvironment.GetConfigurationSettingValue("shareName");
            string userName = RoleEnvironment.GetConfigurationSettingValue("fileshareUserName");
            string password = RoleEnvironment.GetConfigurationSettingValue("fileshareUserPassword");

            string logDir = localPath + "\\" + "logs";
            string fileName = RoleEnvironment.CurrentRoleInstance.Id + ".txt";
            string logFilePath = Path.Combine(logDir, fileName);

            while (true)
            {
                try
                {

                    if (RoleStartupUtils.MapNetworkDrive(localPath, shareName, userName, password, (IsSMBServer?"DNNAzure":"SMBServer")))
                    {
                        // TODO Move this setup to OnStart
                        // Setup IIS - Website and FTP site
                        SetupIISSites();

                        while (true)
                        {
                            // write to the log file. Do some retries in the case of failure to avoid false positives
                            RoleStartupUtils.AppendLogEntryWithRetries(logFilePath, 5);

                            // If the file/share becomes inaccessible, AppendAllText will throw an exception and
                            // the worker role will exit, and then get restarted, and then it fill find the new share
                            Thread.Sleep(RoleStartupUtils.SleepTimeAfterSuccessfulPolling);
                        }
                    }
                    Trace.TraceError("Failed to mount {0} on role instance {1}", shareName, RoleEnvironment.CurrentRoleInstance.Id);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Starting remapping process because of error on role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    Thread.Sleep(RoleStartupUtils.SleepTimeBeforeStartToRemap);
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
                if (CreateDNNWebSite(RoleStartupUtils.GetSetting("hostHeaders"),
                                          RoleStartupUtils.GetSetting("localPath") + "\\" + RoleStartupUtils.GetSetting("dnnFolder"),
                                          RoleStartupUtils.GetSetting("fileshareUserName"),
                                          RoleStartupUtils.GetSetting("fileshareUserPassword"),
                                          RoleStartupUtils.GetSetting("managedRuntimeVersion"),
                                          RoleStartupUtils.GetSetting("managedPipelineMode"),
                                          RoleStartupUtils.GetSetting("appPool.IdleTimeout").ToLower() == "infinite" ?
                                                                  TimeSpan.Zero :
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
                var driveMounted = false;
                try
                {
                    Trace.TraceInformation("Competing for mount {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                    RoleStartupUtils.MountCloudDrive(_drive);
                    driveMounted = true;
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

                if (!driveMounted)
                {
                    Thread.Sleep(5000);
                    continue;   // Compete again for the lease
                }

                // Shares the drive
                _drivePath = RoleStartupUtils.ShareDrive(_drive);

                // Setup the website settings
                RoleStartupUtils.SetupWebSiteSettings(_drive);

                // Now, spin checking if the drive is still accessible.
                RoleStartupUtils.WaitForMoutingFailure(_drive);

                // Drive is not accessible. Remove the share
                RoleStartupUtils.DeleteShare(RoleEnvironment.GetConfigurationSettingValue("shareName"));
                _drivePath = "";
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
            if (RoleStartupUtils.EnableSMBFirewallTraffic() != 0)
            {
                throw new ConfigurationErrorsException("Could not setup the firewall rules. See previous errors");
            }

            if (bool.Parse(RoleStartupUtils.GetSetting("FTP.Enabled", "False")))
            {
                // Enable FTP traffic through the firewall
                if (RoleStartupUtils.EnableFTPFirewallTraffic() != 0)
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
        private static bool CreateDNNWebSite(string hostHeaders, string homePath, string userName, string password, 
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

        /// <summary>
        /// Starts the website.
        /// </summary>
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
                // clean up network drives
                RoleStartupUtils.DeleteMappedNetworkDrive();

                // Remove the share
                if (!string.IsNullOrEmpty(_drivePath))
                {
                    RoleStartupUtils.DeleteShare(RoleEnvironment.GetConfigurationSettingValue("shareName"));

                    if (_drive != null)
                    {
                        Trace.TraceInformation("Unmounting cloud drive on role {0}...",
                                               RoleEnvironment.CurrentRoleInstance.Id);
                        _drive.Unmount();
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
        private static void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
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
                        }
                        break;
                }
                // TODO Otherwise, handle the Changed event for the rest of parameters
            }
        }
    }
}
