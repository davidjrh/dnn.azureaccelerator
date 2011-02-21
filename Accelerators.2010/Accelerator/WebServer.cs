/* 
 *
 * Thanks to Smarx again, and again!
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Class manages the single available instance of a Hosted Web Core server.
    /// </summary>
    public class WebServer
    {
        private const String ParamTemplate = "{{{0}}}";
        public static readonly String DefaultApplicationPool = String.Format(ParamTemplate, Guid.NewGuid());

#region | FIELDS

        private readonly String _sourceApplicationHostFile    = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\Net\applicationHost.config";
        private readonly String _sourceWebConfigFile          = AppDomain.CurrentDomain.BaseDirectory +@"\Resources\Net\web.config";
        private readonly String _sourcePhpApplicationHostFile = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\Php\applicationHost.config";
        private readonly String _sourcePhpWebConfigFile       = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\Php\web.config";

        private Boolean  _isPhpEnabled = false;
        private Boolean  _isClassicModeEnabled = false;
        private String   _applicationPool;
        private String   _configDirectory;
        private String   _loggingDirectory;
        private String   _aspNetTempDirectory;
        private String   _iisCacheDirectory;
        private String   _failedReqLogsDirectory;
        private String   _phpRootDirectory;
        private XElement _machineKey;

        private HashSet<WebServerBinding> _bindings;
        private HashSet<WebServerApplication> _applications;

#endregion
#region | PROPERTIES 
        
        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsRunning
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets or sets a value indicating whether this instance is PHP enabled.  Once the value has been set to true (requiring PHP),
        /// it remains true.  This prevents other dependant, parent or separate applications from removing this resource after 
        /// initially requested.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is PHP enabled; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsPhpEnabled
        {
            get { return _isPhpEnabled; }
            set { _isPhpEnabled = _isPhpEnabled || value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance's application pool must run using the classic IIS pipeline mode.
        /// Once the value has been set to true (requiring a Classic Mode pipeline), it remains true.  This prevents other dependant, 
        /// parent or separate applications from removing this resource after initially requested.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance requires a Classic Mode IIS pipleline; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsClassicModeEnabled
        {
            get { return _isClassicModeEnabled; }
            set { _isClassicModeEnabled = _isClassicModeEnabled || value; }
        }

        /// <summary>
        /// Gets the set of binding configurations.
        /// </summary>
        /// <value>The bindings.</value>
        public HashSet<WebServerBinding> Bindings
        {
            get { return _bindings ?? ( _bindings = new HashSet<WebServerBinding>()); }
        }
        
        /// <summary>
        /// Gets the set of application configurations.
        /// </summary>
        /// <value>The applications.</value>
        public HashSet<WebServerApplication> Applications
        {
            get { return _applications ?? (_applications = new HashSet<WebServerApplication>()); } 
        }

        /// <summary>
        /// Gets or sets the machine key to be used by the hosted web server.
        /// </summary>
        /// <value>The machine key.</value>
        public XElement MachineKey
        {
            get { return _machineKey ?? (_machineKey = new XElement(GenerateMachineKey())); }
            set { _machineKey = value;  }
        }

        /// <summary>
        /// Gets or sets the application pool name to be used by the hosted web server.
        /// </summary>
        /// <value>The application pool.</value>
        public String ApplicationPool
        {
            get { return _applicationPool ?? (_applicationPool = DefaultApplicationPool); }
            set { _applicationPool = value; }
        }

        /// <summary>
        /// Gets the root application configuration for the hosted web server.
        /// </summary>
        /// <value>The root application.</value>
        public WebServerApplication RootApplication
        {
            get { return Applications.Where(a => a.ApplicationPath == "/").SingleOrDefault(); }
        }

        /// <summary>
        /// Gets the root directory for the hosted web server.
        /// </summary>
        /// <value>The root directory.</value>
        public String RootDirectory
        {
            get { return RootApplication.OnValid(ra => ra.PhysicalPath) ?? String.Empty; }
        }

        /// <summary>
        /// Gets the source applicationHost.config configuration file path.
        /// </summary>
        /// <value>The source application host path.</value>
        public String SourceApplicationHostPath
        {
            get { return IsPhpEnabled ? _sourcePhpApplicationHostFile : _sourceApplicationHostFile; }
        }

        /// <summary>
        /// Gets the source web.config configuration file path.
        /// </summary>
        /// <value>The source web config path.</value>
        public String SourceWebConfigPath
        {
            get { return IsPhpEnabled ? _sourcePhpWebConfigFile : _sourceWebConfigFile; }
        }

        /// <summary>
        /// Gets the runtime (target) applicationHost.config configuration file path.
        /// </summary>
        /// <value>The runtime application host path.</value>
        public String RuntimeApplicationHostPath
        {
            get { return Path.Combine(ConfigDirectory, "applicationHost.config"); }
        }

        /// <summary>
        /// Gets the runtime (target) web.config configuration file path.
        /// </summary>
        /// <value>The runtime web config path.</value>
        public String RuntimeWebConfigPath
        {
            get { return Path.Combine(ConfigDirectory, "web.config"); }
        }

        /// <summary>
        /// Gets the working directory for the hosted web server.
        /// </summary>
        /// <value>The working directory.</value>
        public String WorkingDirectory
        {
            get; 
            private set;
        }

        /// <summary>
        /// Gets the PHP processor for use by the hosted web server.  (Used when Php is enabled.)
        /// </summary>
        /// <value>The PHP processor.</value>
        public String PhpProcessor
        {
            get { return Path.Combine(PhpRootDirectory, "php-cgi.exe"); }
        }

        /// <summary>
        /// Gets or sets the PHP root directory.
        /// </summary>
        /// <value>The PHP root directory.</value>
        public String PhpRootDirectory
        {
            get { return _phpRootDirectory ?? Environment.GetEnvironmentVariable("PHPRC") ?? String.Empty; }
            set { _phpRootDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the config directory.
        /// </summary>
        /// <value>The config directory.</value>
        public String ConfigDirectory
        {
            get { return _configDirectory ?? Path.Combine(WorkingDirectory, "Config"); }
            set { _configDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the IIS cache directory.
        /// </summary>
        /// <value>The IIS cache directory.</value>
        public String IisCacheDirectory
        {
            get { return _iisCacheDirectory ?? Path.Combine(WorkingDirectory, "Iis"); }
            set { _iisCacheDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the ASP net temp directory.
        /// </summary>
        /// <value>The ASP net temp directory.</value>
        public String AspNetTempDirectory
        {
            get { return _aspNetTempDirectory ?? Path.Combine(WorkingDirectory, "Asp"); }
            set { _aspNetTempDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the logging directory.
        /// </summary>
        /// <value>The logging directory.</value>
        public String LoggingDirectory
        {
            get { return _loggingDirectory ?? Path.Combine(WorkingDirectory, "Logs"); }
            set { _loggingDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the failed req logs directory.
        /// </summary>
        /// <value>The failed req logs directory.</value>
        public String FailedReqLogsDirectory
        {
            get { return _failedReqLogsDirectory ?? LoggingDirectory; }
            set { _failedReqLogsDirectory = value; }
        }

#endregion
#region | CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public WebServer(String workingDirectory)
        {
            //i| Validate            
            if (String.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory))
                throw new ArgumentException("A valid working direcdtory is required.", "workingDirectory");

            //i| Set fields.
            WorkingDirectory = workingDirectory;
        }

#endregion
#region | CONFIGURATION

        /// <summary>
        /// Adds the application.
        /// </summary>
        /// <param name="applicationPath">The application path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <param name="virtualDirectoryPath">The virtual directory path.</param>
        public WebServerApplication AddApplication(String applicationPath, String physicalPath, String virtualDirectoryPath)
        {
            if (!applicationPath.EndsWith("/"))
                applicationPath += "/";
            if (!virtualDirectoryPath.EndsWith("/"))
                virtualDirectoryPath += "/";
            physicalPath = physicalPath.ToLower().TrimEnd('/', '\\', ' ');
            WebServerApplication app = (
                                            from    a in Applications
                                            where   a.ApplicationPath == applicationPath &&
                                                    a.PhysicalPath == physicalPath &&
                                                    a.VirtualDirectoryPath == virtualDirectoryPath
                                            select  a
                                        ).SingleOrDefault();
            if (app == null)
            {
                app = new WebServerApplication()
                          {
                              ApplicationPath = applicationPath,
                              PhysicalPath = physicalPath,
                              VirtualDirectoryPath = virtualDirectoryPath
                          };
                Applications.Add(app);
            }
            return app;
        }

        /// <summary>
        /// Renders the configuration files.
        /// </summary>
        /// <remarks>
        /// If I had a few more hours; this could be taken all the way to use the real Azure config
        /// dynamicall: will all actions being XML based instead of key replacement. |i|rdm|
        /// </remarks>
        private void RenderConfiguration()
        {
            //i| Validate
            TraceString();
            if (IsPhpEnabled && (String.IsNullOrEmpty(PhpRootDirectory) || !File.Exists(PhpProcessor)))
                throw new FileNotFoundException(String.Format("Unable to locate PHP processor file at '{0}'. Make sure you have defined the PHPRC environment variable to point to the root directory of valid PHP sources.", PhpProcessor));
            if (!File.Exists(SourceApplicationHostPath))
                throw new FileNotFoundException(String.Format("Unable to locate source applicationHost.config file at '{0}'.", SourceApplicationHostPath));
            if (!File.Exists(SourceWebConfigPath))
                throw new FileNotFoundException(String.Format("Unable to locate source web.config file at '{0}'.", SourceWebConfigPath));
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
            if (!Directory.Exists(AspNetTempDirectory)) Directory.CreateDirectory(AspNetTempDirectory);
            if (!Directory.Exists(IisCacheDirectory)) Directory.CreateDirectory(IisCacheDirectory);
            if (!Directory.Exists(LoggingDirectory)) Directory.CreateDirectory(LoggingDirectory);
            if (!Directory.Exists(FailedReqLogsDirectory)) Directory.CreateDirectory(FailedReqLogsDirectory);
            if (File.Exists(RuntimeApplicationHostPath)) File.Delete(RuntimeApplicationHostPath);
            if (File.Exists(RuntimeWebConfigPath)) File.Delete(RuntimeWebConfigPath);

            //i| Load configuration; Perform Substitutions.
            XDocument webConfig = XDocument.Parse(SubstituteParameters(File.ReadAllText(SourceWebConfigPath)));
            XDocument applicationHost = XDocument.Parse(SubstituteParameters(File.ReadAllText(SourceApplicationHostPath)));

            //i| Perform XML config.
            XElement pool = applicationHost.XPathSelectElement("configuration/system.applicationHost/applicationPools");
            pool.Elements("add").Remove();
            pool.Add(
                new XElement("add", 
                    new XAttribute("name", ApplicationPool),
                    IsClassicModeEnabled ? new XAttribute("managedPipelineMode", "Classic") : null)
                    );

            XElement site = applicationHost.XPathSelectElement("configuration/system.applicationHost/sites/site");
            site.Elements("application").Remove();
            Applications.ToList().ForEach(a => site.Add(a.Render()));

            XElement bindings = site.XPathSelectElement("bindings");
            bindings.OnValid(b => b.Elements("binding").Remove());
            Bindings.ToList().ForEach(b => bindings.Add(b.Render()));

            //i| Amend and append PHP config if enabled.
            if (IsPhpEnabled)
            {
                XElement cgiApp = applicationHost.XPathSelectElement("configuration/system.webServer/fastCgi/application");
                cgiApp.SetAttributeValue("fullPath", PhpProcessor);
                cgiApp.Element("environmentVariables").OnValid(evs =>
                    evs.Add(
                        new XElement("environmentVariable", 
                            new XAttribute("name", "PHPRC"),
                            new XAttribute("value", PhpRootDirectory.EnsureEndsWith("\\")))));
            }

            //i| Persist.
            webConfig.Save(RuntimeWebConfigPath);
            applicationHost.Save(RuntimeApplicationHostPath);
        }

        /// <summary>
        ///     Substitutes the parameters.
        /// </summary>
        /// <param name = "source">Source string from param replacement.</param>
        private String SubstituteParameters(String source)
        {
            var sb = new StringBuilder(source);
            GetReplacementParams().ForEach(kvp => sb.Replace(String.Format(ParamTemplate, kvp.Key), kvp.Value));
            return sb.ToString();
        }

        /// <summary>
        ///     Gets the dictionary of configuration replacement keys and values.
        /// </summary>
        private Dictionary<String, String> GetReplacementParams()
        {
            return new Dictionary<String, String>
                       {
                           {"approot",                      RootDirectory.EnsureEndsWith("\\")},
                           {"LogFilesDirectory",            LoggingDirectory},
                           {"FailedReqLogFilesDirectory",   FailedReqLogsDirectory},
                           {"iisCompressionCacheDirectory", IisCacheDirectory.EnsureEndsWith("\\")},
                           {"aspNetTempDirectory",          AspNetTempDirectory.EnsureEndsWith("\\")},
                           {"phpRootDirectory",             PhpRootDirectory.OnValid(d => d.EnsureEndsWith("\\")) ?? String.Empty},
                           {"applicationPoolName",          ApplicationPool},
                           {"machineKeyElement",            MachineKey.ToString()}
                       };
        }

        /// <summary>
        /// Creates a trace string of all configuration and settings values.  (i|rdm| remove after issue resolution)
        /// </summary>
        private void TraceString()
        {
            //i| Dump debug data.
            const string format = "\r\n\t{0,-30} {1}";
            var sb = new StringBuilder();
            sb.Append("\r\n\t[ LOCATIONS ]");
            sb.AppendFormat(format, "AppRoot:", ServiceManager.AppRootPath);
            sb.AppendFormat(format, "SourceApplicationHost:", SourceApplicationHostPath);
            sb.AppendFormat(format, "SourceWebConfig:", SourceWebConfigPath);
            sb.AppendFormat(format, "RuntimeApplicationHost:", RuntimeApplicationHostPath);
            sb.AppendFormat(format, "RuntimeWebConfig:", RuntimeWebConfigPath);
            sb.AppendFormat(format, "ConfigDirectory:", ConfigDirectory);
            sb.AppendFormat(format, "WorkingDirectory:", WorkingDirectory);
            sb.Append("\r\n\t[ PARAMETERS ]");
            GetReplacementParams().ForEach(kvp => sb.AppendFormat(format, kvp.Key + ":", kvp.Value));
            sb.Append("\r\n\t[ WEB APPLICATIONS ]");
            Applications.ForEach(a => sb.AppendFormat("\r\n\t{0}", a.Render()));
            sb.Append("\r\n\t[ BINDINGS ]");
            Bindings.ForEach(b => sb.AppendFormat("\r\n\t{0}", b.Render()));
            LogLevel.Information.TraceContent("WebServer", sb.ToString(), "Settings");
        }
        
#endregion
#region | MACHINE KEY

        /// <summary>
        /// Generates a valid ASP.NET machine key on the fly.
        /// </summary>
        /// <returns>ASP.NET machine key xml element as a string.</returns>
        public static String GenerateMachineKey()
        {
            Func<Int32, String> keyGen = length =>
                                             {
                                                 var buff = new Byte[length];
                                                 new RNGCryptoServiceProvider().GetBytes(buff);
                                                 var sb = new StringBuilder(length*2);
                                                 foreach (byte t in buff)
                                                     sb.AppendFormat("{0:X2}", t);
                                                 return sb.ToString();
                                             };

            String machineKey = String.Format(
                    @"<machineKey " +
                    @"validationKey=""{0}"" " +
                    @"decryptionKey=""{1}"" " +
                    @"validation=""SHA1"" " +
                    @"decryption=""AES"" />",
                    keyGen(64),
                    keyGen(32));
            return machineKey;
        }

#endregion
#region | EVENTS

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                RenderConfiguration();
                Int32 result = HwcpInterop.WebCoreActivate(RuntimeApplicationHostPath, RuntimeWebConfigPath,
                                                           Guid.NewGuid().ToString());
                if (result != 0) Marshal.ThrowExceptionForHR(result);
                else IsRunning = true;
                LogLevel.Information.Trace("WebServer", "Started() : Accepting connections...");
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            Int32 result = HwcpInterop.WebCoreShutdown(true);
            if (result != 0) Marshal.ThrowExceptionForHR(result);
        }

#endregion
#region | NATIVE INTEROP

        /// <summary>
        /// Internal class for instantiating and calling HWC entry points.
        /// </summary>
        internal static class HwcpInterop
        {
            public delegate int FnWebCoreActivate(
                [In, MarshalAs(UnmanagedType.LPWStr)] string appHostConfig,
                [In, MarshalAs(UnmanagedType.LPWStr)] string rootWebConfig,
                [In, MarshalAs(UnmanagedType.LPWStr)] string instanceName);

            public delegate int FnWebCoreShutdown(bool immediate);

            public static FnWebCoreActivate WebCoreActivate;
            public static FnWebCoreShutdown WebCoreShutdown;

            static HwcpInterop()
            {
                // Load the library and get the function pointers for the WebCore entry points
                const String hwcPath = @"%windir%\system32\inetsrv\hwebcore.dll";
                IntPtr hwc = NativeMethods.LoadLibrary(Environment.ExpandEnvironmentVariables(hwcPath));

                IntPtr procaddr = NativeMethods.GetProcAddress(hwc, "WebCoreActivate");
                WebCoreActivate = (FnWebCoreActivate) Marshal.GetDelegateForFunctionPointer(procaddr, typeof (FnWebCoreActivate));

                procaddr = NativeMethods.GetProcAddress(hwc, "WebCoreShutdown");
                WebCoreShutdown = (FnWebCoreShutdown) Marshal.GetDelegateForFunctionPointer(procaddr, typeof (FnWebCoreShutdown));
            }

            public static class NativeMethods
            {
                [DllImport("kernel32.dll")]
                public static extern IntPtr LoadLibrary(string dllToLoad);

                [DllImport("kernel32.dll")]
                public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
            }
        }

#endregion
    }
}