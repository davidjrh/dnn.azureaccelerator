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

namespace DotNetNuke.Azure.Accelerator.Management
{
    public static class Constants
    {
        public const string WindowsAzureServiceManagementNS = "http://schemas.microsoft.com/windowsazure";
        public const string SQLAzureServiceManagementNS = "http://schemas.microsoft.com/sqlazure/2010/12/";
        public const string OperationTrackingIdHeader = "x-ms-request-id";
        public const string VersionHeaderName = "x-ms-version";
        public const string VersionHeaderContent = "2009-10-01";
        public const string VersionHeaderContent20100401 = "2010-04-01";
        public const string VersionHeaderContent20101028 = "2010-10-28";
        public const string VersionHeaderContent20110611 = "2011-06-01";
        public const string VersionHeaderContentSQLAzure = "1.0";
        public const string PrincipalHeader = "x-ms-principal-id";
    }

    public static class DeploymentStatus
    {
        public const string Running = "Running";
        public const string Suspended = "Suspended";
        public const string RunningTransitioning = "RunningTransitioning";
        public const string SuspendedTransitioning = "SuspendedTransitioning";
        public const string Starting = "Starting";
        public const string Suspending = "Suspending";
        public const string Deploying = "Deploying";
        public const string Deleting = "Deleting";
    }

    public static class StorageServiceStatus
    {
        public const string Creating = "Creating";
        public const string ResolvingDns = "ResolvingDns";
        public const string Created = "Created";
        public const string Deleting = "Deleting";
    }

    public static class RoleInstanceStatus
    {
        public const string Initializing = "Initializing";
        public const string Ready = "Ready";
        public const string Busy = "Busy";
        public const string Stopping = "Stopping";
        public const string Stopped = "Stopped";
        public const string Unresponsive = "Unresponsive";
    }

    public static class OperationState
    {
        public const string InProgress = "InProgress";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
    }

    public static class KeyType
    {
        public const string Primary = "Primary";
        public const string Secondary = "Secondary";
    }

    public static class DeploymentSlotType
    {
        public const string Staging = "Staging";
        public const string Production = "Production";
    }

    public static class UpgradeType
    {
        public const string Auto = "Auto";
        public const string Manual = "Manual";
    }

    public static class CurrentUpgradeDomainState
    {
        public const string Before = "Before";
        public const string During = "During";
    }
}
