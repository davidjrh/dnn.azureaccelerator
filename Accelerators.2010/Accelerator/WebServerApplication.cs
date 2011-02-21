using System;
using System.Xml.Linq;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Configuration definition for a hosted web core virtual application.
    /// </summary>
    public class WebServerApplication
    {
        /// <summary>
        /// Gets or sets the application path.
        /// </summary>
        /// <value>The application path.</value>
        public String ApplicationPath
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the virtual directory path.
        /// </summary>
        /// <value>The virtual directory path.</value>
        public String VirtualDirectoryPath
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the physical path.
        /// </summary>
        /// <value>The physical path.</value>
        public String PhysicalPath
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the application pool name.
        /// </summary>
        /// <remarks>
        /// In an Azure hosted web core, there is only a single application pool, and all configured applications must share it.
        /// </remarks>
        public String ApplicationPool
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is site root.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is site root; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsSiteRoot
        {
            get; set;
        }

        /// <summary>
        /// Renders the web server application configuration as an XElement.
        /// </summary>
        /// <returns>Application configuration.</returns>
        public XElement Render()
        {
            return  
                new XElement("application",
                    new XAttribute("path", ApplicationPath ?? "/"),
                    //!String.IsNullOrEmpty(ApplicationPool) ? new XAttribute("applicationPool", ApplicationPool) : null,
                    new XElement("virtualDirectory",
                        new XAttribute("path", VirtualDirectoryPath ?? "/"),
                        new XAttribute("physicalPath", PhysicalPath ?? String.Empty)));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this web server application configuration instance.
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