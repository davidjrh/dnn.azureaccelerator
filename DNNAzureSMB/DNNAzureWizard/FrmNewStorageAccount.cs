﻿using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DotNetNuke.Azure.Accelerator.Management;

namespace DNNAzureWizard
{
    public partial class FrmNewStorageAccount : Form
    {
        public FrmNewStorageAccount()
        {
            InitializeComponent();
        }

        internal FrmDNNAzureWizard Wizard { get; set; }
        internal string ServiceName
        {
            get { return txtHostedServiceName.Text; }
            set { txtHostedServiceName.Text = value; }
        }
        internal string ServiceLocation
        {
            get { return cboLocation.Text; }
            set { cboLocation.SelectedValue = value; }
        }

        internal bool HostedServiceSuccess { get; set; }

        private void FrmNewHostedServiceLoad(object sender, EventArgs e)
        {
            try
            {
                Closing += OnClosing;
                RefreshLocations();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            cboLocation.Enabled = true;
            Cursor = Cursors.Default;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            cancelEventArgs.Cancel = !HostedServiceSuccess;
        }

        private void RefreshLocations()
        {
            cboLocation.SelectedIndex = 0;
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            var locations = Wizard.DatabaseManager.ListLocations(Wizard.Subscription.SubscriptionId);
            cboLocation.Tag = locations;
            cboLocation.Items.Clear();
            foreach (var location in locations)
            {
                if (!location.Name.Contains("Anywhere"))
                    cboLocation.Items.Add(location.Name);
            }
            if (cboLocation.Items.Count > 0)
                cboLocation.SelectedIndex = 0;
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
                if (txtHostedServiceName.Text.Trim() == string.Empty)
                    throw new ApplicationException("You must specify a valid service name");

                if (!Regex.IsMatch(txtHostedServiceName.Text, @"^[a-z0-9](([a-z0-9\-[^\-])){1,21}[a-z0-9]$"))
                    throw new ApplicationException("The storage account name must be between 3 and 24 characters in length and use lower-case letters and numbers only.");


                Enabled = false;
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                Wizard.ServiceManager.CreateStorageService(Wizard.Subscription.SubscriptionId, new CreateStorageServiceInput
                                                                                    {
                                                                                        ServiceName = ServiceName,
                                                                                        Location = ServiceLocation, 
                                                                                        AffinityGroup = null,                                                                                       
                                                                                        Label = Convert.ToBase64String(Encoding.UTF8.GetBytes(ServiceName))
                                                                                    });
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
