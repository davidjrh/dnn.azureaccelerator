using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DotNetNuke.Azure.Accelerator.Management;

namespace DNNAzureWizard
{
    public partial class FrmNewDatabaseServer : Form
    {
        public FrmNewDatabaseServer()
        {
            InitializeComponent();
        }

        internal FrmDNNAzureWizard Wizard { get; set; }
        internal string ServerName { get; set; }
        internal string AdminUser
        {
            get { return txtAdminUser.Text; }
            set { txtAdminUser.Text = value; }
        }
        internal string AdminPassword
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value; }
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
            MessageBox.Show(msg, "An exception ocurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CmdOkClick(object sender, EventArgs e)
        {
            try
            {
                Enabled = false;
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                if ((txtPassword.Text.Length == 0) ||
                    (txtPassword.Text.Contains(txtAdminUser.Text)) ||
                    !Regex.Match(txtPassword.Text, FrmDNNAzureWizard.PasswordStrengthRegex).Success)
                {
                     throw new ApplicationException("The password does not conform to complexity requirements. Ensure the password does not contain the user account name or parts of it. The Password should be at least six characters long and contain a mixture of upper, lower case, digits and symbols.");
                }

                if (txtPassword.Text != txtConfirmPassword.Text)
                    throw new ApplicationException("The passwords are different");

                var databaseServer = Wizard.DatabaseManager.CreateServer(Wizard.Subscription.SubscriptionId, new CreateDatabaseServerInput
                                                                                            {
                                                                                                Location =
                                                                                                    ServiceLocation,
                                                                                                AdministratorLogin =
                                                                                                    AdminUser,
                                                                                                AdministratorLoginPassword
                                                                                                    = AdminPassword
                                                                                            });
                ServerName = databaseServer.Name;
                Wizard.DatabaseManager.CreateOrUpdateFirewallRule(Wizard.Subscription.SubscriptionId,
                                                                  databaseServer.Name, "MicrosoftServices",
                                                                  new CreateOrUpdateFirewallRuleInput
                                                                      {
                                                                          StartIpAddress = "0.0.0.0",
                                                                          EndIpAddress = "0.0.0.0"
                                                                      });
                Wizard.DatabaseManager.CreateOrUpdateFirewallRuleAuto(Wizard.Subscription.SubscriptionId, databaseServer.Name, "DotNetNuke Accelerator");

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
