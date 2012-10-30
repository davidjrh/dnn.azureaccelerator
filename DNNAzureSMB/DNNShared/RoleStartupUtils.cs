using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using DNNShared.Exceptions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;

namespace DNNShared
{
    public class RoleStartupUtils
    {

        #region Cloud Drive operations
        /// <summary>
        /// Mounts the VHD as a local drive
        /// </summary>
        /// <returns>A string with the path of the local mounted drive</returns>
        public static CloudDrive MountCloudDrive(string storageConnectionString, string driveContainerName, string driveName, string driveSize)
        {
            // Mount the Cloud Drive - Lots of tracing in this part

            Trace.TraceInformation("Mounting cloud drive - Begin");
            Trace.TraceInformation("Mounting cloud drive - Accesing acount info");
            var account = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = account.CreateCloudBlobClient();

            Trace.TraceInformation("Mounting cloud drive - Locating VHD container:" + driveContainerName);
            var driveContainer = blobClient.GetContainerReference(driveContainerName);

            Trace.TraceInformation("Mounting cloud drive - Creating VHD container if not exists");
            driveContainer.CreateIfNotExist();

            Trace.TraceInformation("Mounting cloud drive - Local cache initialization");
            var localCache = RoleEnvironment.GetLocalResource("AzureDriveCache");
            CloudDrive.InitializeCache(localCache.RootPath, localCache.MaximumSizeInMegabytes);

            Trace.TraceInformation("Mounting cloud drive - Creating cloud drive");
            var drive = new CloudDrive(driveContainer.GetBlobReference(driveName).Uri, account.Credentials);
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
        /// Maps a Network Drive (SMB Server Share)
        /// </summary>
        /// <param name="SMBMode">Indicate if the service is running with a specif worker role as SMB server</param>
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
                if (server == null)
                    throw new ApplicationException("Can't find an available role where to map the network drive");

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
            return false;
        }
        #endregion

        #region DotNetNuke setup utilities

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
                                var cmdC5 = new SqlCommand(string.Format("EXEC sp_addrolemember 'db_ddladmin', '{0}'", connBuilderOriginal.UserID), dbConnNew);
                                cmdC5.ExecuteNonQuery();
                                var cmdC6 = new SqlCommand(string.Format("EXEC sp_addrolemember 'db_securityadmin', '{0}'", connBuilderOriginal.UserID), dbConnNew);
                                cmdC6.ExecuteNonQuery();
                                var cmdC7 = new SqlCommand(string.Format("EXEC sp_addrolemember 'db_datareader', '{0}'", connBuilderOriginal.UserID), dbConnNew);
                                cmdC7.ExecuteNonQuery();
                                var cmdC8 = new SqlCommand(string.Format("EXEC sp_addrolemember 'db_datawriter', '{0}'", connBuilderOriginal.UserID), dbConnNew);
                                cmdC8.ExecuteNonQuery();

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

        /// <summary>
        /// Modifies web.config to setup database connection settings and other appSettings
        /// </summary>
        /// <param name="webConfigPath">Path to the web config file</param>
        /// <param name="databaseConnectionString">Database connection string</param>
        public static bool SetupWebConfig(string webConfigPath, string databaseConnectionString, string installationDate)
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
                const string localDNNPackageFilename = "DNNPackage.zip";
                string packageFile = Path.Combine(Path.GetTempPath(), localDNNPackageFilename);
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
                    Trace.TraceWarning("The content already exists. No action taken.");
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
            try
            {
                if (RoleEnvironment.IsAvailable)
                    return RoleEnvironment.GetConfigurationSettingValue(key);
            }
            catch (RoleEnvironmentException)  // The configuration setting that was being retrieved does not exist.
            {
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

            // TODO put these settings on the service configuration file
            var transferPeriod = TimeSpan.FromMinutes(1);
            var bufferQuotaInMB = 100;

            // Add Windows Azure Trace Listener
            Trace.Listeners.Add(new DiagnosticMonitorTraceListener());

            // Enable Collection of Crash Dumps
            CrashDumps.EnableCollection(true);

            // Get the Default Initial Config
            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Windows Azure Logs
            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.BufferQuotaInMB = bufferQuotaInMB;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            // File-based logs
            config.Directories.ScheduledTransferPeriod = transferPeriod;
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
                @"\Memory\Available MBytes",
                @"\ASP.NET Applications(__Total__)\Requests Total",
                @"\ASP.NET Applications(__Total__)\Requests/Sec",
                @"\ASP.NET\Requests Queued",
            };

            counters.ForEach(
                counter => config.PerformanceCounters.DataSources.Add(
                    new PerformanceCounterConfiguration { CounterSpecifier = counter, SampleRate = TimeSpan.FromSeconds(60) }));
            config.PerformanceCounters.ScheduledTransferPeriod = transferPeriod;
            config.PerformanceCounters.BufferQuotaInMB = bufferQuotaInMB;

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);
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
                throw new CompressOperationException("An exception ocurred while compressing the folder contents", ex);
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
                throw new CompressOperationException("An exception ocurred while compressing the folder contents", ex);
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
                throw new CompressOperationException("An exception ocurred while unzipping the file", ex);
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
        /// <param name="userName">Username to share the drive with full access permissions</param>
        /// <param name="RDPuserName">(Optional) Second username to grant full access permissions</param>
        /// <param name="path">Path to share</param>
        /// <param name="shareName">Share name</param>
        /// <returns>Returns 0 if success</returns>
        public static int ShareLocalFolder(string userName, string RDPuserName, string path, string shareName)
        {
            string error;
            Trace.TraceInformation("Sharing local folder " + path);
            string grantRDPUserName = "";
            if (RDPuserName != "")
                grantRDPUserName = " /Grant:" + RDPuserName + ",full";
            int exitCode = ExecuteCommand("net.exe", " share " + shareName + "=" + path + " /Grant:" + userName + ",full" + grantRDPUserName, out error, 10000);

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
            int exitCode = 0;
            //Enable SMB traffic through the firewall
            Trace.TraceInformation("Enabling SMB traffic through the firewall");

            string error;
            exitCode = ExecuteCommand("netsh.exe", "firewall set service type=fileandprint mode=enable scope=all", out error, 10000);  

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
            Trace.TraceInformation("Creating user account for sharing");
            int exitCode = ExecuteCommand("net.exe", string.Format("user {0} {1} /expires:never /add", userName, password), out error, 10000);
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
                    Trace.TraceInformation("Setting account password expiration to never");
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
                if (!DownloadAddons(addonsFile, addonsUrl))
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

        private static bool DownloadAddons(string destinationFile, string addonsUrl)
        {
            if (string.IsNullOrEmpty(addonsUrl))
            {
                return false;
            }

            if (string.IsNullOrEmpty(destinationFile)) throw new ArgumentNullException("destinationFile");

            try
            {
                // Create a new WebClient instance.
                var myWebClient = new WebClient();
                Trace.TraceInformation(string.Format("Downloading addons file '{0}' from '{1}'...", destinationFile, addonsUrl));
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(addonsUrl, destinationFile);
                Trace.TraceInformation("Successfully downloaded addons file \"{0}\" from \"{1}\"", destinationFile, addonsUrl);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("Error while downloading addons file '{0}': {1}", addonsUrl, ex));
                return false;
            }            
        }

        #endregion

    }
}
