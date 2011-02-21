using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Provides callbacks to initialize, run and stop instances of the worker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
#region | FIELDS

        private const Int32 ConnectionLimitDefault = 12;
        private const Int32 MillisecondsBetweenStatusCheck = 60000;
        private static String _instanceName;

#endregion
#region | PROPERTIES

        /// <summary>
        /// Gets the name of the current application role instance.
        /// </summary>
        public static String InstanceName
        {
            get { return _instanceName ?? ( _instanceName = RoleEnvironment.CurrentRoleInstance.Role.Name ); }
        }
 
#endregion
#region | EVENTS

        /// <summary>
        /// Run method to start the apache process in the worker role instances.
        /// </summary>
        public override void Run()
        {
            Trace.TraceInformation("{0} : Windows Azure Run() event.", InstanceName);
            
            while ( true )
            {
                Thread.Sleep(MillisecondsBetweenStatusCheck);
                //Trace.TraceInformation("{0} : .:.Pulse.:.", InstanceName);
            }
        }

        /// <summary>
        /// Initializes the service accelerator, loading configuration, and allocating resources.
        /// </summary>
        public override Boolean OnStart()
        {
            Trace.TraceInformation("{0} : Windows Azure OnStart() event.", InstanceName);

            //i| Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = ConnectionLimitDefault;
            RoleEnvironment.Changing += RoleEnvironmentChanging;
            RoleEnvironment.Stopping += (s, e) => ServiceManager.Stop();

            //i|
            //i| Load and configure all applications.
            //i|
            ServiceManager.Start();

            return base.OnStart();
        }

        /// <summary>
        /// Performs the tear-down of the accelerator services in a graceful manner.
        /// </summary>
        /// <returns></returns>
        public override void OnStop()
        {
            Trace.TraceInformation("{0} : Azure OnStop() event.", InstanceName);   
            
            //i|
            //i| Stop all applications gracefully.
            //i|
            ServiceManager.Stop();
 
            base.OnStop();
        }

        /// <summary>
        /// This method is to restart the role instance when a configuration setting is changing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            Trace.TraceWarning("{0} : Windows Azure RoleEnvironmentChanging() event.", InstanceName);   

            //i| Verify configuration setting change.
            if ( e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange) )
            {
                e.Cancel = true;  // Set e.Cancel to true to restart this role instance
            }
        }

#endregion
    }
}