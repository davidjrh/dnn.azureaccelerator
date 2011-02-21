using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.Diagnostics;
using smarx.BlobSync;
using Timer=System.Threading.Timer;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Manages the configuration and execution of an application and its dependencies.
    /// </summary>
    public class Application
    {

#region | PROPERTIES

        public String Name { get { return XAppConfig.GetAttribute("name"); } }
        public String Version { get { return XAppConfig.GetAttribute("version"); } }
        public List<Application> ChildApplications { get; private set; }
        public List<BlobSync> BlobSyncInstances { get; private set; }
        public Dictionary<String, Process> Processes { get; private set; }
        public Dictionary<String, LocalResource> LocalResources { get; private set; }
        public Dictionary<String, CloudStorageAccount> Accounts { get; private set; }

        private XElement XAppConfig { get; set; }
        private XElement Config { get { return XAppConfig.Element("configuration"); } }
        private XElement Dependencies { get { return XAppConfig.Element("dependencies"); } }
        private IEnumerable<XElement> Params { get { return XAppConfig.Descendants("param"); } }
        private IEnumerable<XElement> Vars { get { return XAppConfig.Descendants("variable"); } }
        
#endregion
#region | CONSTRUCTOR

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <remarks>
        /// Variables / Params:
        /// 
        /// Variables overwrite existing keys (whereas parameters are inherited; and do not).  This allows parent
        /// applications the ability to override parameter values in constituent child applications.  
        /// For example, an application may have IIS and PHP as dependant child applications.  The params pointing 
        /// to locations and paths are specific to the application. The application can override any param
        /// declared in IIS or PHP by simply declaring its own param of the same name/key.
        /// 
        /// Processing Order:
        /// 
        /// Params, variables, and process tear-down are the only 3 occasions where the parent applications goes first. 
        /// In all other instances of provisioning resource, configuration, and application start orchestration; it is 
        /// the child applications who perform their actions prior to the parent applications.
        /// </remarks>
        /// <param name="applicationConfigurationSection">The application configuration xml.</param>
        public Application(XElement applicationConfigurationSection)
        {
            XAppConfig = applicationConfigurationSection;
            LogLevel.Information.Trace(Name, "Initialization : Starting...");
            LogLevel.Verbose.TraceContent(Name, XAppConfig.ToString(), "Initialization : Definition :");

            LocalResources    = new Dictionary<String, LocalResource>();
            Accounts          = new Dictionary<String, CloudStorageAccount>();
            Processes         = new Dictionary<String, Process>();
            BlobSyncInstances = new List<BlobSync>();
            ChildApplications = new List<Application>();
                
            //i| 
            //i| Process paramerters.
            //i|
            LogLevel.Information.Trace(Name, "Initialization : Parameters : Loading.");
            foreach ( XElement parameter in Params )
            {
                //i|
                //i| Parameters are disregarded if they have already been set by a loaded application.
                //i|
                if (parameter.IsEnabled()) ProcessVariableElement(parameter, true);
            }
            LogLevel.Information.Trace(Name, "Initialization : Parameters : Loaded.");

            //i|
            //i| Process variables.
            //i|
            LogLevel.Information.Trace(Name, "Initialization : Variables : Loading...");
            foreach ( XElement variable in Vars )
            {
                //i|
                //i| Variables overwrite existing keys (whereas parameters are inherited; and do not).
                //i|
                if (variable.IsEnabled()) ProcessVariableElement(variable, false);
            }
            LogLevel.Information.Trace(Name, "Initialization : Variables : Loaded.");

            //i| 
            //i| Load any child applications required by this application.
            //i|
            foreach ( var a in XAppConfig.XPathSelectElements("dependencies/applications/application").ToList())
            {
                if ( a.IsEnabled() )
                {
                    XElement cs = ServiceManager.GetApplicationConfigurationSection(XAppConfig.Parent.Document, a.GetAttribute("name"), a.GetAttribute("version"));
                    if (!ServiceManager.IsExistingApplication(cs.GetAttribute("name"), cs.GetAttribute("version")))
                        ChildApplications.Add(new Application(cs));
                }
            }
            LogLevel.Information.Trace(Name, "Initialization : Completed.");
        }

        /// <summary>
        /// Determines whether an instance of the application exists as a dependant application.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        public Boolean IsChildApplication(String name, String version)
        {
            foreach ( var a in ChildApplications )
                if ( ( a.Name == name && a.Version == version ) || a.IsChildApplication(name, version) )
                    return true;
            return false;
        }

#endregion
#region | EVENTS

        /// <summary>
        /// Performs initialization and configuration of resources for this and all dependant applications.
        /// </summary>
        public void OnStart()
        {
            //i| Process all dependant application's dependencies and configuration first.
            foreach ( Application application in ChildApplications )
                application.OnStart();

            //i| Process application dependencies.
            if ( Dependencies != null && Dependencies.HasElements )
                ProcessDependencies();

            //i| Process application configuration.
            if ( Config != null && Config.HasElements )
                ProcessConfiguration();
        }

        /// <summary>
        /// Manages the execution of process and dependant processes for the configured accelerator application instance.
        /// </summary>
        public void OnRun()
        {
            //i| Process dependant applications first.
            foreach (Application application in ChildApplications)
                application.OnRun();

            //i| Launch all defined processes for this application.
            foreach ( XElement e in XAppConfig.XPathSelectElements("runtime/onStart/process").ToList() )
                if ( e.IsEnabled() ) ProcessStartElement(e);

            //i| Launch hosted web servers.
            foreach ( XElement e in XAppConfig.XPathSelectElements("runtime/onStart/webServer").ToList() )
                if ( e.IsEnabled() ) ProcessWebServerElement(e);
        }

        /// <summary>
        /// Manages the running components.
        /// </summary>
        public void OnRunning()
        {
            //i| Process dependant applications first.
            foreach (Application application in ChildApplications )
                application.OnRunning();

            //i| Process and runtime application timers.
            foreach ( XElement e in XAppConfig.XPathSelectElements("runtime/onRunning/timer").ToList() )
                if ( e.IsEnabled() ) ProcessTimerElement(e);
        }

        /// <summary>
        /// Performs the tear down of application environment and configuration files using Azure runtime service values.
        /// </summary>
        public void OnStop()
        {
            //i| Start all configuration 'graceful' tear-down processes.
            foreach ( XElement e in XAppConfig.XPathSelectElements("runtime/onStop/process").ToList() )
                if ( e.IsEnabled() ) ProcessStartElement(e);

            //i| Kill any processes we started that still remain.
            foreach ( var process in Processes.Values)
                if ( process != null && !process.HasExited )
                    process.Protect(p => p.Kill());
            Processes.Clear();

            //i| Abort any blob synchronization timers.
            foreach ( var blobSync in BlobSyncInstances )
                blobSync.Stop();
            BlobSyncInstances.Clear();

            //i| Finally, do the same for each of our dependant applications.
            foreach ( Application application in ChildApplications )
                application.OnStop();
        }

        /// <summary>
        /// Performs the intialization of applications dependencies and resources.
        /// </summary>
        private void ProcessDependencies()
        {
            LogLevel.Information.Trace(Name, "Load Dependencies : Starting...");

            //i| Process local file storage and cache.
            foreach ( XElement e in Dependencies.Descendants("localStorage") )
                if (e.IsEnabled()) ProcessLocalStorageElement(e);

            //i| Process provisioned Endpoints
            foreach ( XElement e in Dependencies.Descendants("endPoint") )
                if (e.IsEnabled()) ProcessEndPointElement(e);

            //i| Process azure cloud drives.
            foreach ( XElement e in Dependencies.Descendants("cloudDrive") )
                if (e.IsEnabled()) ProcessCloudDriveElement(e);

            //i| Process blob to local storage synch
            foreach ( XElement e in Dependencies.Descendants("containerSync") )
                if (e.IsEnabled()) ProcessBlobSyncElement(e);

            //i| Process local file copy elements (eg. read-only AppRole files to local storage)
            foreach ( XElement e in Dependencies.Descendants("localCache") )
                if (e.IsEnabled()) ProcessLocalCopyElement(e);

            //i| Perform file validation.
            foreach ( XElement e in Dependencies.Descendants("fileValidation") )
                if (e.IsEnabled()) ProcessFileValidationElement(e);

            LogLevel.Information.Trace(Name, "Load Dependencies : Completed.");
        }

        /// <summary>
        /// Performs the initialization of application environment and configuration files using Azure runtime service values.
        /// </summary>
        private void ProcessConfiguration()
        {
            LogLevel.Information.Trace(Name, "Configuration : Starting...");

            //i| Process configuration files.
            foreach ( XElement e in Config.Descendants("file") )
                if (e.IsEnabled()) ProcessFileConfigurationElement(e);

            //i| Process environment variables.
            foreach ( XElement e in Config.Descendants("environmentVariable") )
                if (e.IsEnabled()) ProcessEnvironmentVariableElement(e);

            LogLevel.Information.Trace(Name, "Configuration : Completed.");
        }

#endregion
#region | CONFIGURATION

        /// <summary>
        /// Processes the variable element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="isParam">if set to <c>true</c> is param.</param>
        private static void ProcessVariableElement(XElement element, Boolean isParam)
        {
            String key = element.GetAttribute("key");
            String value = element.GetAttribute("value");

            //i| Parameters which are already set are not evaluated.  This allows parent applications 
            //i| the ability to override this class of variable in constituent dependant apps.
            if ( String.IsNullOrEmpty(value) || ( ServiceManager.Variables.ContainsKey(key) && isParam ) )
                return;
            ServiceManager.Variables[key] = value;
        }
        
        /// <summary>
        /// Processes the timer element for OnRunning interval processes (such as custom log collection to upload folder).
        /// </summary>
        /// <param name="element">The timer configuration element.</param>
        private void ProcessTimerElement(XElement element)
        {
            var config = new
                             {
                                TimerName       = (String) new Variable(element.GetAttribute("name")),
                                IntervalSeconds = (Int32) new Variable(element.GetAttribute("intervalInSeconds")),
                                Processes       = element.Descendants("process").OnValid(e => e.Where(p => p.IsEnabled()).ToList())
                             };
            if ( config.Processes == null )
                return;
            LogLevel.Information.Trace(Name, "Running() : Creating interval processes execution timer : {{{{ Name: '{0}' }}, {{ IntervalSeconds: '{1}' }}}}.", config.TimerName, config.IntervalSeconds);
            new Timer(timerObject =>
                          {
                              LogLevel.Information.Trace(Name, "Running() : Interval process triggered : {{ '{0}' }}.", config.TimerName);
                              foreach (XElement process in config.Processes)
                                  ProcessStartElement(process);
                          },
                      config,
                      config.IntervalSeconds * 1000,
                      config.IntervalSeconds * 1000);
        }

        /// <summary>
        /// Starts a process based on a process element configuration.
        /// </summary>
        /// <param name="element">The process configuration element.</param>
        private void ProcessStartElement(XElement element)
        {
            String  processKey             = new Variable(element.GetAttribute("processKey"));
            String  command                = Environment.ExpandEnvironmentVariables(new Variable(element.GetAttribute("command")));
            String  args                   = Environment.ExpandEnvironmentVariables(new Variable(element.GetAttribute("args")));
            String  workingDir             = Environment.ExpandEnvironmentVariables(new Variable(element.GetAttribute("workingDir")));
            Boolean waitOnExit             = new Variable(element.GetAttribute("waitOnExit") ?? "false");
            Int32   waitTimeoutInSeconds   = new Variable(element.GetAttribute("waitTimeoutInSeconds") ?? "0");
            Int32   delayContinueInSeconds = element.AttributeAsVariable("delayContinueInSeconds", "0");

            LogLevel.Information.Trace(
                Name,
                "Process : Creating Process : {{{{ {0} }}, {{ '{1} {2}' }}, {{ WorkingDirectory: '{3}' }}, {{ DelaySeconds: '{4}' }}}}.",
                processKey,
                command,
                args,
                workingDir,
                delayContinueInSeconds
                );
            Processes[processKey] = RunProcess(processKey, command, workingDir, args, waitOnExit, waitTimeoutInSeconds);
            LogLevel.Information.Trace(Name, "Process : {0} : Waiting for '{1}' seconds before continue...", processKey, delayContinueInSeconds);
            Thread.Sleep(delayContinueInSeconds * 1000);
        }

        /// <summary>
        /// Processes the web server element (hosted web core).  The hosted web core is started by the 
        /// Service Manager after all defined applications have processed their individual OnStart 
        /// definitions.
        /// </summary>
        /// <param name="element">The configuration element.</param>
        private static void ProcessWebServerElement(XElement element)
        {
            if ( new Variable(element.GetAttribute("enablePhp") ?? "false") )
            {
                //b| NOTE:
                //b|
                //b| This comment belongs elsewhere, but I am writing it now and don't want to burn
                //b| bandwidth to figure out where it belongs.  So, please understand that the comment
                //b| below is much larger in breadth and scope than the place at which I am typing 
                //b| it would reasonably indicate. (i|rdm)
                //b|
                //i| If true, the PHPRC environment variable must be set to the PHP home directory
                //i| prior to runtime start of the hosted web core.  The easiest way to do this
                //i| is to make the PHP application definition a child dependency of the hosted web
                //i| core application.  
                //i|
                //i| This is done is serveral sample apps such as Wordpress (via IIS); where the
                //i| the definition includes IIS and PHP dependencies; and then sets 'enablePhp'
                //i| to true in its own web core definition. (Its web core definitions extends the
                //i| base definitions provided by including the aforementioned child IIS dependency.)
                //i|
                //i| Of course, the Wordpress (via IIS) application section could declare all of the
                //i| IIS (webcore) and PHP related resources in its own definition; and forego any 
                //i| child dependecies, but whats the fun in that?  (Or the reusability?)
                //i|
                //i| Breaking out the definitions for applications such as IIS and PHP enable them
                //i| to be used in multiple instances (such as hosting dozens of ASP.NET apps) in a 
                //i| clean and modular fashion.  With definitions building upon themselves and playing
                //i| well together.  (Or at least as well as I can in 5 weeks worth of work.)
                //i|
                //i| Wordpress (via Apache) would declare Apache and PHP dependencies; and would start
                //i| the Apache httpd process (and not declare a web core element at all).
                ServiceManager.WebServer.IsPhpEnabled = true;
            }

            //i| Once the value has been set to true (requiring a Classic Mode pipeline), it remains true.  
            //i| This prevents other dependant, parent or separate applications from removing this resource
            //i| after initially requested.
            ServiceManager.WebServer.IsClassicModeEnabled = new Variable(element.GetAttribute("enableClassicPipelineMode") ?? "false");

            //i| Add all web applications (they will all share the same application pool.)  There is only 
            //i| one hosted web core per, but it may host an indefinite number of applications ( using 
            //i| the same appplication pool).
            foreach (XElement e in element.Elements("application"))
                ServiceManager.WebServer.AddApplication(
                    new Variable(e.GetAttribute("applicationPath")),
                    new Variable(e.GetAttribute("physicalPath")),
                    new Variable(e.GetAttribute("virtualDirectory") ?? "/")
                    );

            //i| Add all unique bindings. ( Any duplicates bindings added by this or other active applications'
            //i| definitions will simply be ignored, )
            foreach ( XElement e in element.Elements("binding") )
                ServiceManager.WebServer.Bindings.Add(
                        new WebServerBinding
                            {
                                Address = new Variable(e.GetAttribute("address")),
                                Port = new Variable(e.GetAttribute("port")),
                                HostHeader = new Variable(e.GetAttribute("host")),
                                Protocol = new Variable(e.GetAttribute("protocol") ?? "Http").ToString().ToEnum<WebServerBinding.ProtocolType>()
                            }
                    );
        }

        /// <summary>
        /// Processes the local storage element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessLocalStorageElement(XElement element)
        {
            String resourceName = new Variable(element.GetAttribute("configurationResourceName"));
            String pathKey      = new Variable(element.GetAttribute("pathKey"));
            String maxSizeKey   = new Variable(element.GetAttribute("maximumSizeKey"));
            
            LocalResource storage = RoleEnvironment.GetLocalResource(resourceName);
            LocalResources.Add(pathKey, storage); 
            ServiceManager.Variables[pathKey]    = storage.RootPath.TrimEnd('\\', '/', ' ');
            ServiceManager.Variables[maxSizeKey] = storage.MaximumSizeInMegabytes.ToString();
            
            LogLevel.Information.Trace(Name, "LocalStorage : Loaded : {{{{ Name: '{0}' }}, {{ Path, '{1}' }}, {{ Size: '{2}' }}}}.", resourceName, ServiceManager.Variables[pathKey], ServiceManager.Variables[maxSizeKey]);
        }

        /// <summary>
        /// Processes the end point element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessEndPointElement(XElement element)
        {
            String endPointName = new Variable(element.GetAttribute("configurationEndPointName"));
            String portKey      = new Variable(element.GetAttribute("portKey")    ?? ( element.GetAttribute("configurationEndPointName") + "Port" ));
            String addressKey   = new Variable(element.GetAttribute("addressKey") ?? ( element.GetAttribute("configurationEndPointName") + "Address" ));

            IPEndPoint localEndPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endPointName].IPEndpoint;
            ServiceManager.Variables[portKey] = localEndPoint.Port.ToString();
            ServiceManager.Variables[addressKey] = localEndPoint.Address.ToString();
            
            LogLevel.Information.Trace(Name, "EndPoint : Loaded : {{{{ Name: '{0}' }}, {{ Address: '{1}' }}, {{ Port: '{2}' }}}}.", endPointName, ServiceManager.Variables[addressKey], ServiceManager.Variables[portKey]);
        }

        /// <summary>
        /// Processes the cloud drive element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessCloudDriveElement(XElement element)
        {
            String  pathKey           = new Variable(element.GetAttribute("pathKey"));
            String  connectionString  = new Variable(element.GetAttribute("connectionString"));
            String  pageBlobUri       = new Variable(element.GetAttribute("pageBlobUri"));
            Int32   cacheSize         = new Variable(element.GetAttribute("cacheSize"));
            Boolean readOnly          = new Variable(element.GetAttribute("readOnly") ?? "false");
            Boolean copyBlob          = new Variable(element.GetAttribute("copyBlob") ?? "false");
            Boolean createIfNotExists = new Variable(element.GetAttribute("createIfNotExist") ?? "false");

            ServiceManager.InitializeCloudDriveCache(); //i| Initialize Cache (once for all drives)
            try
            {                
                //i|
                //i| Get cloud storage account.
                //i|
                CloudStorageAccount account;
                if ( !ServiceManager.IsRunningInCloud )
                {
                    LogLevel.Information.Trace(Name, "CloudDrive : Running in DevFabric : Using local storage connection.");
                    account = CloudStorageAccount.DevelopmentStorageAccount;

                    //i| The first time running in the local dev fabric; create an empty drive. The new drive will show up as a drive letter,
                    //i| manually copy the application files to the new drive.  In all subsequent tests using the engine you're able to then
                    //i| simulate the cloud based startup and teardown locally.
                    createIfNotExists = true;  
                }
                else
                {
                    String cs = ( connectionString.Contains("DefaultEndpointsProtocol=") ) //i| A variable could be a connection string or a setting.
                        ? connectionString
                        : RoleEnvironment.GetConfigurationSettingValue(connectionString);
                    cs = cs.Replace("https", "http");  //i| Cloud drives cannot use https; change if neccessary.
                    LogLevel.Information.Trace(Name, "CloudDrive : Connection String : {{ '{0}' }}.", cs);
                    account = CloudStorageAccount.Parse(cs);
                }
                LogLevel.Information.TraceContent(Name, account.ToTraceString() ?? String.Empty, "CloudDrive : Account : ");

                if (!account.CreateCloudBlobClient().GetBlobReference(pageBlobUri).Exists() && pageBlobUri.EndsWith("DrivePageBlobUri"))
                    pageBlobUri = DefaultSettings.CloudDriveUri;
            
                //i|
                //i| Check if already mounted.
                //i|
                CloudBlob blob = account.CreateCloudBlobClient().GetBlobReference(pageBlobUri);
                String driveKey = blob.Uri.ToString(); 
                if ( !ServiceManager.CloudDrives.ContainsKey(driveKey) )
                {
                    //i|
                    //i| New drive to mount.
                    //i|
                    CloudDrive drive = account.CreateCloudDrive(pageBlobUri);

                    //i|
                    //i| If specified, create a new empty cloud drive.
                    //i|
                    if ( createIfNotExists )
                    {
                        blob.Container.CreateIfNotExist();
                        drive.Protect(d => d.Create(cacheSize)); //i| Exceptions may be throw on success or pre-existing. Always attempt to mount anyway, so swallow this.    
                    }

                    //i|
                    //i| Mount cloud drive.
                    //i|
                    const Int32 totalAttempts = 3;
                    for ( Int32 attempt = 1; attempt <= totalAttempts; attempt++ )  //i| Per known azure issue multiple retries may be required to force a drive to mount.
                    {
                        try
                        {
                            drive.Mount(cacheSize, ServiceManager.IsRunningInCloud ? DriveMountOptions.Force : DriveMountOptions.None);
                        }
                        catch ( Exception ex )
                        {
                            if (attempt == totalAttempts) 
                                throw new Exception(String.Format("Unable to mount drive (attempt {0} of {1}).\r\n{2}", attempt, totalAttempts, drive.ToTraceString()), ex);
                        }
                        if ( !String.IsNullOrEmpty(drive.LocalPath) ) //i| Check drive path to determine success. (Possible to throw an exception; yet still have a valid mounted drive.)
                            break; 
                    }

                    //i|
                    //i| Add to global collection of mounted drives; keying by the full absolute blob URI.
                    //i|
                    ServiceManager.CloudDrives.Add(driveKey, drive); 
                }

                //i|
                //i| Add path variable. (Get drive from global collection since drive may be newly mounted or previously existing.)
                //i|
                ServiceManager.Variables[pathKey] = ServiceManager.CloudDrives[driveKey].LocalPath.TrimEnd('\\');
                LogLevel.Information.Trace(Name, "CloudDrive : Mounted : {{ Path: '{0}' }}.", ServiceManager.Variables[pathKey]);
            }
            catch ( Exception ex )
            {
                LogLevel.Error.TraceException(Name, ex, "CloudDrive : Failure : An exception occurred while attempting to mount the drive.");
                ServiceManager.ServiceState = ServiceState.MissingDependency;
            }
        }

        /// <summary>
        /// Processes the blob storage sync element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessBlobSyncElement(XElement element)
        {
            String  blobDirectoryUri      = new Variable(element.GetAttribute("blobDirectoryUri"));
            String  localPath             = new Variable(element.GetAttribute("localDirectoryPath"));
            Boolean ignoreAdditionalFiles = new Variable(element.GetAttribute("ignoreAdditionalFiles") ?? "true");
            String  connectionString      = new Variable(element.GetAttribute("connectionString"));
            SyncDirection direction       = ((String)new Variable(element.GetAttribute("direction"))).ToEnum<SyncDirection>() ?? SyncDirection.Download;
            Int32   interval              = new Variable(element.GetAttribute("intervalInSeconds") ?? "0");

            CloudBlobDirectory container = CloudStorageAccount.FromConfigurationSetting(connectionString).CreateCloudBlobClient().GetBlobDirectoryReference(blobDirectoryUri);
            BlobSync           blobSync  = new BlobSync(container, localPath, direction, ignoreAdditionalFiles);

            if ( interval > 0 ) //i| Spawns a thread to sync on timed interval.
            {
                blobSync.Start(TimeSpan.FromSeconds(interval));
                BlobSyncInstances.Add(blobSync);                
            }
            else //i| Sync once then continue.
            {
                blobSync.SyncAll();
            }
        }

        /// <summary>
        /// Processes a file copy between local locations. (eg. From approot read-only upload location to local drive storage.)
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessLocalCopyElement(XElement element)
        {
            String source      = new Variable(element.GetAttribute("source"));
            String destination = new Variable(element.GetAttribute("destination"));

            LogLevel.Information.Trace(Name, "FileCopy : Starting : {{{{ Source: '{0}' }}, {{ Destination, '{1}' }}}}.", source, destination);
            Int32 filesCopied = CopyDirectory(source, destination, 0);
            LogLevel.Information.Trace(Name, "FileCopy : Finished  : '{0}' files copied.", filesCopied);
        }

        /// <summary>
        /// Processes the file validation element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessFileValidationElement(XElement element)
        {
            String  path        = new Variable(element.GetAttribute("path"));
            Boolean checkAccess = new Variable(element.GetAttribute("checkAccess") ?? "false");
            Boolean required    = new Variable(element.GetAttribute("required") ?? "false");

            if ( File.Exists(path) )
            {
                if ( !checkAccess )
                {
                    LogLevel.Information.Trace(Name, "FileCheck : Verified : {{ '{0}' }}.", path);
                }
                else
                {
                    var ac = File.GetAccessControl(path);
                    var at = File.GetAttributes(path);
                    LogLevel.Information.TraceContent(Name,
                                                  String.Format(
                                                      "\r\n\t[(\"ACCESS CONTROL\" )]\r\n{0}\r\n\t[(\"ATTRIBUTES\" )]\r\n{1}",
                                                      ac.ToString("\t[ {0} : {1} ]\r\n", true, true),
                                                      at.ToString("\t[ {0} : {1} ]\r\n", true)
                                                      ), String.Format("FileCheck : Validated : {{ '{0}' }}.", path));
                }
            }
            else if (required)
            {
                ServiceManager.ServiceState = ServiceState.MissingDependency;
                LogLevel.Error.Trace(Name, "FileCheck : File Not Found : {{ '{0}' }}.", path);
            }
            else
            {
                LogLevel.Warning.Trace(Name, "FileCheck : File Not Found : {{ '{0}' }}.", path);                    
            }            
        }

        /// <summary>
        /// Processes the file configuration element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessFileConfigurationElement(XElement element)
        {
            //i| Check for a valid existing settings archive
            String  filePath     = new Variable(element.GetAttribute("path"));
            Boolean logging      = new Variable(element.GetAttribute("logging") ?? "true");
            String  archFilePath = filePath + @".azure";
            LogLevel.Information.Trace(Name, "FileConfig : Loading file : {{ '{0}' }}.", filePath);
            if ( File.Exists(archFilePath) && File.GetCreationTime(filePath).Subtract(File.GetCreationTime(archFilePath)).Seconds < 60 )
                File.Copy(archFilePath, filePath, true);
            File.Copy(filePath, archFilePath, true);

            //i| Read the file contents.
            String contents;
            using ( var reader = new StreamReader(filePath) )
                contents = reader.ReadToEnd();

            foreach ( XElement directive in element.Elements().Where(e => e.IsEnabled()) )
            {
                String  pattern                    = directive.GetAttribute("pattern");
                String  replacement                = directive.GetAttribute("replacement");
                String  replacePathChar            = new Variable(directive.GetAttribute("replacePathChar"));
                Boolean evalPatternVariables       = new Variable(directive.GetAttribute("evalVariablesInPattern") ?? "true");
                Boolean evalReplacementVariables   = new Variable(directive.GetAttribute("evalVariablesInReplacement") ?? "true");
                Boolean evalMatchVariables         = new Variable(directive.GetAttribute("evalVariablesInMatch") ?? "false");
                Boolean overwriteExistingVariables = new Variable(directive.GetAttribute("overwriteExistingVariables") ?? "false");
                Boolean explicitCapture            = new Variable(directive.GetAttribute("explicitCapture") ?? "false");

                if ( evalPatternVariables )
                    if ( directive.Name == "loadVariables" || directive.Name == "regex" ) //i| if the method is regex-based; ensure we escape the valiables 
                        pattern = new Variable(pattern).Render(Regex.Escape);             //i| replaced in the pattern so they don't invalidate the logic.
                    else
                        pattern = new Variable(pattern);
                if ( evalReplacementVariables )
                    replacement = new Variable(replacement);

                //i|
                //i| RegEx Load Variables
                //i|
                if ( directive.Name == "loadVariables" )
                {
                    LogLevel.Information.Trace(Name, "FileConfig : Variables : Loading...");
                    Regex regex = new Regex(pattern, explicitCapture ? RegexOptions.ExplicitCapture : RegexOptions.None);
                    Match match = regex.Match(contents);
                    if (match.Success)
                        foreach(String name in regex.GetGroupNames())
                        {
                            Group group = match.Groups[name];
                            if ( group != null && group.Success && ( !ServiceManager.Variables.ContainsKey(name) || overwriteExistingVariables ) )
                            {
                                LogLevel.Information.Trace(Name, "FileConfig : Variables : Matched : {{ \"{0}\": '{1}' }}", name, group.Value);
                                ServiceManager.Variables[name] = group.Value;
                            }

                        }
                    LogLevel.Information.Trace(Name, "FileConfig : Variables : Loaded.");
                }

                //i|
                //i| RegEx Update
                //i|
                else if ( directive.Name == "regex" )
                {
                    LogLevel.Information.Trace(Name, "FileConfig : RegEx : Applying...");
                    Regex regex = new Regex(pattern, explicitCapture ? RegexOptions.ExplicitCapture : RegexOptions.None);
                    contents = regex.Replace(contents, (match) =>
                    {
                        String value;
                        //i|
                        //i| Use the standard regex replacement (if a replacement was explicitily declared).
                        //i|
                        if ( !String.IsNullOrEmpty(replacement) )
                        {
                            value = match.Result(replacement);
                        } 
                        //i|
                        //i| Otherwise, use the pattern's named groups to locate the variable keys.
                        //i|
                        else
                        {
                            var varMatch = (   from n in regex.GetGroupNames().Where(names => match.Groups[names].Success)
                                               from v in ServiceManager.Variables
                                               where v.Key == n
                                               select new {
                                                        Key = v.Key,
                                                        Variable = v.Value
                                               }
                                           ).FirstOrDefault();
                            if (varMatch == null)
                                LogLevel.Warning.Trace(Name, "FileConfig : RegEx : Match : Corresponding variable key not found : '{0}'.", match.ToTraceString());
                            else
                                LogLevel.Information.Trace(Name, "FileConfig : RegEx : Matched : {{{{ Key: '{0}' }}, {{ Matched: '{1}' }}, {{ Replacement: '{2}' }}}}.", varMatch.Key, match.Groups[varMatch.Key].Value, varMatch.Variable);
                            value = varMatch != null ? (String) varMatch.Variable : String.Empty;
                        }

                        if ( evalMatchVariables )
                            value = new Variable(value);
                        
                        if (!String.IsNullOrEmpty(replacePathChar) )
                            value = value.ReplacePathChar(replacePathChar[0]); 

                        return value ?? String.Empty;
                    });
                    LogLevel.Information.Trace(Name, "FileConfig : RegEx : Applied.");
                }

                //i|
                //i| String Replace
                //i|
                else if ( directive.Name == "replace" )
                {
                    LogLevel.Information.Trace(Name, "FileConfig : Update : Replacing...");
                    if ( !String.IsNullOrEmpty(replacePathChar) )
                    {
                        //x| pattern = replacePathChar[0] == '\\' ? pattern.Replace('/', '\\') : pattern.Replace('\\', replacePathChar[0]);
                        replacement = replacement.ReplacePathChar(replacePathChar[0]);
                    }
                    contents = contents.Replace(pattern, replacement);
                    LogLevel.Information.Trace(Name, "FileConfig : Update : Replaced.");
                }

                //i|
                //i| String Append
                //i|
                else if ( directive.Name == "append" )
                {
                    LogLevel.Information.Trace(Name, "FileConfig : Update : Appending...");
                    if ( !String.IsNullOrEmpty(replacePathChar) )
                        pattern = pattern.ReplacePathChar(replacePathChar[0]);
                    contents = contents + pattern;
                    LogLevel.Information.Trace(Name, "FileConfig : Update : Appended.");                    
                }
            }

            //i| Write the file contents.
            LogLevel.Information.Trace(Name, "FileConfig : Saving updated file : {{ '{0}' }}.", filePath);
            using ( var writer = new StreamWriter(filePath) )
                writer.Write(contents);
            if (logging)
                LogLevel.Information.TraceContent(Name, contents, "FileConfig : File Saved : ");
            else
                LogLevel.Information.Trace(Name, "FileConfig : File saved.");
        }

        /// <summary>
        /// Processes the environment variable element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void ProcessEnvironmentVariableElement(XElement element)
        {
            String name            = new Variable(element.GetAttribute("name"));
            String value           = new Variable(element.GetAttribute("value"));
            String replacePathChar = new Variable(element.GetAttribute("replacePathChar"));
            Boolean evalReplacementVariables = new Variable(element.GetAttribute("evalEnvironmentVariablesInReplacement") ?? element.GetAttribute("evalInReplacement") ?? "false");

            LogLevel.Information.Trace(Name, "Environment : Updating environment variable : {{ '{0}' }}.", name);

            //i| Get the value; change the path separator char if required.
            if ( !String.IsNullOrEmpty(replacePathChar) )
                value = value.ReplacePathChar(replacePathChar[0]);
                
            //i| Evaluate the value for existing environment variables.
            if ( evalReplacementVariables ) 
                value = Environment.ExpandEnvironmentVariables(value);
            
            if (name.ToLower() == "path")
                value = new HashSet<String>(value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)).Aggregate((p,a) => a = p + ";" + a);
            
            //i| Set the worker process environment variable.
            Environment.SetEnvironmentVariable(name, value); 
            ServiceManager.EnvironmentVariables[name] = value;
            LogLevel.Information.Trace(Name, "Environment : Variable updated : {{{{ {0}: '{1}' }}}}.", name, value);            
        }

        /// <summary>
        /// Copies the files and folders recursively from source to destination
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="fileCount">The file count.</param>
        /// <returns>The file count of copied files in this folder and child folders.</returns>
        private Int32 CopyDirectory(String source, String destination, Int32 fileCount)
        {
            try
            {
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);
                String[] childDirs = Directory.GetDirectories(source);
                foreach (String directory in childDirs)
                {
                    Int32 pos = directory.LastIndexOf('\\');
                    String target = Path.Combine(destination, directory.Substring(pos + 1));
                    fileCount += CopyDirectory(directory, target, fileCount);
                }
                String[] files = Directory.GetFiles(source);
                foreach (String file in files)
                {
                    String targetFile = Path.Combine(destination, Path.GetFileName(file));
                    File.Copy(file, targetFile);
                    fileCount++;
                }
            }
            catch (Exception ex)
            {
                LogLevel.Error.TraceException(Name, ex, "FileCopy : An exception occurred while copying files : {{{{ Source: '{0}' }}, {{ Target: '{1}' }}, {{ FileCount: '{2}' }}}}.", source, destination, fileCount);
            }
            return fileCount;
        }

        /// <summary>
        /// Starts a new process with the Accelerator environment.
        /// </summary>
        /// <param name="processKey">The process key.</param>
        /// <param name="command">The file path.</param>
        /// <param name="workingDir">The working dir.</param>
        /// <param name="args">The args.</param>
        /// <param name="waitOnExit">if set to <c>true</c> [wait on exit].</param>
        /// <param name="waitTimeoutInSeconds">The wait timeout in seconds.</param>
        /// <returns></returns>
        private Process RunProcess(String processKey, String command, String workingDir, String args, Boolean waitOnExit, Int32 waitTimeoutInSeconds)
        {
            Process process = new Process();
            try
            {
                //i| setting the required properties for the process
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError  = true;
                process.StartInfo.RedirectStandardInput  = false; //bugbug| this might be wrong
                process.StartInfo.UseShellExecute        = false;
                process.StartInfo.CreateNoWindow         = true;
                process.StartInfo.WindowStyle            = ProcessWindowStyle.Hidden;

                //i| Set the process filename and args.
                process.StartInfo.FileName         = command;
                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.Arguments        = args;

                //i| Set the environment.
                foreach (var v in ServiceManager.EnvironmentVariables)
                    process.StartInfo.EnvironmentVariables[v.Key] = v.Value;

                //i| Set the console output handlers.
                process.ErrorDataReceived += ProcessOutputHandler;
                process.OutputDataReceived += ProcessOutputHandler;

                //i| Starting the process
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                LogLevel.Information.Trace(Name, "Process : {0} : Started : {{{{ Key: '{0}' }}, {{ ProcessId: '{1}' }}}}.", processKey, process.Id);
            }
            catch ( Exception ex )
            {
                LogLevel.Error.TraceException(Name, ex, "Process : {0} : An exception occured in the application.", processKey);
            }

            return process;
        }

        /// <summary>
        /// Processes the program(s) standard output and standard error streams.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="args">The <see cref="System.Diagnostics.DataReceivedEventArgs"/> instance containing the event data.</param>
        private void ProcessOutputHandler(Object process, DataReceivedEventArgs args)
        {
            if ( !String.IsNullOrEmpty(args.Data) )
            {
                String processString = "Process";
                var p = process as Process;
                if ( p != null )
                    if ( !p.HasExited )
                    {
                        lock (p)
                        {
                            processString = String.Format("{0} : {1} : {2}", processString, p.Id, p.ProcessName);
                        }
                    }
                    else
                    {
                        processString = String.Format("{0} : {1}Process has exited.", processString, p.ProcessName + ": ");
                    }
                LogLevel.Information.Trace(Name, "{0} : {1}", processString, args.Data);
            }
        }

#endregion
    }
}