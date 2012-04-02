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
                Subscriptions.Add(new Subscription
                                      {
                                          SubscriptionId = subscription.Attribute("Id").Value,
                                          Name = subscription.Attribute("Name").Value                                         
                                      });
            var publishProfile = _document.Descendants("PublishProfile").Single();
            if (publishProfile != null)
                Certificate = new X509Certificate2(Convert.FromBase64String(publishProfile.Attribute("ManagementCertificate").Value));
        }

        public void Save(string filename)
        {
            _document.Save(filename);
        }
    }
}
