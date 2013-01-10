using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Xml.Linq;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.Accelerator.Properties;
using Microsoft.WindowsAzure.StorageClient;
using smarx.BlobSync;

namespace Microsoft.WindowsAzure.Accelerator
{
#region | PROGRAM

    /// <summary>
    /// Accelerator console support application endpoint.  |i| rdm |
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Mains entry point for the Accelerator console application.
        /// </summary>
        /// <param name="args">Console args.</param>
        public static void Main(String[] args)
        {
            Boolean validArgs = true;
            
            //i| Grab the switches, ignores case, use only first char.  Allowing for full switch names 
            //i| (e.g. /upload, -upload, /u, -u, /U, ... are all valid)
            var switches = new List<Char>(args.Where(i => i[0] == '/' || i[0] =='-').Select(i => Char.ToLower(i[1])));

            //i| Grab the params (non-switches); case-sensitive for blob storage.
            var param = new List<String>(args.Where(i => i[0] != '/' && i[0] != '-'));
            try
            {

                if ( switches.Count < 1 ) validArgs = false;
                else //i| Switches can be in any order.
                {
                    //i|
                    //i| Upload
                    //i|
                    if ( switches.Exists(i => i == 'u') )
                    {
                        //i|
                        //i| BlobSync
                        //i|
                        if ( switches.Exists(i => i == 's') && param.Count == 2 ) SynchBlobStorage(SyncDirection.Upload, param[1], param[0]);
                        //i|
                        //i| CloudDrive
                        //i|
                        else if ( switches.Exists(i => i == 'v') )
                        {
                            UploadFile(param[0], param.Count == 1 ? Settings.DefaultCloudDriveUri : param[1], BlobType.PageBlob, switches.Exists(i => i == 'q'));
                        }
                        //i|
                        //i| WebSite
                        //i|
                        else if (switches.Exists(i => i == 'w'))
                        {
                            UploadWebSite(param.Count > 0 ? param[0] : Settings.DefaultLocalSitePath, param.Count > 1 ? param[1] : Settings.DefaultCloudDriveUri, BlobType.PageBlob, switches.Exists(i => i == 'q'));
                        }
                        //i|
                        //i| BlockBlob 
                        //i|
                        else if (param.Count == 2) UploadFile(param[0], param[1], BlobType.BlockBlob, switches.Exists(i => i == 'q'));
                    }
                    //i|
                    //i| Download
                    //i|
                    else if ( switches.Exists(i => i == 'd') && param.Count == 2 )
                    {
                        //i|
                        //i| BlobSync
                        //i|
                        if ( switches.Exists(i => i == 's') ) SynchBlobStorage(SyncDirection.Download, param[0], param[1]);
                        //i|
                        //i| BlockBlob
                        //i|
                        else DownloadFile(param[0], param[1], switches.Exists(i => i == 'q'));
                    }
                    //i|
                    //i| Service Console
                    //i|
                    else if ( switches.Exists(i => i == 'c') ) CreateServiceConsole(param[0] ?? String.Empty);
                    //i|
                    //i| Trace Console
                    //i|
                    else if ( switches.Exists(i => i == 't') ) CreateTraceListener(param.Count > 0 ? param[0] : String.Empty);
                    else validArgs = false;
                }
            }
            catch(FileLoadException ex)
            {
                Console.Error.WriteLine("\r\n{0}\r\n'{1}'", ex.ToTraceString(), ex.FileName);
            }
            catch(FileNotFoundException ex)
            {
                Console.Error.WriteLine("\r\n{0}\r\n'{1}'", ex.ToTraceString(), ex.FileName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("\r\n{0}", ex.ToTraceString());
            }
            //i| Display help.
            if ( !validArgs )
            {
                Console.WriteLine(Resources.UsageText);
            }
        }

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="localPath">The local path.</param>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="blobType">Type of the BLOB.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite BLOB if exists].</param>
        private static void UploadWebSite(String localPath, String virtualPath, BlobType blobType, Boolean overwrite)
        {
            var resourceFiles = Path.GetFullPath(@".\Resources");
            var publishCMD = Path.GetFullPath(@".\PublishApplication.cmd");
            var createVHD = Path.Combine(resourceFiles, "createVhd.cmd");
            var unmountVHD = Path.Combine(resourceFiles, "unmountVhd.cmd");
            var localVHD = Path.GetFullPath(Path.GetFileName(virtualPath));
            Environment.SetEnvironmentVariable("_cloudDriveBlobUri", virtualPath.TrimEnd('/', '\\', ' '));
            Environment.SetEnvironmentVariable("_cloudDriveFile", localVHD);
            Environment.SetEnvironmentVariable("_cloudDriveSource", localPath.TrimEnd('/','\\',' '));
            if (overwrite)
                Environment.SetEnvironmentVariable("_cloudDriveOverwrite", overwrite.ToString());
            var args = (Settings.ApplicationName ?? "IIS") + (overwrite ? " /q" : String.Empty);
            AcceleratorExtensions.RunProcess(publishCMD, Path.GetFullPath("."), args);
            //x| UploadFile(localVHD, virtualPath, blobType, overwrite);
        }

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="localPath">The local path.</param>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="blobType">Type of the BLOB.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite BLOB if exists].</param>
        private static void UploadFile(String localPath, String virtualPath, BlobType blobType, Boolean overwrite)
        {
            String path = Path.GetFullPath(localPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("The system cannot find the file specified.", path);
            CloudBlob blob = Settings.CloudStorageAccount.CreateCloudBlobClient().GetBlobReference(virtualPath);
            if ( blob.Exists() && !overwrite )
            {
                Console.Error.Write("\r\nA blob already exists at storage location '{0}'.\r\n\r\nDo you wish to overwrite (y/n)? ", blob.Uri);
                ConsoleKeyInfo result = Console.ReadKey(false);
                if ( Char.ToLower(result.KeyChar) != 'y' )
                    return;                
            }
            Console.WriteLine("\r\nUploading file to storage...\r\nSource:  '{0}'\r\nTarget:  '{1}'", path, blob.Uri);
            try
            {
                if (blobType == BlobType.PageBlob)
                    Settings.CloudStorageAccount.UploadVhd(localPath, virtualPath, true);
                else
                    Settings.CloudStorageAccount.UploadFile(localPath, virtualPath, true, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during upload.\r\n{0}.", ex.FormatException());
                return;
            }
            Console.WriteLine("Upload completed!");
        }

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="overwrite">if set to <c>true</c> overwrites any existing file.</param>
        private static void DownloadFile(String virtualPath, String localPath, Boolean overwrite)
        {
            String path = Path.GetFullPath(localPath);
            if (File.Exists(path) && !overwrite)
            {
                Console.Error.Write("\r\nA file already exists at '{0}'.\r\n\r\nDo you wish to overwrite (y/n)? ", path);
                ConsoleKeyInfo result = Console.ReadKey(false);
                if ( Char.ToLower(result.KeyChar) != 'y' )
                    return;
            }
            CloudBlob blob = Settings.CloudStorageAccount.CreateCloudBlobClient().GetBlobReference(virtualPath);
            if (blob == null || !blob.Exists())
                throw new FileNotFoundException("The system could not find a blob at the storage location specified.", blob.OnValid(b => b.Uri.ToString()) ?? virtualPath);
            Console.WriteLine("\r\nDownloading blob from storage...\r\nSource:  '{0}'\r\nTarget:  '{1}'", blob.Uri, path);
            Settings.CloudStorageAccount.DownloadToFile(localPath, virtualPath, true);
            Console.WriteLine("Download complete.");
        }

        /// <summary>
        /// Synches the BLOB storage.
        /// </summary>
        /// <param name="syncDirection">The sync direction.</param>
        /// <param name="containerUri">The container URI.</param>
        /// <param name="localPath">The local path.</param>
        /// <returns></returns>
        private static void SynchBlobStorage(SyncDirection syncDirection, String containerUri, String localPath)
        {
            var sync = new BlobSync
                           {
                               SyncDirection = syncDirection,
                               BlobSyncDirectory = Settings.CloudStorageAccount.CreateCloudBlobClient().GetBlobDirectoryReference(containerUri),
                               IgnoreAdditionalFiles = true,
                               LocalSyncRoot = localPath
                           };

            sync.SyncAll();
        }

        /// <summary>
        /// Creates the service bus service console.
        /// </summary>
        /// <param name="servicePathOverride">The service path override.</param>
        private static void CreateServiceConsole(String servicePathOverride)
        {
            if (!String.IsNullOrEmpty(servicePathOverride))
                Settings.ServiceBusConnection.ServicePath = servicePathOverride;
            Console.WriteLine("[( Service Console Client )]");
            Console.WriteLine("Connecting to Azure Service Bus...");
            Console.WriteLine("\r\n{0}\r\n", Settings.ServiceBusConnection.ToTraceString());

            ServiceConsole.CreateClientConsole(Settings.ServiceBusConnection);            
        }

        /// <summary>
        /// Creates the real-time trace listener.
        /// </summary>
        /// <param name="servicePathOverride">The service path override.</param>
        private static void CreateTraceListener(String servicePathOverride)
        {
            if ( !String.IsNullOrEmpty(servicePathOverride) )
                Settings.ServiceBusConnection.ServicePath = servicePathOverride;

            Console.WriteLine("[( Trace Console Server )]");
            Console.WriteLine("Connecting to Azure Service Bus...");

            //i| Create the Service Host 
            var host = new ServiceHost(typeof(CloudTraceService), Settings.ServiceBusConnection.GetServiceUri());
            var serviceEndPoint = host.AddServiceEndpoint(typeof(ICloudTraceContract), new NetEventRelayBinding(), String.Empty);
            serviceEndPoint.Behaviors.Add(Settings.ServiceBusConnection.GetTransportClientEndpointBehavior());

            //i| Open the Host
            host.Open();
            Console.WriteLine("Connected to: {0}", Settings.ServiceBusConnection.GetServiceUri());
            Console.WriteLine("Hit [Enter] to exit");

            //i| Wait Until the Enter Key is Pressed and Close the Host
            Console.ReadLine();
            host.Close();
        }
    }

#endregion
#region | SETTINGS

    /// <summary>
    /// Manages settings queries from ServiceConfiguration.csfg for AccelCon.  |i| rdm |
    /// </summary>
    public class Settings
    {
        private const String _ServiceConfigurationFile = "ServiceConfiguration.cscfg";
        private const String _ServiceDefinitionFile = "ServiceConfiguration.csdef";

        private static ServiceBusConnection _serviceBusConnection;
        private static CloudStorageAccount _cloudStorageAccount;
        private static Dictionary<String, String> _diagSettings;
        private static Dictionary<String, String> _configSettings;

        /// <summary>
        /// Gets the packed diagnostics setting values.
        /// </summary>
        private static Dictionary<String, String> DiagSettings
        {
            get 
            {
                if ( _diagSettings == null )
                {
                    if ( !_configSettings.ContainsKey("Diagnostics") )
                        throw new KeyNotFoundException(String.Format("Unable to locate 'Diagnostics' key in configuration settings file `{0}'.", _ServiceConfigurationFile));
                    _diagSettings = _configSettings["Diagnostics"].SplitToDictionary<String, String>(new[] { ';' }, new[] { '=' });
                }
                return _diagSettings;
            }
        }

        /// <summary>
        /// Gets the diagnostics service bus connection.
        /// </summary>
        public static ServiceBusConnection ServiceBusConnection
        {
            get { return _serviceBusConnection ?? ( _serviceBusConnection = ServiceBusConnection.Parse(_configSettings["DiagnosticsServiceBus"]) ); }
        }

        /// <summary>
        /// Gets the default cloud drive URI.
        /// </summary>
        public static String DefaultCloudDriveUri
        {
            get { return _configSettings.Protect(cs => cs["AcceleratorDrivePageBlobUri"]) ?? DefaultSettings.CloudDriveUri;  }
        }

        /// <summary>
        /// Gets the default local site path.
        /// </summary>
        public static String DefaultLocalSitePath
        {
            get { return _configSettings["LocalSitePath"]; }
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public static String ApplicationName
        {
            get { return _configSettings["AcceleratorApplication"].Substring(0, _configSettings["AcceleratorApplication"].IndexOfAny(new char[] {',', ';'})); }            
        }

        /// <summary>
        /// Gets the cloud storage account.
        /// </summary>
        public static CloudStorageAccount CloudStorageAccount
        {
            get { return _cloudStorageAccount ?? ( _cloudStorageAccount = CloudStorageAccount.Parse(_configSettings["AcceleratorConnectionString"])); }
        }
        
        /// <summary>
        /// Initializes the <see cref="Settings"/> class.
        /// </summary>
        static Settings()
        {
            if ( File.Exists(_ServiceConfigurationFile) )
            {
                try
                {
                    XNamespace ns = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
                    var xdoc = XDocument.Load(_ServiceConfigurationFile );
                    _configSettings = xdoc.Descendants(ns + "Setting").ToDictionary(e => (String)e.Attribute("name"), e => (String)e.Attribute("value"));
                    Console.Error.WriteLine("{{ Azure settings loaded from service configuration file: `{0}'. }}", Path.GetFullPath(_ServiceConfigurationFile));  //i| Writing trivial output to stderr; should probably just pull.
                }
                catch { }
            }
            if (_configSettings == null)
            {
                _configSettings = ConfigurationManager.AppSettings.AllKeys.ToDictionary(k => k.ToString(), k => ConfigurationManager.AppSettings[k].ToString());
                Console.Error.WriteLine("{{ Azure settings loaded from app.config file. }}"); //i| Writing trivial output to stderr; should probably just pull.
            }
        }
    }

#endregion
}