using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace DotNetNuke.Azure.Accelerator.Management
{
    public class PublishSettings
    {
        private List<Subscription> _subscriptions;
        public List<Subscription> Subscriptions
        {
            get { return _subscriptions ?? new List<Subscription>(); }
            set { _subscriptions = value; }
        }
        public X509Certificate2 Certificate { get; set; }
        private XDocument _document;

        public PublishSettings(string xml)
        {
            _document = XDocument.Parse(xml);
            Parse();
        }
        public PublishSettings(XDocument document)
        {
            _document = document;
            Parse();
        }

        private void Parse()
        {
            Subscriptions = new List<Subscription>();
            Certificate = null;
            if (_document == null) return;
            var subscriptions = from subs in _document.Descendants("Subscription")
                                select subs;
            foreach (var subscription in subscriptions)
            {
                var isDefault = subscription.Attribute("Default") != null && bool.Parse(subscription.Attribute("Default").Value);
                Subscriptions.Add(new Subscription
                {
                    SubscriptionId = subscription.Attribute("Id").Value,
                    Name = subscription.Attribute("Name").Value,
                    Default = isDefault
                });
            }
            var publishProfile = _document.Descendants("PublishProfile").Single();
            if (publishProfile != null)
                Certificate = new X509Certificate2(Convert.FromBase64String(publishProfile.Attribute("ManagementCertificate").Value), "", X509KeyStorageFlags.Exportable);
        }

        public void Save(string filename)
        {
            var subscriptions = from subs in _document.Descendants("Subscription")
                                select subs;

            foreach (var subscription in subscriptions)
            {
                var sub = Subscriptions.FirstOrDefault(x => x.SubscriptionId == subscription.Attribute("Id").Value);
                subscription.SetAttributeValue(XName.Get("Default"), sub.Default.ToString());
            }
            _document.Save(filename);
        }
    }
}
