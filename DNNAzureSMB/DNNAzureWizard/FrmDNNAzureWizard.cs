using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using DotNetNuke.Azure.Accelerator.Management;


namespace DNNAzureWizard
{
    public partial class FrmDNNAzureWizard : Form
    {
        private const int WizardWidth = 700;
        private const int WizardHeight = 500;
        private const string CreateNewLabel = "<Create New...>";
        private const string RefreshLabel = "<Refresh...>";

        /* The following expression to validate a pwd of 6 to 16 characters and contain three of the following 4 items: 
         * upper case letter, lower case letter, a symbol, a number
         * An explanation of individual components:
         *      (?=^[^\s]{6,16}$) - contain between 8 and 16 non-whitespace characters
         *      (?=.*?\d) - contains 1 numeric
         *      (?=.*?[A-Z]) - contains 1 uppercase character
         *      (?=.*?[a-z]) - contains 1 lowercase character
         *      (?=.*?[^\w\d\s]) - contains 1 symbol
         */
        internal const string PasswordStrengthRegex = @"(?=^[^\s]{6,16}$)((?=.*?\d)(?=.*?[A-Z])(?=.*?[a-z])|(?=.*?\d)(?=.*?[^\w\d\s])(?=.*?[a-z])|(?=.*?[^\w\d\s])(?=.*?[A-Z])(?=.*?[a-z])|(?=.*?\d)(?=.*?[A-Z])(?=.*?[^\w\d\s]))^.*";

        private enum WizardTabs
        {
            TabHome = 1,
            TabDeploymentType = 2,
            TabHostedServices = 3,
            TabWindowsAzureSettings = 4,
            TabSQLAzureSettings = 5,
            TabRDPAzureSettings = 6,
            TabConnectSettings = 7,
            TabPackages = 8,
            TabSummary = 9,
            TabUploading = 10,
            TabFinish = 11
        }


        Process _p;



        public FrmDNNAzureWizard()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Width = WizardWidth;
            Height = WizardHeight;
        }

        #region " Event Handlers "

