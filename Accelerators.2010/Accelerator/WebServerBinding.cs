using System;
using System.Xml.Linq;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    ///     Binding definition for a hosted web core virtual application.
    /// </summary>
    public class WebServerBinding
    {
        /// <summary>
        /// Protocol type being bound.
        /// </summary>
        public enum ProtocolType
        {
            //i| Tcp resulted in issues when binding HWC on Azure.  Use http(s)
            //i| for WebServer binding unless certain otherwise. |i|rdm|
            Tcp,
            Http,
            Https
        }

        /// <summary>
        /// Gets or sets the port being bound.
        /// </summary>
        public Int32 Port
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the IP address being bound.
        /// </summary>
        public String Address
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the host header being bound.
        /// </summary>
        public String HostHeader
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the protocol being bound.
        /// </summary>
        /// <value>The protocol.</value>
        public ProtocolType? Protocol
        {
            get; set;
        }

        /// <summary>
        /// Renders the web server binding configuration as an XElement.
        /// </summary>
        /// <returns>Binding configuration.</returns>
        public XElement Render()
        {
            return  
                new XElement("binding",
                    new XAttribute("protocol", (Protocol ?? ProtocolType.Http).ToString().ToLower()),
                    new XAttribute("bindingInformation", String.Format("{0}:{1}:{2}", Address ?? "*", Port, HostHeader ?? String.Empty)));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> representation of the web server binding xml for use in value comparissons (such as equal to).
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Render().ToString();
        }
    }
}