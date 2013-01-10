﻿//---------------------------------------------------------------------------------
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
    /// A list of hosted services
    /// </summary>
    [CollectionDataContract(Name = "HostedServices", ItemName = "HostedService", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class HostedServiceList : List<HostedService>
    {
        public HostedServiceList()
        {
        }

        public HostedServiceList(IEnumerable<HostedService> hostedServices)
            : base(hostedServices)
        {
        }
    }
    
    /// <summary>
    /// A hosted service
    /// </summary>
    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class HostedService : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public Uri Url { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string ServiceName { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public HostedServiceProperties HostedServiceProperties { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false)]
        public DeploymentList Deployments { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// A list of deployments contained in the hosted service
    /// </summary>
    [CollectionDataContract(Name = "Deployments", ItemName = "Deployment", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class DeploymentList : List<Deployment>
    {
        public DeploymentList()
        {
        }

        public DeploymentList(IEnumerable<Deployment> deployments)
            : base(deployments)
        {
        }
    }

    /// <summary>
    /// A hosted service
    /// </summary>
    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class HostedServiceProperties : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Description { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string AffinityGroup { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Location { get; set; }

        [DataMember(Order = 4)]
        public string Label { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// List of locations
    /// </summary>
    [CollectionDataContract(Name = "Locations", ItemName = "Location", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class LocationList : List<Location>
    {
        public LocationList()
        {
        }

        public LocationList(IEnumerable<Location> locations)
            : base(locations)
        {
        }
    }

    /// <summary>
    /// A location constraint
    /// </summary>
    [DataContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class Location : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// CreateHostedService contract
    /// </summary>
    [DataContract(Name = "CreateHostedService", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class CreateHostedServiceInput : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string ServiceName { get; set; }

        [DataMember(Order = 2)]
        public string Label { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false)]
        public string Location { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public string AffinityGroup { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }


    /// <summary>
    /// UpdateHostedService contract
    /// </summary>
    [DataContract(Name = "UpdateHostedService", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class UpdateHostedServiceInput : IExtensibleDataObject
    {
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string Label { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string Description { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    /// <summary>
    /// The hosted services related part of the Service Management API
    /// </summary>
    public partial interface IServiceManagement
    {
        
        #region CreateHostedService
        /// <summary>
        /// Creates a hosted service
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices")]
        IAsyncResult BeginCreateHostedService(string subscriptionId, CreateHostedServiceInput input, AsyncCallback callback, object state);
        void EndCreateHostedService(IAsyncResult asyncResult);
        #endregion

        #region UpdateHostedService
        /// <summary>
        /// Updates a hosted service 
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "PUT", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}")]
        IAsyncResult BeginUpdateHostedService(string subscriptionId, string serviceName,UpdateHostedServiceInput input, AsyncCallback callback, object state);
        void EndUpdateHostedService(IAsyncResult asyncResult);
        #endregion
        
        #region DeleteHostedService
        /// <summary>
        /// Deletes a hosted service
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}")]
        IAsyncResult BeginDeleteHostedService(string subscriptionId, string serviceName, AsyncCallback callback, object state);
        void EndDeleteHostedService(IAsyncResult asyncResult);
        #endregion

        /// <summary>
        /// Lists the hosted services associated with a given subscription.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices")]
        IAsyncResult BeginListHostedServices(string subscriptionId, AsyncCallback callback, object state);
        HostedServiceList EndListHostedServices(IAsyncResult asyncResult);

        /// <summary>
        /// Gets the properties for the specified hosted service.
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}")]
        IAsyncResult BeginGetHostedService(string subscriptionId, string serviceName, AsyncCallback callback, object state);
        HostedService EndGetHostedService(IAsyncResult asyncResult);

        /// <summary>
        /// Gets the detailed properties for the specified hosted service. 
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}?embed-detail={embedDetail}")]
        IAsyncResult BeginGetHostedServiceWithDetails(string subscriptionId, string serviceName, bool embedDetail, AsyncCallback callback, object state);
        HostedService EndGetHostedServiceWithDetails(IAsyncResult asyncResult);

        /// <summary>
        /// List the locations supported by a given subscription. 
        /// </summary>
        [OperationContract(AsyncPattern = true)]
        [WebGet(UriTemplate = @"{subscriptionId}/locations")]
        IAsyncResult BeginListLocations(string subscriptionId, AsyncCallback callback, object state);
        LocationList EndListLocations(IAsyncResult asyncResult);
    }

    public static partial class ServiceManagementExtensionMethods
    {
        
        public static void CreateHostedService(this IServiceManagement proxy, string subscriptionId, CreateHostedServiceInput input)
        {
            try
            {
                proxy.EndCreateHostedService(proxy.BeginCreateHostedService(subscriptionId, input, null, null));
            }
            catch (CommunicationException cex)
            {
                ServiceManagementError error;
                if (ServiceManagementHelper.TryGetExceptionDetails(cex, out error))
                    throw new WindowsAzureException(error.Message) { Code = error.Code };
                throw;
            }            
        }
        
        public static void UpdateHostedService(this IServiceManagement proxy, string subscriptionId, string serviceName, UpdateHostedServiceInput input)
        {
            proxy.EndUpdateHostedService(proxy.BeginUpdateHostedService(subscriptionId, serviceName, input, null, null));
        }
        
        public static void DeleteHostedService(this IServiceManagement proxy, string subscriptionId, string serviceName)
        {
            proxy.EndDeleteHostedService(proxy.BeginDeleteHostedService(subscriptionId, serviceName, null, null));
        }

        public static HostedServiceList ListHostedServices(this IServiceManagement proxy, string subscriptionId)
        {
            return proxy.EndListHostedServices(proxy.BeginListHostedServices(subscriptionId, null, null));
        }

        public static HostedService GetHostedService(this IServiceManagement proxy, string subscriptionId, string serviceName)
        {
            return proxy.EndGetHostedService(proxy.BeginGetHostedService(subscriptionId, serviceName, null, null));
        }

        public static HostedService GetHostedServiceWithDetails(this IServiceManagement proxy, string subscriptionId, string serviceName, bool embedDetail)
        {
            return proxy.EndGetHostedServiceWithDetails(proxy.BeginGetHostedServiceWithDetails(subscriptionId, serviceName, embedDetail, null, null));
        }

        public static LocationList ListLocations(this IServiceManagement proxy, string subscriptionId)
        {
            return proxy.EndListLocations(proxy.BeginListLocations(subscriptionId, null, null));
        }
    }
}
