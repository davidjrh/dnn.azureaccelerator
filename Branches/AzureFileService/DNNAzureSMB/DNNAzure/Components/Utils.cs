#region Usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;

#endregion

namespace DNNAzure.Components
{
    /// <summary>
    /// Role startup utilities
    /// </summary>
    public class Utils
    {
        public const int SleepTimeAfterSuccessfulPolling = 10000;
        public const int SleepTimeBetweenWriteErrors = 1000;
        public const int SleepTimeBeforeStartToRemap = 5000;


        #region Network share mount
        [DllImport("Mpr.dll",
                    EntryPoint = "WNetAddConnection2",
                    CallingConvention = CallingConvention.Winapi)]
        private static extern int WNetAddConnection2(NETRESOURCE lpNetResource,
                                                     string lpPassword,
                                                     string lpUsername,
                                                     System.UInt32 dwFlags);

        [DllImport("Mpr.dll",
                   EntryPoint = "WNetCancelConnection2",
                   CallingConvention = CallingConvention.Winapi)]
        private static extern int WNetCancelConnection2(string lpName,
                                                        System.UInt32 dwFlags,
                                                        System.Boolean fForce);

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope;
            public ResourceType dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        };

        public enum ResourceType
        {
            RESOURCETYPE_DISK = 1,
        };

        public static int MountShare(string shareName,
                              string driveLetterAndColon,
                              string username,
                              string password)
        {
            if (RoleEnvironment.IsEmulated)
                return 0;

            if (string.IsNullOrEmpty(shareName)) throw new ArgumentNullException("shareName");
            if (string.IsNullOrEmpty(driveLetterAndColon)) throw new ArgumentNullException("driveLetterAndColon");
            Trace.TraceInformation("Mounting share {0}...", shareName);

            // Make sure we aren't using this driveLetter for another mapping
            WNetCancelConnection2(driveLetterAndColon, 0, true);

            var nr = new NETRESOURCE();
            nr.dwType = ResourceType.RESOURCETYPE_DISK;
            nr.lpRemoteName = shareName;
            nr.lpLocalName = driveLetterAndColon;

            return WNetAddConnection2(nr, password, username, 0);
        }

        public static int UnmountShare(string driveLetterAndColon)
        {
            if (string.IsNullOrEmpty(driveLetterAndColon)) throw new ArgumentNullException("driveLetterAndColon");
            return WNetCancelConnection2(driveLetterAndColon, 0, true);
        }

        public static string GetRemoteSharePath()
        {
            if (RoleEnvironment.IsEmulated)
                return RoleEnvironment.GetConfigurationSettingValue("shareName");

            var credentials =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"))
                    .Credentials;
            credentials.ExportBase64EncodedKey();


            var accountName =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"))
                    .Credentials.AccountName;
            return string.Format(@"\\{0}.file.core.windows.net\{1}", accountName, RoleEnvironment.GetConfigurationSettingValue(
                "shareName"));
        }

        #endregion

        #region Cloud Storage operations

