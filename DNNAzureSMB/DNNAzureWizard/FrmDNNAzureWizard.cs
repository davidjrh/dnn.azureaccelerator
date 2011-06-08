using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace DNNAzureWizard
{
    public partial class FrmDNNAzureWizard : Form
    {

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
                    if (this.ActivePageIndex() == 6)
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
                    if (this.ActivePageIndex() == 6) // Finish
                    {
                        btnCancel_Click(sender, e);
                        return;
                    }

                    UseWaitCursor = true;
                    if (ValidateStep())
                        MoveSteps(1);
                    if (this.ActivePageIndex() == 5)
                    {
                        btnBack.Enabled = false;
                        btnOK.Enabled = false;
                        UploadToWindowsAzure();
                        txtLogFinal.Text = txtLOG.Text;
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
                catch (Exception ex) { }
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
                        catch (Exception ex) { }
                    }
                }

                // Deletes temporary configuration files generated
                try
                {
                    System.IO.File.Delete(Environment.CurrentDirectory + "\\ServiceConfiguration.cscfg");
                }
                catch (Exception ex) { };
                try
                {
                    System.IO.File.Delete(Environment.CurrentDirectory + "\\bin\\ServiceConfiguration.cscfg");
                }
                catch (Exception ex) { };
            }
            private void RefreshUI()
            {
                InitializePages();
                ShowPage(1);

            }
            private static void LogException(Exception ex)
            {
                MessageBox.Show(ex.Message, "An exception ocurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    case 1: // Home tab, nothing to validate
                        return true;
                    case 2: // SQL Azure tab
                        ValidateSQLAzureSettings();
                        return true;
                    case 3:
                        ValidateAzureSettings();
                        txtConfig.Text = ProcessConfigFile("ServiceConfiguration.cscfg");
                        return true;
                    case 4:
                        return (MessageBox.Show("The wizard will begin now to deploy DotNetNuke on Windows Azure with the specified settings. Are you sure that you want to continue?", "Deploy", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK);
                    default:
                        return false;
                }
            }

            private void ValidateSQLAzureSettings()
            {
                if (txtDBServer.Text.Trim() == "")
                    throw new Exception("You must specify the SQL Azure server name");
                if (txtDBAdminUser.Text.Trim() == "")
                    throw new Exception("You must specify a user that can create databases through the master database");
                if (txtDBAdminPassword.Text.Trim() == "")
                    throw new Exception("You must specify the admin user password");
                if (txtDBName.Text.Trim() == "")
                    throw new Exception("You must specify the name of the database that will be created");
                if (txtDBUser.Text.Trim() == "")
                    throw new Exception("You must specify that will be created and used for the database");
                if (txtDBPassword.Text == "")
                    throw new Exception("You must specify the password for the new database user");
                if (txtDBRePassword.Text == "")
                    throw new Exception("You must confirm the password");
                if (txtDBPassword.Text.Trim() != txtDBRePassword.Text.Trim())
                    throw new Exception("The passwords are not identical");
            }

            private void ValidateAzureSettings()
            {
                if (txtStorageName.Text == "")
                    throw new Exception("You must specify the Storage Account name");
                if (txtStorageKey.Text == "")
                    throw new Exception("You must specify the Storage Account key");
                if (txtBindings.Text == "")
                    throw new Exception("You must specify at least one host header for the webrole");
            }

            private string ProcessConfigFile(string configfilename)
            {
                string CfgStr = "";
                using (System.IO.TextReader mConfig = System.IO.File.OpenText(Environment.CurrentDirectory + "\\config\\" + configfilename))
                {
                    CfgStr = mConfig.ReadToEnd();
                }

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

                return CfgStr;
            }

            private void UploadToWindowsAzure()
            {
                try
                {
                    txtLOG.Text = "";

                    // Creates the Accelerator file
                    using (System.IO.TextWriter Config = System.IO.File.CreateText(Environment.CurrentDirectory + "\\ServiceConfiguration.cscfg"))
                        Config.Write(ProcessConfigFile("ServiceConfigurationAccelCon.cscfg"));

                    // Creates the Service Configuration file that will be uploaded to Azure
                    using (System.IO.TextWriter Config = System.IO.File.CreateText(Environment.CurrentDirectory + "\\bin\\ServiceConfiguration.cscfg"))
                        Config.Write(ProcessConfigFile("ServiceConfiguration.cscfg"));


                    CheckForIllegalCrossThreadCalls = false;
                    ExecuteCommand(Environment.CurrentDirectory + "\\bin\\accelcon.exe", "-e " + Environment.CurrentDirectory + "\\bin\\DeployDotNetNuke.bat");
                }
                catch (Exception e)
                {
                    try
                    {
                        // Deletes temporary configuration files generated
                        System.IO.File.Delete(Environment.CurrentDirectory + "\\ServiceConfiguration.cscfg");
                        System.IO.File.Delete(Environment.CurrentDirectory + "\\bin\\ServiceConfiguration.cscfg");
                    }
                    catch (Exception ex) { };

                    throw;
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
                p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                p.ErrorDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                p.Close();
                p = null;
            }
        #endregion
        
    }
}
