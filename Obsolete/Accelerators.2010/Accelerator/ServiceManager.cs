using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Controller class for all loading and managing accelerator configuration and processes.
    /// </summary>
    public static class ServiceManager
    {
#region | FIELDS

        private static CloudStorageAccount _cloudStorageAccount;
        private static readonly Dictionary<String, Variable> _variables = new Dictionary<String, Variable>();
        private static readonly Dictionary<String,String>    _environment = new Dictionary<String, String>();
        private static readonly Dictionary<String, String>   _environmentRollback = new Dictionary<String, String>();
        private static readonly List<Application>            _childApplications = new List<Application>();
        private static String                                _acceleratorName = DefaultSettings.AcceleratorTracingName; 
        private static LocalResource _acceleratorLocalStorage;
        private static LocalResource _acceleratorCloudDriveStorage;
        private static WebServer _webServer;
        private static Dictionary<String, CloudDrive> _drives;
                
#endregion
#region | PROPERTIES

        /// <summary>
        /// Gets the name of the current application role instance.
        /// </summary>
        public static String AcceleratorName
        {
            get { return _acceleratorName ?? (_acceleratorName = DefaultSettings.AcceleratorTracingName); } 
            private set { _acceleratorName = value; }
        }
        
        /// <summary>
        /// Gets or sets the overall service state.
        /// </summary>
        public static ServiceState ServiceState
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets the list of intialized role applications. 
        /// </summary>
        public static List<Application> ChildApplications
        {
            get { return _childApplications; }
        }

        /// <summary>
        /// Gets the dictionary of all configuration variables.
        /// </summary>
        public static Dictionary<String, Variable> Variables
        {
            get { return _variables; }
        }

        /// <summary>
        /// Gets the environment variables to be configured for each applications.
        /// </summary>
        public static Dictionary<String, String> EnvironmentVariables
        {
            get { return _environment; }
        }

        /// <summary>
        /// Gets the global dictionary of accelerator mounted cloud drives (by uri).
        /// </summary>
        public static Dictionary<String, CloudDrive> CloudDrives
        {
            get { return _drives ?? ( _drives = new Dictionary<String, CloudDrive>()); }
        }

        /// <summary>
        /// Gets the local storage area for use by the accelerators.  This is also the cache backing storage for all accelerator cloud drives.
        /// </summary>
        public static LocalResource LocalStorage
        {
            get { return _acceleratorLocalStorage ?? ( _acceleratorLocalStorage = RoleEnvironment.GetLocalResource("LocalStorage") ); }
        }

        /// <summary>
        /// Gets the accelerator local storage path.
        /// </summary>
        public static String LocalStoragePath
        {
            get { return LocalStorage.RootPath.TrimEnd('\\', '/'); }
        }

        /// <summary>
        /// Gets the local storage area for use by the accelerators.  This is also the cache backing storage for all accelerator cloud drives.
        /// </summary>
        public static LocalResource CloudDriveCacheStorage
        {
            get { return _acceleratorCloudDriveStorage ?? ( _acceleratorCloudDriveStorage = RoleEnvironment.GetLocalResource("CloudDriveCache") ); }
        }

        /// <summary>
        /// Gets the primary configuration file.
        /// </summary>
        private static XDocument XDefinitionsDocument
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets an instance of the CloudStorageAccount.
        /// </summary>
        private static CloudStorageAccount CloudStorageAccount
        {
            get
            {
                if ( _cloudStorageAccount == null )
                    if ( !CloudStorageAccount.TryParse(ConnectionString, out _cloudStorageAccount) )
                        if ( IsDevStorageEnabled ) _cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                return _cloudStorageAccount;
            }
        }

        /// <summary>
        /// Gets the web server; creating the single allowable instance the first time used.
        /// </summary>
        public static WebServer WebServer
        {
            get
            {
                if (_webServer == null)
                    _webServer = new WebServer(LocalStoragePath)
                                     {
                                         MachineKey = GetConfigSetting("AcceleratorMachineKey").OnValid(s => s.Protect(x => XElement.Parse(x))),
                                         LoggingDirectory = DiagnosticsSettings.LogStoragePath
                                     };
                return _webServer;
            }
        }

        /// <summary>
        /// Gets the role's root path.
        /// </summary>
        public static String RoleRootPath
        {
            get { return Environment.GetEnvironmentVariable("RoleRoot").OnValid(ar => ar.Trim('\\', '/')); }
        }

        /// <summary>
        /// Gets the role's app root path.
        /// </summary>
        public static String AppRootPath
        {
            get { return RoleRootPath + @"\approot"; }
        }

        /// <summary>
        /// Gets the location of the application definitions blob (if in use).
        /// </summary>
        private static String DefinitionsBlobUri
        {
            get { return GetConfigSetting("AcceleratorConfigBlobUri"); } //x| DefaultSettings.CloudDriveUri; }  (we don't want a default value for this; if it is not defined it is not in use.)
        }

        /// <summary>
        /// Gets the Azure storage account connection string.
        /// </summary>
        private static String ConnectionString
        {
            get { return RoleEnvironment.GetConfigurationSettingValue("AcceleratorConnectionString"); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is dev storage enabled.
        /// </summary>
        public static Boolean IsDevStorageEnabled
        {
            get { return !IsRunningInCloud && GetConfigSetting("EnableDevStorage").As<Boolean>(); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running in cloud.
        /// </summary>
        public static Boolean IsRunningInCloud
        {
            get { return !RoleEnvironment.DeploymentId.EndsWith(")"); /*i| hack, but for some things like forcing drive locks; we need to know the difference to prevent dev from stealing production accidentally. */ }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the cloud drive cache has already been initialized for this role instance.
        /// </summary>
        private static Boolean IsCloudDriveCacheInitialized
        {
            get; 
            set;
        }

#endregion
#region | EVENTS

        /// <summary>
        /// Loads the applications definitions from the configuration files.
        /// </summary>
        public static void InitializeServiceManager()
        {
            ServiceState = ServiceState.Healthy;

            //i| Load the accelerator services configuration.
            LogLevel.Information.Trace(AcceleratorName, "Initialization : Environment : Saving...");
            foreach ( DictionaryEntry kvp in Environment.GetEnvironmentVariables() )
                _environmentRollback[(String)kvp.Key] = kvp.Value as String;
            LogLevel.Information.Trace(AcceleratorName, "Initialization : Environment : Saved.");

            LogLevel.Information.Trace(AcceleratorName, "Initialization : Definitions : Loading...");
            XDefinitionsDocument = XDocument.Load(DefaultSettings.DefinitionsFilePath);
            XElement acceleratorConfig = XDefinitionsDocument.XPathSelectElement("serviceAccelerator/role");
            String roleRoot     = acceleratorConfig.OnValid(c => c.Element("roleRoot").GetAttribute("pathKey")) ?? "RoleRoot";
            String appRoot      = acceleratorConfig.OnValid(c => c.Element("appRoot").GetAttribute("pathKey")) ?? "AppRoot";
            String diagStorage  = acceleratorConfig.OnValid(c => c.Element("diagnoticsStorage").GetAttribute("pathKey")) ?? "DiagnosticLogs";
            String accelStorage = acceleratorConfig.OnValid(c => c.Element("localStorage").GetAttribute("pathKey")) ?? "LocalStorage";
            Variables[roleRoot]     = RoleRootPath;
            Variables[appRoot]      = AppRootPath;
            Variables[diagStorage]  = DiagnosticsSettings.LogStoragePath;
            Variables[accelStorage] = LocalStoragePath;
            LogLevel.Information.Trace(AcceleratorName, "Initialization : Definitions : Loaded.");
        }

        /// <summary>
        /// Called when accelerator is starting.
        /// </summary>
        public static void Start()
        {
            //i|
            //i| Initialize the global service manager resources.
            //i| 
            try 
            {
                LogLevel.Information.Trace(AcceleratorName, "Initialization : Starting...");
                InitializeServiceManager();
                LogLevel.Information.Trace(AcceleratorName, "Initialization : Completed.");
            }
            catch (Exception ex) 
            {
                ServiceState = ServiceState.Faulted;
                LogLevel.Error.TraceException(AcceleratorName, ex, "Initialization : Failed : An exception occured while loading and parsing application configuration file.");
                return;
            }

            //i|
            //i| Load and configure applications.
            //i|
            try
            {
                LogLevel.Information.Trace(AcceleratorName, "Configuration : Starting...");
                LoadApplications();
                StartApplications();
                RunApplications();
                LogLevel.Information.Trace(AcceleratorName, "Configuration : Completed.");
            }
            catch ( Exception ex )
            {
                ServiceState = ServiceState.Faulted;
                LogLevel.Error.TraceException(AcceleratorName, ex, "Configuration : Failed : An unhandled exception occured while intitializing application dependencies.");
                return;
            }
        }

        /// <summary>
        /// Performs the tear down of application environment and configuration files using Azure runtime service values.
        /// </summary>
        public static void Stop()
        {
            //i| Start the tear-down processes; dependents first.
            foreach (Application application in ChildApplications)
            {
                LogLevel.Information.Trace(AcceleratorName, "Stopping {0}...", application.Name); 
                application.Protect(a => a.OnStop());
                LogLevel.Information.Trace(AcceleratorName, "{0} stopped.", application.Name);
            }

            //i| Stop the hosted web core if it was started.
            if ( _webServer != null )
            {
                LogLevel.Information.Trace(AcceleratorName, "Stopping hosted web server...");
                _webServer.Protect(ws => ws.Stop());
                _webServer = null;
                LogLevel.Information.Trace(AcceleratorName, "Hosted web server stopped.");
            }
            
            //i| Release any locks created on cloud drives. //f| good opportunity to create a snapshot too
            foreach ( var drive in CloudDrives.Values )
                drive.Protect(d => d.Unmount());
            CloudDrives.Clear();

            LogLevel.Information.Trace(AcceleratorName, "All Services Stopped.");
            //b| bug need to rollback environment variables.
            Variables.Clear();
            EnvironmentVariables.Clear();
            ChildApplications.Clear();
            _environmentRollback.Clear();
            XDefinitionsDocument = null;

            Thread.Sleep(5000);
        }

        /// <summary>
        /// Performs a warm reset of this instance.
        /// </summary>
        /// <remarks>
        /// Using either of the two diagnostics console implementation; this is very useful for changing a blob based configuration and trying it out without having to wait for a complete recycle of the role.
        /// </remarks>
        public static void Reset()
        {
            Stop();
            Start();
        }

#region | APPLICATION 

        /// <summary>
        /// Manages the initialization and configuration of accelerator applications.
        /// </summary>
        private static void LoadApplications()
        {
            //i|
            //i| Active applications are defined in the CSCFG |x|rdm|depricated[( or if not there, under the accelerator.config role element )]
            //i|
            Dictionary<String, String> cloudApplications = RoleEnvironment.GetConfigurationSettingValue("AcceleratorApplication").OnValid(
                    setting => setting.SplitToDictionary<String, String>(new[] { ';' }, new[] { ',' }) 
                        ?? new Dictionary<String, String> { { setting, String.Empty } });

            if ( cloudApplications == null || cloudApplications.Count < 1 ) 
            {
                LogLevel.Error.Trace(AcceleratorName, "Configuration : Failure : Application startup setting not found. Verify that the setting AcceleratorApplication exists in the cloud configuration file, and that it includes the name and version of the configuration section for the application to deploy.");
                ServiceState = ServiceState.ConfigurationError;
                return;
            }

            //i| First, check for an explicitly defined custom application definition in blob storage.
            XDocument xdoc = null;
            if (!String.IsNullOrEmpty(DefinitionsBlobUri))
            {
                LogLevel.Information.Trace(AcceleratorName, "Initialization : Definitions : Attempting to load application definitions file from blob storage: {{ '{0}' }}.", DefinitionsBlobUri);
                xdoc = CloudStorageAccount.Protect(csa => XDocument.Parse(CloudStorageAccount.DownloadText(DefinitionsBlobUri)));
            }
            
            //i|
            //i| Create all applications (and any corresponding dependant applications).  Each application may be instantiated only once; 
            //i| regardless of mutiple explicit declarations or child-application dependency requirements.
            //i|)
            foreach ( var appKvp in cloudApplications )
            {
                XElement configSection = GetApplicationConfigurationSection(xdoc, appKvp.Key, appKvp.Value);
                String   name          = configSection.GetAttribute("name");
                String   version       = configSection.GetAttribute("version");
                if (!IsExistingApplication(name, version))
                {
                    ChildApplications.Add(new Application(configSection));
                }
            }
        }

        /// <summary>
        /// Starts the applications.
        /// </summary>
        private static void StartApplications()
        {
            //i|
            //i| Call the OnStart() event of each of the child application (just initialized above).
            //i|
            foreach ( Application application in ChildApplications )
                application.OnStart();
            WriteVariablesToTraceLog();
        }

        /// <summary>
        /// Manages the execution of accelerator processes.
        /// </summary>
        private static void RunApplications()
        {
            //i|
            //i| Start the processes (recursively dependents first).
            //i|
            foreach ( Application application in ChildApplications )
            {
                LogLevel.Information.Trace(AcceleratorName, "Starting '{0}'...", application.Name);
                try { application.OnRun(); LogLevel.Information.Trace(AcceleratorName, "'{0}' started.", application.Name); }
                catch ( Exception ex ) { LogLevel.Error.TraceException(AcceleratorName, ex, "An exception occured while starting '{0}'.", application.Name); }
            }

            //i|
            //i| Start the hosted web core if necessary.
            //i|
            if ( _webServer != null )
            {
                LogLevel.Information.Trace(AcceleratorName, "Starting hosted web server...");
                try { _webServer.Start(); LogLevel.Information.Trace(AcceleratorName, "Hosted web server started."); }
                catch (Exception ex) { LogLevel.Error.TraceException(AcceleratorName, ex, "An exception occured while starting the hosted web server."); }
            }

            //i| 
            //i| Start any OnRunning() interval or maintenance processes. Use to intiated maintenance or interval processes (backups, snapshots, log file copy to diagnostics directories, etc.).
            //i|
            foreach ( Application application in ChildApplications )
                try { application.OnRunning(); }
                catch (Exception ex) { LogLevel.Error.TraceException(AcceleratorName, ex, "An OnRunning() exception occured in '{0}'.", application.Name); }

            LogLevel.Information.Trace(AcceleratorName, "All Services Running...");
        }


#endregion

        /// <summary>
        /// Gets the application definition from the configuration file if it exists.
        /// </summary>
        /// <param name="xDefinitions">The applications definitions document.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns></returns>
        private static XElement GetDefinition(XDocument xDefinitions, String applicationName, String applicationVersion)
        {
            if (xDefinitions == null)
                return null;
            var appSections = xDefinitions.XPathSelectElements(String.Format("serviceAccelerator/application[@name=\"{0}\"]", applicationName)).ToList();
            return appSections.Where(s => s.GetAttribute("version") == applicationVersion).FirstOrDefault() ?? appSections.FirstOrDefault();
        }

        /// <summary>
        /// Gets the application configuration section matching the name and version attributes of the supplied dependency element.
        /// </summary>
        /// <param name="xDefinition">An applications definitions document.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns>Application configuration section.</returns>
        public static XElement GetApplicationConfigurationSection(XDocument xDefinition, String applicationName, String applicationVersion)
        {
            XDocument xdoc = xDefinition;
            Func<String, XDocument> load = (path) =>
                                               {
                                                    if (!String.IsNullOrEmpty(path) && File.Exists(path))
                                                    {
                                                        if ((xdoc = path.Protect(p => XDocument.Load(p))) != null)
                                                            LogLevel.Information.Trace(AcceleratorName, "Initialization : Definitions : Loaded application definitions file: {{ '{0}' }}.", path);
                                                    }
                                                    return xdoc;
                                               };
            XElement xdef = 
                   GetDefinition(xdoc, applicationName, applicationVersion)  //i| Check for definition in existing context.
                ?? GetDefinition(load(Path.Combine(DefaultSettings.DefinitionsFolder, applicationName + ".config")), applicationName, applicationVersion) //i| Attempt to load using application specific file.
                ?? GetDefinition(XDefinitionsDocument, applicationName, applicationVersion); //i| Attempt to load using the default file.
            return xdef;
        }

        /// <summary>
        /// Gets the config setting from Azure or application settings.
        /// </summary>
        public static String GetConfigSetting(String settingName)
        {
            return settingName.Protect(n => RoleEnvironment.GetConfigurationSettingValue(n)) ?? System.Configuration.ConfigurationManager.AppSettings[settingName];
        }

        /// <summary>
        /// Determines whether an instance of the specified application already exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        public static Boolean IsExistingApplication(String name, String version)
        {
            foreach ( var a in ChildApplications )
                if ((a.Name == name && a.Version == version) || a.IsChildApplication(name, version))
                    return true;
            return false;
        }

        /// <summary>
        /// Determines whether the specified element is enabled. Default is true.
        /// </summary>
        public static Boolean IsEnabled(this XElement element)
        {

            return element.AttributeAsVariable("enabled", Boolean.TrueString);
        }

        /// <summary>
        /// Returns the elements attribute as a variable or the default if not exists.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="variableDefault">The variable default.</param>
        public static Variable AttributeAsVariable(this XElement element, String attributeName, String variableDefault)
        {
            return new Variable(element.GetAttribute(attributeName) ?? variableDefault);
        }

        /// <summary>
        /// Writes the variables to trace log.
        /// </summary>
        public static void WriteVariablesToTraceLog()
        {
            LogLevel.Verbose.TraceContent(AcceleratorName, EnvironmentVariables.ToTraceString("ENVIRONMENT VARIABLES"), "Environment Variables");
            LogLevel.Verbose.TraceContent(AcceleratorName, Variables.ToTraceString("SERVICE MANAGER VARIABLES"), "Application Definition Variables");
        }

        /// <summary>
        /// Initializes the global cloud drive cache.  (Cache is per-role instance; and not per-drive mounted.)
        /// </summary>
        public static void InitializeCloudDriveCache()
        {
            if (!IsCloudDriveCacheInitialized)
            {
                LogLevel.Information.Trace(AcceleratorName, "Cloud Drives : Global Cache : Initializing...");
                var path = CloudDriveCacheStorage.RootPath.TrimEnd('\\', '/');
                var size = CloudDriveCacheStorage.MaximumSizeInMegabytes;
                CloudDrive.InitializeCache(path, size);
                IsCloudDriveCacheInitialized = true;
                LogLevel.Information.Trace(AcceleratorName, "Cloud Drives : Global Cache : {{{{ [Size]: '{0}' }}, {{ [Path]: '{1}' }}}}.", size, path);
                LogLevel.Information.Trace(AcceleratorName, "Cloud Drives : Global Cache : Initialized.");
            }
        }

#endregion
    }

    /// <summary>
    /// Default settings for Azure accelerators.
    /// </summary>
    public static class DefaultSettings
    {
        public const String AcceleratorTracingName = "ServiceManager";
        public const String CloudDriveUri = @"cloud-drives/accelerator.vhd";
        public static String DefinitionsFolder { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Definitions"); } }
        public static String DefinitionsFilePath { get { return Path.Combine(DefinitionsFolder, "Applications.config"); } }
    }

    /// <summary>
    /// Status of application and services startup orchestration.
    /// </summary>
    public enum ServiceState
    {
        Healthy = 0,
        MissingDependency = 1,
        ConfigurationError = 3,
        Faulted = 5
    }
}