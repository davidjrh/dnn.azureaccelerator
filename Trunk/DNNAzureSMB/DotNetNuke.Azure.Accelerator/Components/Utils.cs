using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetNuke.Azure.Accelerator.Management;

namespace DotNetNuke.Azure.Accelerator.Components
{
    public class Utils
    {
        public static PublishSettings GetWAPublishingSettings()
        {
            var frm = new Forms.FrmWASettingsDownload();
            frm.DownloadSettings();
            frm.ShowDialog();
            return frm.PublishingSettings;
        }
    }
}
