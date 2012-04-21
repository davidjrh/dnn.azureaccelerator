using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Xml.Serialization;

namespace DotNetNuke.Azure.Accelerator.Management
{
    /// <summary>
    /// A list of hosted services
    /// </summary>
    [CollectionDataContract(Name = "Servers", ItemName = "Server", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class DatabaseServerList : List<DatabaseServer>
    {
        public DatabaseServerList()
        {
        }

        public DatabaseServerList(IEnumerable<DatabaseServer> databaseServers)
            : base(databaseServers)
        {
        }
    }

    /// <summary>
    /// A database server
    /// </summary>
    [DataContract(Namespace = Constants.SQLAzureServiceManagementNS)]
    public class DatabaseServer : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public string AdministratorLogin { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Location { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// CreateDatabaseServer input contract
    /// </summary>
    [XmlRoot(ElementName = "Server", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class CreateDatabaseServerInput 
    {
        [XmlElement(Order = 1)]
        public string AdministratorLogin { get; set; }

        [XmlElement(Order = 2)]
        public string AdministratorLoginPassword { get; set; }

        [XmlElement(Order = 3)]
        public string Location { get; set; }
    }

    [XmlRoot(ElementName = "ServerName", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class ServerName
    {
        [XmlText]
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("ServerName[Name={0}]", this.Name);
        }
    }

    /// <summary>
    /// SetServerPassword input contract
    /// </summary>
    [XmlRoot(ElementName = "AdministratorLoginPassword", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class SetServerPasswordInput
    {
        [XmlText]
        public string NewPassword { get; set; }
    }

    /// <summary>
    /// A list of firewall rules
    /// </summary>
    [CollectionDataContract(Name = "FirewallRules", ItemName = "FirewallRule", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class FirewallRulesList : List<FirewallRule>
    {
        public FirewallRulesList()
        {
        }

        public FirewallRulesList(IEnumerable<FirewallRule> firewallRules)
            : base(firewallRules)
        {
        }
    }

    /// <summary>
    /// A SQL Azure firewall rule
    /// </summary>
    [DataContract(Namespace = Constants.SQLAzureServiceManagementNS)]
    public class FirewallRule : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public string StartIpAddress { get; set; }

        [DataMember(Order = 3)]
        public string EndIpAddress { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// SetServerPassword input contract
    /// </summary>
    [XmlRoot(ElementName = "FirewallRule", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class CreateOrUpdateFirewallRuleInput
    {
        [XmlElement(Order = 1)]
        public string StartIpAddress { get; set; }

        [XmlElement(Order = 2)]
        public string EndIpAddress { get; set; }
    }
   

    /// <summary>
    /// The sql Azure specific interface of the resource model service.
    /// </summary>
    public partial interface IDatabaseManagement
    {
        /// <summary>
        /// Lists the database servers associated with a given subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/servers")]
        IAsyncResult BeginListDatabaseServers(string subscriptionId, AsyncCallback callback, object state);
        DatabaseServerList EndListDatabaseServers(IAsyncResult asyncResult);

        /// <summary>
        /// Creates a database server associated with a given subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/servers", BodyStyle = WebMessageBodyStyle.Bare)]
        [XmlSerializerFormat]
        IAsyncResult BeginCreateServer(string subscriptionId, CreateDatabaseServerInput input, AsyncCallback callback, object state);
        ServerName EndCreateServer(IAsyncResult asyncResult);

        /// <summary>
        /// Drops a database server associated with a given subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/servers/{serverName}")]
        IAsyncResult BeginDropServer(string subscriptionId, string serverName, AsyncCallback callback, object state);
        void EndDropServer(IAsyncResult asyncResult);

        /// <summary>
        /// Sets the administrative password of a SQL Azure server for a subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/servers/{serverName}?op=ResetPassword", BodyStyle = WebMessageBodyStyle.Bare)]
        [XmlSerializerFormat]
        IAsyncResult BeginSetServerPassword(string subscriptionId, string serverName, SetServerPasswordInput input, AsyncCallback callback, object state);
        void EndSetServerPassword(IAsyncResult asyncResult);

        /// <summary>
        /// Retrieves a list of all the firewall rules for a SQL Azure server that belongs to a subscription
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/servers/{serverName}/firewallrules")]
        IAsyncResult BeginListFirewallRules(string subscriptionId, string serverName, AsyncCallback callback, object state);
        FirewallRulesList EndListFirewallRules(IAsyncResult asyncResult);

        /// <summary>
        /// Updates an existing firewall rule or adds a new firewall rule for a SQL Azure server that belongs to a subscription
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "PUT", UriTemplate = @"{subscriptionId}/servers/{serverName}/firewallrules/{ruleName}")]
        [XmlSerializerFormat]
        IAsyncResult BeginCreateOrUpdateFirewallRule(string subscriptionId, string serverName, string ruleName, CreateOrUpdateFirewallRuleInput input, AsyncCallback callback, object state);
        void EndCreateOrUpdateFirewallRule(IAsyncResult asyncResult);

        /// <summary>
        /// Adds a new firewall rule or updates an existing firewall rule for a SQL Azure server with requester’s IP address
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/servers/{serverName}/firewallrules/{ruleName}?op=AutoDetectClientIP")]
        IAsyncResult BeginCreateOrUpdateFirewallRuleAuto(string subscriptionId, string serverName, string ruleName, AsyncCallback callback, object state);
        void EndCreateOrUpdateFirewallRuleAuto(IAsyncResult asyncResult);

        /// <summary>
        /// Deletes a firewall rule from a SQL Azure server that belongs to a subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/servers/{serverName}/firewallrules/{ruleName}")]
        IAsyncResult BeginDeleteFirewallRule(string subscriptionId, string serverName, string ruleName, AsyncCallback callback, object state);
        void EndDeleteFirewallRule(IAsyncResult asyncResult);
    }


    public static partial class ServiceManagementExtensionMethods
    {
        public static DatabaseServerList ListDatabaseServers(this IDatabaseManagement proxy, string subscriptionId)
        {
            try
            {
                return proxy.EndListDatabaseServers(proxy.BeginListDatabaseServers(subscriptionId, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static DatabaseServer CreateServer(this IDatabaseManagement proxy, string subscriptionId, CreateDatabaseServerInput input)
        {
            try
            {
                var serverName = proxy.EndCreateServer(proxy.BeginCreateServer(subscriptionId, input, null, null));
                return new DatabaseServer
                {
                    AdministratorLogin = input.AdministratorLogin,
                    Location = input.Location,
                    Name = serverName.Name
                };
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static void DropServer(this IDatabaseManagement proxy, string subscriptionId, string serverName)
        {
            try
            {
                proxy.EndDropServer(proxy.BeginDropServer(subscriptionId, serverName, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static void SetServerPassword(this IDatabaseManagement proxy, string subscriptionId, string serverName, SetServerPasswordInput input)
        {
            try
            {
                proxy.EndSetServerPassword(proxy.BeginSetServerPassword(subscriptionId, serverName, input, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static FirewallRulesList ListFirewallRules(this IDatabaseManagement proxy, string subscriptionId, string serverName)
        {
            try
            {
                return proxy.EndListFirewallRules(proxy.BeginListFirewallRules(subscriptionId, serverName, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static void CreateOrUpdateFirewallRule(this IDatabaseManagement proxy, string subscriptionId, string serverName, string ruleName, CreateOrUpdateFirewallRuleInput input)
        {
            try
            {
                proxy.EndCreateOrUpdateFirewallRule(proxy.BeginCreateOrUpdateFirewallRule(subscriptionId, serverName, ruleName, input, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }            
        }

        public static void CreateOrUpdateFirewallRuleAuto(this IDatabaseManagement proxy, string subscriptionId, string serverName, string ruleName)
        {
            try
            {
                proxy.EndCreateOrUpdateFirewallRuleAuto(proxy.BeginCreateOrUpdateFirewallRuleAuto(subscriptionId, serverName, ruleName, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static void DeleteFirewallRule(this IDatabaseManagement proxy, string subscriptionId, string serverName, string ruleName)
        {
            try
            {
                proxy.EndDeleteFirewallRule(proxy.BeginDeleteFirewallRule(subscriptionId, serverName, ruleName, null, null));
            }
            catch (CommunicationException webex)
            {
                DatabaseManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(webex, out error))
                    throw new SQLAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

    }
}
