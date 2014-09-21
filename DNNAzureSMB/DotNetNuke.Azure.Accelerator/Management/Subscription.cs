using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DotNetNuke.Azure.Accelerator.Management
{
    public class Subscription
    {
        public string SubscriptionId { get; set; }
        public string Name { get; set; }
        public bool Default { get; set; }
        public X509Certificate2 Certificate { get; set; }
    }
}