            private void FrmDNNAzureWizardLoad(object sender, EventArgs e)
            {
                try
                {
                    RefreshUI();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void BtnCancelClick(object sender, EventArgs e)
            {
                try
                {
                    CleanUp();
                    Close();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void BtnBackClick(object sender, EventArgs e)
            {
                try
                {
                    switch (ActivePageIndex())
                    {
                        case (int) WizardTabs.TabFinish:
                            btnOK.Text = "Next >";
                            btnCancel.Enabled = true;
                            MoveSteps(-2);
                            break;
                        case (int) WizardTabs.TabSQLAzureSettings:
                            if (optSubscription.Checked)
                                MoveSteps(-2);
                            else
                                MoveSteps(-1);
                            break;
                        case (int) WizardTabs.TabWindowsAzureSettings:
                            MoveSteps(-2);
                            break;
                        default:
                            MoveSteps(-1);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void BtnOkClick(object sender, EventArgs e)
            {
                try
                {
                    if (ActivePageIndex() == (int) WizardTabs.TabFinish) // Finish
                    {
                        BtnCancelClick(sender, e);
                        return;
                    }

                    UseWaitCursor = true;
                    if (ValidateStep())
                    {
                        switch (ActivePageIndex())
                        {
                            case (int)WizardTabs.TabDeploymentType:
                                MoveSteps(!optSubscription.Checked ? 2 : 1);
                                break;
                            case (int)WizardTabs.TabHostedServices:
                                MoveSteps(2);
                                break;
                            default:
                                MoveSteps(1);
                                break;
                        }
                        if (ActivePageIndex() == (int)WizardTabs.TabHostedServices)
                        {
                            RefreshServices();
                        }
                        if (ActivePageIndex() == (int)WizardTabs.TabSQLAzureSettings)
                        {
                            txtDBServer.Visible = !optSubscription.Checked;
                            cboDatabase.Visible = optSubscription.Checked;
                            if (optSubscription.Checked)
                                ThreadPool.QueueUserWorkItem(o => RefreshDatabaseServers());
                        }
                        if (ActivePageIndex() == (int)WizardTabs.TabUploading)
                        {
                            btnBack.Enabled = false;
                            btnOK.Enabled = false;
                            UploadToWindowsAzure();
                            txtLogFinal.Text = GetFinalLog();
                            MoveSteps(1);
                            btnOK.Text = "Finish";
                            btnCancel.Enabled = false;
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
                btnBack.Enabled = true;
                btnOK.Enabled = true;
                UseWaitCursor = false;
            }

        private void RefreshServices()
        {
            ThreadPool.QueueUserWorkItem(o => RefreshHostedServices());
            ThreadPool.QueueUserWorkItem(o => RefreshStorageAccounts());
        }

        private string GetFinalLog()
            {
                bool success = true;
                var log = new StringBuilder("====================== UPLOAD LOG ======================");
                log.AppendLine("");
                foreach (ListViewItem li in lstTasks.Items)
                {
                    log.AppendLine("- " + li.Text + ": " + li.SubItems[1].Text);
                    if (li.SubItems[1].ForeColor != Color.DarkGreen)
                        success = false;
                }
                if (!success)
                {
                    lblSuccess.Text = "Upload failed!";
                    log.AppendLine("Upload failed!");
                }
                else
                {
                    lblSuccess.Text = "Success!";
                    log.AppendLine("Success!");
                }

                return log.ToString();
            }

            private void BtnTestDBClick(object sender, EventArgs e)
            {
                try
                {
                    var cnStr = new System.Data.SqlClient.SqlConnectionStringBuilder
                                    {
                                        {"Data Source", (txtDBServer.Visible ? txtDBServer.Text: cboDatabase.Text) + ".database.windows.net"},
                                        {"Initial Catalog", "master"},
                                        {"User ID", txtDBAdminUser.Text},
                                        {"Password", txtDBAdminPassword.Text},
                                        {"Trusted_Connection", "False"},
                                        {"Encrypt", "True"}
                                    };

                    using (var cn = new System.Data.SqlClient.SqlConnection(cnStr.ConnectionString))
                    {
                        cn.Open();
                    }

                    MessageBox.Show("Test connection successfull", "Test connection successfull", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void BtnTestStorageClick(object sender, EventArgs e)
            {
                try
                {
                    var credentials = new StorageCredentialsAccountAndKey(txtStorageName.Text, txtStorageKey.Text);
                    var storage = new CloudStorageAccount(credentials, chkStorageHTTPS.Checked);
                    var cloudTableClient = new CloudBlobClient(storage.BlobEndpoint.AbsoluteUri, credentials);
                    var containers = cloudTableClient.ListContainers();
                    int totalContainers = containers.Count();
                    MessageBox.Show("Test connection successfull", "Test connection successfull", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void TxtStorageNameTextChanged(object sender, EventArgs e)
            {
                lblStTest.Text = string.Format("Test credentials to http{0}://{1}.blob.windows.core.net", (chkStorageHTTPS.Checked ? "s" : ""), txtStorageName.Text);
            }

            private void ChkStorageHttpsCheckedChanged(object sender, EventArgs e)
            {
                lblStTest.Text = string.Format("Test credentials to http{0}://{1}.blob.windows.core.net", (chkStorageHTTPS.Checked ? "s" : ""), txtStorageName.Text);
            }

        #endregion

        #region " Wizard utilities "
            private void CleanUp()
            {

                // Is the upload process running? If yes and after user confirmation, kill all the process tree before exit
                if (_p != null)
                {
                    if (!_p.HasExited && (MessageBox.Show("The upload process is still running. Are you sure that you want to cancel?", "Cancel upload", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                    {
                        try
                        {
                            ProcessUtility.KillTree(_p.Id);
                            _p = null;
                        }
                        catch { }
                    }
                }

                // Deletes temporary generated configuration files 
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, "ServiceConfiguration.cscfg")))
                    File.Delete(Path.Combine(Environment.CurrentDirectory, "ServiceConfiguration.cscfg"));
                if (File.Exists(Path.Combine(new [] {Environment.CurrentDirectory, "bin", "ServiceConfiguration.cscfg" })))
                    File.Delete(Environment.CurrentDirectory + "\\bin\\ServiceConfiguration.cscfg");
            }

            private FileVersionInfo _versionInfo;
            private FileVersionInfo VersionInfo
            {
                get
                {
                    if (_versionInfo == null)
                    {
                        System.Reflection.Assembly oAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        _versionInfo = FileVersionInfo.GetVersionInfo(oAssembly.Location);
                    }
                    return _versionInfo;
                }
            }

            private void RefreshUI()
            {
                
                Text += " (" + VersionInfo.ProductVersion + ")";
#if DEBUG
                Text += " - (Debug)";
#endif
                InitializePages();                
                ShowPage((int) WizardTabs.TabHome);
                SetupAppSettings();
                RefreshSubscriptions();
                ReloadDeploymentPackages();
                ReloadX509Certificates();
            }
            private void RefreshSubscriptions()
            {
                cboSubscriptions.Items.Clear();
                if (File.Exists(PublishSettingsFilename))
                {
                    PublishSettings = new PublishSettings(XDocument.Load(PublishSettingsFilename));
                    foreach (var subscription in PublishSettings.Subscriptions)
                        cboSubscriptions.Items.Add(subscription);

                }
                if (cboSubscriptions.Items.Count == 0)
                    cboSubscriptions.Items.Add("");
                cboSubscriptions.Items.Add(RefreshLabel);
                cboSubscriptions.SelectedIndex = 0;
            }

            private static void LogException(Exception ex)
            {
                string msg = ex.Message;
#if DEBUG
                msg += " - Stack trace: " + ex.StackTrace;
#endif
                MessageBox.Show(msg, "An exception ocurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            private void ShowPage(int pageNumber)
            {
                foreach (Panel p in pnl.Controls)
                    p.Visible = p.Name == "pnl" + pageNumber;
                btnBack.Enabled = pageNumber > 1;
            }
            private void InitializePages()
            {
                foreach (Panel p in pnl.Controls)
                {
                    p.Dock = DockStyle.Fill;
                    p.Visible = false;
                }
                ChkEnableRDPCheckedChanged(null, null);
                ChkAzureConnectCheckedChanged(null, null);
                cboEnvironment.SelectedIndex = 0;
            }
            private int ActivePageIndex()
            {
                return (from Panel p in pnl.Controls 
                        where p.Visible 
                        select int.Parse(p.Name.Replace("pnl", ""))).FirstOrDefault();
            }

        private void MoveSteps(int steps)
            {
                ShowPage(ActivePageIndex() + steps);
            }
            private bool ValidateStep()
            {
                switch (ActivePageIndex())
                {
                    case (int) WizardTabs.TabHome: // Home tab, nothing to validate
                        return true;
                    case (int) WizardTabs.TabDeploymentType: // Deployment type selection
                        return ValidateDeploymentType();
                    case (int) WizardTabs.TabHostedServices: // Hosted services
                        return ValidateHostingServices();
                    case (int) WizardTabs.TabSQLAzureSettings: // SQL Azure tab
                        return ValidateSQLAzureSettings();                        
                    case (int) WizardTabs.TabWindowsAzureSettings: // Windows Azure tab
                        return ValidateAzureSettings();                        
                    case (int) WizardTabs.TabRDPAzureSettings: // RDP tab
                        return ValidateRDPSettings();                       
                    case (int) WizardTabs.TabConnectSettings: // Virtual Network tab
                        return ValidateConnectSettings();
                    case (int) WizardTabs.TabPackages: // Deployment packages
                        bool validated= ValidatePackagesSelectionSettings();
                        txtConfig.Text = GetSettingsSummary();
                        return validated;
                    case (int) WizardTabs.TabSummary:
                        return (MessageBox.Show("The wizard will begin now to deploy DotNetNuke on Windows Azure with the specified settings. Are you sure that you want to continue?", "Deploy", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK);
                    default:
                        return false;
                }
            }

        private bool ValidateHostingServices()
        {
            bool invalidInput = false;

            CboHostingServiceOnValidating(cboHostingService, null);
            CboStorageOnValidating(cboStorage, null);
            TxtPackagesContainerOnValidating(txtPackagesContainer, null);
            TxtVhdNameOnValidating(txtVHDName, null);
            TxtVhdDriveSizeOnValidating(txtVHDDriveSize, null);

            if (pnlHostingServices.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0))
                invalidInput = true;
            return !invalidInput;
        }



        private bool ValidateDeploymentType()
        {
            bool invalidInput = false;
            if (optSubscription.Checked)
            {
                CboSubscriptionsSelectedIndexChanged(cboSubscriptions, null);
                invalidInput = errProv.GetError(cboSubscriptions).Length != 0;
            }
            return !invalidInput;
        }

        private bool ValidateConnectSettings()
            {
                bool invalidInput = false;
                if (chkAzureConnect.Checked)
                {
                    TxtConnectActivationTokenValidating(txtConnectActivationToken, null);
                    if (pnlAzureConnect.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0))
                        invalidInput = true;
                }
                return !invalidInput;
            }

            private bool ValidateRDPSettings()
            {
                bool invalidInput = false;
                if (chkEnableRDP.Checked)
                {
                    TxtRDPUserValidating(txtRDPUser, null);
                    TxtRDPPasswordValidating(txtRDPPassword, null);
                    TxtRDPConfirmPasswordValidating(txtRDPConfirmPassword, null);
                    CboCertificatesValidating(cboCertificates, null);

                    if (pnlRDP.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0))
                        invalidInput = true;
                }
                return !invalidInput;
            }

            private string GetSettingsSummary()
            {
                var summary = new StringBuilder("======================= SUMMARY OF SETTINGS =======================");
                summary.AppendLine("");
                summary.AppendLine("DATABASE SETTINGS:");
                summary.AppendLine("- DB Server Name: " +  (txtDBServer.Visible? txtDBServer.Text: cboDatabase.Text) + ".database.windows.net");
                summary.AppendLine("- DB Admin user name: " + txtDBAdminUser.Text.Trim());
                summary.AppendLine("- DB Admin password: <not shown>");
                summary.AppendLine("- DB user name: " + txtDBUser.Text.Trim());
                summary.AppendLine("- DB password: <not shown>");
                summary.AppendLine("");
                summary.AppendLine("STORAGE SETTINGS:");
                summary.AppendLine("- Storage name: " + txtStorageName.Text.Trim());
                summary.AppendLine("- Storage key: " + txtStorageKey.Text.Trim());
                summary.AppendLine("- Storage package container: " + txtStorageContainer.Text.Trim());
                summary.AppendLine("- VHD blob name: " + txtVHDBlobName.Text.Trim());
                summary.AppendLine("- VHD size: " + txtVHDSize.Text.Trim());
                summary.AppendLine("");
                summary.AppendLine("IIS SETTINGS:");
                summary.AppendLine("- Bindings: " + txtBindings.Text.Trim());
                summary.AppendLine("");
                
                summary.AppendLine("RDP SETTINGS:");
                summary.AppendLine("- RDP enabled: " + (chkEnableRDP.Checked?"true":"false"));
                if (chkEnableRDP.Checked)
                {
                    summary.AppendLine("- Certificate Friendly Name: " + Certificate.FriendlyName);
                    summary.AppendLine("- Certificate Issued By: " + Certificate.Issuer);
                    summary.AppendLine("- Certificate Thumbprint: " + Certificate.Thumbprint);
                    summary.AppendLine("- User name: " + txtRDPUser.Text);
                    
#if DEBUG
                    summary.AppendLine("- Password: " + EncryptWithCertificate(txtRDPPassword.Text, Certificate));
#else
                    summary.AppendLine("- Password: <not shown>");
#endif
                    summary.AppendLine("- Expires: " + cboRDPExpirationDate.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"));
                }
                summary.AppendLine("");

                summary.AppendLine("VIRTUAL NETWORK SETTINGS:");
                summary.AppendLine("- Azure Connect enabled: " + (chkAzureConnect.Checked ? "true" : "false"));
                if (chkAzureConnect.Checked)
                {
                    summary.AppendLine("- Activation Token: " + txtConnectActivationToken.Text.Trim());                    
                }
                summary.AppendLine("");

                summary.AppendLine("SELECTED PACKAGES:");
                foreach (ListViewItem li in lstPackages.Items)
                    if (li.Checked)
                        summary.AppendLine("- " + li.Text);
                return summary.ToString();
            }

            private bool ValidateSQLAzureSettings()
            {
                TxtDBServerValidating(txtDBServer, null);             
                CboDatabaseOnValidating(cboDatabase, null);
                TxtDBAdminUserValidating(txtDBAdminUser, null);
                TxtDBAdminPasswordValidating(txtDBAdminPassword, null);
                TxtDBNameValidating(txtDBName, null);
                TxtDBUserValidating(txtDBUser, null);
                TxtDBPasswordValidating(txtDBPassword, null);
                TxtDBRePasswordValidating(txtDBRePassword, null);
                bool invalidInput = DBSettings.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0);
                return !invalidInput;
            }

            private void CboDatabaseOnValidating(object sender, CancelEventArgs cancelEventArgs)
            {
                string error = null;
                if (cboDatabase.Visible && (cboDatabase.Text == string.Empty || cboDatabase.Text.Contains(".")))
                {
                    error = "Please enter your database server name (this is <databasename>.database.windows.net)";
                }
                errProv.SetError((Control)sender, error);
            }

            private bool ValidateAzureSettings()
            {
                TxtStorageNameValidating(txtStorageName, null);
                TxtStorageKeyValidating(txtStorageKey, null);
                TxtBindingsValidating(txtBindings, null);
                TxtStorageContainerValidating(txtStorageContainer, null);
                TxtVhdBlobNameValidating(txtVHDBlobName, null);
                TxtVhdSizeValidating(txtVHDSize, null);
                bool invalidInput = AzureSettings.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0);
                return !invalidInput;
            }

            private bool ValidatePackagesSelectionSettings()
            {
                LstPackagesValidating(lstPackages, null);
                bool invalidInput = PackageSettings.Controls.Cast<Control>().Any(control => errProv.GetError(control).Length != 0);
                return !invalidInput;
            }

            private string ReplaceTokens(string cfgStr)
            {
                // Replace the tokens - SQL Azure settings
                cfgStr = cfgStr.Replace("@@DBSERVER@@", txtDBServer.Text.Trim());
                cfgStr = cfgStr.Replace("@@DBADMINUSER@@", txtDBAdminUser.Text.Trim());
                cfgStr = cfgStr.Replace("@@DBADMINPASSWORD@@", txtDBAdminPassword.Text);
                cfgStr = cfgStr.Replace("@@DBNAME@@", txtDBName.Text.Trim());
                cfgStr = cfgStr.Replace("@@DBUSER@@", txtDBUser.Text.Trim());
                cfgStr = cfgStr.Replace("@@DBPASSWORD@@", txtDBPassword.Text);

                // Replace the tokens - Windows Azure settings
                cfgStr = cfgStr.Replace("@@STORAGEPROTOCOL@@", "http" + (chkStorageHTTPS.Checked ? "s" : ""));
                cfgStr = cfgStr.Replace("@@STORAGEACCOUNTNAME@@", txtStorageName.Text.Trim());
                cfgStr = cfgStr.Replace("@@STORAGEKEY@@", txtStorageKey.Text.Trim());

                // Replace the tokens - IIS settings
                cfgStr = cfgStr.Replace("@@HOSTHEADERS@@", txtBindings.Text.Trim());

                // Replace the tokens - Paths
                cfgStr = cfgStr.Replace("@@APPPATH@@", Environment.CurrentDirectory + '\\');
                cfgStr = cfgStr.Replace("@@PACKAGECONTAINER@@", txtStorageContainer.Text.Trim().ToLower());

                // Replace the tokens - VHD settings
                cfgStr = cfgStr.Replace("@@VHDBLOBNAME@@", txtVHDBlobName.Text.Trim().ToLower());
                int driveSize;
                int.TryParse(txtVHDSize.Text, out driveSize);
                cfgStr = cfgStr.Replace("@@VHDBLOBSIZE@@", txtVHDSize.Text.Trim().ToLower());    
            
                // Replace the tokens - RDP settings
                if (chkEnableRDP.Checked)
                {
                    cfgStr = cfgStr.Replace("@@RDPENABLED@@", "true");
                    cfgStr = cfgStr.Replace("@@RDPUSERNAME@@", txtRDPUser.Text.Trim());
                    cfgStr = cfgStr.Replace("@@RDPPASSWORD@@", EncryptWithCertificate(txtRDPPassword.Text, Certificate));
                    cfgStr = cfgStr.Replace("@@RDPEXPIRATIONDATE@@", cboRDPExpirationDate.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"));                    
                }
                else
                {
                    cfgStr = cfgStr.Replace("@@RDPENABLED@@", "false");
                    cfgStr = cfgStr.Replace("@@RDPUSERNAME@@", "");
                    cfgStr = cfgStr.Replace("@@RDPPASSWORD@@", "");
                    cfgStr = cfgStr.Replace("@@RDPEXPIRATIONDATE@@", DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"));
                }

                // Replace the tokens - Virtual Network settings
                cfgStr = cfgStr.Replace("@@CONNECTACTIVATIONTOKEN@@", chkAzureConnect.Checked ? txtConnectActivationToken.Text.Trim() : "");

                // Replace the tokens - Certificate settings
                if (chkEnableRDP.Checked && (Certificate != null))
                    cfgStr = cfgStr.Replace("@@RDPTHUMBPRINT@@", Certificate.Thumbprint);    
                else
                    cfgStr = cfgStr.Replace("@@RDPTHUMBPRINT@@", "");                    


                return cfgStr;

            }

            private string ReplaceFileTokens(string filePath)
            {
                string cfgStr;
                using (TextReader mConfig = File.OpenText(filePath))
                    cfgStr = mConfig.ReadToEnd();
                return ReplaceTokens(cfgStr);
            }
            


        /// <summary>
        /// Prepares the list of tasks for uploading contents to Windows Azure
        /// </summary>
            private void PrepareTasks()
            {
                lstTasks.Items.Clear();                
                var xtasks = new XmlDocument();
                xtasks.Load(Environment.CurrentDirectory + "\\config\\DeploymentTasks.xml");

            var xmlNodeList = xtasks.SelectNodes("/DeploymentTasks/Task");
            if (xmlNodeList != null)
                foreach (XmlNode task in xmlNodeList)
                {
                    if (task.Attributes != null && task.Attributes["type"].InnerText == "UploadPackages")
                    {
                        foreach (ListViewItem p in lstPackages.Items)
                        {
                            if (p.Checked)
                            {
                                var pNode = (XmlNode) p.Tag;
                                var selectSingleNode = pNode.SelectSingleNode("ConfigurationFile");
                                if (selectSingleNode != null)
                                {
                                    var li = new ListViewItem(new [] { "Upload service configuration file '" + selectSingleNode.InnerText + "'", "Pending" });
                                    li.Tag = selectSingleNode;
                                    li.UseItemStyleForSubItems = false;
                                    lstTasks.Items.Add(li);
                                    var singleNode = pNode.SelectSingleNode("PackageFile");
                                    if (singleNode != null)
                                        li = new ListViewItem(new [] { "Upload service package file '" + singleNode.InnerText + "'", "Pending" });
                                    li.Tag = pNode.SelectSingleNode("PackageFile");
                                    li.UseItemStyleForSubItems = false;
                                    lstTasks.Items.Add(li);
                                }
                            }
                        }
                    }
                    else
                    {
                        var selectSingleNode = task.SelectSingleNode("Description");
                        if (selectSingleNode != null)
                        {
                            var li = new ListViewItem(new[] {selectSingleNode.InnerText, "Pending"})
                                         {Tag = task, UseItemStyleForSubItems = false};
                            lstTasks.Items.Add(li);
                        }
                    }
                }
            }

        /// <summary>
        /// Setup the environment variables for the subsequent command line calls 
        /// </summary>
        /// <param name="vars"></param>
            private void SetupEnvironmentVariables(XmlNode vars)
        {
            var xmlNodeList = vars.SelectNodes("Action/Variable");
            if (xmlNodeList != null)
                foreach (XmlNode var in xmlNodeList)
                    if (var.Attributes != null)
                        Environment.SetEnvironmentVariable(var.Attributes["name"].InnerText, ReplaceTokens(var.Attributes["value"].InnerText), EnvironmentVariableTarget.Process);
        }

        private void RemoveEnvironmentVariables(XmlNode vars)
            {
                try
                {
                    var xmlNodeList = vars.SelectNodes("Action/Variable");
                    if (xmlNodeList != null)
                        foreach (XmlNode var in xmlNodeList)
                            if (var.Attributes != null)
                                Environment.SetEnvironmentVariable(var.Attributes["name"].InnerText, "", EnvironmentVariableTarget.Process);
                }
                catch { }
            }


            private void ProcessTasks()
            {
                XmlNode environmentVariables = null;
                foreach (ListViewItem task in lstTasks.Items)
                {
                    try
                    {
                        var xTag = (XmlNode)task.Tag;
                        if (xTag.Name == "Task" && xTag.Attributes != null)
                        {
                            switch (xTag.Attributes["type"].InnerText)
                            {
                                case "SetupVariables":
                                    task.SubItems[1].Text = "Running...";
                                    Application.DoEvents();
                                    environmentVariables = xTag;
                                    SetupEnvironmentVariables(xTag);
                                    task.SubItems[1].Text = "Completed";
                                    task.SubItems[1].ForeColor = Color.DarkGreen;
                                    break;
                                case "CommandLineAction":
                                    task.SubItems[1].Text = "Running...";
                                    Application.DoEvents();
                                    CheckForIllegalCrossThreadCalls = false;
                                    var selectSingleNode = xTag.SelectSingleNode("Action/CommandLine");
                                    if (selectSingleNode != null)
                                    {
                                        string commandLine = ReplaceTokens(selectSingleNode.InnerText);
                                        var singleNode = xTag.SelectSingleNode("Action/Parameters");
                                        if (singleNode != null)
                                        {
                                            string parameters = ReplaceTokens(singleNode.InnerText);
                                            ExecuteCommand(commandLine, parameters);
                                        }
                                    }
                                    task.SubItems[1].Text = "Completed";
                                    task.SubItems[1].ForeColor = Color.DarkGreen;
                                    break;
                                case "UploadBlob": 
                                    task.SubItems[1].Text = "Running...";
                                    Application.DoEvents();
                                    UploadItemToAzure(task);
                                    task.SubItems[1].Text = "Completed";
                                    task.SubItems[1].ForeColor = Color.DarkGreen;
                                    break;
                            }
                        }
                        else // is a Upload service package
                        {
                            task.SubItems[1].Text = "Running...";
                            Application.DoEvents();
                            UploadItemToAzure(task);
                            task.SubItems[1].Text = "Completed";
                            task.SubItems[1].ForeColor = Color.DarkGreen;
                        }
                    }
                    catch (Exception ex)
                    {
                        task.SubItems[1].Text = "Error: " + ex.Message;
                        task.SubItems[1].ForeColor = Color.Red;
                    }
                    Application.DoEvents();
                }
                if (environmentVariables != null)
                    RemoveEnvironmentVariables(environmentVariables);
            }

            /// <summary>
            /// Upload the contents to Windows Azure
            /// </summary>
            private void UploadToWindowsAzure()
            {
                try
                {
                    CheckForIllegalCrossThreadCalls = false;
                    txtLOG.Text = "";
                    PrepareTasks();
                    Application.DoEvents();

                    ProcessTasks();

                }
                finally
                {
                    CheckForIllegalCrossThreadCalls = true;
                }
            }

            /// <summary>
            /// Executes an external .exe command
            /// </summary>
            /// <param name="exe">EXE path</param>
            /// <param name="arguments">Arguments</param>
            public void ExecuteCommand(string exe, string arguments)
            {                
                _p = new Process();
                
                _p.StartInfo.FileName = exe;
                _p.StartInfo.Arguments = arguments;
                _p.StartInfo.CreateNoWindow = true;
                _p.StartInfo.UseShellExecute = false;
                _p.StartInfo.RedirectStandardError = true;
                _p.StartInfo.RedirectStandardOutput = true;
                
                _p.Start();
                _p.WaitForExit();

                int exitCode = _p.ExitCode;
                string errorDesc = _p.StandardError.ReadToEnd();
                string outputDesc = _p.StandardOutput.ReadToEnd();
                
                _p.Close();
                _p = null;

                if (exitCode != 0)
                    if (errorDesc == "")
                        throw new Exception("An error ocurred while executing the process. See the 'Logs' folder for more info");
                    else
                        throw new Exception(errorDesc);
            }


            /// <summary>
            /// Reads the settings from the .config file to initialize the wizard boxes
            /// </summary>
            public void SetupAppSettings()
            {
                txtDBServer.Text = ConfigurationManager.AppSettings["DBServer"].Replace(".database.windows.net", "");
                txtDBAdminUser.Text = ConfigurationManager.AppSettings["DBAdminUser"];
                txtDBAdminPassword.Text = ConfigurationManager.AppSettings["DBAdminPassword"];
                txtDBName.Text = ConfigurationManager.AppSettings["DBName"];
                txtDBUser.Text = ConfigurationManager.AppSettings["DBUser"];
                txtDBPassword.Text = ConfigurationManager.AppSettings["DBPassword"];
                txtDBRePassword.Text = txtDBPassword.Text;

                txtStorageName.Text = ConfigurationManager.AppSettings["AzureStorageName"];
                txtStorageKey.Text = ConfigurationManager.AppSettings["AzureStorageKey"];
                chkStorageHTTPS.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["AzureStorageHTTPS"]);
                ChkStorageHttpsCheckedChanged(null, null);
                txtStorageContainer.Text = ConfigurationManager.AppSettings["AzureStoragePackageContainer"];
                txtBindings.Text = ConfigurationManager.AppSettings["Bindings"];

                txtVHDBlobName.Text = ConfigurationManager.AppSettings["VHDBlobBName"];
                txtVHDSize.Text = ConfigurationManager.AppSettings["VHDSizeInMb"];

                _uploadBlockSize = Convert.ToInt32(ConfigurationManager.AppSettings["UploadBlockSize"]);

                chkEnableRDP.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["RDPEnabled"]);
                txtRDPUser.Text = ConfigurationManager.AppSettings["RDPUser"];
                txtRDPPassword.Text = ConfigurationManager.AppSettings["RDPPassword"];
                txtRDPConfirmPassword.Text = txtRDPPassword.Text;
                ChkEnableRDPCheckedChanged(null, null);

                chkAzureConnect.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["ConnectEnabled"]);
                txtConnectActivationToken.Text = ConfigurationManager.AppSettings["ConnectActivationToken"];
                ChkAzureConnectCheckedChanged(null, null);

                cboRDPExpirationDate.Value = DateTime.Now.Date.AddMonths(1);
            }

        /// <summary>
        /// Reads all the definition packages files from ./packages folder and loads them into the packages listview
        /// </summary>
            public void ReloadDeploymentPackages()
            {
                lstPackages.Items.Clear();
                foreach (FileInfo filedef in new DirectoryInfo(Environment.CurrentDirectory + "\\packages").GetFiles("*.xml"))
                {
                    var doc = new XmlDocument();
                    doc.Load(filedef.FullName);
                    bool isRDPPackage = ((doc.SelectSingleNode("/ServicePackage/RDPEnabled") != null) && Convert.ToBoolean(doc.SelectSingleNode("/ServicePackage/RDPEnabled").InnerText));

                    if ((chkEnableRDP.Checked && isRDPPackage) || (!chkEnableRDP.Checked && !isRDPPackage))
                    {
                        var selectSingleNode = doc.SelectSingleNode("/ServicePackage/Name");
                        if (selectSingleNode != null)
                        {
                            var singleNode = doc.SelectSingleNode("/ServicePackage/Description");
                            if (singleNode != null)
                            {
                                var li = new ListViewItem(new[] {selectSingleNode.InnerText, singleNode.InnerText})
                                             {Tag = doc.SelectSingleNode("ServicePackage"), Checked = true};
                                lstPackages.Items.Add(li);
                            }
                        }
                    }
                }
            }

        /// <summary>
        /// Loads the installed certificates into the dropdown list
        /// </summary>
            private void ReloadX509Certificates()
            {
                cboCertificates.Items.Clear();

                // Open Certificates Store
                var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Read Certificates
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=DNN Azure Accelerator", false);
                foreach (X509Certificate2 cert in certs)
                    cboCertificates.Items.Add(cert);

                /* certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=" + Environment.UserName, false);
                foreach (X509Certificate cert in certs)
                    cboCertificates.Items.Add(cert);
                certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=Windows Azure Tools", false);                
                foreach (X509Certificate cert in certs)
                    cboCertificates.Items.Add(cert);*/

                cboCertificates.Items.Add(CreateNewLabel);
            }

            private X509Certificate2 Certificate
            {
                get
                {
                    if ((cboCertificates.SelectedItem != null) && (cboCertificates.SelectedItem is X509Certificate))
                        return (X509Certificate2)cboCertificates.SelectedItem;
                    return null;
                }
            }


        #endregion



        #region " Background upload worker "

            private int _uploadBlockSize;
            

            private string GetParsedConfigurationFilePath(string filePath)
            {
                string tmpFile = Path.GetTempFileName();
                File.WriteAllText(tmpFile, ReplaceFileTokens(filePath));
                return tmpFile;
            }


            /// <summary>
            /// Upload blobs tasks to Windows Azure 
            /// </summary>
            /// <param name="UploadFileItem">Task list view item</param>
            private void UploadItemToAzure(ListViewItem UploadFileItem)
            {
                int totalBytesRead;
                long totalFileSize = 0;

                string filePath = "";
                string containerName = "";
                string blobName = "";
                var xNode = (XmlNode)UploadFileItem.Tag;
                switch (xNode.Name)
                {
                    case "Task":
                        filePath = ReplaceTokens(xNode.SelectSingleNode("Action/SourceFile").InnerText);
                        string destinationPath = ReplaceTokens(xNode.SelectSingleNode("Action/DestinationBlob").InnerText);
                        containerName = Path.GetDirectoryName(destinationPath);
                        blobName = Path.GetFileName(destinationPath);
                        break;
                    case "PackageFile":
                        filePath = Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText;
                        containerName = txtStorageContainer.Text.Trim().ToLower();
                        blobName = Path.GetFileName(filePath);
                        break;
                    case "ConfigurationFile":
                        filePath = GetParsedConfigurationFilePath(Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText);
                        containerName = txtStorageContainer.Text.Trim().ToLower();
                        blobName = Path.GetFileName(Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText);
                        break;
                }
             


                UploadFileItem.SubItems[1].Text = "Connecting...";
                var account = new CloudStorageAccount(new StorageCredentialsAccountAndKey(txtStorageName.Text.Trim(), txtStorageKey.Text.Trim()), chkStorageHTTPS.Checked);
                var blobClient = new CloudBlobClient(account.BlobEndpoint.AbsoluteUri, account.Credentials);
                
                var container = blobClient.GetContainerReference(containerName);
                container.CreateIfNotExist();


                var blob = container.GetBlobReference(blobName).ToBlockBlob;
                FileStream fileStream = null;
                try
                {
                    UploadFileItem.SubItems[1].Text = "Uploading...0%";
                    Application.DoEvents();

                    fileStream = File.Open(filePath, FileMode.Open);

                    totalFileSize = fileStream.Length;
                    // Progressively read the file stream. Read [blockSize] bytes each time.
                    int bytesRead = 0;
                    totalBytesRead = 0;
                    int i = 0;
                    byte[] buffer = new byte[_uploadBlockSize];
                    // Keep a list of block IDs.
                    var blockIDList = new List<string>();
                    // Read the first block.
                    bytesRead = fileStream.Read(buffer, 0, _uploadBlockSize);
                    while (bytesRead > 0)
                    {
                        using (MemoryStream ms = new MemoryStream(buffer, 0, bytesRead))
                        {
                            char[] tempID = new char[6];
                            string iStr = i.ToString();
                            for (int j = tempID.Length - 1; j > tempID.Length - iStr.Length - 1; j--)
                            {
                                tempID[j] = iStr[tempID.Length - j - 1];
                            }
                            byte[] blockIDBeforeEncoding = Encoding.UTF8.GetBytes(tempID);
                            string blockID = Convert.ToBase64String(blockIDBeforeEncoding);
                            blockIDList.Add(blockID);

                            blob.PutBlock(blockID, ms, null);
                        }
                        totalBytesRead += bytesRead;
                        i++;
                        var dIndex = (double)(totalBytesRead);
                        var dTotal = (double)fileStream.Length;
                        var dProgressPercentage = (dIndex / dTotal);
                        var iProgressPercentage = (int)(dProgressPercentage * 100);

                        UploadFileItem.SubItems[1].Text = "Uploading..." + iProgressPercentage.ToString(CultureInfo.InvariantCulture) + "%";
                        Application.DoEvents();

                        bytesRead = fileStream.Read(buffer, 0, _uploadBlockSize);
                    }
                    blob.PutBlockList(blockIDList);
                    UploadFileItem.SubItems[1].Text = "Completed";
                    UploadFileItem.SubItems[1].ForeColor = Color.DarkGreen;
                }
                catch (Exception ex)
                {
                    UploadFileItem.SubItems[1].Text = "Error: " + ex.Message;
                    UploadFileItem.SubItems[1].ForeColor = Color.Red;
                }
                finally
                {
                    if (fileStream != null)
                        fileStream.Close();

                    // Deletes the temporal configuration file
                    if ((xNode.Name == "ConfigurationFile") && (File.Exists(filePath)))
                        File.Delete(filePath);
                }
                
            }


        #endregion

            private void ChkEnableRDPCheckedChanged(object sender, EventArgs e)
            {
                pnlRDP.Enabled = chkEnableRDP.Checked;
                foreach (Control ctl in pnlRDP.Controls)
                {
                    ctl.Enabled = pnlRDP.Enabled;
                    errProv.SetError(ctl, null);
                }
                if (!chkEnableRDP.Checked)
                    cboCertificates.SelectedItem = null;
                cmdViewCertificate.Enabled = pnlRDP.Enabled && Certificate != null;

                // Reload packages
                ReloadDeploymentPackages();
            }

            private void CmdViewCertificateClick(object sender, EventArgs e)
            {
                if (Certificate != null)
                    X509Certificate2UI.DisplayCertificate(Certificate, this.Handle);
            }

            private void CboCertificatesSelectedIndexChanged(object sender, EventArgs e)
            {
                cmdViewCertificate.Enabled = pnlRDP.Enabled && Certificate != null;
                if (cboCertificates.SelectedIndex == cboCertificates.Items.Count - 1)                     // Launch the create certificate window
                {
                    var frm = new FrmCreateCertificate();
                    if (frm.ShowDialog() == DialogResult.Cancel)
                        cboCertificates.SelectedItem = null;
                    else
                    {
                        ReloadX509Certificates();
                        foreach (Object item in cboCertificates.Items)
                            if ((item is X509Certificate) && (((X509Certificate2) item).SerialNumber == frm.SerialNumber))
                            {
                                cboCertificates.SelectedItem = item;
                                break;
                            }                        
                    }
                }
            }

            private static string EncryptWithCertificate(string clearText, X509Certificate2 certificate)
            {
                var encoding = new UTF8Encoding();
                Byte[] clearTextsByte = encoding.GetBytes(clearText);
                var contentinfo = new ContentInfo(clearTextsByte);
                var envelopedCms = new EnvelopedCms(contentinfo);
                envelopedCms.Encrypt(new CmsRecipient(certificate));
                return Convert.ToBase64String(envelopedCms.Encode());
            }

            #region Control validation

            #region RDP settings

            void TxtRDPUserValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtRDPUser.Text.Length == 0)
                {
                    error = "Please enter a user name";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtRDPPasswordValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if ((txtRDPPassword.Text.Length == 0) ||
                    (txtRDPPassword.Text.Contains(txtRDPUser.Text)) ||
                    !Regex.Match(txtRDPPassword.Text, PasswordStrengthRegex).Success)
                {
                    error = "The password does not conform to complexity requirements. Ensure the password does not contain the user account name or parts of it. The Password should be at least six characters long and contain a mixture of upper, lower case, digits and symbols.";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtRDPConfirmPasswordValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtRDPPassword.Text != txtRDPConfirmPassword.Text)
                {
                    error = "The passwords don't match.";
                }
                errProv.SetError((Control)sender, error);
                
            }

            void CboCertificatesValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (Certificate == null)
                {
                    error = "You must select an existing certificate or create a new one";
                }
                errProv.SetError((Control)sender, error);
            }
            #endregion

        #region SQL Azure settings
            void TxtDBServerValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBServer.Visible && txtDBServer.Text.Length == 0)
                {
                    error = "Please enter your database server name (this is <databasename>.database.windows.net)";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtDBAdminUserValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBAdminUser.Text.Length == 0)
                {
                    error = "Please enter the database admin user name";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtDBAdminPasswordValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBAdminPassword.Text.Length == 0)
                {
                    error = "Please enter the database admin password";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtDBNameValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBName.Text.Length == 0)
                {
                    error = "Please enter the database name for the new database";
                }
                errProv.SetError((Control)sender, error);
            }
            void TxtDBUserValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBUser.Text.Length == 0)
                {
                    error = "Please enter the database user name for the new database";
                }
                errProv.SetError((Control)sender, error);
            }
            void TxtDBPasswordValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if ((txtDBPassword.Text.Length == 0) ||
                    (txtDBPassword.Text.Contains(txtDBUser.Text)) ||
                    !Regex.Match(txtDBPassword.Text, PasswordStrengthRegex).Success)
                {
                    error = "The password does not conform to complexity requirements. Ensure the password does not contain the user account name or parts of it. The Password should be at least six characters long and contain a mixture of upper, lower case, digits and symbols.";
                }
                errProv.SetError((Control)sender, error);
            }
            void TxtDBRePasswordValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBRePassword.Text != txtDBPassword.Text)
                {
                    error = "The passwords are not identical";
                }
                errProv.SetError((Control)sender, error);
            }

        #endregion 

        #region Windows Azure Settings
            void TxtStorageNameValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtStorageName.Text.Length == 0)
                {
                    error = "Please enter your account storage name";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtStorageKeyValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtStorageKey.Text.Length == 0)
                {
                    error = "Please enter your account storage key";
                }
                errProv.SetError((Control)sender, error);
            }
            void TxtBindingsValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtBindings.Text.Length == 0)
                {
                   // error = "Please enter the bindings for your web application. You may include 'myapp.cloudapp.net' in order to access through the Azure access portal.";
                }
                errProv.SetError((Control)sender, error);
            }                       

        #endregion

        #region Package Settings
            void TxtStorageContainerValidating(object sender, CancelEventArgs e)
            {
                // Container names must be valid DNS names, and must conform to these rules:
                // * Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.
                // * Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.
                // * All letters in a container name must be lowercase.
                // * Container names must be from 3 through 63 characters long.

                string error = null;
                if ((txtStorageContainer.Text.Length == 0) ||
                    !Regex.IsMatch(txtStorageContainer.Text, @"^[a-z0-9](([a-z0-9\-[^\-])){1,61}[a-z0-9]$"))
                {
                    error = "Please a valid container name. Container names muse be valid DNS names and must conform to these rules: ";
                    error += "Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character; ";
                    error += "Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names; ";
                    error += "All letters in a container name must be lowercase; ";
                    error += "Container names must be from 3 through 63 characters long.";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtVhdBlobNameValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtVHDBlobName.Text.Length == 0)
                {
                    error = "You must specify a blob name for the VHD";
                }
                errProv.SetError((Control)sender, error);
            }

            void TxtVhdSizeValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                int driveSize;                
                if ((txtVHDSize.Text.Length == 0) ||
                    !int.TryParse(txtVHDSize.Text, out driveSize) ||
                    (driveSize < 128) || (driveSize > 1048576))
                {
                    error = "You must specify a valid VHD size (recommended minumum: 512Mb; maximum 1Tb=1048576Mb)";
                }
                errProv.SetError((Control)sender, error);
            }

