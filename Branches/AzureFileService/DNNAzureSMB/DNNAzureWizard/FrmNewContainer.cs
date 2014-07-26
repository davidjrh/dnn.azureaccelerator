using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DotNetNuke.Azure.Accelerator.Management;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace DNNAzureWizard
{
    public partial class FrmNewContainer : Form
    {
        public FrmNewContainer()
        {
            InitializeComponent();
        }

        internal FrmDNNAzureWizard Wizard { get; set; }
        internal string ContainerName
        {
            get { return txtContainerName.Text; }
            set { txtContainerName.Text = value; }
        }
        internal string ServiceLocation
        {
            get { return cboAccessLevel.Text; }
            set { cboAccessLevel.SelectedValue = value; }
        }

        internal bool HostedServiceSuccess { get; set; }

        private void FrmNewHostedServiceLoad(object sender, EventArgs e)
        {
            try
            {
                Closing += OnClosing;
                cboAccessLevel.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            Cursor = Cursors.Default;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            cancelEventArgs.Cancel = !HostedServiceSuccess;
        }

        private static void LogException(Exception ex)
        {
            string msg = ex.Message;
#if DEBUG
                msg += " - Stack trace: " + ex.StackTrace;
#endif
            MessageBox.Show(msg, "An exception occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CmdOkClick(object sender, EventArgs e)
        {
            try
            {
                if (txtContainerName.Text.Trim() == string.Empty)
                    throw new ApplicationException("You must specify a valid service name");

                if (!Regex.IsMatch(txtContainerName.Text, @"^[a-z0-9](([a-z0-9\-[^\-])){1,61}[a-z0-9]$"))
                    throw new ApplicationException("Invalid container name. Container names must be valid DNS names, and must conform to these rules: \n - Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.\n- Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.\n- All letters in a container name must be lowercase.\n- Container names must be from 3 through 63 characters long.");


                Enabled = false;
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                if (Wizard.cboStorage.Tag != null)
                {
                    var storageAccounts = (List<StorageService>)Wizard.cboStorage.Tag;
                    var storageAccount = storageAccounts.FirstOrDefault(x => x.ServiceName == Wizard.cboStorage.Text);
                    if (storageAccount != null)
                    {
                        var storageKeys = Wizard.ServiceManager.GetStorageKeys(Wizard.Subscription.SubscriptionId,
                                                                        storageAccount.ServiceName);
                        var primaryKey = storageKeys.StorageServiceKeys.Primary;
                        var credentials = new StorageCredentialsAccountAndKey(storageAccount.ServiceName, primaryKey);
                        var storage = new CloudStorageAccount(credentials, true);
                        var blobClient = new CloudBlobClient(storage.BlobEndpoint.AbsoluteUri, credentials);
                        var container = blobClient.GetContainerReference(txtContainerName.Text);
                        container.CreateIfNotExist();
                        container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off });
                    }
                    else
                        throw new ApplicationException("The storage account can't be located");
                }
                else
                    throw new ApplicationException("You must select a storage account first");
                HostedServiceSuccess = true;
            }
            catch (Exception ex)
            {                
                LogException(ex);
            }
            Enabled = true;
            Cursor = Cursors.Default;
        }

        private void CmdCancelClick(object sender, EventArgs e)
        {
            HostedServiceSuccess = true;
            Close();
        }

    }
}
