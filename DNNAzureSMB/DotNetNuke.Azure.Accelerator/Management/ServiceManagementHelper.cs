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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.ServiceModel.Description;
using System.Net;
using System.ServiceModel.Dispatcher;
using System.Xml;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace DotNetNuke.Azure.Accelerator.Management
{
    public static class ServiceManagementHelper
    {

        #region Windows Azure Service Management
        public static IServiceManagement CreateServiceManagementChannel(X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>();
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(Binding binding, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(binding);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(ServiceEndpoint endpoint, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(endpoint);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(string endpointConfigurationName, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(endpointConfigurationName);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }


        public static IServiceManagement CreateServiceManagementChannel(Type channelType, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(channelType);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(Uri remoteUri, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(remoteUri);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(Binding binding, Uri remoteUri, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(binding, remoteUri);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        public static IServiceManagement CreateServiceManagementChannel(string endpointConfigurationName, Uri remoteUri, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IServiceManagement>(endpointConfigurationName, remoteUri);
            factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        #endregion 

        #region SQL Azure Database Management

        public static IDatabaseManagement CreateDatabaseManagementChannel(string endpointConfigurationName, X509Certificate2 cert)
        {
            var factory = new WebChannelFactory<IDatabaseManagement>(endpointConfigurationName);
            factory.Endpoint.Behaviors.Add(new DatabaseClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;

            var channel = factory.CreateChannel();
            return channel;
        }

        #endregion

        #region Exception handling

        public static bool TryGetExceptionDetails(CommunicationException exception, out ServiceManagementError errorDetails)
        {
            HttpStatusCode httpStatusCode;
            string operationId;
            return TryGetExceptionDetails(exception, out errorDetails, out httpStatusCode, out operationId);
        }

        public static bool TryGetExceptionDetails(CommunicationException exception, out DatabaseManagementError errorDetails)
        {
            HttpStatusCode httpStatusCode;
            string operationId;
            return TryGetExceptionDetails(exception, out errorDetails, out httpStatusCode, out operationId);
        }

        public static bool TryGetExceptionDetails(CommunicationException exception, out ServiceManagementError errorDetails, out HttpStatusCode httpStatusCode, out string operationId)
        {
            errorDetails = null;
            
            if (TryGetExceptionDetails(exception, out httpStatusCode, out operationId))
            {
                using (var s = ((WebException) exception.InnerException).Response.GetResponseStream())
                {
                    if (s == null || s.Length == 0)
                    {
                        return false;
                    }

                    try
                    {
                        using (var reader = XmlDictionaryReader.CreateTextReader(s, new XmlDictionaryReaderQuotas()))
                        {
                            var ser = new DataContractSerializer(typeof (ServiceManagementError));
                            errorDetails = (ServiceManagementError) ser.ReadObject(reader, true);
                        }
                    }
                    catch (SerializationException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool TryGetExceptionDetails(CommunicationException exception, out DatabaseManagementError errorDetails, out HttpStatusCode httpStatusCode, out string operationId)
        {
            errorDetails = null;

            if (TryGetExceptionDetails(exception, out httpStatusCode, out operationId))
            {
                using (var s = ((WebException)exception.InnerException).Response.GetResponseStream())
                {
                    if (s == null || s.Length == 0)
                    {
                        return false;
                    }

                    try
                    {
                        using (var reader = XmlDictionaryReader.CreateTextReader(s, new XmlDictionaryReaderQuotas()))
                        {
                            var ser = new DataContractSerializer(typeof(DatabaseManagementError));
                            errorDetails = (DatabaseManagementError)ser.ReadObject(reader, true);
                        }
                    }
                    catch (SerializationException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool TryGetExceptionDetails(CommunicationException exception, out HttpStatusCode httpStatusCode, out string operationId)
        {
            httpStatusCode = 0;
            operationId = null;

            if (exception == null)
            {
                return false;
            }

            if (exception.Message == "Internal Server Error")
            {
                httpStatusCode = HttpStatusCode.InternalServerError;
                return true;
            }

            var wex = exception.InnerException as WebException;

            if (wex == null)
                return false;

            var response = wex.Response as HttpWebResponse;
            if (response == null)
                return false;

            httpStatusCode = response.StatusCode;
            if (httpStatusCode == HttpStatusCode.Forbidden)
                return true;

            if (response.Headers != null)
                operationId = response.Headers[Constants.OperationTrackingIdHeader];
            return true;
        }

        #endregion



        public static string EncodeToBase64String(string original)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        }

        public static string DecodeFromBase64String(string original)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(original));
        }
    }

    public class ClientOutputMessageInspector : IClientMessageInspector, IEndpointBehavior
    {
        #region IClientMessageInspector Members

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
            if (property.Headers[Constants.VersionHeaderName] == null)
                property.Headers.Add(Constants.VersionHeaderName, Constants.VersionHeaderContent20110611);
            return null;
        }

        #endregion

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }

        #endregion

    }

    public class DatabaseClientOutputMessageInspector : IClientMessageInspector, IEndpointBehavior
    {
        #region IClientMessageInspector Members

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
            if (property.Headers[Constants.VersionHeaderName] == null)
            {
                property.Headers.Add(Constants.VersionHeaderName, Constants.VersionHeaderContentSQLAzure);
            }
            return null;
        }

        #endregion

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }

        #endregion

    }
}
