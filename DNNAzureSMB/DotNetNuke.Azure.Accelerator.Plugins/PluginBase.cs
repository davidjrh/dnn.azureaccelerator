using Microsoft.WindowsAzure.ServiceRuntime;

namespace DotNetNuke.Azure.Accelerator.Plugins
{
    public class PluginBase : IPlugin
    {
        public virtual void OnStart()
        { }

        public virtual void OnStop()
        { }

        public virtual void RoleEnvironmentOnChanged(object sender, RoleEnvironmentChangedEventArgs e)
        { }

        public virtual void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
        { }

        public virtual void RoleEnvironmentOnStatusCheck(object sender, RoleInstanceStatusCheckEventArgs roleInstanceStatusCheckEventArgs)
        { }

        public virtual void OnSiteReady()
        { }
    }
}
