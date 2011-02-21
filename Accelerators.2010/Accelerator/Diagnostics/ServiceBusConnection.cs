using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    /// <summary>
    /// Class provides credentials and location information for connecting to the AppFabic Service Bus. |i| rdm |
    /// </summary>
    public class ServiceBusConnection
    {
        private const String _IssuerSecretProtectionString = "[( HIDDEN: NOT AVAILABLE FOR QUERY )]";
        private String _issuerSecret;

        /// <summary>
        /// Gets the setting separator.
        /// </summary>
        /// <value>The setting separator.</value>
        protected Char[] SettingSeparator = new[] {';'};

        /// <summary>
        /// Gets the key value separator.
        /// </summary>
        /// <value>The key value separator.</value>
        protected Char[] KeyValueSeparator = new[] {'='};

/*
        /// <summary>
        /// Dictionary of settings and values as strings.
        /// </summary>
        private Dictionary<String, String> SettingsDictionary;
*/

        /// <summary>
        /// The scheme of the URI.
        /// </summary>
        public const String Scheme = "sb";

        /// <summary>
        /// Gets or service namespace name used by the application.
        /// </summary>
        public String ServiceNamespace { get; set; }

        /// <summary>
        /// Gets or sets the issuer secret key.
        /// </summary>
        public String IssuerSecret
        {
            get { return String.IsNullOrEmpty(_issuerSecret) ? String.Empty : _IssuerSecretProtectionString; }
            set { _issuerSecret = value; }
        }

        /// <summary>
        /// Gets or sets the issuer name.
        /// </summary>
        public String IssuerName { get; set; }

        /// <summary>
        /// The service path that follows the host name section of the URI.
        /// </summary>
        public String ServicePath { get; set; }

        //public ServiceEndpoint ServiceEndpoint { get; set; }

        //public ServiceHost ServiceHost { get; set; }

        //public ContractDescription ContractDescription { get; set; }

        //public TransportClientEndpointBehavior TransportClientEndpointBehavior { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusConnection"/> class.
        /// </summary>
        /// <param name="serviceNamespace">The service namespace.</param>
        /// <param name="servicePath">The service path.</param>
        /// <param name="issuerName">Name of the issuer.</param>
        /// <param name="issuerSecret">The issuer secret.</param>
        public ServiceBusConnection(String serviceNamespace, String servicePath, String issuerName, String issuerSecret)
        {
            ServiceNamespace = serviceNamespace ?? String.Empty; 
            ServicePath = servicePath ?? String.Empty;
            IssuerName = issuerName ?? String.Empty;
            _issuerSecret = issuerSecret ?? String.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusConnection"/> class.
        /// </summary>
        protected ServiceBusConnection(String settingString)
        {
            var settingsDictionary = SplitToDictionary(settingString);
            ServiceNamespace = settingsDictionary["ServiceNamespace"] ?? String.Empty;
            ServicePath = settingsDictionary["ServicePath"] ?? String.Empty;
            IssuerName = settingsDictionary["IssuerName"] ?? String.Empty;
            _issuerSecret = settingsDictionary["IssuerSecret"] ?? String.Empty;
        }
        
        /// <summary>
        /// Parses a configuration string and returns a Microsoft.WindowsAzure.Diagnostics.ServiceBusConnection object based on the string.
        /// </summary>
        /// <param name="settingValue">The configuration string.</param>
        /// <returns>ServiceBusConnection object.</returns>
        public static ServiceBusConnection Parse(String settingValue)
        {
            if (String.IsNullOrEmpty(settingValue))
                return null;
            return new ServiceBusConnection(settingValue);
        }

        /// <summary>
        /// Retrieves the value of the service bus settings from the configuration file.
        /// </summary>
        /// <param name="settingName">Name of the setting.</param>
        /// <returns>ServiceBusConnection object.</returns>
        public static ServiceBusConnection FromConfigurationSetting(String settingName)
        {
            if ( RoleEnvironment.IsAvailable )
                return Parse(RoleEnvironment.GetConfigurationSettingValue(settingName));
            return Parse(ConfigurationManager.AppSettings[settingName]);
        }

        /// <summary>
        /// Gets the service URI for the application, using the specified scheme, service namespace name, and service path.
        /// </summary>
        /// <returns>Returns a Uri for the service.</returns>
        public Uri GetServiceUri()
        {
            return ServiceBusEnvironment.CreateServiceUri(Scheme, ServiceNamespace, ServicePath ?? String.Empty);
        }

        /// <summary>
        /// Creates a new TransportClientEndpointBehavior based on the service bus credentials.
        /// </summary>
        /// <returns></returns>
        public TransportClientEndpointBehavior GetTransportClientEndpointBehavior()
        {
            var behavior = new TransportClientEndpointBehavior
                               {
                                   CredentialType = TransportClientCredentialType.SharedSecret
                               };
            behavior.Credentials.SharedSecret.IssuerName = IssuerName;
            behavior.Credentials.SharedSecret.IssuerSecret = _issuerSecret;
            return behavior;
        }

        /// <summary>
        /// Creates a new ServiceHost based on the type, contract, binding, and service bus credentials.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="binding">The binding.</param>
        /// <returns></returns>
        public ServiceHost CreateServiceHost(Type serviceType, Type contractType, Binding binding)
        {
            var host = new ServiceHost(serviceType, GetServiceUri());
            ContractDescription contractDescription = ContractDescription.GetContract(contractType, serviceType);
            var serviceEndPoint = new ServiceEndpoint(contractDescription)
            {
                Address = new EndpointAddress(GetServiceUri()),
                Binding = binding,
                Behaviors = {GetTransportClientEndpointBehavior()}
            };
            host.Description.Endpoints.Add(serviceEndPoint);
            return host;
        }

        /// <summary>
        /// Creates a new channel factory based on the binding, type and service bus credentials.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binding">The binding.</param>
        /// <returns></returns>
        public ChannelFactory<T> CreateChannelFactory<T>(Binding binding)
        {
            var channelFactory = new ChannelFactory<T>(binding, new EndpointAddress(GetServiceUri()));
            channelFactory.Endpoint.Behaviors.Add(GetTransportClientEndpointBehavior());
            return channelFactory;
        }

        /// <summary>
        /// Splits a string into pairs, then into keys and values, returning a dictionary.
        /// </summary>
        protected Dictionary<String, String> SplitToDictionary(String packedSettings)
        {
            return packedSettings.Split(SettingSeparator, StringSplitOptions.RemoveEmptyEntries).Select(pairs
                => pairs.Split(KeyValueSeparator, 2, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(kvp
                    => kvp[0], kvp => kvp[1]);
        }
    }

    ///// <summary>
    ///// An abstract base class representing the key/values in packed configuration and setting strings.
    ///// </summary>
    //public class PackedSettingsString
    //{
    //    protected static Char[] SettingSeparator = new[] {';'};
    //    protected static Char[] KeyValueSeparator = new[] {'='};
    //    protected static Dictionary<String, String> SettingsDictionary;

    //    protected PackedSettingsString(String settingString)
    //    {
    //        SettingsDictionary = SplitToDictionary<String, String>(settingString, SettingSeparator, KeyValueSeparator); 
    //    }

    //    /// <summary>
    //    /// Parses a configuration string and returns a Microsoft.WindowsAzure.Diagnostics.ServiceBusConnection object based on the string.
    //    /// </summary>
    //    /// <param name="settingString">The configuration string.</param>
    //    /// <returns>PackedSettingsString object.</returns>
    //    public static PackedSettingsString Parse<T>(String settingString) where T : PackedSettingsString, new()
    //    {
    //        if ( String.IsNullOrEmpty(settingString) )
    //            return null;


    //        var packedSettingsString = new T();
            
    //        SettingsDictionary = settingString.SplitToDictionary<String, String>(new[] { ';' }, new[] { '=' });
    //        return new ServiceBusConnection()
    //        {
    //            ServiceNamespace = settings["ServiceNamespace"] ?? String.Empty,
    //            ServicePath = settings["ServicePath"] ?? String.Empty,
    //            IssuerName = settings["IssuerName"] ?? String.Empty,
    //            IssuerSecret = settings["IssuerSecret"] ?? String.Empty
    //        };
    //    }

    //    /// <summary>
    //    /// Retrieves the value of the service bus settings from the configuration file.
    //    /// </summary>
    //    /// <param name="settingName">Name of the setting.</param>
    //    /// <returns>PackedSettingsString object.</returns>
    //    public static PackedSettingsString FromConfigurationSetting<T>(String settingName) where T : PackedSettingsString, new()
    //    {
    //        if ( RoleEnvironment.IsAvailable )
    //            return Parse<T>(RoleEnvironment.GetConfigurationSettingValue(settingName));
    //        return Parse<T>(ConfigurationManager.AppSettings[settingName]);
    //    }

    //    /// <summary>
    //    /// Splits a string into pairs, then into keys and values, returning a dictionary.
    //    /// </summary>
    //    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    //    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    //    /// <param name="settings">The string to splid.</param>
    //    /// <param name="settingSeparator">The paired items separator.</param>
    //    /// <param name="keyValueSeparator">The key value separator for each pair.</param>
    //    /// <returns>Dictionary</returns>
    //    protected static Dictionary<TKey, TValue> SplitToDictionary<TKey, TValue>(String settings, Char[] settingSeparator, Char[] keyValueSeparator)
    //        where TKey : IConvertible
    //        where TValue : IConvertible
    //    {
    //        return settings.Split(settingSeparator, StringSplitOptions.RemoveEmptyEntries).Select(pairs
    //            => pairs.Split(keyValueSeparator, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(kvp
    //                => kvp[0].As<TKey>(), kvp => kvp[1].As<TValue>());
    //    }
    //}
}