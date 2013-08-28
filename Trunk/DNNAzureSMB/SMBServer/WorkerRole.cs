using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using DNNShared;

namespace SMBServer
{
    /// <summary>
    /// The SMB Server worker role entry point
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {

        private static string _drivePath;
        private static CloudDrive _drive;
        private static bool _driveMounted;

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
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            
            // Inits the Diagnostic Monitor
            Utils.ConfigureDiagnosticMonitor();

            // Setup OnChanging event
            RoleEnvironment.Changing += RoleEnvironmentOnChanging;
            
            try
            {
                // Create windows user accounts for shareing the drive and other FTP related
                Utils.CreateUserAccounts();

                // Enable SMB traffic through the firewall
                EnableSMBFirewallTraffic();

                // Setup the drive object
                _drive = Utils.InitializeCloudDrive(Utils.GetSetting("AcceleratorConnectionString"),
                                                        Utils.GetSetting("driveContainer"),
                                                        Utils.GetSetting("driveName"),
                                                        Utils.GetSetting("driveSize"));
                
                Trace.TraceInformation("Exiting SMB Server OnStart...");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fatal error on the OnStart event: {0}", ex);
                throw;
            }

            return base.OnStart();
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
            Trace.TraceInformation("SMBServer entry point called");

            CompeteForMount();
        }


        /// <summary>
        /// Competes for the cloud drive lease.
        /// </summary>
        private static void CompeteForMount()
        {

            for (; ; )
            {
                _driveMounted = false;
                _drivePath = "";
                try
                {
                    Trace.TraceInformation("Competing for mount {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                    Utils.MountCloudDrive(_drive);
                    _driveMounted = true;
                    Trace.TraceInformation("{0} Successfully mounted the drive!", RoleEnvironment.CurrentRoleInstance.Id);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Equals("ERROR_LEASE_LOCKED"))
                    {
                        //Trace.TraceInformation("{0} could not mount the drive. The lease is locked. Will retry in 5 seconds.",
                        //                   RoleEnvironment.CurrentRoleInstance.Id);
                    }
                    else
                    {
                        Trace.TraceWarning("{0} could not mount the drive, Will retry in 5 seconds. Reason: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    }
                }

                if (!_driveMounted)
                {
                    Thread.Sleep(5000);
                    continue;   // Compete again for the lease
                }

                // Shares the drive
                _drivePath = Utils.ShareDrive(_drive);

                // Setup the website settings
                Utils.SetupWebSiteSettings(_drive);

                // Setup the offline site settings
                Utils.SetupOfflineSiteSettings(_drive.LocalPath);

                // Now, spin checking if the drive is still accessible.
                Utils.WaitForMoutingFailure(_drive);

                // Drive is not accessible. Remove the share
                Utils.DeleteShare(Utils.GetSetting("shareName"));
                try
                {
                    Trace.TraceInformation("Unmounting cloud drive on role {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                    _drive.Unmount();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error while unmounting the cloud drive on role {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                }
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns



        /// <summary>
        /// Enables the SMB firewall traffic.
        /// </summary>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">Could not setup the firewall rules. See previous errors</exception>
        private static void EnableSMBFirewallTraffic()
        {
            if (Utils.EnableSMBFirewallTraffic() != 0)
            {
                throw new ConfigurationErrorsException("Could not setup the firewall rules. See previous errors");
            }
        }




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
            try
            {
                // Remove the share
                if (!string.IsNullOrEmpty(_drivePath))
                {
                    Utils.DeleteShare(Utils.GetSetting("shareName"));
                }

                if (_driveMounted && _drive != null)
                {
                    try
                    {
                        Trace.TraceInformation("Unmounting cloud drive on role {0}...", RoleEnvironment.CurrentRoleInstance.Id);
                        _drive.Unmount();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error while unmounting the cloud drive on role {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while stopping the role instance {0}: {1}", RoleEnvironment.CurrentRoleInstance.Id, ex);
            }

            base.OnStop();
        }

        /// <summary>
        /// This event is called after configuration changes have been submited to Windows Azure but before they have been applied in this instance
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoleEnvironmentChangingEventArgs" /> instance containing the event data.</param>
        private static void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // Implements the changes after restarting the role instance
            foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentConfigurationSettingChange))
            {
                Trace.TraceInformation("Configurations are changing...");
                switch (settingChange.ConfigurationSettingName)
                {
                    case "AcceleratorConnectionString":
                    case "driveName":
                    case "driveSize":
                    case "fileshareUserName":
                    case "fileshareUserPassword":
                    case "shareName":
                    case "driveContainer":
                        Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
                        e.Cancel = true;
                        break;
                }
                // TODO Otherwise, handle the Changed event for the rest of parameters
            }
        }
    }
}