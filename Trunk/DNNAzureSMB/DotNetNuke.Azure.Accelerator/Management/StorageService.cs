//---------------------------------------------------------------------------------
// Microsoft (R) Windows Azure SDK
// Software Development Kit
// 
// Copyright (c) Microsoft Corporation. All rights reserved.  
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
//---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace DotNetNuke.Azure.Accelerator.Management
{
    /// <summary>
    /// List of storage services
    /// </summary>
    [CollectionDataContract(Name = "StorageServices", ItemName = "StorageService", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class StorageServiceList : List<StorageService>
    {
        public StorageServiceList()
        {
        }

        public StorageServiceList(IEnumerable<StorageService> storageServices)
            : base(storageServices)
        {
        }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class StorageService : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public Uri Url { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string ServiceName { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public StorageServiceProperties StorageServiceProperties { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false)]
        public StorageServiceKeys StorageServiceKeys { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class CreateStorageServiceInput : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string ServiceName { get; set; }

        [DataMember(Order = 2)]
        public string Description { get; set; }

        [DataMember(Order = 3)]
        public string Label { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false)]
        public string Location { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public string AffinityGroup { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class UpdateStorageServiceInput : IExtensibleDataObject
    {

        [DataMember(Order = 1)]
        public string Description { get; set; }

        [DataMember(Order = 2)]
        public string Label { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class StorageServiceProperties : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Description { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string AffinityGroup { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Location { get; set; }

        [DataMember(Order = 4)]
        public string Label { get; set; }

        [DataMember(Order = 5)]
        public string Status { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class StorageServiceKeys : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Primary { get; set; }

        [DataMember(Order = 2)]
        public string Secondary { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class RegenerateKeys: IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string KeyType { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// The storage service-related part of the API
    /// </summary>
    public partial interface IServiceManagement
    {
        /// <summary>
        /// Lists the storage services associated with a given subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/storageservices")]
        IAsyncResult BeginListStorageServices(string subscriptionId, AsyncCallback callback, object state);
        StorageServiceList EndListStorageServices(IAsyncResult asyncResult);

        /// <summary>
        /// Gets a storage service.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}")]
        IAsyncResult BeginGetStorageService(string subscriptionId, string serviceName, AsyncCallback callback, object state);
        StorageService EndGetStorageService(IAsyncResult asyncResult);

        /// <summary>
        /// Gets the key of a storage service.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}/keys")]
        IAsyncResult BeginGetStorageKeys(string subscriptionId, string serviceName, AsyncCallback callback, object state);
        StorageService EndGetStorageKeys(IAsyncResult asyncResult);

        /// <summary>
        /// Regenerates keys associated with a storage service.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}/keys?action=regenerate")]
        IAsyncResult BeginRegenerateStorageServiceKeys(string subscriptionId, string serviceName, RegenerateKeys regenerateKeys, AsyncCallback callback, object state);
        StorageService EndRegenerateStorageServiceKeys(IAsyncResult asyncResult);

        /// <summary>
        /// Creates a new storage account in Windows Azure
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/storageservices")]
        IAsyncResult BeginCreateStorageAccount(string subscriptionId, CreateStorageServiceInput input, AsyncCallback callback, object state);
        void EndCreateStorageAccount(IAsyncResult asyncResult);

        /// <summary>
        /// Updates the label and/or the description for a storage account in Windows Azure
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "PUT", UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}")]
        IAsyncResult BeginUpdateStorageAccount(string subscriptionId, string serviceName, UpdateStorageServiceInput input, AsyncCallback callback, object state);
        void EndUpdateStorageAccount(IAsyncResult asyncResult);

        /// <summary>
        /// Deletes the specified storage account from Windows Azure
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}")]
        IAsyncResult BeginDeleteStorageAccount(string subscriptionId, string serviceName, AsyncCallback callback, object state);
        void EndDeleteStorageAccount(IAsyncResult asyncResult);  

   }

    public static partial class ServiceManagementExtensionMethods
    {
        public static StorageServiceList ListStorageServices(this IServiceManagement proxy, string subscriptionId)
        {
            try
            {
                return proxy.EndListStorageServices(proxy.BeginListStorageServices(subscriptionId, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static StorageService GetStorageService(this IServiceManagement proxy, string subscriptionId, string name)
        {
            try
            {
                return proxy.EndGetStorageService(proxy.BeginGetStorageService(subscriptionId, name, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static StorageService GetStorageKeys(this IServiceManagement proxy, string subscriptionId, string name)
        {
            try
            {
                return proxy.EndGetStorageKeys(proxy.BeginGetStorageKeys(subscriptionId, name, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static StorageService RegenerateStorageServiceKeys(this IServiceManagement proxy, string subscriptionId, string name, RegenerateKeys regenerateKeys)
        {
            try 
            {
                return proxy.EndRegenerateStorageServiceKeys(proxy.BeginRegenerateStorageServiceKeys(subscriptionId, name, regenerateKeys, null, null));
            }            
            catch(CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) {Code = error.Code};
                throw;                
            }
        }

        public static void CreateStorageService(this IServiceManagement proxy, string subscriptionId, CreateStorageServiceInput input)
        {
            try
            {
                proxy.EndCreateStorageAccount(proxy.BeginCreateStorageAccount(subscriptionId, input, null, null));
            }            
            catch(CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) {Code = error.Code};
                throw;                
            }
        }

        public static void UpdateStorageService(this IServiceManagement proxy, string subscriptionId, string serviceName, UpdateStorageServiceInput input)
        {
            try
            {
                proxy.EndUpdateStorageAccount(proxy.BeginUpdateStorageAccount(subscriptionId, serviceName, input, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }

        public static void DeleteStorageService(this IServiceManagement proxy, string subscriptionId, string serviceName)
        {
            try
            {
                proxy.EndDeleteStorageAccount(proxy.BeginDeleteStorageAccount(subscriptionId, serviceName, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }
        }
    }
}
