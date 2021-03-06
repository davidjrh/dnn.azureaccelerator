﻿#region Usings
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DNNShared.Exceptions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
#endregion

namespace DNNShared
{
    /// <summary>
    /// Role startup utilities
    /// </summary>
    public class Utils
    {
        public const int SleepTimeAfterSuccessfulPolling = 10000;
        public const int SleepTimeBetweenWriteErrors = 1000;
        public const int SleepTimeBeforeStartToRemap = 5000;

        #region Cloud Drive operations

        private static void SaveWinnerToBlockBlob()
        {
            try
            {
                var account = CloudStorageAccount.Parse(GetSetting("AcceleratorConnectionString"));
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference(GetSetting("driveContainer"));
                var blobName = string.Format("{0}.log", Path.GetFileNameWithoutExtension(GetSetting("driveName")));
                var blob = container.GetBlockBlobReference(blobName);
                blob.UploadText(string.Format("{0} - Drive {1} mounted by {2}", DateTime.UtcNow, GetSetting("driveName"), RoleEnvironment.CurrentRoleInstance.Id));
                blob.Metadata.Clear();
                blob.Metadata.Add("DeploymentId", RoleEnvironment.DeploymentId);
                blob.Metadata.Add("LeasedToInstanceId", RoleEnvironment.CurrentRoleInstance.Id);
                blob.Metadata.Add("LeasedOn", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                var instanceIndex = int.Parse(
                    RoleEnvironment.CurrentRoleInstance.Id
                        .Substring(RoleEnvironment.CurrentRoleInstance.Id.LastIndexOf("_", StringComparison.Ordinal) + 1));
                blob.Metadata.Add("MSDeployEndPointPort", (8172 + instanceIndex).ToString(CultureInfo.InvariantCulture));
                blob.SetMetadata();

            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while saving the competition winner on instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
            }
        }


        /// <summary>
        /// Mounts the VHD as a local drive
        /// </summary>
        /// <returns>A string with the path of the local mounted drive</returns>
        public static string MountCloudDrive(CloudDrive drive)
        {
            if (drive == null) throw new ArgumentNullException("drive");

            // Mount the Cloud Drive    
            var driveLetter = drive.Mount(RoleEnvironment.GetLocalResource("AzureDriveCache").MaximumSizeInMegabytes, DriveMountOptions.None);
            try
            {
                AppendLogEntryWithRetries(driveLetter + "\\logs\\MountHistory.log", 5,
                                          string.Format("Drive mounted by {0}", RoleEnvironment.CurrentRoleInstance.Id));

                SaveWinnerToBlockBlob();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while writing on the mount history log file on role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
            } 
            return driveLetter;
        }

        /// <summary>
        /// Appends the current date and time to the log file, retrying the operation to avoid false positives
        /// </summary>
        /// <param name="logFilePath">Path to the log file</param>
        /// <param name="maxAttempts">Maximum attempts to retry the operation</param>
        /// <param name="message">The message.</param>
        public static void AppendLogEntryWithRetries(string logFilePath, int maxAttempts, string message = "")
        {
            if (logFilePath == null) throw new ArgumentNullException("logFilePath");
            var i = 0;
            while (i < maxAttempts)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)); // Ensure directory exists
                    File.AppendAllText(logFilePath, string.Format("{0} - {1}", DateTime.UtcNow, message) + Environment.NewLine);
                    break;
                }
                catch (Exception)
                {
                    i++;
                    if (i >= maxAttempts)
                    {
                        throw;
                    }
                    Thread.Sleep(SleepTimeBetweenWriteErrors);
                }
            }
        }

        /// <summary>
        /// Shares the drive.
        /// </summary>
        /// <param name="drive">The drive.</param>
        /// <returns></returns>
        public static string ShareDrive(CloudDrive drive)
        {
            // Modify path to share a specific directory on the drive
            var drivePath = drive.LocalPath;
            if (RoleEnvironment.IsEmulated)
            {
                drivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                          @"dftmp\wadd\devstoreaccount1\drivecontainer",
                                          GetSetting("driveName"));
            }

            // Share it using SMB (add permissions for RDP user if it's configured)
            var rdpUserName = "";
            try
            {
                rdpUserName =
                    GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername");
            }
            catch
            {
                Trace.TraceWarning("No RDP user name was specified. Consider enabling RDP for better maintenance options.");
            }

            // The cloud drive can't be shared if it is running on Windows Azure Compute Emulator. 
            ShareLocalFolder(new[]
                                    {
                                        GetSetting("fileshareUserName"),
                                        rdpUserName,
                                        GetSetting("FTP.Root.Username"),
                                        GetSetting("FTP.Portals.Username")
                                    },
                                drivePath,
                                GetSetting("shareName"));
            return drivePath;
        }


        /// <summary>
        /// Initializes the drive.
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string.</param>
        /// <param name="driveContainerName">Name of the drive container.</param>
        /// <param name="driveName">Name of the drive.</param>
        /// <param name="driveSize">Size of the drive.</param>
        /// <returns></returns>
        public static CloudDrive InitializeCloudDrive(string storageConnectionString, string driveContainerName, string driveName, string driveSize)
        {
            try
            {
                Trace.TraceInformation("Setting up drive object...");
                var blobClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudBlobClient();

                var driveContainer = blobClient.GetContainerReference(driveContainerName);
                Trace.TraceInformation("Creating VHD container if not exists...");
                driveContainer.CreateIfNotExist();

                Trace.TraceInformation("Drive local cache initialization...");
                var localCache = RoleEnvironment.GetLocalResource("AzureDriveCache");
                CloudDrive.InitializeCache(localCache.RootPath, localCache.MaximumSizeInMegabytes);

                Trace.TraceInformation("Creating cloud drive...");
                var drive = new CloudDrive(driveContainer.GetBlobReference(driveName).Uri, blobClient.Credentials);
                try
                {
                    drive.CreateIfNotExist(int.Parse(driveSize));
                }
                catch (CloudDriveException ex)
                {
                    Trace.TraceWarning("Error while creating cloud drive on SMB worker role: {0}", ex);
                }
                return drive;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fatal error while setting up the Drive object: {0}", ex);
                throw;
            }
        }

        public static bool CreateSymbolicLink(string localPath, string shareName, string userName, string password, string smbRoleName)
        {

            // Cleanup stale mounts
            DeleteSymbolicLink(localPath);

            int i = 1;
            bool found;
            string machineIP = null;

            Trace.TraceInformation("Creating symbolic link {0} on role instance {1}...", localPath, RoleEnvironment.CurrentRoleInstance.Id);



            while (true)
            {
                found = false;
                Trace.TraceInformation("Looking for an available SMB server to create the symbolic link on role instance {0}...", RoleEnvironment.CurrentRoleInstance.Id);

                int countServers = RoleEnvironment.Roles[smbRoleName].Instances.Count;
                for (int instance = 0; instance < countServers; instance++)
                {
                    var server = RoleEnvironment.Roles[smbRoleName].Instances[instance];
                    Trace.TraceInformation("Trying to create symbolic link to SMB Server {0} on role instance {1}...", server.Id, RoleEnvironment.CurrentRoleInstance.Id);
                    machineIP = server.InstanceEndpoints["SMB"].IPEndpoint.Address.ToString();
                    if (RoleEnvironment.IsEmulated)
                    {
                        machineIP = "127.0.0.1";
                    }
                    machineIP = "\\\\" + machineIP + "\\";
                    var target = machineIP + shareName;

                    try
                    {                        
                        if (Directory.Exists(target))
                        {
                            SymbolicLink.CreateDirectoryLink(localPath, target);
                            Trace.TraceInformation(
                                "Created symbolic link {0} to SMB Server {1} on role instance {2}. Verifying...",
                                localPath, server.Id, RoleEnvironment.CurrentRoleInstance.Id);

                            if (!Directory.Exists(localPath))
                                throw new IOException(string.Format("Directory {0} not found", localPath));

                            if (!SymbolicLink.Exists(localPath))
                                throw new IOException(string.Format("Symbolic link {0} not found", localPath));

                            var currentTarget = SymbolicLink.GetTarget(localPath);
                            if (currentTarget != target)
                                throw new IOException(
                                    string.Format("Symbolic link {0} targets {1} and does not match {2}", localPath,
                                                  currentTarget, target));

                            found = true;
                            break;
                        }
                        else
                        {
                            Trace.TraceInformation("Shared folder {0} does not exist on role {1}. Trying next role instance...", target, server.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error creating symbolic link to SMB Server {0} on role instance {1}: {2}", server.Id, RoleEnvironment.CurrentRoleInstance.Id, ex);
                        DeleteSymbolicLink(localPath);                        
                    }
                }

                if (!found)
                {
                    Trace.TraceWarning("Error creating the symbolic link on role instance {0}: No available SMB server found. Retrying in 5 seconds (attempt {1} of 1000)...", RoleEnvironment.CurrentRoleInstance.Id, i);
                    Thread.Sleep(SleepTimeBeforeStartToRemap);

                    i++;
                    if (i > 1000)
                    {
                        break;
                    }
                }
                else
                    break;
            }

            if (found)
            {
                Trace.TraceInformation("Success: created symbolic link {0} to location {1} on role {2}", localPath, machineIP + shareName, RoleEnvironment.CurrentRoleInstance.Id);
                return true;
            }
            Trace.TraceError("Error creating symbolic link on role instance {0}: Could not find an available SMB Server and maximum attemtps reached", RoleEnvironment.CurrentRoleInstance.Id);
            return false;
        }


        public static void DeleteSymbolicLink(string localPath)
        {
            try
            {
                if (Directory.Exists(localPath))
                {
                    Trace.TraceInformation("Deleting symbolic link {0} on worker role {1}...", localPath, RoleEnvironment.CurrentRoleInstance.Id);
                    if (SymbolicLink.Exists(localPath))
                    {
                        Directory.Delete(localPath);
                    }
                    else
                    {
                        Directory.Delete(localPath, true);
                    }   
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while deleting symbolic link {0} on worker role {1}: {2}", localPath, RoleEnvironment.CurrentRoleInstance.Id, ex);
            }
            string error;
        }
        #endregion

        #region DotNetNuke setup utilities

        /// <summary>
        /// Creates the Windows user accounts for sharing the drive and FTP access
        /// </summary>
        public static void CreateUserAccounts()
        {
            // Create a local account for sharing the drive
            CreateUserAccount(GetSetting("fileshareUserName"),
                              GetSetting("fileshareUserPassword"));

            // To ensure FTP users can access the shared folder
            if (bool.Parse(GetSetting("FTP.Enabled", "False")))
            {
                // Create a local account for the FTP root user
                CreateUserAccount(GetSetting("FTP.Root.Username"), DecryptPassword(GetSetting("FTP.Root.EncryptedPassword")));

                if (!string.IsNullOrEmpty(GetSetting("FTP.Portals.Username")))
                {
                    // Optionally create a local account for the FTP portals user
                    CreateUserAccount(GetSetting("FTP.Portals.Username"), DecryptPassword(GetSetting("FTP.Portals.EncryptedPassword")));
                }
            }
        }

        /// <summary>
        /// Setups the web site settings.
        /// </summary>
        /// <param name="drive">The drive.</param>
        public static void SetupWebSiteSettings(CloudDrive drive)
        {
            // Check for the database existence
            Trace.TraceInformation("Checking for database existence...");
            if (!SetupDatabase(GetSetting("DBAdminUser"),
                                        GetSetting("DBAdminPassword"),
                                        GetSetting("DatabaseConnectionString")))
                Trace.TraceError("Error while setting up the database. Check previous messages.");

            // Check for the creation of the Website contents from Azure storage
            Trace.TraceInformation("Check for website content...");
            if (!SetupWebSiteContents(drive.LocalPath + "\\" + GetSetting("dnnFolder"),
                                                    GetSetting("AcceleratorConnectionString"),
                                                    GetSetting("packageContainer"),
                                                    GetSetting("package"),
                                                    GetSetting("packageUrl")))
                Trace.TraceError("Website content could not be prepared. Check previous messages.");


            // Setup Database Connection string
            SetupWebConfig(drive.LocalPath + "\\" + GetSetting("dnnFolder") + "\\web.config",
                                            GetSetting("DatabaseConnectionString"),
                                            GetSetting("InstallationDate"),
                                            GetSetting("UpdateService.Source"));

            // Setup DotNetNuke.install.config
            SetupInstallConfig(
                                Path.Combine(new[]
                                                         {
                                                             drive.LocalPath, GetSetting("dnnFolder"),
                                                             "Install\\DotNetNuke.install.config"
                                                         }),
                                GetSetting("AcceleratorConnectionString"),
                                GetSetting("packageContainer"),
                                GetSetting("packageInstallConfiguration"));

            // Setup post install addons (always overwrite)
            InstallAddons(GetSetting("AddonsUrl"),
                                            drive.LocalPath + "\\" + GetSetting("dnnFolder"));            

        }


        /// <summary>
        /// Setups the offline site settings.
        /// </summary>
        /// <param name="contentsRoot">The contents root.</param>
        public static void SetupOfflineSiteSettings(string contentsRoot)
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

                        Trace.TraceInformation(string.Format("Downloading package '{0}' to '{1}'...", packageName, packageFile));
                        blobContainer.GetBlobReference(packageName).DownloadToFile(packageFile);                        
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

                    Trace.TraceInformation(string.Format("Downloading customized installation settings '{0}' to '{1}'...", packageInstallConfig, localInstallConfig));
                    blobContainer.GetBlobReference(packageInstallConfig).DownloadToFile(localInstallConfig);
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


        /// <summary>
        /// Decrypts the password.
        /// </summary>
        /// <param name="encryptedPassword">The encrypted password.</param>
        /// <returns></returns>
        /// <exception cref="System.Security.SecurityException">Unable to decrypt password. Make sure that the cert used for encryption was uploaded to the Azure service</exception>
        public static string DecryptPassword(string encryptedPassword)
        {
            SecureString password = null;
            if (string.IsNullOrEmpty(encryptedPassword))
            {
                password = null;
            }
            else
            {
                try
                {
                    var encryptedBytes = Convert.FromBase64String(encryptedPassword);
                    var envelope = new EnvelopedCms();
                    envelope.Decode(encryptedBytes);
                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    envelope.Decrypt(store.Certificates);
                    char[] passwordChars = Encoding.UTF8.GetChars(envelope.ContentInfo.Content);
                    password = new SecureString();
                    foreach (var character in passwordChars)
                    {
                        password.AppendChar(character);
                    }
                    Array.Clear(envelope.ContentInfo.Content, 0, envelope.ContentInfo.Content.Length);
                    Array.Clear(passwordChars, 0, passwordChars.Length);
                    password.MakeReadOnly();
                }
                catch (CryptographicException)
                {
                    // Unable to decrypt password. Make sure that the cert used for encryption was uploaded to the Azure service
                    password = null;
                }
                catch (FormatException)
                {
                    // Encrypted password is not a valid base64 string
                    password = null;
                }
            }
            if (password == null)
            {
                throw new SecurityException("Unable to decrypt password. Make sure that the cert used for encryption was uploaded to the Azure service");
            }
            return GetUnsecuredString(password);
        }

        /// <summary>
        /// Gets the unsecured string.
        /// </summary>
        /// <param name="secureString">The secure string.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">secureString</exception>
        public static string GetUnsecuredString(SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException("secureString");
            }

            IntPtr ptrUnsecureString = IntPtr.Zero;

            try
            {
                ptrUnsecureString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(ptrUnsecureString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptrUnsecureString);
            }
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

        #region Mime types
        internal static string GetMimeType(FileInfo fileInfo)
        {
            var mimeType = "application/octet-stream";
            var regKey = Registry.ClassesRoot.OpenSubKey(fileInfo.Extension.ToLower());
            if (regKey != null)
            {
                var contentType = regKey.GetValue("Content Type");
                if (contentType != null) mimeType = contentType.ToString();
            }
            return mimeType;
        }

        internal static string GetMimeType(string path)
        {
            var mimeType = "application/octet-stream";
            var extension = Path.GetExtension(path);
            if (extension != null)
            {
                var regKey = Registry.ClassesRoot.OpenSubKey(extension.ToLower());
                if (regKey != null)
                {
                    var contenType = regKey.GetValue("Content Type");
                    if (contenType != null) mimeType = contenType.ToString();
                }
            }
            return mimeType;
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
        /// <summary>
        /// Shares a local folder
        /// </summary>
        /// <param name="userNames">Username list to share the drive with full access permissions</param>
        /// <param name="path">Path to share</param>
        /// <param name="shareName">Share name</param>
        /// <returns>Returns 0 if success</returns>
        public static int ShareLocalFolder(string[] userNames, string path, string shareName)
        {
            string error;
            var grants = userNames.Where(userName => !string.IsNullOrEmpty(userName)).Aggregate("", (current, userName) => current + string.Format(" /Grant:{0},full", userName));
            Trace.TraceInformation("Sharing local folder {0} with grants: {1}", path, grants);
            int exitCode = ExecuteCommand("net.exe", " share " + shareName + "=" + path + grants, out error, 10000);

            if (exitCode != 0)
                //Log error and continue since the drive may already be shared
                Trace.TraceError("Error creating fileshare, error msg:" + error);
            return exitCode;
        }

        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="shareName">The share name.</param>
        /// <returns></returns>
        public static int DeleteShare(string shareName)
        {
            var i = 0;
            int exitCode = 0;
            while (i <= 10)
            {
                i++;
                Trace.TraceInformation("Removing share on role {0} ({1} of 10)...", RoleEnvironment.CurrentRoleInstance.Id, i);
                string error;
                exitCode = ExecuteCommand("net.exe", " share /d " + shareName + " /Y", out error, 10000);

                if (exitCode != 0)
                {
                    //Log error and continue
                    Trace.TraceWarning("Error deleting fileshare on role {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id,
                                       error);
                    Thread.Sleep(SleepTimeBeforeStartToRemap);
                }
                else
                {
                    Trace.TraceInformation("Successfully deleted the share");
                    return exitCode;                
                }
            }
            return exitCode;
        }

        public static int EnableFTPFirewallTraffic()
        {
            int exitCode = 0;
            //Enable SMB traffic through the firewall
            Trace.TraceInformation("Enabling FTP traffic through the firewall");     

            if (UseAdvancedFirewall())
            {
                Trace.TraceInformation("Enabling FTP traffic through the firewall using advanced firewall");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (FTP Server - Data)", "In", "Public", "TCP", "20", "Any", "Any", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (FTP Server - Command)", "In", "Public", "TCP", "21", "Any", "Any", "");
            }
            else
            {
                Trace.TraceInformation("Enabling FTP traffic through the firewall using non advanced firewall"); 
                string error;
                exitCode |= ExecuteCommand("netsh.exe", "firewall set portopening TCP 20 \"DotNetNuke Azure Accelerator (FTP Server - Data)\"", out error, 10000);
                exitCode |= ExecuteCommand("netsh.exe", "firewall set portopening TCP 21 \"DotNetNuke Azure Accelerator (FTP Server - Command)\"", out error, 10000);
            }

            return exitCode;
        }


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
        /// Enables SMB traffic through the firewall
        /// </summary>
        /// <returns>Returns 0 if success</returns>
        public static int EnableSMBFirewallTraffic()
        {
            //Enable SMB traffic through the firewall
            Trace.TraceInformation("Enabling SMB traffic through the firewall");

            string error;
            int exitCode = ExecuteCommand("netsh.exe", "firewall set service type=fileandprint mode=enable scope=all", out error, 10000);  

            if (UseAdvancedFirewall()) // Is Windows Server 2008 R2? (OS Family == "2" in the service configuration file)
            {
                // This rules are needed after enabling the fileandprint service on Windows Server 2008 R2, to enable
                // mapping the drive using Windows Azure Connect on a remote machine
                // Changed to the new netsh firewall syntax. For more info, see http://support.microsoft.com/kb/947709/. 
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-In) - Public", "In", "Public", "ICMPv4", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-In) - Domain", "In", "Domain", "ICMPv4", "Any", "Any", "Any", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-In) - Private", "In", "Private", "ICMPv4", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-In) - Public", "In", "Public", "ICMPv6", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-In) - Domain", "In", "Domain", "ICMPv6", "Any", "Any", "Any", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-In) - Private", "In", "Private", "ICMPv6", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-In) - Public", "In", "Public", "UDP", "138", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-In) - Domain", "In", "Domain", "UDP", "138", "Any", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-In) - Private", "In", "Private", "UDP", "138", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-In) - Public", "In", "Public", "UDP", "137", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-In) - Domain", "In", "Domain", "UDP", "137", "Any", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-In) - Private", "In", "Private", "UDP", "137", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-In) - Public", "In", "Public", "TCP", "139", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-In) - Domain", "In", "Domain", "TCP", "139", "Any", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-In) - Private", "In", "Private", "TCP", "139", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-In) - Public", "In", "Public", "TCP", "445", "Any", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-In) - Domain", "In", "Domain", "TCP", "445", "Any", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-In) - Private", "In", "Private", "TCP", "445", "Any", "LocalSubnet", "System");

                // Outbound rules
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-Out) - Public", "Out", "Public", "ICMPv4", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-Out) - Domain", "Out", "Domain", "ICMPv4", "Any", "Any", "Any", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv4-Out) - Private", "Out", "Private", "ICMPv4", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-Out) - Public", "Out", "Public", "ICMPv6", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-Out) - Domain", "Out", "Domain", "ICMPv6", "Any", "Any", "Any", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (Echo Request - ICMPv6-Out) - Private", "Out", "Private", "ICMPv6", "Any", "Any", "LocalSubnet", "");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-Out) - Public", "Out", "Public", "UDP", "Any", "138", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-Out) - Domain", "Out", "Domain", "UDP", "Any", "138", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Datagram-Out) - Private", "Out", "Private", "UDP", "Any", "138", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-Out) - Public", "Out", "Public", "UDP", "Any", "137", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-Out) - Domain", "Out", "Domain", "UDP", "Any", "137", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Name-Out) - Private", "Out", "Private", "UDP", "Any", "137", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-Out) - Public", "Out", "Public", "TCP", "Any", "139", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-Out) - Domain", "Out", "Domain", "TCP", "Any", "139", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (NB-Session-Out) - Private", "Out", "Private", "TCP", "Any", "139", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-Out) - Public", "Out", "Public", "TCP", "Any", "445", "LocalSubnet", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-Out) - Domain", "Out", "Domain", "TCP", "Any", "445", "Any", "System");
                exitCode |= SetupAdvancedFirewallRule("DotNetNuke Azure Accelerator (SMB-Out) - Private", "Out", "Private", "TCP", "Any", "445", "LocalSubnet", "System");         
            }

            return exitCode;
        }

        /// <summary>
        /// Returns true if the OS version is 6.1 or higher (Windows Server 2008 R2, Windows 7, Windows 8)
        /// </summary>
        /// <returns></returns>
        private static bool UseAdvancedFirewall()
        {
            return (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1) ||
                   (Environment.OSVersion.Version.Major > 6);
        }

        private static int SetupAdvancedFirewallRule(string ruleName, string direction, string profile, string protocol, string localPort, string remotePort, string remoteIp, string program)
        {
            // Try to update the rule first
            var rule = string.Format(" dir={0}", direction);
            rule += string.Format(" profile={0}", profile);
            rule += string.Format(" protocol={0}", protocol);
            if (protocol.ToUpperInvariant() == "TCP" || protocol.ToUpperInvariant() == "UDP")
            {
                rule += string.Format(" localport={0}", localPort);
                rule += string.Format(" remoteport={0}", remotePort);
            }

            rule += string.Format(" remoteip={0}", remoteIp);
            if (!string.IsNullOrEmpty(program))
                rule += string.Format(" program={0}", program);
            rule += " action=Allow enable=Yes";

            var updateArgs = string.Format("advfirewall firewall set rule name=\"{0}\" new {1}", ruleName, rule);
            var createArgs = string.Format("advfirewall firewall add rule name=\"{0}\" {1}", ruleName, rule);

            string error;
            var exitCode = ExecuteCommand("netsh.exe", updateArgs, out error, 10000, false);
            if (exitCode != 0) // If not exists, then create the rule
            {
                exitCode = ExecuteCommand("netsh.exe", createArgs, out error, 10000);
            }
            return exitCode;
        }

        /// <summary>
        /// Creates an user account on the local machine
        /// </summary>
        /// <param name="userName">Name for the user account</param>
        /// <param name="password">Password for the user account</param>
        /// <param name="passwordNeverExpires">Boolean to indicate if the password never expires (true by default)</param>
        /// <returns>Returns 0 if success</returns>
        public static int CreateUserAccount(string userName, string password, bool passwordNeverExpires = true)
        {
            string error;

            //Create the user account    
            Trace.TraceInformation("Creating user account '{0}'...", userName);
            int exitCode = ExecuteCommand("net.exe", string.Format("user {0} {1} /expires:never /add /Y", userName, password), out error, 10000);
            if (exitCode != 0)
            {
                //Log error and continue since the user account may already exist
                Trace.TraceWarning("Error creating user account, error msg:" + error);
            }
            else
            {
                // Password never expires
                if (passwordNeverExpires)
                {
                    Trace.TraceInformation("Setting account password expiration to never for user account '{0}'...", userName);
                    exitCode = ExecuteCommand("wmic.exe", string.Format("USERACCOUNT WHERE \"Name='{0}'\" SET PasswordExpires=FALSE", userName), out error, 10000);
                    if (exitCode != 0)
                    {
                        Trace.TraceWarning("Error while setting account password expiration, error msg:" + error);
                    }
                }                
            }
            return exitCode;
        }

        /// <summary>
        /// Deletes an user account on the local machine
        /// </summary>
        /// <param name="userName">User name of the account</param>
        /// <returns>Returns 0 if success</returns>
        public static int DeleteUserAccount(string userName)
        {
            string error;

            //Create the user account    
            Trace.TraceInformation(string.Format("Deleting user account '{0}'...", userName));
            int exitCode = ExecuteCommand("net.exe", string.Format("user {0} /delete", userName), out error, 10000);
            if (exitCode != 0)
            {
                //Log error and continue since the user account may already exist
                Trace.TraceWarning("Error deleting user account, error msg:" + error);
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

        #region Impersonation
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;

        [DllImport("advapi32.dll")]
        public static extern int LogonUserA(String lpszUserName, String lpszDomain, String lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken, int impersonationLevel, ref IntPtr hNewToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        public static WindowsImpersonationContext ImpersonateValidUser(String userName, String domain, String password)
        {
            WindowsIdentity tempWindowsIdentity;
            IntPtr token = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            if (RevertToSelf())
            {
                if (LogonUserA(userName, domain, password, LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT, ref token) != 0)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                        WindowsImpersonationContext impersonationContext = tempWindowsIdentity.Impersonate();
                        if (impersonationContext != null)
                        {
                            CloseHandle(token);
                            CloseHandle(tokenDuplicate);
                            return impersonationContext;
                        }
                    }
                }
            }
            if (token != IntPtr.Zero)
                CloseHandle(token);
            if (tokenDuplicate != IntPtr.Zero)
                CloseHandle(tokenDuplicate);
            return null;
        }

        #endregion

    }
}
