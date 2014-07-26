using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetNuke.Azure.Accelerator.Management
{
    public class Subscription
    {
        public string SubscriptionId { get; set; }
        public string Name { get; set; }
        public bool Default { get; set; }
    }
}