        public static void CreateStorageFileShare()
        {
            if (RoleEnvironment.IsEmulated)
                return;
            var account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(
                "AcceleratorConnectionString"));
            var client = account.CreateCloudFileClient();
            var share = client.GetShareReference(RoleEnvironment.GetConfigurationSettingValue(
                "shareName"));
            share.CreateIfNotExistsAsync().Wait();
        }
        #endregion

        #region DotNetNuke setup utilities
        /// <summary>
        /// Setups the web site settings.
        /// </summary>
        public static void SetupContents(string localPath)
        {
            // Check for the database existence
            Trace.TraceInformation("Checking for database existence...");
            if (!SetupDatabase(GetSetting("DBAdminUser"),
                                        GetSetting("DBAdminPassword"),
                                        GetSetting("DatabaseConnectionString")))
                Trace.TraceError("Error while setting up the database. Check previous messages.");

            // Check for the creation of the Website contents from Azure storage
            Trace.TraceInformation("Check for website content...");
            if (!SetupWebSiteContents(localPath + "\\" + GetSetting("dnnFolder"),
                                                    GetSetting("AcceleratorConnectionString"),
                                                    GetSetting("packageContainer"),
                                                    GetSetting("package"),
                                                    GetSetting("packageUrl")))
                Trace.TraceError("Website content could not be prepared. Check previous messages.");


            // Setup Database Connection string
            SetupWebConfig(localPath + "\\" + GetSetting("dnnFolder") + "\\web.config",
                                            GetSetting("DatabaseConnectionString"),
                                            GetSetting("InstallationDate"),
                                            GetSetting("UpdateService.Source"));

            // Setup DotNetNuke.install.config
            SetupInstallConfig(
                                Path.Combine(new[]
                                                         {
                                                             localPath, GetSetting("dnnFolder"),
                                                             "Install\\DotNetNuke.install.config"
                                                         }),
                                GetSetting("AcceleratorConnectionString"),
                                GetSetting("packageContainer"),
                                GetSetting("packageInstallConfiguration"));

            // Setup post install addons (always overwrite)
            InstallAddons(GetSetting("AddonsUrl"),
                                            localPath + "\\" + GetSetting("dnnFolder"));            

        }


        /// <summary>
        /// Setups the offline site settings.
        /// </summary>
        /// <param name="contentsRoot">The contents root.</param>
        public static void SetupOfflineContents(string contentsRoot)
        {
            try
            {
                Trace.TraceInformation("Setting up offline site contents...");
                var offlineRoot = contentsRoot + "\\" +
                                  GetSetting("AppOffline.Folder");
                if (!Directory.Exists(offlineRoot))
                {
                    Trace.TraceInformation("Initializing offline site contents...");
                    Directory.CreateDirectory(offlineRoot);
                }

                var customAppOffline = contentsRoot + "\\" +
                                       GetSetting("dnnFolder") +
                                       "\\Portals\\_default\\App_Offline.htm";
                var defaultAppOffline = Path.Combine(Environment.CurrentDirectory, "html", "App_Offline.htm");

                if (File.Exists(customAppOffline))
                {
                    Trace.TraceInformation("Using custom App_Offline.htm file...");
                    File.Copy(customAppOffline, Path.Combine(offlineRoot, "App_Offline.htm"), true);
                }
                else
                {
                    Trace.TraceInformation("Using default App_Offline.htm file...");
                    File.Copy(defaultAppOffline, Path.Combine(offlineRoot, "App_Offline.htm"), true);                    
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while setting up the offline site contents: {0}", ex);
            }
        }

        /// <summary>
        /// Try to setup the database if not exists
        /// </summary>
        /// <param name="dbAdmin">User with admin permissions on the server</param>
        /// <param name="dbPassword">Password for the admin user</param>
        /// <param name="dbConnectionString">Connection string of the database to setup</param>
        /// <returns></returns>
        public static bool SetupDatabase(string dbAdmin, string dbPassword, string dbConnectionString)
        {
            try
            {
                // Check for Database existence
                try
                {
                    using (var dbConn = new SqlConnection(dbConnectionString))
                        dbConn.Open();
                    Trace.TraceInformation("Database connection success!");
                    return true;
                }
                catch
                {
                    Trace.TraceWarning("Can't connect to the database with specified connection string. Trying to create database...");
                }

                if (string.IsNullOrEmpty(dbAdmin) || string.IsNullOrEmpty(dbPassword))
                {
                    Trace.TraceError("Database admin user or password is empty. Can't connect to the server to check for database existence.");
                    return false;
                }

                // Connect to the database as admin user
                var connBuilderOriginal = new SqlConnectionStringBuilder(dbConnectionString);
                var connBuilder = new SqlConnectionStringBuilder(dbConnectionString) { UserID = dbAdmin, Password = dbPassword, InitialCatalog = "master" };

                try
                {
                    using (var dbConn = new SqlConnection(connBuilder.ConnectionString))
                    {
                        dbConn.Open();

                        // Check for the database
                        var cmd = new SqlCommand("SELECT COUNT(NAME) FROM sys.databases WHERE NAME=@dbName", dbConn);
                        cmd.Parameters.AddWithValue("dbName", connBuilderOriginal.InitialCatalog);
                        bool dbExists = (int)cmd.ExecuteScalar() != 0;

                        // If not exists, create the database
                        if (!dbExists)
                        {
                            // Create the database
                            Trace.TraceInformation("The specified database does not exist, creating new database...");
                            var cmdC1 = new SqlCommand(string.Format("CREATE DATABASE {0}", connBuilderOriginal.InitialCatalog), dbConn);
                            cmdC1.ExecuteNonQuery();

                            // Check for the principal existence
                            Trace.TraceInformation("Check for the user login existence...");
                            var cmd2 = new SqlCommand("SELECT COUNT(NAME) FROM sys.sql_logins WHERE NAME=@loginName", dbConn);
                            cmd2.Parameters.AddWithValue("loginName", connBuilderOriginal.UserID);
                            if ((int)cmd2.ExecuteScalar() == 0)
                            {
                                Trace.TraceInformation("User login does not exist, creating user login...");
                                var cmdUs = new SqlCommand(string.Format("CREATE LOGIN {0} WITH PASSWORD = '{1}'", connBuilderOriginal.UserID, connBuilderOriginal.Password), dbConn);
                                cmdUs.ExecuteNonQuery();
                            }

                            // Setup permisssions connecting to the new database with the admin credentials
                            connBuilder.InitialCatalog = connBuilderOriginal.InitialCatalog;
                            using (var dbConnNew = new SqlConnection(connBuilder.ConnectionString))
                            {
                                dbConnNew.Open();
                                var cmdC3 = new SqlCommand(string.Format("CREATE USER {0} FROM LOGIN {1}", connBuilderOriginal.UserID, connBuilderOriginal.UserID), dbConnNew);
                                cmdC3.ExecuteNonQuery();
                                var cmdC4 = new SqlCommand(string.Format("EXEC sp_addrolemember 'db_owner', '{0}'", connBuilderOriginal.UserID), dbConnNew);
                                cmdC4.ExecuteNonQuery();
                                return true;
                            }
                        }
                        Trace.TraceError("The specified database exists, but can't login with the specified credentials.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Can't connect to the database server as dbAdmin: " + ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error on the database setup process: " + ex);
                return false;
            }
        }

        public static SqlConnectionSettings GetSqlConnectionSettings(string webConfigPath)
        {
            var settings = new SqlConnectionSettings();
            var webconfig = new ConfigXmlDocument();
            webconfig.Load(webConfigPath);

            var csNode = webconfig.SelectSingleNode("/configuration/connectionStrings/add[@name='SiteSqlServer']");
            if (csNode != null)
            {
                if (csNode.Attributes["connectionString"] != null)
                {
                    settings.ConnectionString = csNode.Attributes["connectionString"].Value;
                }
                if (csNode.Attributes["databaseOwner"] != null)
                {
                    settings.DatabaseOwner = csNode.Attributes["databaseOwner"].Value;
                }
                if (csNode.Attributes["objectQualifier"] != null)
                {
                    settings.ObjectQualifier = csNode.Attributes["objectQualifier"].Value;
                }

            }
            return settings;
        }

        /// <summary>
        /// Setups the offline site portal aliases.
        /// </summary>
        /// <param name="dbConnectionString">The db connection string.</param>
        /// <param name="offlinePort">The offline port.</param>
        /// <param name="offlinePortSsl">The offline port SSL.</param>
        public static void SetupOfflineSitePortalAliases(string webConfigPath, int offlinePort, int offlinePortSsl)
        {
            try
            {
                Trace.TraceInformation("Setting offline site portal aliases...");
                bool aliasAdded = false;

                var dbSettings = GetSqlConnectionSettings(webConfigPath);
                using (var dbConn = new SqlConnection(dbSettings.ConnectionString))
                {
                    dbConn.Open();

                    // Search portal aliases not including port
                    var cmd =
                        new SqlCommand(
                            string.Format(
                                "SELECT PortalID, HTTPAlias FROM [{0}].[{1}PortalAlias] WHERE HTTPAlias NOT LIKE '%:%'",
                                dbSettings.DatabaseOwner, dbSettings.ObjectQualifier), dbConn);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var httpAlias = new UriBuilder((string)rdr["HTTPAlias"]) { Port = offlinePort }.ToString().Replace("http://", "");
                            var httpAliasSsl = new UriBuilder((string)rdr["HTTPAlias"]) { Port = offlinePortSsl }.ToString().Replace("http://", "");
                            if (httpAlias.EndsWith("/")) httpAlias = httpAlias.Substring(0, httpAlias.Length - 1);
                            if (httpAliasSsl.EndsWith("/")) httpAliasSsl = httpAliasSsl.Substring(0, httpAliasSsl.Length - 1);

                            using (var dbConn2 = new SqlConnection(dbSettings.ConnectionString))
                            {
                                dbConn2.Open();
                                var sql = string.Format("INSERT INTO [{0}].[{1}PortalAlias] ([PortalID],[HTTPAlias],[CreatedByUserID],[CreatedOnDate],[LastModifiedByUserID],[LastModifiedOnDate]) " +
                                                   "VALUES (@PortalID, @HTTPAlias, -1, GETDATE(), -1, GETDATE())", dbSettings.DatabaseOwner, dbSettings.ObjectQualifier);

                                if (!HttpAliasExists(dbConn2, (int)rdr["PortalID"], httpAlias))
                                {
                                    Trace.TraceInformation("Adding portal alias '{0}'...", rdr["HTTPAlias"] + ":" + offlinePort.ToString());
                                    var cmdIns = new SqlCommand(sql, dbConn2);
                                    cmdIns.Parameters.AddWithValue("PortalID", rdr["PortalID"]);
                                    cmdIns.Parameters.AddWithValue("HTTPAlias", httpAlias);
                                    cmdIns.ExecuteNonQuery();
                                    aliasAdded = true;
                                }

                                if (GetSetting("SSL.CertificateThumbprint") != "" && !HttpAliasExists(dbConn2, (int)rdr["PortalID"], httpAliasSsl))
                                {
                                    Trace.TraceInformation("Adding portal alias '{0}'...", rdr["HTTPAlias"] + ":" + offlinePortSsl.ToString());
                                    var cmdInsSsl = new SqlCommand(sql, dbConn2);
                                    cmdInsSsl.Parameters.AddWithValue("PortalID", rdr["PortalID"]);
                                    cmdInsSsl.Parameters.AddWithValue("HTTPAlias", httpAliasSsl);
                                    cmdInsSsl.ExecuteNonQuery();
                                    aliasAdded = true;
                                }
                            }
                        }
                    }
                }

                // If any alias was added, touch the web.config file to reload the aliases
                if (aliasAdded)
                {
                    TouchFile(webConfigPath);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while setting the offline site portal aliases: {0}", ex);
            }
        }

        public static void TouchFile(string filePath)
        {
            var webConfigPath = filePath;
            File.SetLastWriteTime(webConfigPath, DateTime.Now);
        }

        private static bool HttpAliasExists(SqlConnection dbConn, int portalId, string httpAlias)
        {
            var cmd = new SqlCommand("SELECT COUNT(HTTPAlias) FROM PortalAlias WHERE PortalID=@PortalID AND HTTPAlias=@HTTPAlias", dbConn);
            cmd.Parameters.AddWithValue("PortalID", portalId);
            cmd.Parameters.AddWithValue("HTTPAlias", httpAlias);
            return (int)cmd.ExecuteScalar() != 0;
        }

        /// <summary>
        /// Modifies web.config to setup database connection settings and other appSettings
        /// </summary>
        /// <param name="webConfigPath">Path to the web config file</param>
        /// <param name="databaseConnectionString">Database connection string</param>
        /// <param name="installationDate"></param>
        /// <param name="source"></param>
        public static bool SetupWebConfig(string webConfigPath, string databaseConnectionString, string installationDate, string source)
        {
            bool success = false;
            try
            {
                // Modifiy web.config settings: connection string, appsettings
                Trace.TraceInformation("Modifying web.config settings to set database connection string");
                var webconfig = new ConfigXmlDocument();
                webconfig.Load(webConfigPath);

                var csNode = webconfig.SelectSingleNode("/configuration/connectionStrings/add[@name='SiteSqlServer']");
                if (csNode != null && csNode.Attributes["connectionString"] != null)
                    csNode.Attributes["connectionString"].Value = databaseConnectionString;

                var bcNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='SiteSqlServer']");
                if (bcNode != null && bcNode.Attributes["value"] != null)
                    bcNode.Attributes["value"].Value = databaseConnectionString;

                // Modify web.config settings: setting up "IsWebFarm" setting
                var wfNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='IsWebFarm']");
                if (wfNode == null)
                {
                    wfNode = webconfig.CreateElement("add");
                    var attkey = webconfig.CreateAttribute("key");
                    attkey.Value = "IsWebFarm";
                    wfNode.Attributes.Append(attkey);
                    var attvalue = webconfig.CreateAttribute("value");
                    attvalue.Value = "true";
                    wfNode.Attributes.Append(attvalue);
                    webconfig.SelectSingleNode("/configuration/appSettings").AppendChild(wfNode);
                }
                wfNode.Attributes["value"].Value = "true";

                // Modify web.config settings: setting up "InstallationDate" setting
                DateTime installationDateValue;
                if (!string.IsNullOrEmpty(installationDate) && DateTime.TryParse(installationDate, out installationDateValue))
                {
                    var idNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='InstallationDate']");
                    if (idNode == null)
                    {
                        idNode = webconfig.CreateElement("add");
                        var attkey = webconfig.CreateAttribute("key");
                        attkey.Value = "IsWebFarm";
                        idNode.Attributes.Append(attkey);
                        var attvalue = webconfig.CreateAttribute("value");
                        attvalue.Value = installationDate;
                        idNode.Attributes.Append(attvalue);
                        webconfig.SelectSingleNode("/configuration/appSettings").AppendChild(idNode);
                    }
                    idNode.Attributes["value"].Value = installationDate;                       
                }

                // Modify web.config settings: setting up "Source" setting
                if (!string.IsNullOrEmpty(source))
                {
                    var srNode = webconfig.SelectSingleNode("/configuration/appSettings/add[@key='Source']");
                    if (srNode == null)
                    {
                        srNode = webconfig.CreateElement("add");
                        var attkey = webconfig.CreateAttribute("key");
                        attkey.Value = "Source";
                        srNode.Attributes.Append(attkey);
                        var attvalue = webconfig.CreateAttribute("value");
                        attvalue.Value = source;
                        srNode.Attributes.Append(attvalue);
                        webconfig.SelectSingleNode("/configuration/appSettings").AppendChild(srNode);
                    }
                    srNode.Attributes["value"].Value = source;                    
                }
                
                    
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
        /// Checks for the existence of the web site contents. If it does not exist, downloads the content package
        /// from Azure storage and unzip the contents
        /// </summary>
        /// <param name="webSitePath">Web site root path</param>
        /// <param name="storageConnectionString">Azure storage connection string for downloading the package</param>
        /// <param name="packageContainer">Azure storage blob container name where the package resides</param>
        /// <param name="packageName">Azure storage blob name of the package</param>
        /// <param name="packageUrl">Url of an external package. If not empty, will try to download first.</param>
        /// <returns></returns>
        public static bool SetupWebSiteContents(string webSitePath, string storageConnectionString, string packageContainer, string packageName, string packageUrl)
        {
            try
            {
                // Create website folder if not exists
                if (!Directory.Exists(webSitePath))
                {
                    Trace.TraceInformation(string.Format("Creating folder '{0}'...", webSitePath));
                    Directory.CreateDirectory(webSitePath);
                }
                const string localDnnPackageFilename = "DNNPackage.zip";
                string packageFile = Path.Combine(RoleEnvironment.GetLocalResource("WebPITemp").RootPath, localDnnPackageFilename);
                // Delete previous failed attemps of web site creation
                if (File.Exists(packageFile))
                {
                    Trace.TraceWarning(string.Format("Deleting previous failed package deployment '{0}'...", packageFile));
                    File.Delete(packageFile);
                }

                // Check for folder content, and if it's empty, import content from Azure Storage or an external Url (i.e. CodePlex)
                var wsFolder = new DirectoryInfo(webSitePath);
                if (wsFolder.GetFiles().Length == 0 && wsFolder.GetDirectories().Length == 0)
                {
                    Trace.TraceInformation("Web site content not initialized. Downloading package...");

                    // Try to download from Url first
                    bool downloaded = DownloadLatestDNNCEPackage(packageFile, packageUrl);

                    if (!downloaded)
                    {
                        Trace.TraceInformation("Downloading package from Azure storage...");
                        var account = CloudStorageAccount.Parse(storageConnectionString);
                        var blobClient = account.CreateCloudBlobClient();

                        Trace.TraceInformation("Locating package container: " + packageContainer);
                        var blobContainer = blobClient.GetContainerReference(packageContainer);
                        blobContainer.FetchAttributes(); // Check for the container existence

                        Trace.TraceInformation("Downloading package '{0}' to '{1}'...", packageName, packageFile);
                        blobContainer.GetBlockBlobReference(packageName).DownloadToFile(packageFile, FileMode.CreateNew);                        
                    }

                    // Unzip downloaded file
                    Trace.TraceInformation("Decompressing package...");
                    try
                    {
                        UnzipFile(packageFile, webSitePath);
                    }
                    catch (CompressOperationException ex)
                    {
                        Trace.TraceError("Error while decompresing the package: " + ex.Message);
                        return false;
                    }


                    Trace.TraceInformation(string.Format("Deleting package file '{0}'...", packageFile));
                    File.Delete(packageFile);

                    Trace.TraceInformation("Web site content successfully deployed.");
                }
                else
                    Trace.TraceInformation("The content already exists. No action taken.");
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while web site contents setup: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Downloads a customized installation configuration file to local file system
        /// </summary>
        /// <param name="localInstallConfig">Path to the DotNetNuke.install.config file</param>
        /// <param name="storageConnectionString">Azure storage connection string</param>
        /// <param name="packageContainer">Container where the customized installation configuration file resides</param>
        /// <param name="packageInstallConfig">Customized installation configuration file stored on Azure Storage</param>
        public static void SetupInstallConfig(string localInstallConfig, string storageConnectionString, string packageContainer, string packageInstallConfig)
        {
            if (!string.IsNullOrEmpty(packageInstallConfig))
            {
                try
                {

                    Trace.TraceInformation("Check for customized installation configuration settings...");
                    var account = CloudStorageAccount.Parse(storageConnectionString);
                    var blobClient = account.CreateCloudBlobClient();
                    var blobContainer = blobClient.GetContainerReference(packageContainer);

                    Trace.TraceInformation("Downloading customized installation settings '{0}' to '{1}'...", packageInstallConfig, localInstallConfig);
                    blobContainer.GetBlockBlobReference(packageInstallConfig).DownloadToFile(localInstallConfig, FileMode.CreateNew);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while downloading customized installation file: {0}", ex.Message);
                }

            }
        }

        /// <summary>
        /// Gets the external IP of the current host
        /// </summary>
        /// <param name="providerUrl">The web page that we are using to get the IP</param>
        /// <param name="regexPattern">The regex pattern to get the IP from the web page returned by this url</param>
        /// <returns>External IP address of the current host</returns>
        public static string GetExternalIP(string providerUrl, string regexPattern)
        {
            var direction = "";
            try
            {
                var regReplace = new Regex(regexPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                var request = WebRequest.Create(providerUrl);
                using (var response = request.GetResponse())
                {
                    using (var stream = new StreamReader(response.GetResponseStream()))
                    {
                        direction = stream.ReadToEnd();
                    }
                }
                direction = regReplace.Replace(direction, "");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while getting the external IP address: {0}", ex.Message);
            }
            return direction;
        }

        /// <summary>
        /// Returns the first IPv4 local IP Network Address
        /// </summary>
        /// <returns></returns>
        public static string GetFirstIPv4LocalNetworkAddress()
        {
            // Get host name
            var strHostName = Dns.GetHostName();
            // Find host by name
            var iphostentry = Dns.GetHostEntry(strHostName);
            var localAddress = iphostentry.AddressList.FirstOrDefault(
                    x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return localAddress != null ? localAddress.ToString() : "";
        }

        public static string GetSetting(string key, string defaultValue = "")
        {
            if (RoleEnvironment.IsAvailable)
            {
                //try
                //{
                //        var prefix = RoleEnvironment.GetConfigurationSettingValue("Prefix");
                //        return RoleEnvironment.GetConfigurationSettingValue(prefix + key);
                //}
                //catch (RoleEnvironmentException)  // The configuration setting that was being retrieved does not exist.
                //{}
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(key);
                }
                catch (RoleEnvironmentException)  // The configuration setting that was being retrieved does not exist.
                {}
            }

            return ConfigurationManager.AppSettings.AllKeys.Contains(key) ? ConfigurationManager.AppSettings[key] : defaultValue;
        }

        #endregion

        #region Diagnostic monitor initialization

        /// <summary>
        /// Initializes the DiagnosticMonitor
        /// </summary>
        public static void ConfigureDiagnosticMonitor()
        {
            Trace.TraceInformation("Configuring diagnostic monitor...");
            var diagnosticsConnectionString =
                RoleEnvironment.GetConfigurationSettingValue(
                    "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");

            // TODO put these settings on the service configuration file
            var transferPeriod = TimeSpan.FromMinutes(1);
            var transferDirectoriesPeriod = TimeSpan.FromMinutes(5);
            var bufferQuotaInMB = 512;

            // Add Windows Azure Trace Listener
            Trace.Listeners.Add(new DiagnosticMonitorTraceListener());
            Trace.Listeners.Add(new EventLogTraceListener("DotNetNuke"));

            // Enable Collection of Crash Dumps
            CrashDumps.EnableCollection(true);

            // Get the Default Initial Config
            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Windows Azure Logs
            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.BufferQuotaInMB = bufferQuotaInMB;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            // File-based logs
            config.Directories.ScheduledTransferPeriod = transferDirectoriesPeriod;
            config.Directories.BufferQuotaInMB = bufferQuotaInMB;

            config.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = transferPeriod;
            config.DiagnosticInfrastructureLogs.BufferQuotaInMB = bufferQuotaInMB;
            config.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogLevel.Warning;

            // Windows Event logs
            config.WindowsEventLog.DataSources.Add("Application!*");
            config.WindowsEventLog.DataSources.Add("System!*");
            config.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;
            config.WindowsEventLog.ScheduledTransferLogLevelFilter = LogLevel.Information;
            config.WindowsEventLog.BufferQuotaInMB = bufferQuotaInMB;

            // Performance Counters
            var counters = new List<string> {
                @"\Processor(_Total)\% Processor Time",
                @"\Memory\Available MBytes"};

            counters.ForEach(
                counter => config.PerformanceCounters.DataSources.Add(
                    new PerformanceCounterConfiguration { CounterSpecifier = counter, SampleRate = TimeSpan.FromSeconds(60) }));
            config.PerformanceCounters.ScheduledTransferPeriod = transferPeriod;
            config.PerformanceCounters.BufferQuotaInMB = bufferQuotaInMB;

            // Stores the diagnostics configuration in storage
            try
            {
                var diagManager = new DeploymentDiagnosticManager(diagnosticsConnectionString,
                    RoleEnvironment.DeploymentId);
                var roleInstanceManager =
                    diagManager.GetRoleInstanceDiagnosticManager(RoleEnvironment.CurrentRoleInstance.Role.Name,
                        RoleEnvironment.CurrentRoleInstance.Id);
                roleInstanceManager.SetCurrentConfiguration(config);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error while updating the diagnostics configuration in storage: " + ex.Message);
            }

            // Starts the diagnostics monitor
            try
            {
                DiagnosticMonitor.StartWithConnectionString(diagnosticsConnectionString, config);
            }
            catch (ArgumentException aex)
            {
                 Trace.WriteLine("Couldn’t start the DiagnosticMonitor." + aex.Message);
            }
            
            Trace.TraceInformation("Diagnostics Setup complete");
        }

        #endregion


        #region Zip

        #region Public Methods

        /// <summary>
        /// GZips the file into a zip file
        /// </summary>
        /// <param name="sourceFileName">Source file name</param>
        /// <param name="destinationFile">Target destination .zip file</param>
        /// <param name="deleteDestinationFile">Overwrites the destination .zip file deleting the previous one</param>
        /// <param name="password">Password for the zip file</param>
        public static void ZipFile(string sourceFileName, string destinationFile, bool deleteDestinationFile = false, string password = "")
        {
            if (sourceFileName == null) throw new ArgumentNullException("sourceFileName");
            if (destinationFile == null) throw new ArgumentNullException("destinationFile");

            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException(string.Format("Source file '{0}' not found", sourceFileName));
            if (File.Exists(destinationFile))
            {
                if (!deleteDestinationFile)
                    throw new CompressOperationException("Detination file already exists");
                File.Delete(destinationFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

            try
            {
                using (var s = new ZipOutputStream(File.Create(destinationFile)))
                {
                    s.SetLevel(3); // 0 - store only to 9 - means best compression
                    s.Password = password;

                    var buffer = new byte[4096];

                    var entry = new ZipEntry(Path.GetFileName(sourceFileName));
                    entry.DateTime = new FileInfo(sourceFileName).LastWriteTime;
                    s.PutNextEntry(entry);

                    using (var fs = File.OpenRead(sourceFileName))
                    {
                        // Using a fixed size buffer here makes no noticeable difference for output
                        // but keeps a lid on memory usage.
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            s.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                    s.Finish();
                    s.Close();
                }
            }
            catch (Exception ex)
            {
                throw new CompressOperationException("An exception occurred while compressing the folder contents", ex);
            }
        }

        /// <summary>
        /// GZips the folder contents into a zip file
        /// </summary>
        /// <param name="sourceFolderName">Source folder name</param>
        /// <param name="destinationFile">Target destination .zip file</param>
        /// <param name="deleteDestinationFile">Overwrites the destination .zip file deleting the previous one</param>
        /// <param name="password">Password for the zip file</param>
        public static void ZipFolder(string sourceFolderName, string destinationFile, bool deleteDestinationFile = false, string password = "")
        {
            if (sourceFolderName == null) throw new ArgumentNullException("sourceFolderName");
            if (destinationFile == null) throw new ArgumentNullException("destinationFile");

            if (!Directory.Exists(sourceFolderName))
                throw new DirectoryNotFoundException(string.Format("Source folder '{0}' not found", sourceFolderName));
            if (File.Exists(destinationFile))
            {
                if (!deleteDestinationFile)
                    throw new CompressOperationException("Detination file already exists");
                File.Delete(destinationFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

            try
            {

                var fastZip = new FastZip
                {
                    CreateEmptyDirectories = true,
                    RestoreAttributesOnExtract = true,
                    RestoreDateTimeOnExtract = true,
                    Password = password
                };
                fastZip.CreateZip(destinationFile, sourceFolderName, true, "");

            }
            catch (Exception ex)
            {
                throw new CompressOperationException("An exception occurred while compressing the folder contents", ex);
            }

        }

        /// <summary>
        /// Unzips a compressed file into a folder
        /// </summary>
        /// <param name="sourceFile">Source compressed .zip file</param>
        /// <param name="destinationFolder">Destination folder</param>
        /// <param name="deleteDestination">Deletes the destination folder if exists</param>
        /// <param name="password">Password for the zip file</param>
        public static void UnzipFile(string sourceFile, string destinationFolder, bool deleteDestination = false, string password = "")
        {
            if (sourceFile == null) throw new ArgumentNullException("sourceFile");
            if (destinationFolder == null) throw new ArgumentNullException("destinationFolder");

            if (!File.Exists(sourceFile))
                throw new FileNotFoundException("Can not find the source file", sourceFile);

            if (Directory.Exists(destinationFolder))
            {
                if (deleteDestination)
                    Directory.Delete(destinationFolder, true);
            }

            Directory.CreateDirectory(destinationFolder);

            try
            {
                var fastZip = new FastZip
                {
                    CreateEmptyDirectories = true,
                    RestoreAttributesOnExtract = true,
                    RestoreDateTimeOnExtract = true,
                    Password = password
                };
                fastZip.ExtractZip(sourceFile, destinationFolder, "");
            }
            catch (Exception ex)
            {
                throw new CompressOperationException("An exception occurred while unzipping the file", ex);
            }


        }

        #endregion

        #region Private functions

        private static void AddFolderToZip(string rootFolderName, string folderName, byte[] buffer, ZipOutputStream zipOutput)
        {
            // Get all the filenames including subfolders
            var filenames = Directory.GetFiles(Path.Combine(rootFolderName, folderName));

            // Create the folder zip entry if it is an empty folder
            if ((folderName != "") && (filenames.Length == 0))
            {
                var fentry = new ZipEntry(folderName + "/");
                fentry.DateTime = new DirectoryInfo(folderName).LastWriteTime;
                zipOutput.PutNextEntry(fentry);
            }

            // Add all the subdirectories
            var subFolders = Directory.GetDirectories(Path.Combine(rootFolderName, folderName));
            foreach (var subFolder in subFolders)
                AddFolderToZip(rootFolderName, subFolder.Substring(rootFolderName.Length + 1), buffer, zipOutput);


            foreach (var file in filenames)
            {

                // Using GetFileName makes the result compatible with XP
                // as the resulting path is not absolute.
                var entry = new ZipEntry(Path.Combine(folderName, Path.GetFileName(file)));

                // Setup the entry data as required.                        

                // Crc and size are handled by the library for seakable streams
                // so no need to do them here.

                // Could also use the last write time or similar for the file.
                entry.DateTime = new FileInfo(file).LastWriteTime;

                //entry. = Path.Combine(sourceFolderName, Path.GetFileName(file));
                zipOutput.PutNextEntry(entry);

                using (var fs = File.OpenRead(file))
                {
                    // Using a fixed size buffer here makes no noticeable difference for output
                    // but keeps a lid on memory usage.
                    int sourceBytes;
                    do
                    {
                        sourceBytes = fs.Read(buffer, 0, buffer.Length);
                        zipOutput.Write(buffer, 0, sourceBytes);
                    } while (sourceBytes > 0);
                }
            }
        }
        #endregion

        #endregion 

        #region Non-managed code utilities

        public static  int RestartService(string serviceName)
        {
            int exitCode = 0;
            Trace.TraceInformation("Restarting service {0}...", serviceName);
            string error;
            exitCode |= ExecuteCommand("net.exe", "stop " + serviceName, out error, 10000);
            exitCode |= ExecuteCommand("net.exe", "start " + serviceName, out error, 10000);

            if (exitCode != 0) {
                Trace.TraceWarning("There was an error while starting the {0} service: {1}", serviceName, error);
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
            var p = new Process
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            p.Start();
            error = p.StandardError.ReadToEnd();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeout);
            int exitCode = p.ExitCode;
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
        /// <param name="traceError">Trace error if the exit code is not zero</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string error, int timeout, bool traceError = true)
        {
            string output;
            int exitCode = ExecuteCommand(exe, arguments, out output, out error, timeout);
            if (exitCode != 0 && traceError)
            {
                Trace.TraceWarning("Error executing command: {0}", error);
            }
            return exitCode;
        }
        #endregion

        #region CodePlex package download

        public static bool DownloadLatestDNNCEPackage(string destinationFile, string packageUrl = "")
        {
            if (string.IsNullOrEmpty(destinationFile)) throw new ArgumentNullException("destinationFile");

            if (string.IsNullOrEmpty(packageUrl))
            {
                Trace.TraceInformation("Package Url not specified");
                return false;
            }
                
            try
            {
                // Create a new WebClient instance.
                var myWebClient = new WebClient();
                
                //TODO Change the DotNetNuke-Appgallery to DotNetNuke-Cloud
                myWebClient.Headers.Add("User-Agent", "DotNetNuke-Appgallery/1.0.0.0(Microsoft Windows NT 6.1.7600.0)");

                Trace.TraceInformation(string.Format("Downloading file '{0}' from '{1}'...", destinationFile, packageUrl));
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(packageUrl, destinationFile);
                Trace.TraceInformation("Successfully downloaded file \"{0}\" from \"{1}\"", destinationFile, packageUrl);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("Error while downloading file '{0}': {1}", packageUrl, ex));
                return false;
            }
        }        


        #endregion

        #region OnStart Addons

        public static bool InstallAddons(string addonsUrl, string webSitePath)
        {
            try
            {
                if (string.IsNullOrEmpty(addonsUrl))
                    return false;

                // Create website folder if not exists
                if (!Directory.Exists(webSitePath))
                {
                    Trace.TraceInformation(string.Format("Creating folder '{0}'...", webSitePath));
                    Directory.CreateDirectory(webSitePath);
                }
                const string localDNNPackageFilename = "DNNAddons.zip";
                string addonsFile = Path.Combine(Path.GetTempPath(), localDNNPackageFilename);
                // Delete previous failed attemps of web site creation
                if (File.Exists(addonsFile))
                {
                    Trace.TraceWarning(string.Format("Deleting previous addons file '{0}'...", addonsFile));
                    File.Delete(addonsFile);
                }                
                if (!DownloadFile(addonsFile, addonsUrl))
                    return false;
                
                // Unzip downloaded file
                Trace.TraceInformation("Decompressing addons file...");
                try
                {
                    UnzipFile(addonsFile, webSitePath);
                }
                catch (CompressOperationException ex)
                {
                    Trace.TraceError("Error while decompresing the addons file: {0}", ex);
                    return false;
                }

                Trace.TraceInformation(string.Format("Deleting addons file '{0}'...", addonsFile));
                File.Delete(addonsFile);

                Trace.TraceInformation("Addons successfully deployed");
                return true;
                
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("Error while installing addons file '{0}': {1}", addonsUrl, ex));
                return false;
            }
        }

        public static bool DownloadFile(string destinationFile, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            if (string.IsNullOrEmpty(destinationFile)) throw new ArgumentNullException("destinationFile");

            try
            {
                // Create a new WebClient instance.
                var myWebClient = new WebClient();
                Trace.TraceInformation(string.Format("Downloading addons file '{0}' from '{1}'...", destinationFile, url));
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(url, destinationFile);
                Trace.TraceInformation("Successfully downloaded addons file \"{0}\" from \"{1}\"", destinationFile, url);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("Error while downloading addons file '{0}': {1}", url, ex));
                return false;
            }            
        }

        #endregion

    }
}
