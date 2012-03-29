using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DNNAzure.Components
{
    public class WadLogEntity
           : Microsoft.WindowsAzure.StorageClient.TableServiceEntity
    {
        public string DeploymentId { get; set; }
        public string Role { get; set; }
        public string RoleInstance { get; set; }
        public int Level { get; set; }
        public string Message { get; set; }
        public int Pid { get; set; }
        public int Tid { get; set; }
        public int EventId { get; set; }
    }
}