using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;


namespace DNNAzureWizard
{
    public partial class FrmDNNAzureWizard : Form
    {
        /* The following expression to validate a pwd of 6 to 16 characters and contain three of the following 4 items: 
         * upper case letter, lower case letter, a symbol, a number
         * An explanation of individual components:
         *      (?=^[^\s]{6,16}$) - contain between 8 and 16 non-whitespace characters
         *      (?=.*?\d) - contains 1 numeric
         *      (?=.*?[A-Z]) - contains 1 uppercase character
         *      (?=.*?[a-z]) - contains 1 lowercase character
         *      (?=.*?[^\w\d\s]) - contains 1 symbol
         */
        private string PasswordStrengthRegex = @"(?=^[^\s]{6,16}$)((?=.*?\d)(?=.*?[A-Z])(?=.*?[a-z])|(?=.*?\d)(?=.*?[^\w\d\s])(?=.*?[a-z])|(?=.*?[^\w\d\s])(?=.*?[A-Z])(?=.*?[a-z])|(?=.*?\d)(?=.*?[A-Z])(?=.*?[^\w\d\s]))^.*";

        private enum WizardTabs
        {
            tabHome = 1,
            tabSQLAzureSettings = 2,
            tabWindowsAzureSettings = 3,
            tabRDPAzureSettings = 4,
            tabConnectSettings = 5,
            tabPackages = 6,
            tabSummary = 7,
            tabUploading = 8,
            tabFinish = 9
        }


        Process p = null;

        public FrmDNNAzureWizard()
        {
            InitializeComponent();
        }

        #region " Event Handlers "

            private void FrmDNNAzureWizard_Load(object sender, EventArgs e)
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

            private void btnCancel_Click(object sender, EventArgs e)
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

