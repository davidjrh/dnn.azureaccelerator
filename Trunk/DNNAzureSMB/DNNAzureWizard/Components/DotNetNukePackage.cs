using System;

namespace DNNAzureWizard.Components
{

    public class DotNetNukePackage
    {
        public const string DNN_CE = "DNNCORP.CE";
        public const string DNN_PE = "DNNCORP.PE";
        public const string DNN_EE = "DNNCORP.EE";

        public string PackageName { get; set; }
        public string Description { 
            get { 
                const string desc = "DotNetNuke {0} Edition {1} {2}";
                string versionDesc;
                switch (PackageName)
                {
                    case DNN_CE:
                        versionDesc = "Community";
                        break;
                    case DNN_PE:
                        versionDesc = "Professional";
                        break;
                    case DNN_EE:
                        versionDesc = "Enterprise";
                        break;
                    default:
                        versionDesc = "Unknown";
                        break;                        
                }

                return string.Format(desc, versionDesc, FormatVersion(Version), (Latest ? "(Latest)" : ""));
            }
        }
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public bool Latest { get; set; }
        private string FormatVersion(string version)
        {
            try
            {
                return string.Format("{0}.{1}.{2}", version.Substring(0, 2), version.Substring(2, 2),
                                     version.Substring(4, 2));
            }
            catch 
            {
                return version;
            }
        }
    }
}
