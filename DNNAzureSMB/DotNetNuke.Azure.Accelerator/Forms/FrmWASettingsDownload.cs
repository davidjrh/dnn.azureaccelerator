using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using DotNetNuke.Azure.Accelerator.Management;

namespace DotNetNuke.Azure.Accelerator.Forms
{
    public partial class FrmWASettingsDownload : Form
    {
        private const string DownloadUrl = "https://windows.azure.com/download.publishsettings";
        private const string DownloadUrlSuccess = "https://windows.azure.com/landing?target=/download.publishsettings&wa=wsignin1.0";
        private const string UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

        public PublishSettings PublishingSettings { get; set; }

        public FrmWASettingsDownload()
        {
            InitializeComponent();
        }
        
        public void DownloadSettings()
        {
            PublishingSettings = null;
            web.Navigating += WebOnNavigating;
            web.Navigate(DownloadUrl);
        }

        private void WebOnNavigating(object sender, WebBrowserNavigatingEventArgs args)
        {
            if (args.Url.ToString().ToLowerInvariant() == DownloadUrlSuccess.ToLowerInvariant())
            {
                args.Cancel = true;
                // The user has been authenticated and the publish profile generated. 
                // Don't download using the webbrowser control, instead download using a WebRequest call
                if (DownloadSettingsFile())
                    Close();
            }
        }

        private bool DownloadSettingsFile()
        {
            if (web.Document == null) 
                return false;

            var eleNAP = web.Document.All["NAP"];
            var eleANON = web.Document.All["ANON"];
            var elet = web.Document.All["t"];
            if (eleNAP == null || eleANON == null || elet == null) 
                return false;

            // Prepare POST parameters
            var inputNAP = eleNAP.GetAttribute("value");
            var inputANON = eleANON.GetAttribute("value");
            var inputt = elet.GetAttribute("value");

            var encoding = new ASCIIEncoding();
            string postData = "userid=" + inputNAP;
            postData += "&username=" + inputANON;
            postData += "&t=" + inputt;
            byte[] data = encoding.GetBytes(postData);

            // Create a 'WebRequest' object with the specified url.                 
            var request = (HttpWebRequest) WebRequest.Create(DownloadUrlSuccess);
            request.Method = "POST";
            request.UserAgent = UserAgent;
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.CookieContainer = GetCookieContainer();

            // Send the data
            using (var requestStream = request.GetRequestStream())
                requestStream.Write(data, 0, data.Length);
            
            // Get the response            
            using (var response = request.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                if (responseStream == null)
                    return false;
                using (var reader = new StreamReader(responseStream))
                {
                    if (response.ResponseUri.ToString().ToLowerInvariant() == DownloadUrl.ToLowerInvariant())
                    {
                        PublishingSettings = new PublishSettings(reader.ReadToEnd());
                        return true;
                    }
                    web.Document.Write(reader.ReadToEnd());
                    return false;
                }
            }
        }

        private CookieContainer GetCookieContainer()
        {
            var container = new CookieContainer();

            if (web.Document != null)
            {
                foreach (string cookie in web.Document.Cookie.Split(';'))
                {
                    var name = cookie.Split('=')[0];
                    var value = cookie.Substring(name.Length + 1);                    
                    const string path = "/";
                    const string domain = "windows.azure.com"; 
                    container.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
                }                
            }

            return container;
        }


    }
}