            private void btnBack_Click(object sender, EventArgs e)
            {
                try
                {
                    if (this.ActivePageIndex() == (int) WizardTabs.tabFinish)
                    {
                        btnOK.Text = "Next >";
                        btnCancel.Enabled = true;
                        MoveSteps(-2);
                    }
                    else
                        MoveSteps(-1);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void btnOK_Click(object sender, EventArgs e)
            {
                try
                {
                    if (this.ActivePageIndex() == (int) WizardTabs.tabFinish) // Finish
                    {
                        btnCancel_Click(sender, e);
                        return;
                    }

                    UseWaitCursor = true;
                    if (ValidateStep())
                        MoveSteps(1);
                    if (this.ActivePageIndex() == (int) WizardTabs.tabUploading)
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
                catch (Exception ex)
                {
                    LogException(ex);
                }
                btnBack.Enabled = true;
                btnOK.Enabled = true;
                UseWaitCursor = false;
            }

            private string GetFinalLog()
            {
                bool Success = true;
                System.Text.StringBuilder log = new System.Text.StringBuilder("====================== UPLOAD LOG ======================");
                log.AppendLine("");
                foreach (ListViewItem li in lstTasks.Items)
                {
                    log.AppendLine("- " + li.Text + ": " + li.SubItems[1].Text);
                    if (li.SubItems[1].ForeColor != Color.DarkGreen)
                        Success = false;
                }
                if (!Success)
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

            private void btnTestDB_Click(object sender, EventArgs e)
            {
                try
                {
                    var cnStr = new System.Data.SqlClient.SqlConnectionStringBuilder();
                    cnStr.Add("Data Source", txtDBServer.Text + ".database.windows.net");
                    cnStr.Add("Initial Catalog", "master");
                    cnStr.Add("User ID", txtDBAdminUser.Text);
                    cnStr.Add("Password", txtDBAdminPassword.Text);
                    cnStr.Add("Trusted_Connection", "False");
                    cnStr.Add("Encrypt", "True");

                    using (var cn = new System.Data.SqlClient.SqlConnection(cnStr.ConnectionString.ToString()))
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

            private void btnTestStorage_Click(object sender, EventArgs e)
            {
                try
                {
                    Microsoft.WindowsAzure.StorageCredentialsAccountAndKey credentials = new Microsoft.WindowsAzure.StorageCredentialsAccountAndKey(txtStorageName.Text, txtStorageKey.Text);
                    Microsoft.WindowsAzure.CloudStorageAccount storage = new Microsoft.WindowsAzure.CloudStorageAccount(credentials, chkStorageHTTPS.Checked);
                    Uri blobURI = storage.BlobEndpoint;
                    var cloudTableClient = new Microsoft.WindowsAzure.StorageClient.CloudBlobClient(storage.BlobEndpoint.AbsoluteUri, credentials);
                    IEnumerable<Microsoft.WindowsAzure.StorageClient.CloudBlobContainer> containers = cloudTableClient.ListContainers();
                    int totalContainers = containers.Count();
                    MessageBox.Show("Test connection successfull", "Test connection successfull", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void txtStorageName_TextChanged(object sender, EventArgs e)
            {
                lblStTest.Text = string.Format("Test credentials to http{0}://{1}.blob.windows.core.net", (chkStorageHTTPS.Checked ? "s" : ""), txtStorageName.Text);
            }

            private void chkStorageHTTPS_CheckedChanged(object sender, EventArgs e)
            {
                lblStTest.Text = string.Format("Test credentials to http{0}://{1}.blob.windows.core.net", (chkStorageHTTPS.Checked ? "s" : ""), txtStorageName.Text);
            }

            void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                try
                {
                    Application.DoEvents();
                    txtLOG.AppendText(e.Data + System.Environment.NewLine);
                }
                catch { }
            }

        #endregion

        #region " Wizard utilities "
            private void CleanUp()
            {

                // Is the upload process running? If yes and after user confirmation, kill all the process tree before exit
                if (p != null)
                {
                    if (!p.HasExited && (MessageBox.Show("The upload process is still running. Are you sure that you want to cancel?", "Cancel upload", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                    {
                        try
                        {
                            ProcessUtility.KillTree(p.Id);
                            p = null;
                        }
                        catch { }
                    }
                }

                // Deletes temporary generated configuration files 
                try
                {
                    System.IO.File.Delete(Environment.CurrentDirectory + "\\ServiceConfiguration.cscfg");
                }
                catch { };
                try
                {
                    System.IO.File.Delete(Environment.CurrentDirectory + "\\bin\\ServiceConfiguration.cscfg");
                }
                catch { };
            }

            private FileVersionInfo _VersionInfo = null;
            private FileVersionInfo VersionInfo
            {
                get
                {
                    if (_VersionInfo == null)
                    {
                        System.Reflection.Assembly oAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        _VersionInfo = FileVersionInfo.GetVersionInfo(oAssembly.Location);
                    }
                    return _VersionInfo;
                }
            }

            private void RefreshUI()
            {
                
                this.Text += " (" + VersionInfo.ProductVersion + ")";
#if DEBUG
                this.Text += " - (Debug)";
#endif
                InitializePages();                
                ShowPage((int) WizardTabs.tabHome);
                SetupAppSettings();
                ReloadDeploymentPackages();
                ReloadX509Certificates();
            }

            private static void LogException(Exception ex)
            {
                string msg = ex.Message;
#if DEBUG
                msg += " - Stack trace: " + ex.StackTrace.ToString();
#endif
                MessageBox.Show(msg, "An exception ocurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            private void ShowPage(int PageNumber)
            {
                foreach (Panel p in this.pnl.Controls)
                    p.Visible = p.Name == "pnl" + PageNumber;
                btnBack.Enabled = PageNumber > 1;
            }
            private void InitializePages()
            {
                foreach (Panel p in this.pnl.Controls)
                {
                    p.Dock = DockStyle.Fill;
                    p.Visible = false;
                }
                chkEnableRDP_CheckedChanged(null, null);
                chkAzureConnect_CheckedChanged(null, null);
            }
            private int ActivePageIndex()
            {
                foreach (Panel p in this.pnl.Controls)
                    if (p.Visible)
                        return int.Parse(p.Name.Replace("pnl", ""));
                return 0;
            }
            private void MoveSteps(int Steps)
            {
                ShowPage(ActivePageIndex() + Steps);
            }
            private bool ValidateStep()
            {
                switch (this.ActivePageIndex())
                {
                    case (int) WizardTabs.tabHome: // Home tab, nothing to validate
                        return true;
                    case (int) WizardTabs.tabSQLAzureSettings: // SQL Azure tab
                        return ValidateSQLAzureSettings();                        
                    case (int) WizardTabs.tabWindowsAzureSettings: // Windows Azure tab
                        return ValidateAzureSettings();                        
                    case (int) WizardTabs.tabRDPAzureSettings: // RDP tab
                        return ValidateRDPSettings();                       
                    case (int) WizardTabs.tabConnectSettings: // Virtual Network tab
                        return ValidateConnectSettings();
                    case (int) WizardTabs.tabPackages: // Deployment packages
                        bool validated= ValidatePackagesSelectionSettings();
                        txtConfig.Text = GetSettingsSummary();
                        return validated;
                    case (int) WizardTabs.tabSummary:
                        return (MessageBox.Show("The wizard will begin now to deploy DotNetNuke on Windows Azure with the specified settings. Are you sure that you want to continue?", "Deploy", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK);
                    default:
                        return false;
                }
            }

            private bool ValidateConnectSettings()
            {
                bool invalidInput = false;
                if (chkAzureConnect.Checked)
                {
                    txtConnectActivationToken_Validating(txtConnectActivationToken, null);
                    foreach (Control control in this.pnlAzureConnect.Controls)
                    {
                        if (this.errProv.GetError(control).Length != 0)
                        {
                            invalidInput = true;
                            break;
                        }
                    }                    
                }
                return !invalidInput;
            }

            private bool ValidateRDPSettings()
            {
                bool invalidInput = false;
                if (chkEnableRDP.Checked)
                {
                    txtRDPUser_Validating(txtRDPUser, null);
                    txtRDPPassword_Validating(txtRDPPassword, null);
                    txtRDPConfirmPassword_Validating(txtRDPConfirmPassword, null);
                    cboCertificates_Validating(cboCertificates, null);

                    foreach (Control control in this.pnlRDP.Controls)
                    {
                        if (this.errProv.GetError(control).Length != 0)
                        {
                            invalidInput = true;
                            break;
                        }
                    }
                }
                return !invalidInput;
            }

            private string GetSettingsSummary()
            {
                System.Text.StringBuilder summary = new System.Text.StringBuilder("======================= SUMMARY OF SETTINGS =======================");
                summary.AppendLine("");
                summary.AppendLine("DATABASE SETTINGS:");
                summary.AppendLine("- DB Server Name: " +  txtDBServer.Text.Trim() + ".database.windows.net");
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
                    summary.AppendLine("- Password: " + EncryptWithCertificate(txtRDPPassword.Text, this.Certificate));
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
                return summary.ToString(); ;
            }

            private bool ValidateSQLAzureSettings()
            {
                bool invalidInput = false;
                txtDBServer_Validating(txtDBServer, null);                
                txtDBAdminUser_Validating(txtDBAdminUser, null);
                txtDBAdminPassword_Validating(txtDBAdminPassword, null);
                txtDBName_Validating(txtDBName, null);
                txtDBUser_Validating(txtDBUser, null);
                txtDBPassword_Validating(txtDBPassword, null);
                txtDBRePassword_Validating(txtDBRePassword, null);
                foreach (Control control in this.DBSettings.Controls)
                {
                    if (this.errProv.GetError(control).Length != 0)
                    {
                        invalidInput = true;
                        break;
                    }
                }
                return !invalidInput;
            }

            private bool ValidateAzureSettings()
            {
                bool invalidInput = false;
                txtStorageName_Validating(txtStorageName, null);
                txtStorageKey_Validating(txtStorageKey, null);
                txtBindings_Validating(txtBindings, null);
                foreach (Control control in this.AzureSettings.Controls)
                {
                    if (this.errProv.GetError(control).Length != 0)
                    {
                        invalidInput = true;
                        break;
                    }
                }
                return !invalidInput;
            }

            private bool ValidatePackagesSelectionSettings()
            {
                bool invalidInput = false;
                lstPackages_Validating(lstPackages, null);
                txtStorageContainer_Validating(txtStorageContainer, null);
                txtVHDBlobName_Validating(txtVHDBlobName, null);
                txtVHDSize_Validating(txtVHDSize, null);
                foreach (Control control in this.PackageSettings.Controls)
                {
                    if (this.errProv.GetError(control).Length != 0)
                    {
                        invalidInput = true;
                        break;
                    }
                }
                return !invalidInput;
            }

            private string ReplaceTokens(string CfgStr)
            {
                // Replace the tokens - SQL Azure settings
                CfgStr = CfgStr.Replace("@@DBSERVER@@", txtDBServer.Text.Trim());
                CfgStr = CfgStr.Replace("@@DBADMINUSER@@", txtDBAdminUser.Text.Trim());
                CfgStr = CfgStr.Replace("@@DBADMINPASSWORD@@", txtDBAdminPassword.Text);
                CfgStr = CfgStr.Replace("@@DBNAME@@", txtDBName.Text.Trim());
                CfgStr = CfgStr.Replace("@@DBUSER@@", txtDBUser.Text.Trim());
                CfgStr = CfgStr.Replace("@@DBPASSWORD@@", txtDBPassword.Text);

                // Replace the tokens - Windows Azure settings
                CfgStr = CfgStr.Replace("@@STORAGEPROTOCOL@@", "http" + (chkStorageHTTPS.Checked ? "s" : ""));
                CfgStr = CfgStr.Replace("@@STORAGEACCOUNTNAME@@", txtStorageName.Text.Trim());
                CfgStr = CfgStr.Replace("@@STORAGEKEY@@", txtStorageKey.Text.Trim());

                // Replace the tokens - IIS settings
                CfgStr = CfgStr.Replace("@@HOSTHEADERS@@", txtBindings.Text.Trim());

                // Replace the tokens - Paths
                CfgStr = CfgStr.Replace("@@APPPATH@@", Environment.CurrentDirectory + '\\');
                CfgStr = CfgStr.Replace("@@PACKAGECONTAINER@@", txtStorageContainer.Text.Trim().ToLower());

                // Replace the tokens - VHD settings
                CfgStr = CfgStr.Replace("@@VHDBLOBNAME@@", txtVHDBlobName.Text.Trim().ToLower());
                int driveSize = 0;
                int.TryParse(txtVHDSize.Text, out driveSize);
                CfgStr = CfgStr.Replace("@@VHDBLOBSIZE@@", txtVHDSize.Text.Trim().ToLower());    
            
                // Replace the tokens - RDP settings
                if (chkEnableRDP.Checked)
                {
                    CfgStr = CfgStr.Replace("@@RDPENABLED@@", "true");
                    CfgStr = CfgStr.Replace("@@RDPUSERNAME@@", txtRDPUser.Text.Trim());
                    CfgStr = CfgStr.Replace("@@RDPPASSWORD@@", EncryptWithCertificate(txtRDPPassword.Text, this.Certificate));
                    CfgStr = CfgStr.Replace("@@RDPEXPIRATIONDATE@@", cboRDPExpirationDate.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"));                    
                }
                else
                {
                    CfgStr = CfgStr.Replace("@@RDPENABLED@@", "false");
                    CfgStr = CfgStr.Replace("@@RDPUSERNAME@@", "");
                    CfgStr = CfgStr.Replace("@@RDPPASSWORD@@", "");
                    CfgStr = CfgStr.Replace("@@RDPEXPIRATIONDATE@@", System.DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"));
                }

                // Replace the tokens - Virtual Network settings
                if (chkAzureConnect.Checked)
                    CfgStr = CfgStr.Replace("@@CONNECTACTIVATIONTOKEN@@", txtConnectActivationToken.Text.Trim());
                else
                    CfgStr = CfgStr.Replace("@@CONNECTACTIVATIONTOKEN@@", "");

                // Replace the tokens - Certificate settings
                if (chkEnableRDP.Checked && (Certificate != null))
                    CfgStr = CfgStr.Replace("@@RDPTHUMBPRINT@@", Certificate.Thumbprint);    
                else
                    CfgStr = CfgStr.Replace("@@RDPTHUMBPRINT@@", "");                    


                return CfgStr;

            }

            private string ReplaceFileTokens(string FilePath)
            {
                string CfgStr = "";
                using (System.IO.TextReader mConfig = System.IO.File.OpenText(FilePath))
                    CfgStr = mConfig.ReadToEnd();
                return ReplaceTokens(CfgStr);
            }
            
            private string ProcessConfigFile(string configfilename)
            {
                return ReplaceFileTokens(Environment.CurrentDirectory + "\\config\\" + configfilename);
            }


        /// <summary>
        /// Prepares the list of tasks for uploading contents to Windows Azure
        /// </summary>
            private void PrepareTasks()
            {
                lstTasks.Items.Clear();                
                XmlDocument xtasks = new XmlDocument();
                xtasks.Load(Environment.CurrentDirectory + "\\config\\DeploymentTasks.xml");

                foreach (XmlNode task in xtasks.SelectNodes("/DeploymentTasks/Task"))
                {
                    if (task.Attributes["type"].InnerText == "UploadPackages")
                    {
                        foreach (ListViewItem p in lstPackages.Items)
                        {
                            if (p.Checked)
                            {
                                XmlNode pNode = (XmlNode) p.Tag;
                                ListViewItem li = new ListViewItem(new string[] { "Upload service configuration file '" + pNode.SelectSingleNode("ConfigurationFile").InnerText + "'", "Pending" });
                                li.Tag = pNode.SelectSingleNode("ConfigurationFile");
                                li.UseItemStyleForSubItems = false;
                                lstTasks.Items.Add(li);
                                li = new ListViewItem(new string[] { "Upload service package file '" + pNode.SelectSingleNode("PackageFile").InnerText + "'", "Pending" });
                                li.Tag = pNode.SelectSingleNode("PackageFile");
                                li.UseItemStyleForSubItems = false;
                                lstTasks.Items.Add(li);

                            }
                        }
                    }
                    else
                    {
                        ListViewItem li = new ListViewItem(new string[] { task.SelectSingleNode("Description").InnerText, "Pending" });
                        li.Tag = task;
                        li.UseItemStyleForSubItems = false;
                        lstTasks.Items.Add(li);
                    }
                }
            }

        /// <summary>
        /// Setup the environment variables for the subsequent command line calls 
        /// </summary>
        /// <param name="vars"></param>
            private void SetupEnvironmentVariables(XmlNode vars)
            {
                foreach (XmlNode var in vars.SelectNodes("Action/Variable"))
                    Environment.SetEnvironmentVariable(var.Attributes["name"].InnerText, ReplaceTokens(var.Attributes["value"].InnerText), EnvironmentVariableTarget.Process);

            }
            private void RemoveEnvironmentVariables(XmlNode vars)
            {
                try
                {
                    foreach (XmlNode var in vars.SelectNodes("Action/Variable"))
                        Environment.SetEnvironmentVariable(var.Attributes["name"].InnerText, "", EnvironmentVariableTarget.Process);
                }
                catch { };
            }


            private void ProcessTasks()
            {
                XmlNode EnvironmentVariables = null;
                foreach (ListViewItem task in lstTasks.Items)
                {
                    try
                    {
                        XmlNode xTag = (XmlNode)task.Tag;
                        if (xTag.Name == "Task")
                        {
                            switch (xTag.Attributes["type"].InnerText)
                            {
                                case "SetupVariables":
                                    task.SubItems[1].Text = "Running...";
                                    Application.DoEvents();
                                    EnvironmentVariables = xTag;
                                    SetupEnvironmentVariables(xTag);
                                    task.SubItems[1].Text = "Completed";
                                    task.SubItems[1].ForeColor = Color.DarkGreen;
                                    break;
                                case "CommandLineAction":
                                    task.SubItems[1].Text = "Running...";
                                    Application.DoEvents();
                                    CheckForIllegalCrossThreadCalls = false;
                                    string CommandLine = ReplaceTokens(xTag.SelectSingleNode("Action/CommandLine").InnerText);
                                    string Parameters = ReplaceTokens(xTag.SelectSingleNode("Action/Parameters").InnerText);
                                    ExecuteCommand(CommandLine, Parameters);
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
                                default:
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
                if (EnvironmentVariables != null)
                    RemoveEnvironmentVariables(EnvironmentVariables);
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
                p = new Process();
                
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                
                p.Start();
                p.WaitForExit();

                int exitCode = p.ExitCode;
                string errorDesc = p.StandardError.ReadToEnd();
                string outputDesc = p.StandardOutput.ReadToEnd();
                
                p.Close();
                p = null;

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
                chkStorageHTTPS_CheckedChanged(null, null);
                txtStorageContainer.Text = ConfigurationManager.AppSettings["AzureStoragePackageContainer"];
                txtBindings.Text = ConfigurationManager.AppSettings["Bindings"];

                txtVHDBlobName.Text = ConfigurationManager.AppSettings["VHDBlobBName"];
                txtVHDSize.Text = ConfigurationManager.AppSettings["VHDSizeInMb"];

                uploadBlockSize = Convert.ToInt32(ConfigurationManager.AppSettings["UploadBlockSize"]);

                chkEnableRDP.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["RDPEnabled"]);
                txtRDPUser.Text = ConfigurationManager.AppSettings["RDPUser"];
                txtRDPPassword.Text = ConfigurationManager.AppSettings["RDPPassword"];
                txtRDPConfirmPassword.Text = txtRDPPassword.Text;
                chkEnableRDP_CheckedChanged(null, null);

                chkAzureConnect.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["ConnectEnabled"]);
                txtConnectActivationToken.Text = ConfigurationManager.AppSettings["ConnectActivationToken"];
                chkAzureConnect_CheckedChanged(null, null);

                cboRDPExpirationDate.Value = System.DateTime.Now.Date.AddMonths(1);
            }

        /// <summary>
        /// Reads all the definition packages files from ./packages folder and loads them into the packages listview
        /// </summary>
            public void ReloadDeploymentPackages()
            {
                lstPackages.Items.Clear();
                foreach (FileInfo filedef in new System.IO.DirectoryInfo(Environment.CurrentDirectory + "\\packages").GetFiles("*.xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filedef.FullName);
                    bool IsRDPPackage = ((doc.SelectSingleNode("/ServicePackage/RDPEnabled") != null) && Convert.ToBoolean(doc.SelectSingleNode("/ServicePackage/RDPEnabled").InnerText));

                    if ((chkEnableRDP.Checked && IsRDPPackage) || (!chkEnableRDP.Checked && !IsRDPPackage))
                    {
                        ListViewItem li = new ListViewItem(new string[] { doc.SelectSingleNode("/ServicePackage/Name").InnerText, doc.SelectSingleNode("/ServicePackage/Description").InnerText });
                        li.Tag = doc.SelectSingleNode("ServicePackage");
                        li.Checked = true;
                        lstPackages.Items.Add(li);
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
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Read Certificates
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=DNN Azure Accelerator", false);
                foreach (X509Certificate cert in certs)
                    cboCertificates.Items.Add(cert);

                /* certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=" + Environment.UserName, false);
                foreach (X509Certificate cert in certs)
                    cboCertificates.Items.Add(cert);
                certs = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=Windows Azure Tools", false);                
                foreach (X509Certificate cert in certs)
                    cboCertificates.Items.Add(cert);*/

                cboCertificates.Items.Add("<Create new...>");
            }

            private X509Certificate2 Certificate
            {
                get
                {
                    if ((cboCertificates.SelectedItem != null) && (cboCertificates.SelectedItem is X509Certificate))
                        return (X509Certificate2)cboCertificates.SelectedItem;
                    else
                        return null;
                }
            }


        #endregion



        #region " Background upload worker "

            private int uploadBlockSize = 0;
            

            private string GetParsedConfigurationFilePath(string filePath)
            {
                string TmpFile = System.IO.Path.GetTempFileName();
                System.IO.File.WriteAllText(TmpFile, ReplaceFileTokens(filePath));
                return TmpFile;
            }


            /// <summary>
            /// Upload blobs tasks to Windows Azure 
            /// </summary>
            /// <param name="UploadFileItem">Task list view item</param>
            private void UploadItemToAzure(ListViewItem UploadFileItem)
            {
                int totalBytesRead = 0;
                long totalFileSize = 0;

                string filePath = "";
                string containerName = "";
                string blobName = "";
                XmlNode xNode = (XmlNode)UploadFileItem.Tag;
                switch (xNode.Name)
                {
                    case "Task":
                        filePath = ReplaceTokens(xNode.SelectSingleNode("Action/SourceFile").InnerText);
                        string destinationPath = ReplaceTokens(xNode.SelectSingleNode("Action/DestinationBlob").InnerText);
                        containerName = System.IO.Path.GetDirectoryName(destinationPath);
                        blobName = System.IO.Path.GetFileName(destinationPath);
                        break;
                    case "PackageFile":
                        filePath = Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText;
                        containerName = txtStorageContainer.Text.Trim().ToLower();
                        blobName = System.IO.Path.GetFileName(filePath);
                        break;
                    case "ConfigurationFile":
                        filePath = GetParsedConfigurationFilePath(Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText);
                        containerName = txtStorageContainer.Text.Trim().ToLower();
                        blobName = System.IO.Path.GetFileName(Environment.CurrentDirectory + "\\packages\\" + xNode.InnerText);
                        break;
                    default:
                        break;
                }
             


                UploadFileItem.SubItems[1].Text = "Connecting...";
                CloudStorageAccount account = new CloudStorageAccount(new StorageCredentialsAccountAndKey(txtStorageName.Text.Trim(), txtStorageKey.Text.Trim()), chkStorageHTTPS.Checked);
                CloudBlobClient blobClient = new CloudBlobClient(account.BlobEndpoint.AbsoluteUri, account.Credentials);
                
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                container.CreateIfNotExist();


                CloudBlockBlob blob = container.GetBlobReference(blobName).ToBlockBlob;
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
                    byte[] buffer = new byte[uploadBlockSize];
                    // Keep a list of block IDs.
                    List<string> blockIDList = new List<string>();
                    // Read the first block.
                    bytesRead = fileStream.Read(buffer, 0, uploadBlockSize);
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
                        double dIndex = (double)(totalBytesRead);
                        double dTotal = (double)fileStream.Length;
                        double dProgressPercentage = (dIndex / dTotal);
                        int iProgressPercentage = (int)(dProgressPercentage * 100);

                        UploadFileItem.SubItems[1].Text = "Uploading..." + iProgressPercentage.ToString() + "%";
                        Application.DoEvents();

                        bytesRead = fileStream.Read(buffer, 0, uploadBlockSize);
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
                    if ((xNode.Name == "ConfigurationFile") && (System.IO.File.Exists(filePath)))
                        System.IO.File.Delete(filePath);
                }
                
            }


        #endregion

            private void chkEnableRDP_CheckedChanged(object sender, EventArgs e)
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

            private void cmdViewCertificate_Click(object sender, EventArgs e)
            {
                if (Certificate != null)
                    X509Certificate2UI.DisplayCertificate(Certificate, this.Handle);
            }

            private void cboCertificates_SelectedIndexChanged(object sender, EventArgs e)
            {
                cmdViewCertificate.Enabled = pnlRDP.Enabled && Certificate != null;
                if (cboCertificates.SelectedIndex == cboCertificates.Items.Count - 1)                     // Launch the create certificate window
                {
                    FrmCreateCertificate frm = new FrmCreateCertificate();
                    if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
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
                UTF8Encoding encoding = new UTF8Encoding();
                Byte[] clearTextsByte = encoding.GetBytes(clearText);
                ContentInfo contentinfo = new ContentInfo(clearTextsByte);
                EnvelopedCms envelopedCms = new EnvelopedCms(contentinfo);
                envelopedCms.Encrypt(new CmsRecipient(certificate));
                return Convert.ToBase64String(envelopedCms.Encode());
            }

            #region Control validation

            #region RDP settings

            void txtRDPUser_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtRDPUser.Text.Length == 0)
                {
                    error = "Please enter a user name";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtRDPPassword_Validating(object sender, CancelEventArgs e)
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

            void txtRDPConfirmPassword_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtRDPPassword.Text != txtRDPConfirmPassword.Text)
                {
                    error = "The passwords don't match.";
                }
                errProv.SetError((Control)sender, error);
                
            }

            void cboCertificates_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (this.Certificate == null)
                {
                    error = "You must select an existing certificate or create a new one";
                }
                errProv.SetError((Control)sender, error);
            }
            #endregion

        #region SQL Azure settings
            void txtDBServer_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBServer.Text.Length == 0)
                {
                    error = "Please enter your database server name (this is <databasename>.database.windows.net)";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtDBAdminUser_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBAdminUser.Text.Length == 0)
                {
                    error = "Please enter the database admin user name";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtDBAdminPassword_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBAdminPassword.Text.Length == 0)
                {
                    error = "Please enter the database admin password";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtDBName_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBName.Text.Length == 0)
                {
                    error = "Please enter the database name for the new database";
                }
                errProv.SetError((Control)sender, error);
            }
            void txtDBUser_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtDBUser.Text.Length == 0)
                {
                    error = "Please enter the database user name for the new database";
                }
                errProv.SetError((Control)sender, error);
            }
            void txtDBPassword_Validating(object sender, CancelEventArgs e)
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
            void txtDBRePassword_Validating(object sender, CancelEventArgs e)
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
            void txtStorageName_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtStorageName.Text.Length == 0)
                {
                    error = "Please enter your account storage name";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtStorageKey_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtStorageKey.Text.Length == 0)
                {
                    error = "Please enter your account storage key";
                }
                errProv.SetError((Control)sender, error);
            }
            void txtBindings_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtBindings.Text.Length == 0)
                {
                    error = "Please enter the bindings for your web application. You may include 'myapp.cloudapp.net' in order to access through the Azure access portal.";
                }
                errProv.SetError((Control)sender, error);
            }                       

        #endregion

        #region Package Settings
            void txtStorageContainer_Validating(object sender, CancelEventArgs e)
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

            void txtVHDBlobName_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtVHDBlobName.Text.Length == 0)
                {
                    error = "You must specify a blob name for the VHD";
                }
                errProv.SetError((Control)sender, error);
            }

            void txtVHDSize_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                int driveSize = 0;                
                if ((txtVHDSize.Text.Length == 0) ||
                    !int.TryParse(txtVHDSize.Text, out driveSize) ||
                    (driveSize < 128) || (driveSize > 1048576))
                {
                    error = "You must specify a valid VHD size (recommended minumum: 512Mb; maximum 1Tb=1048576Mb)";
                }
                errProv.SetError((Control)sender, error);
            }

            void lstPackages_Validating(object sender, CancelEventArgs e)
            {
                string error = null;

                bool IsChecked = false;
                foreach (ListViewItem li in lstPackages.Items)
                    if (li.Checked)
                    {
                        IsChecked = true;
                        break;
                    }
                if (!IsChecked)
                    error = "You must select at least one package to upload to Azure Storage";

                errProv.SetError((Control)sender, error);
            }




        #endregion

            #region Azure Connect Settings
            private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
                try
                {
                    string ConnectHelpURL = "http://go.microsoft.com/fwlink/?LinkId=203834";
                    System.Diagnostics.Process.Start(ConnectHelpURL);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            private void chkAzureConnect_CheckedChanged(object sender, EventArgs e)
            {
                txtConnectActivationToken.Enabled = chkAzureConnect.Checked;
            }

            void txtConnectActivationToken_Validating(object sender, CancelEventArgs e)
            {
                string error = null;
                if (txtConnectActivationToken.Text.Length == 0)
                {
                    error = "Please enter a valid Azure Connect Activation token.";
                }
                errProv.SetError((Control)sender, error);
            }    




            #endregion

            #endregion

    }
}
