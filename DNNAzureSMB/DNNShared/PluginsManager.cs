using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using DotNetNuke.Azure.Accelerator.Plugins;

namespace DNNShared
{
    ///<summary>
    /// This class is responsible for the plugins management, i.e. it loads all the assemblies that implements the <see cref="PluginInterface.IPlugin"/> interface, and it calls the corresponding 
    /// methods that are implemented by that interface
    /// </summary>
    public class PluginsManager : IPlugin
    {
        #region Constants

        private const string PluginsPath = @".\plugins";

        #endregion

        #region Properties

        ///<summary>
        /// This property keeps the list of assemblies that implement the <see cref="PluginInterface.IPlugin"/> interface
        /// </summary>
        private ICollection<IPlugin> _plugins;
        private ICollection<IPlugin> Plugins {
            get { return _plugins ?? (_plugins = new List<IPlugin>()); }
            set { _plugins = value; }
        }

        #endregion

        #region Implementation of IPlugin

        public void OnStart()
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.OnStart());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public void OnStop()
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.OnStop());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public void RoleEnvironmentOnStatusCheck(object sender, RoleInstanceStatusCheckEventArgs roleInstanceStatusCheckEventArgs)
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.RoleEnvironmentOnStatusCheck(sender, roleInstanceStatusCheckEventArgs));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.RoleEnvironmentOnChanging(sender, e));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public void RoleEnvironmentOnChanged(object sender, RoleEnvironmentChangedEventArgs e)
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.RoleEnvironmentOnChanged(sender, e));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        public void OnSiteReady()
        {
            try
            {
                Parallel.ForEach(Plugins, p => p.OnSiteReady());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while processing the '{0}' method in the plugins: {1}", MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        #endregion

        #region Constructors

        public PluginsManager(string pluginsUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(pluginsUrl))
                {
                    var pluginsZipPath = Path.GetTempFileName();
                    DNNShared.Utils.DownloadFile(pluginsZipPath, pluginsUrl);
                    DNNShared.Utils.UnzipFile(pluginsZipPath, PluginsPath, true);
                    // We no longer need the downloaded ZIP file
                    File.Delete(pluginsZipPath);

                    Plugins = LoadPlugins(PluginsPath);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error while loading the plugins: " + ex);
            }
        }

        #endregion

        #region Private functions

        ///<summary>
        /// Loads all the assemblies that implements IPlugin and that are located under the <param name="path">path</param> folder
        /// </summary>
        /// <param name="path">Plugins folder</param>
        private static ICollection<IPlugin> LoadPlugins(string path)
        {
            var dllFileNames = new List<string>();
            if (Directory.Exists(path))
            {
                dllFileNames.AddRange(Directory.GetFiles(path, "*.dll"));
            }

            ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Count);
            foreach (var dllFile in dllFileNames)
            {
                var an = AssemblyName.GetAssemblyName(dllFile);
                var assembly = Assembly.Load(an);
                assemblies.Add(assembly);
            }

            var pluginType = typeof(IPlugin);
            ICollection<Type> pluginTypes = new List<Type>();
            foreach (var assembly in assemblies)
            {
                if (assembly != null)
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (!type.IsInterface && !type.IsAbstract)
                        {
                            if (type.GetInterface(pluginType.FullName) != null)
                            {
                                pluginTypes.Add(type);
                            }
                        }
                    }
                }
            }

            ICollection<IPlugin> plugins = new List<IPlugin>(pluginTypes.Count);
            foreach (Type type in pluginTypes)
            {
                var plugin = (IPlugin)Activator.CreateInstance(type);
                plugins.Add(plugin);
            }
            return plugins;
        }

        #endregion
    }
}
