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
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

[assembly: CLSCompliant(true)]

namespace DotNetNuke.Azure.Accelerator.Management
{
    /// <summary>
    /// Provides the Windows Azure Service Management Api. 
    /// </summary>
    [ServiceContract(Namespace = Constants.WindowsAzureServiceManagementNS)]
    public partial interface IServiceManagement
    {
    }

    /// <summary>
    /// Provides the SQL Azure Service Management Api. 
    /// </summary>
    [ServiceContract(Namespace = Constants.SQLAzureServiceManagementNS)]
    public partial interface IDatabaseManagement
    {
    }

}
