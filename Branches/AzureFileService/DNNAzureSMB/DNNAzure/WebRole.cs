using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DNNAzure.Components;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.Storage;
using Site = Microsoft.Web.Administration.Site;

namespace DNNAzure
{
    public class WebRole : RoleEntryPoint
    {
        private const string WebSiteName = "DotNetNuke";
        private const string OfflineSiteName = "Offline";
        private const string AppPoolName = "DotNetNukeApp";

        private volatile bool _busy = true;
        internal bool Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                Trace.TraceInformation(_busy ? "Role instance {0} is going Busy" : "Role instance {0} is going Ready",
                                       RoleEnvironment.CurrentRoleInstance.Id);
            }
        }

        private static string _localPath;
        private static string LocalPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_localPath)) return _localPath;
                if (RoleEnvironment.IsEmulated)
                {
                    _localPath = RoleEnvironment.GetConfigurationSettingValue("shareName");
                }
                else
                {
                    var account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(
                        "AcceleratorConnectionString"));
                    _localPath =
                        string.Format(@"\\{0}.file.core.windows.net\{1}", account.Credentials.AccountName,
                            RoleEnvironment.GetConfigurationSettingValue("shareName"));
                }
                return _localPath;
            }
        }

        private PluginsManager _plugins;
        private PluginsManager Plugins
        {
            get { return _plugins ?? (_plugins = new PluginsManager(Utils.GetSetting("Plugins.Url"))); }
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
            try
            {
                #region Tips for performance

                // Set the maximum number of concurrent connections (for Azure Storage better performance)
                // http://social.msdn.microsoft.com/Forums/en-US/windowsazuredata/thread/d84ba34b-b0e0-4961-a167-bbe7618beb83
                ServicePointManager.DefaultConnectionLimit = 1000;
                // int.Parse(Utils.GetSetting("DefaultConnectionLimit", "1000"));

                // Turn off 100-continue (saves 1 roundtrip)
                ServicePointManager.Expect100Continue = false;

                // Turning off Nagle may help Inserts/Updates
                ServicePointManager.UseNagleAlgorithm = false;

                #endregion

                // Setup OnStatuscheck event
                RoleEnvironment.StatusCheck += RoleEnvironmentOnStatusCheck;

                // Setup OnChanging event
                RoleEnvironment.Changing += RoleEnvironmentOnChanging;

                // Setup OnChanged event
                RoleEnvironment.Changed += RoleEnvironmentOnChanged;

                // Inits the Diagnostic Monitor
                Utils.ConfigureDiagnosticMonitor();

                Trace.TraceInformation("Creating DNNAzure instance as a SMB Client");

                // Create Azure Storage File Share 
                Utils.CreateStorageFileShare();

                // Mounts the drive to cache the credentials for current thread
                var account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(
                    "AcceleratorConnectionString"));
                Utils.MountShare(
                    string.Format(@"\\{0}.file.core.windows.net\{1}", account.Credentials.AccountName,
                        RoleEnvironment.GetConfigurationSettingValue(
                            "shareName")), "X:", account.Credentials.AccountName,
                    account.Credentials.ExportBase64EncodedKey());

                // Setup IIS - Website and FTP site
                SetupIisSites();

                // Setup the website settings
                Utils.SetupContents(LocalPath);

                // Setup the offline site settings
                Utils.SetupOfflineContents(LocalPath);

                Plugins.OnStart();

                Utils.UnmountShare("X:");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error on role OnStart: {0}", ex);
            }
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
                        // Setup the offline site contents
                        Utils.SetupOfflineContents(LocalPath);

                        // Ensure that the portal aliases for the offline site have been created (only needed if the AppOffline.Enabled == "true")
                        if (appOfflineEnabled)
                        {
                            Utils.SetupOfflineSitePortalAliases(LocalPath + "\\" + Utils.GetSetting("dnnFolder") + "\\web.config",
                                                                           RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpInOffline"].IPEndpoint.Port,
                                                                           RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HttpsInOffline"].IPEndpoint.Port);
                        }
                        // Setup IIS sites
                        SetupIisSites();
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
                // Change busy status
                Busy = false;

                while (true)
                {
                    Thread.Sleep(Utils.SleepTimeAfterSuccessfulPolling);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fatal error on Run: {0}", ex);
            }
        }

        #region Private functions

        /// <summary>
        /// Setups the IIS sites.
        /// </summary>
        private void SetupIisSites()
        {
            Trace.TraceInformation("Setting up IIS configuration...");
            // Create the DNN Web site
            try
            {
                if (CreateWebSite(Utils.GetSetting("hostHeaders"),
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
                        var externalIp =
                            Utils.GetExternalIP(
                                Utils.GetSetting("FTP.ExternalIpProvider.Url", @"http://checkip.dyndns.org/"),
                                Utils.GetSetting("FTP.ExternalIpProvider.RegexPattern", @"[^\d\.]*"));

                        CreateFtpSite(Utils.GetSetting("hostHeaders"),
                                         Utils.GetSetting("FTP.Root.Username"),
                                         Path.Combine(LocalPath, Utils.GetSetting("dnnFolder")),
                                         WebSiteName,
                                         externalIp);
                        if (!string.IsNullOrEmpty(externalIp))
                        {
                            Utils.RestartService("FTPSVC");
                        }
                    }

                    // Ensure the website is started
                    StartWebsite(WebSiteName);
                    StartWebsite(OfflineSiteName);

                    // Inform the plugins that the site is up and running
                    Plugins.OnSiteReady();
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

            SetupAuditPrivilege();
        }

        /// <summary>
        /// Changes the local security policies needed for WCF services to generate security audits by including the Application pool name to 
        /// the "Local Policies\User Rights Assignment\Generate security audits" policy. This security setting determines which accounts can be 
        /// used by a process to add entries to the security log. The security log is used to trace unauthorized system access. Misuse of this user 
        /// right can result in the generation of many auditing events, potentially hiding evidence of an attack or causing a denial of service 
        /// if the Audit: Shut down system immediately if unable to log security audits security policy setting is enabled. For more information 
        /// see Audit: Shut down system immediately if unable to log security audits.
        /// </summary>
        private static void SetupAuditPrivilege()
        {
            string tmp1 = "", tmp2 = "";
            try
            {
                const string accountToSetup = @"IIS AppPool\" + AppPoolName;
                Trace.TraceInformation("Configuring audit privileges for account {0}...", accountToSetup);

                var principal = new System.Security.Principal.NTAccount(accountToSetup);
                var sid = principal.Translate(typeof(System.Security.Principal.SecurityIdentifier));
                var sidstr = sid.Value;

                if (string.IsNullOrEmpty(sidstr))
                {
                    Trace.TraceWarning("Can't setup adudit privileges: account not found");
                    return;
                }

                // Exports current local security policy
                tmp1 = Path.GetTempFileName();
                string errorDes;
                var error = Utils.ExecuteCommand("secedit.exe", string.Format("/export /cfg \"{0}\"", tmp1), out errorDes,
                    10000);
                if (error != 0)
                {
                    Trace.TraceError("Could not export the local security policy: {0}", errorDes);
                    return;
                }

                // Search for the current values
                var content = File.ReadAllText(tmp1);
                var regEx = new Regex(@"(SeAuditPrivilege\s=\s(?<currentSettingValue>.*\n))");
                var match = regEx.Match(content);
                if (match.Success)
                {
                    // If the security setting has been added just exit
                    if (match.Groups["currentSettingValue"].Success &&
                        match.Groups["currentSettingValue"].Value.Contains(sidstr))
                    {
                        Trace.TraceInformation("Audit privileges already configured for application pool account");
                        return;
                    }

                    // Generate a file to import with the merged SID value
                    var newValue = "*" + sidstr +
                                   (match.Groups["currentSettingValue"].Success
                                       ? "," + match.Groups["currentSettingValue"].Value
                                       : "");
                    var outfileContents = string.Format(@"[Unicode]
Unicode=yes
[Version]
signature=""$CHICAGO$""
Revision=1
[Privilege Rights]
SeAuditPrivilege = {0}
", newValue);
                    tmp2 = Path.GetTempFileName();
                    File.WriteAllText(tmp2, outfileContents, Encoding.Unicode);

                    // Import the security setting
                    error = Utils.ExecuteCommand("secedit.exe", string.Format("/configure /db \"secedit.sdb\" /cfg \"{0}\" /areas USER_RIGHTS ", tmp2), out errorDes,
                        10000);
                    if (error != 0)
                    {
                        Trace.TraceWarning("Failed to setup audit privileges for the App pool identity. See previous errors.");
                    }
                }
                else
                {
                    Trace.TraceError(
                        "'Generate security audits' local security policy not found. Can't setup WCF permissions.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to setup audit privileges for the App pool identity: {0}", ex.Message);
            }
            finally
            {
                // Cleanup
                try
                {
                    if (File.Exists(tmp1))
                    {
                        File.Delete(tmp1);
                    }
                    if (File.Exists(tmp2))
                    {
                        File.Delete(tmp2);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error while deleting the temp files on the Audit privileges: {0}", ex.Message);
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
        /// <returns></returns>
        private static bool CreateFtpSite(string hostHeaders, string rootUsername, string siteRoot, string webSiteName, string externalIP)
        {
            Trace.TraceInformation("Creating FTP site...");
            try
            {
                const string protocol = "ftp";
                var ftproot = Environment.SystemDirectory.Substring(0, 2) + @"\inetpub\ftproot";
                // Internal endpoints not available on the emulator
                var port = RoleEnvironment.IsEmulated
                    ? 21
                    : RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPCmd"].IPEndpoint.Port;
                var ftpPassivePort = RoleEnvironment.IsEmulated
                    ? 20000
                    : RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPDataPassive"].IPEndpoint.Port;
                var ftpSiteName = string.Format("{0}_FTP", webSiteName);
                string[] headers = hostHeaders.Split(';');

                using (var serverManager = new ServerManager())
                {
                    var site = serverManager.Sites.FirstOrDefault(x => x.Name == ftpSiteName);
                    if (site == null)
                    {
                        var localIpAddress = Utils.GetFirstIPv4LocalNetworkAddress();
                        if (localIpAddress == "")
                            localIpAddress = "*";
                        var binding = localIpAddress + ":" + port + ":";
                        Trace.TraceInformation(
                            "Creating FTP (SiteName={0}; Protocol={1}; Bindings={2}; RootPath={3};",
                            ftpSiteName, "ftp", binding, siteRoot);
                        site = serverManager.Sites.Add(ftpSiteName, protocol, localIpAddress + ":" + port + ":", ftproot);

                        for (int i = 1; i < headers.Length; i++)
                            site.Bindings.Add(localIpAddress + ":" + port + ":" + headers[i], protocol);
                    }
                    else
                    {
                        Trace.TraceInformation("FTP site was already created");
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
                        firewallSupportSection["lowDataChannelPort"] = ftpPassivePort;
                        firewallSupportSection["highDataChannelPort"] = ftpPassivePort;
                    }
                    else
                    {
                        Trace.TraceWarning("Cannot enable FTP passive mode");
                    }

                    // Change root folder and permissions
                    var vdir = site.Applications[0].VirtualDirectories["/"];
                    vdir.PhysicalPath = siteRoot;
                    vdir.LogonMethod = AuthenticationLogonMethod.ClearText;
                    vdir.UserName = Utils.GetSetting("fileshareUserName");
                    vdir.Password = Utils.GetSetting("fileshareUserPassword");

                    // Enable authorization for users in the root folder (read only)
                    Trace.TraceInformation("Setting up FTP permissions...");
                    var authorizationSection = config.GetSection("system.ftpServer/security/authorization", ftpSiteName);
                    var authorizationCollection = authorizationSection.GetCollection();
                    authorizationCollection.Clear();
                    var authElement = authorizationCollection.CreateElement("add");
                    authElement["accessType"] = @"Allow";
                    authElement["users"] = rootUsername;
                    authElement["permissions"] = @"Read, Write";
                    authorizationCollection.Add(authElement);

                    // Remove SSL requirement
                    Trace.TraceInformation("Removing SSL requirement for FTP...");
                    site.GetChildElement("ftpServer").GetChildElement("security").GetChildElement("ssl")["controlChannelPolicy"] = 0;
                    site.GetChildElement("ftpServer").GetChildElement("security").GetChildElement("ssl")["dataChannelPolicy"] = 0;

                    // Enable user isolation to "User name directory"
                    Trace.TraceInformation("Setting user isolation to 'None'...");
                    site.GetChildElement("ftpServer").GetChildElement("userIsolation")["mode"] = "None";

                    site.LogFile.Period = LoggingRolloverPeriod.Hourly;
                    site.LogFile.Directory = Path.Combine(RoleEnvironment.GetLocalResource("DiagnosticStore").RootPath,
                        @"LogFiles\Ftp");

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
        private static bool CreateWebSite(string hostHeaders, string homePath, string offlinePath, string userName, string password, string sslThumbprint, string sslHostHeader)
        {


            // Build bindings based on HostHeaders
            Trace.TraceInformation("Creating website...");
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
                if (!RoleEnvironment.IsEmulated)
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

                    site.LogFile.Period = LoggingRolloverPeriod.Hourly;
                    site.LogFile.Directory = Path.Combine(RoleEnvironment.GetLocalResource("DiagnosticStore").RootPath,
                        @"LogFiles\Web");
                    offlineSite.LogFile.Period = site.LogFile.Period;
                    offlineSite.LogFile.Directory = site.LogFile.Directory;


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
            appPool.ProcessModel.LoadUserProfile = true;

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
            var site = serverManager.Sites.FirstOrDefault(x => x.Name == webSiteName);
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
                site.Applications["/"].VirtualDirectories["/"].PhysicalPath = homePath;
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
            Trace.TraceInformation("OnStop called from Worker Role.");
            OnStopCleanup();
            Trace.TraceInformation("Worker role stopped");
        }

        private void OnStopCleanup()
        {
            try
            {
                Trace.TraceInformation("Stopping worker role instance {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                // Change the status to Busy
                Busy = true;
                Plugins.OnStop();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while stopping the role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
            }            
            // At least we will have the trace events in the event viewer
            Trace.Flush();
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

            if (Busy)
            {
                roleInstanceStatusCheckEventArgs.SetBusy();
            }
        }
    }
}