            void LstPackagesValidating(object sender, CancelEventArgs e)
            {
                string error = null;

                bool isChecked = lstPackages.Items.Cast<ListViewItem>().Any(li => li.Checked);
                if (!isChecked)
                    error = "You must select at least one package to upload to Azure Storage";

                errProv.SetError((Control)sender, error);
            }




        #endregion

            #region Azure Connect Settings
            private void LinkLabel1LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
                try
                {
                    const string connectHelpURL = "http://go.microsoft.com/fwlink/?LinkId=203834";
                    Process.Start(connectHelpURL);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void ChkAzureConnectCheckedChanged(object sender, EventArgs e)
            {
                txtConnectActivationToken.Enabled = chkAzureConnect.Checked;
            }

            void TxtConnectActivationTokenValidating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtConnectActivationToken.Text.Length == 0)
                {
                    error = "Please enter a valid Azure Connect Activation token.";
                }
                errProv.SetError((Control)sender, error);
            }    




            #endregion

            #region Subscriptions

            private void CboSubscriptionsSelectedIndexChanged(object sender, EventArgs e)
            {
                try
                {
                    if (cboSubscriptions.Text == RefreshLabel)
                    {
                        cboSubscriptions.SelectedIndex = 0;
                        var publishSettings = DotNetNuke.Azure.Accelerator.Components.Utils.GetWAPublishingSettings();
                        if (publishSettings != null)
                        {
                            PublishSettings = publishSettings;
                            PublishSettings.Save(PublishSettingsFilename);
                            RefreshSubscriptions();
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void CboSubscriptionsValidating(object sender, CancelEventArgs e)
            {
                try
                {
                    string error = null;
                    if (cboSubscriptions.Text == string.Empty)
                    {
                        error = "You must select an existing subscription";
                    }
                    errProv.SetError((Control)sender, error);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
            #endregion

            private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
            {
                try
                {
                    Process.Start("http://www.windowsazure.com/en-us/pricing/free-trial/");
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            #endregion

        #region Azure Mangement API calls

            private IServiceManagement _serviceManager;
            internal IServiceManagement ServiceManager
            {
                get
                {
                    return _serviceManager ??
                           (_serviceManager =
                            ServiceManagementHelper.CreateServiceManagementChannel("WindowsAzureEndPoint", PublishSettings.Certificate));
                }
                set { _serviceManager = value; }
            }

            private IDatabaseManagement _databaseManager;
            internal IDatabaseManagement DatabaseManager
            {
                get
                {
                    return _databaseManager ??
                           (_databaseManager =
                            ServiceManagementHelper.CreateDatabaseManagementChannel("SQLAzureEndPoint", PublishSettings.Certificate));
                }
                set { _databaseManager = value; }
            }

            private PublishSettings PublishSettings { get; set; }
            internal string PublishSettingsFilename
            {
                get
                {
                    return (Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),
                                         Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".publishsettings"));
                }
            }
        internal Subscription Subscription
        {
            get { return (Subscription) cboSubscriptions.SelectedItem; }
        }

        private void RefreshHostedServices()
        {
            
            cboHostingService.Items.Clear();
            cboHostingService.Items.Add("Loading...");
            cboHostingService.SelectedIndex = 0;
            cboHostingService.Enabled = false;
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                var hostedServices = ServiceManager.ListHostedServices(Subscription.SubscriptionId);                
                cboHostingService.Tag = hostedServices;
                cboHostingService.Items.Clear();
                foreach (var hostedService in hostedServices)
                {
                    cboHostingService.Items.Add(hostedService.ServiceName);
                }            
            }
            catch (Exception ex)
            {
                cboHostingService.Tag = null;
                LogException(ex);
            }
            finally
            {
                cboHostingService.Enabled = true;
                if (cboHostingService.Items.Count == 0)
                    cboHostingService.Items.Add("");

                cboHostingService.SelectedIndex = 0;
                cboHostingService.Items.Add(CreateNewLabel);
                cboHostingService.Items.Add(RefreshLabel);
                Cursor = Cursors.Default;
            }
            Application.DoEvents();

        }

        private void RefreshStorageAccounts()
        {

            cboStorage.Items.Clear();
            cboStorage.Items.Add("Loading...");
            cboStorage.SelectedIndex = 0;
            cboStorage.Enabled = false;            
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                var storageAccounts = ServiceManager.ListStorageServices(Subscription.SubscriptionId);
                cboStorage.Tag = storageAccounts;
                cboStorage.Items.Clear();
                foreach (var storageAccount in storageAccounts)
                {
                    cboStorage.Items.Add(storageAccount.ServiceName);
                }
            }
            catch (Exception ex)
            {
                cboStorage.Tag = null;
                LogException(ex);
            }
            finally
            {
                cboStorage.Enabled = true;
                if (cboStorage.Items.Count == 0)
                    cboStorage.Items.Add("");

                cboStorage.SelectedIndex = 0;
                cboStorage.Items.Add(CreateNewLabel);
                cboStorage.Items.Add(RefreshLabel);
                Cursor = Cursors.Default;
            }
            Application.DoEvents();
        }

        private void RefreshDatabaseServers()
        {

            cboDatabase.Items.Clear();
            cboDatabase.Items.Add("Loading...");
            cboDatabase.SelectedIndex = 0;
            cboDatabase.Enabled = false;
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                var databaseServers = DatabaseManager.ListDatabaseServers(Subscription.SubscriptionId);
                cboDatabase.Tag = databaseServers;
                cboDatabase.Items.Clear();
                foreach (var databaseServer in databaseServers)
                {
                    cboDatabase.Items.Add(databaseServer.Name);
                }
            }
            catch (Exception ex)
            {
                cboDatabase.Tag = null;
                LogException(ex);
            }
            finally
            {
                cboDatabase.Enabled = true;
                if (cboDatabase.Items.Count == 0)
                    cboDatabase.Items.Add("");

                cboDatabase.SelectedIndex = 0;
                cboDatabase.Items.Add(CreateNewLabel);
                cboDatabase.Items.Add(RefreshLabel);
                Cursor = Cursors.Default;
            }
            Application.DoEvents();
        }

        //private void RefreshContainers()
        //{
        //    cboContainers.Items.Clear();
        //    cboContainers.Items.Add("Loading...");
        //    cboContainers.SelectedIndex = 0;
        //    cboContainers.Enabled = false;
        //    Cursor = Cursors.WaitCursor;
        //    Application.DoEvents();
        //    try
        //    {
        //        if (cboStorage.Tag != null)
        //        {
        //            var storageAccounts = (List<StorageService>) cboStorage.Tag;
        //            var storageAccount = storageAccounts.FirstOrDefault(x => x.ServiceName == cboStorage.Text);
        //            if (storageAccount != null)
        //            {
        //                var storageKeys = ServiceManager.GetStorageKeys(Subscription.SubscriptionId,
        //                                                                storageAccount.ServiceName);
        //                var primaryKey = storageKeys.StorageServiceKeys.Primary;
        //                var credentials = new StorageCredentialsAccountAndKey(storageAccount.ServiceName, primaryKey);
        //                var storage = new CloudStorageAccount(credentials, true);
        //                var blobClient = new CloudBlobClient(storage.BlobEndpoint.AbsoluteUri, credentials);
        //                var containers = blobClient.ListContainers();
                        
        //                cboContainers.Items.Clear();
        //                // Only add private containers
        //                bool defaultContainerExists = false;
        //                foreach (var container in containers.Where(container => container.GetPermissions().PublicAccess == BlobContainerPublicAccessType.Off))
        //                {
        //                    if (container.Name == "dotnetnuke-packages")
        //                        defaultContainerExists = true;
        //                    cboContainers.Items.Add(container.Name);
        //                }
        //                if (!defaultContainerExists)
        //                {
        //                    var container = blobClient.GetContainerReference("dotnetnuke-packages");
        //                    container.CreateIfNotExist();
        //                    container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off });
        //                    cboContainers.Items.Add("dotnetnuke-packages");
        //                }
        //            }
        //            else
        //            {
        //                cboContainers.Items.Clear();
        //            }
        //        }
        //        else
        //            cboContainers.Items.Clear();
        //    }
        //    catch (Exception ex)
        //    {
        //        cboContainers.Tag = null;
        //        LogException(ex);
        //    }
        //    finally
        //    {
        //        cboContainers.Enabled = true;
        //        if (cboContainers.Items.Count == 0)
        //            cboContainers.Items.Add("");
        //        try
        //        {
        //            cboContainers.Text = "dotnetnuke-packages";
        //        }
        //        catch
        //        {
        //            cboContainers.SelectedIndex = 0;
        //        }
                
        //        cboContainers.Items.Add(CreateNewLabel);
        //        cboContainers.Items.Add(RefreshLabel);
        //        Cursor = Cursors.Default;
        //    }
        //    Application.DoEvents();            
        //}

        #endregion

        private void CboHostingServiceSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboHostingService.Text == RefreshLabel)
                    RefreshHostedServices();
                if (cboHostingService.Text == CreateNewLabel)
                {
                    cboHostingService.Text = "";
                    var frm = new FrmNewHostedService {Wizard = this};
                    if (frm.ShowDialog() == DialogResult.OK)
                        RefreshHostedServices();
                    cboHostingService.SelectedValue = frm.ServiceName;
                }

            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CboHostingServiceOnValidating(object sender, CancelEventArgs cancelEventArgs)
        {
            string error = null;
            if (cboHostingService.Text == string.Empty || cboHostingService.Text.Contains("."))
            {
                error = "Please select or create a hosting service";
            }
            errProv.SetError((Control)sender, error);
        }

        private void CboStorageOnValidating(object sender, CancelEventArgs cancelEventArgs)
        {
            string error = null;
            if (cboStorage.Text == string.Empty || cboStorage.Text.Contains("."))
            {
                error = "Please select or create a storage account";
            }
            errProv.SetError((Control)sender, error);            
        }


        private void CboStorageSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboStorage.Text == RefreshLabel)
                {
                    RefreshStorageAccounts();
                    return;
                }
                    
                if (cboStorage.Text == CreateNewLabel)
                {
                    cboStorage.Text = "";
                    var frm = new FrmNewStorageAccount { Wizard = this };
                    if (frm.ShowDialog() == DialogResult.OK)
                        RefreshStorageAccounts();
                    cboStorage.SelectedValue = frm.ServiceName;
                    return;
                }
                //ThreadPool.QueueUserWorkItem(o => RefreshContainers());

            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TxtPackagesContainerOnValidating(object sender, CancelEventArgs cancelEventArgs)
        {
            string error = null;

           if (!Regex.IsMatch(txtPackagesContainer.Text, @"^[a-z0-9](([a-z0-9\-[^\-])){1,61}[a-z0-9]$"))
                error = "Invalid container name. Container names must be valid DNS names, and must conform to these rules: \n - Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.\n- Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.\n- All letters in a container name must be lowercase.\n- Container names must be from 3 through 63 characters long.";
            errProv.SetError((Control)sender, error);                   
        }

        private void TxtVhdNameOnValidating(object sender, CancelEventArgs cancelEventArgs)
        {
            string error = null;
            if (txtVHDName.Text.Length == 0)
            {
                error = "You must specify a blob name for the VHD";
            }
            errProv.SetError((Control)sender, error);
        }

        private void TxtVhdDriveSizeOnValidating(object sender, CancelEventArgs cancelEventArgs)
        {
            string error = null;
            int driveSize;
            if ((txtVHDDriveSize.Text.Length == 0) ||
                !int.TryParse(txtVHDDriveSize.Text, out driveSize) ||
                (driveSize < 128) || (driveSize > 1048576))
            {
                error = "You must specify a valid VHD size (recommended minumum: 512Mb; maximum 1Tb=1048576Mb)";
            }
            errProv.SetError((Control)sender, error);
        }

        private void CboDatabaseSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboDatabase.Text == RefreshLabel)
                {
                    RefreshDatabaseServers();
                    return;
                }

                if (cboDatabase.Text == CreateNewLabel)
                {
                    cboDatabase.Text = "";
                    var frm = new FrmNewDatabaseServer { Wizard = this };
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        RefreshDatabaseServers();
                        cboDatabase.SelectedValue = frm.ServerName;
                        txtDBAdminUser.Text = frm.AdminUser;
                        txtDBAdminPassword.Text = frm.AdminPassword;
                    }
                        
                    
                    return;
                }
                //ThreadPool.QueueUserWorkItem(o => RefreshContainers());

            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }




        //private void cboContainers_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (cboContainers.Text == RefreshLabel)
        //        {
        //            ThreadPool.QueueUserWorkItem(o => RefreshContainers());
        //            return;
        //        }

        //        if (cboContainers.Text == CreateNewLabel)
        //        {
        //            cboStorage.Text = "";
        //            var frm = new FrmNewContainer { Wizard = this };
        //            if (frm.ShowDialog() == DialogResult.OK)
        //                RefreshContainers();
        //            cboStorage.SelectedValue = frm.ContainerName;
        //            return;
        //        }                
        //    }
        //    catch (Exception ex)
        //    {
        //        LogException(ex);
        //    }
        //}

    }
}
