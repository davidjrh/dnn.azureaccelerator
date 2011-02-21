using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;
using System.Linq;
using Microsoft.WindowsAzure.Diagnostics;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    /// <summary>
    /// Singleton class tracking the settings for an application.
    /// </summary>
    public class DiagnosticsSettings
    {
#region | FIELDS

        private const String DefaultApplicationName                 = "Accelerator";
        private const String DefaultBlobContainerName               = "wa-accelerator-logs";
        private const String DefaultDiagnosticsConnectionStringName = "DiagnosticsConnectionString";

        private static CloudStorageAccount        _cloudStorageAccount;
        private static Dictionary<String, String> _config;
        private static TraceSource                _traceSource;
        private static LocalResource              _localStorageResource;
        private static String                     _logStoragePath;
        private static String                     _applicationName;
        private static String                     _blobContainerName;
        
#endregion
#region | PROPERTIES

        /// <summary>
        /// Gets the packed configuration settings value if available.
        /// </summary>
        private static Dictionary<String, String> Config
        {
            get
            {
                if (_config == null)
                try {
                    _config = RoleEnvironment.GetConfigurationSettingValue("Diagnostics").OnValid(s => s.SplitToDictionary<String, String>(new[] { ';' }, new[] { '=' })) ?? new Dictionary<String, String>();
                }
                catch {
                    _config = new Dictionary<String, String>();
                }
                return _config;
            }
        }

        /// <summary>
        /// Gets or sets the trace source for diagnostics logging.
        /// </summary>
        internal static TraceSource TraceSource
        {
            get 
            { 
                if (_traceSource == null)
                    TraceSource = new TraceSource("AcceleratorTraceSource");
                return _traceSource;
            }
            set
            {
                if (_traceSource != null)
                    _traceSource.Close();
                _traceSource = value;
                if (IsLoggingEnabled)
                {
                    if (_traceSource.Switch.Level == SourceLevels.Off)
                        _traceSource.Switch.Level = SourceLevels.All;
                    foreach (TraceListener listener in Trace.Listeners)
                        _traceSource.Listeners.Add(listener);
                }
            }
        }
        
        /// <summary>
        /// Gets the local storage resource for diagnostics and trace data.
        /// </summary>
        public static LocalResource LocalStorage
        {
            get
            {
                if (_localStorageResource == null)
                    _localStorageResource = RoleEnvironment.GetLocalResource("DiagnosticLogs");
                return _localStorageResource;
            } 
            set
            {
                _localStorageResource = value;
            }
        }

        /// <summary>
        /// Gets or sets the log storage path.
        /// </summary>
        public static String LogStoragePath
        {
            get
            {   
                if (String.IsNullOrEmpty(_logStoragePath) && LocalStorage != null )
                    LogStoragePath = Path.Combine(LocalStorage.RootPath, DefaultBlobContainerName);
                return _logStoragePath;
            } 
            set
            {
                _logStoragePath = value;
                if ( !Directory.Exists(_logStoragePath) )
                    Directory.CreateDirectory(_logStoragePath);
                Environment.SetEnvironmentVariable("DiagnosticsLogs", _logStoragePath);
            }
        }
        
        /// <summary>
        /// Instance identifier for this deployment.
        /// </summary>
        public String InstanceId
        {
            get { return RoleEnvironment.CurrentRoleInstance.Id; }
        }

        /// <summary>
        /// Application name as specified in azure configuration.
        /// </summary>
        public String RoleName
        {
            get { return RoleEnvironment.CurrentRoleInstance.Role.Name; }
        }

        /// <summary>
        /// The virtual host name.
        /// </summary>
        public String VirtualHostName
        {
            get { return Environment.MachineName; }
        } 

        /// <summary>
        /// Determins whether to log trace events of sufficient severity to the Azure diagnostics system.
        /// </summary>
        public static Boolean IsLoggingEnabled
        {
            get { return (GetConfig("EnableLogging") ?? "true").As<Boolean>(); }
        }

        /// <summary>
        /// Gets the status of realtime tracking through the service bus.
        /// </summary>
        public static Boolean IsRealtimeTracingEnabled
        {
            get { return GetConfig("RealtimeTracing").As<Boolean>(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether socket console is enabled.
        /// </summary>
        public static Boolean IsDiagnosticsConsoleEnabled
        {
            get { return GetConfig("DiagnosticsConsole").As<Boolean>(); }
        }

        /// <summary>
        /// Transfer interval for diagnotics logs.
        /// </summary>
        public static TimeSpan DiagnosticsTransferInterval 
        { 
            get { return TimeSpan.FromMinutes((GetConfig("LogTransferInterval") ?? "5").As<Int32>()); } 
        }

        /// <summary>
        /// Gets or sets the logging priority filter.
        /// </summary>
        /// <value>The logging priority filter.</value>
        public static LogLevel LogFilter
        {
            get { return GetConfig("LogFilter").ToEnum<LogLevel>() ?? LogLevel.Verbose; }
        }

        /// <summary>
        /// Gets the name of the log.
        /// </summary>
        public static String ApplicationName
        {
            get { return _applicationName ?? ( _applicationName = GetConfig("ApplicationName") ?? DefaultApplicationName ); }
        }

        /// <summary>
        /// Gets the name of the blob container.
        /// </summary>
        public static String BlobContainerName
        {
            get { return _blobContainerName ?? (_blobContainerName = DefaultBlobContainerName); } 
            set { _blobContainerName = value; }
        }

        /// <summary>
        /// Get the default buffer quota in MB.
        /// </summary>
        public static Int32 BufferQuotaInMB 
        {
            get { return (GetConfig("BufferQuotaInMB") ?? "256").As<Int32>(); }
        }
        
        /// <summary>
        /// Get the runtime deployment status of the role.  True for development fabric; False for live Azure.
        /// </summary>
        public static Boolean IsDevStorageEnabled
        {
            get { return !IsRunningInCloud && RoleEnvironment.GetConfigurationSettingValue("EnableDevStorage").As<Boolean>(); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running in cloud. 
        /// </summary>
        public static Boolean IsRunningInCloud
        {
            get { return RoleEnvironment.DeploymentId.EndsWith(")"); }  /*i| hack, but required so that production doesn't accidentally try to use dev storage regardless of cscfg. */
        }

        /// <summary>
        /// Gets or sets a value indicating whether azure diagnostics has been started.
        /// </summary>
        /// <remarks>
        /// Multiple issuances of the Diagnostics.Start results in duplicated trace records.
        /// </remarks>
        public static Boolean IsAzureDiagnosticsStarted
        {
            get; private set;
        }

        /// <summary>
        /// Gets the instance of the SocketConsole service (if initalized).  Otherwise returns null.
        /// </summary>
        public static SocketConsole SocketConsole
        {
            get; private set;
        }

        /// <summary>
        /// Gets an instance of the CloudStorageAccount.
        /// </summary>
        public static CloudStorageAccount CloudStorageAccount
        {
            get
            {
                if ( _cloudStorageAccount == null )
                    if ( !CloudStorageAccount.TryParse(RoleEnvironment.GetConfigurationSettingValue(DefaultDiagnosticsConnectionStringName), out _cloudStorageAccount) )
                        if ( IsDevStorageEnabled ) _cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                return _cloudStorageAccount;
            }
        }

#endregion
#region | INITIALIZATION

        /// <summary>
        /// Initializes the <see cref="DiagnosticsSettings"/> class.
        /// </summary>
        static DiagnosticsSettings()
        {
            Trace.TraceInformation("Diagnostics : Initializing...");
            //i| Set the configuration settings handler.
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                                                                     {
                                                                         configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
                                                                         RoleEnvironment.Changed += (sender, arg) =>
                                                                                                        {
                                                                                                            if ( arg.Changes.OfType<RoleEnvironmentConfigurationSettingChange>().Any((change) => ( change.ConfigurationSettingName == configName )) )
                                                                                                                if ( !configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)) )
                                                                                                                    RoleEnvironment.RequestRecycle();
                                                                                                        };
                                                                     });
            try
            {
                if ( IsRealtimeTracingEnabled )
                    InitializeRealtimeTracing();
                if ( IsDiagnosticsConsoleEnabled )
                    InitializeDiagnosticsConsole();
                if ( IsLoggingEnabled )
                    InitializeDiagnostics();
                Trace.TraceInformation("Diagnostics : Initialized.");
            }
            catch ( Exception ex )
            {
                Trace.TraceError("Diagnostics : Failure : An unhandled exception occured during initialization.\r\n{0}", ex);
            }
        }

        /// <summary>
        /// Initializes the diagnostics console.
        /// </summary>
        private static void InitializeDiagnosticsConsole()
        {
            Trace.TraceInformation("Diagnostics : Socket Console : Starting...");
            try
            {
                //i| 
                //i| Use direct TCP endpoint if declared.
                //i|
                RoleInstanceEndpoint endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["DiagnosticsConsole"];
                if ( endPoint != null )
                {
                    SocketConsole = new SocketConsole(endPoint.IPEndpoint, SocketConsole.InstanceType.Server);
                }

                //i|
                //i| Use service bus connection if available. (b|rdm| Bug: SocketConsole service bus implementation isn't finished. Use the ServiceConsole version instead if you want this functionality.)
                //i|
                else
                {
                    ServiceBusConnection connection = ServiceBusConnection.FromConfigurationSetting("DiagnosticsServiceBus");
                    if ( connection != null )
                    {
                        connection.ServicePath = connection.ServicePath + "/console";
                        Trace.TraceInformation("Diagnostics : Socket Console : Service Bus Connection...\r\n{0}", connection.ToTraceString());
                        SocketConsole = new SocketConsole(connection, SocketConsole.InstanceType.Server);
                    }
                }
                if (SocketConsole != null) RoleEnvironment.Stopping += (s, e) => SocketConsole.Close();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Diagnostics : Socket Console: An unhandled exception occured.\r\nException:\r\n{0}\r\nStack Trace:\r\n{1}\r\n{2}", ex.Message, ex.StackTrace, ex.ToString());
                return;
            }
            Trace.TraceInformation("Diagnostics : Socket Console : Started.");
        }

        /// <summary>
        /// Adds the realtime trace listener to the the stack.
        /// </summary>
        private static void InitializeRealtimeTracing()
        {
            Trace.TraceInformation("Diagnostics : Realtime Tracing : Starting...");
            CloudTraceListener traceListener = null;
            try
            {
                ServiceBusConnection connection = ServiceBusConnection.FromConfigurationSetting("DiagnosticsServiceBus");
                if (connection != null)
                {
                    connection.ServicePath = connection.ServicePath;
                    Trace.TraceInformation("Diagnostics : Realtime Tracing : Service Bus Connection...\r\n{0}", connection.ToTraceString());
                    traceListener = new CloudTraceListener(connection);
                    TraceSource.Listeners.Add(traceListener);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Diagnostics : Realtime Tracing : An exception occurred initializing realtime tracing.\r\nException:\r\n{0}\r\nStack Trace:\r\n{1}\r\n", ex.Message, ex.StackTrace);
                if ( traceListener != null )
                    try
                    {
                        Trace.Listeners.Remove(traceListener);
                    }
                    catch { }
                return;
            }
            Trace.TraceInformation("Diagnostics : Realtime Tracing : Started.");
        }

        /// <summary>
        /// Starts the diagnostics.
        /// </summary>
        private static void InitializeDiagnostics()
        {
            Trace.TraceInformation("Diagnostics : Azure Diagnostics : Starting...");

            //i| Load azure diagnotics configurations.
            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
            //Int32 totalQuota = BufferQuotaInMB * 14;
            
            //i| Trace Logs Infrastructure.
            dmc.Logs.ScheduledTransferLogLevelFilter = LogFilter;
            dmc.Logs.ScheduledTransferPeriod = DiagnosticsTransferInterval;
            dmc.Logs.BufferQuotaInMB = BufferQuotaInMB;

            //i| Diagnostics Infrastructure.
            dmc.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogFilter;
            dmc.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = DiagnosticsTransferInterval;
            dmc.DiagnosticInfrastructureLogs.BufferQuotaInMB = BufferQuotaInMB;

            //i| Windows Event Logging.
            dmc.WindowsEventLog.DataSources.Add("Application!*");
            dmc.WindowsEventLog.DataSources.Add("Application!*[System[Provider[@Name='HostableWebCore']]]");
            dmc.WindowsEventLog.ScheduledTransferLogLevelFilter = LogFilter;
            dmc.WindowsEventLog.ScheduledTransferPeriod = DiagnosticsTransferInterval;
            dmc.WindowsEventLog.BufferQuotaInMB = BufferQuotaInMB;
            
            //i| Directories Inclusions.
            CloudBlobContainer blobContainer = CloudStorageAccount.CreateCloudBlobClient().GetContainerReference(BlobContainerName);
            blobContainer.CreateIfNotExist();
            
            dmc.Directories.ScheduledTransferPeriod = DiagnosticsTransferInterval;
            dmc.Directories.BufferQuotaInMB = BufferQuotaInMB;
            dmc.Directories.DataSources.Add(new DirectoryConfiguration
                                                {
                                                    Container = blobContainer.Name,
                                                    Path = LogStoragePath,
                                                    //DirectoryQuotaInMB = BufferQuotaInMB
                                                });
            //totalQuota += BufferQuotaInMB;
            
            //i| Set Initial Diagnostics Config.
            DeploymentDiagnosticManager.AllowInsecureRemoteConnections = true;
            DiagnosticMonitor.AllowInsecureRemoteConnections = true;
            CrashDumps.EnableCollectionToDirectory(LogStoragePath, true);
            CrashDumps.EnableCollection(true);
            //dmc.OverallQuotaInMB = totalQuota;
            dmc.ConfigurationChangePollInterval = DiagnosticsTransferInterval;
            DiagnosticMonitor.Start(CloudStorageAccount, dmc);
            IsAzureDiagnosticsStarted = true;
            Trace.TraceInformation("Diagnostics : Azure Diagnostics : Started.");
        }

#endregion
#region | HELPER METHODS

        /// <summary>
        /// Gets the config.
        /// </summary>
        private static String GetConfig(String settingName)
        {
            return Config.ContainsKey(settingName) 
                ? Config[settingName]
                : RoleEnvironment.GetConfigurationSettingValue(settingName);
        }

#endregion
    }
}