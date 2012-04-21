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

using System.Runtime.Serialization;

namespace DotNetNuke.Azure.Accelerator.Management
{
    [DataContract(Name = "Error", Namespace = Constants.WindowsAzureServiceManagementNS)]
    public class ServiceManagementError : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Code { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public ConfigurationWarningsList ConfigurationWarnings { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [DataContract(Name = "Error", Namespace = Constants.SQLAzureServiceManagementNS)]
    public class DatabaseManagementError : IExtensibleDataObject
    {
        [DataMember(Order = 1)]
        public string Code { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public ConfigurationWarningsList ConfigurationWarnings { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    public class SQLAzureException : System.Exception
    {
        public string Code { get; set; }

        public SQLAzureException(string message) : base(message)
        {
        }
    }

    public class WindowsAzureException : System.Exception
    {
        public string Code { get; set; }

        public WindowsAzureException(string message)
            : base(message)
        {
        }
    }


    public static class ErrorCode
    {
        public const string MissingOrIncorrectVersionHeader = "MissingOrIncorrectVersionHeader";
        public const string InvalidRequest = "InvalidRequest";
        public const string InvalidXmlRequest = "InvalidXmlRequest";
        public const string InvalidContentType = "InvalidContentType";
        public const string MissingOrInvalidRequiredQueryParameter = "MissingOrInvalidRequiredQueryParameter";
        public const string InvalidHttpVerb = "InvalidHttpVerb";
        public const string InternalError = "InternalError";
        public const string BadRequest = "BadRequest";
        public const string AuthenticationFailed = "AuthenticationFailed";
        public const string ResourceNotFound = "ResourceNotFound";
        public const string SubscriptionDisabled = "SubscriptionDisabled";
        public const string ServerBusy = "ServerBusy";
        public const string TooManyRequests = "TooManyRequests";
        public const string ConflictError = "ConflictError";
        public const string ConfiguraitonError = "ConfigurationError";
    }
}
