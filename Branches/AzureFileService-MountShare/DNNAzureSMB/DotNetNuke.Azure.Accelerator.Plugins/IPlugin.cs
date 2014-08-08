using Microsoft.WindowsAzure.ServiceRuntime;

namespace DotNetNuke.Azure.Accelerator.Plugins
{
    ///<summary>
    /// Interface provided by DNNAzure, to extend the functionality. It extends the main events managed by the Web Role
    /// </summary>
    public interface IPlugin
    {
        ///<summary>
        /// Called at the end of the OnStart() method of the web role
        /// </summary>
        void OnStart();

        ///<summary>
        /// Called at the begining of the OnStop() method of the web role
        /// </summary>
        void OnStop();

        ///<summary>
        /// Called at the begining of the RoleEnvironmentOnChanged() method of the web role
        /// </summary> 
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoleEnvironmentChangedEventArgs" /> instance containing the event data.</param>
        void RoleEnvironmentOnChanged(object sender, RoleEnvironmentChangedEventArgs e);

        ///<summary>
        /// Called at the begining of the RoleEnvironmentOnChanging() method of the web role
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoleEnvironmentChangingEventArgs" /> instance containing the event data.</param>
        void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e);

        ///<summary>
        /// Called at the begining of the RoleEnvironmentOnStatusCheck() method of the web role
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="roleInstanceStatusCheckEventArgs">The <see cref="RoleInstanceStatusCheckEventArgs"/> instance containing the event data.</param>
        void RoleEnvironmentOnStatusCheck(object sender, RoleInstanceStatusCheckEventArgs roleInstanceStatusCheckEventArgs);

        ///<summary>
        /// Called when the DNN site is up and running
        /// </summary>
        void OnSiteReady();
    }
}
