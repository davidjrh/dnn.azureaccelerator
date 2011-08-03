using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;

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
                    if (this.ActivePageIndex() == 7)
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
                    if (this.ActivePageIndex() == 7) // Finish
                    {
                        btnCancel_Click(sender, e);
                        return;
                    }

                    UseWaitCursor = true;
                    if (ValidateStep())
                        MoveSteps(1);
                    if (this.ActivePageIndex() == 6)
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

                // Deletes temporary configuration files generated
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
            private void RefreshUI()
            {
                InitializePages();                
                ShowPage(1);
                SetupAppSettings();
                ReloadDeploymentPackages();
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
                        return true;
                    case 4:
                        ValidatePackagesSelectionSettings();
                        txtConfig.Text = GetSettingsSummary();
                        return true;
                    case 5:
                        return (MessageBox.Show("The wizard will begin now to deploy DotNetNuke on Windows Azure with the specified settings. Are you sure that you want to continue?", "Deploy", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK);
                    default:
                        return false;
                }
            }

            private string GetSettingsSummary()
            {
                System.Text.StringBuilder summary = new System.Text.StringBuilder("======================= SUMMARY OF SETTINGS =======================");
                summary.AppendLine("");
                summary.AppendLine("DATABASE SETTINGS:");
                summary.AppendLine("- DB Server Name: " +  txtDBServer.Text.Trim() + ".database.windows.net");
                summary.AppendLine("- DB Admin user name: " + txtDBAdminUser.Text.Trim());
                summary.AppendLine("- DB Admin password: <not showed>");
                summary.AppendLine("- DB user name: " + txtDBUser.Text.Trim());
                summary.AppendLine("- DB password: <not showed>");
                summary.AppendLine("");
                summary.AppendLine("STORAGE SETTINGS:");
                summary.AppendLine("- Storage name: " + txtStorageName.Text.Trim());
                summary.AppendLine("- Storage key: " + txtStorageKey.Text.Trim());
                summary.AppendLine("- Storage package container: " + txtStorageContainer.Text.Trim());
                summary.AppendLine("");
                summary.AppendLine("IIS SETTINGS:");
                summary.AppendLine("- Bindings: " + txtBindings.Text.Trim());
                summary.AppendLine("");
                summary.AppendLine("SELECTED PACKAGES:");
                foreach (ListViewItem li in lstPackages.Items)
                    if (li.Checked)
                        summary.AppendLine("- " + li.Text);
                return summary.ToString(); ;
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
                if (txtDBPassword.Text != txtDBRePassword.Text)
                    throw new Exception("The passwords are not identical");
            }

            private void ValidateAzureSettings()
            {
                if (txtStorageName.Text.Trim() == "")
                    throw new Exception("You must specify the Storage Account name");
                if (txtStorageKey.Text.Trim() == "")
                    throw new Exception("You must specify the Storage Account key");
                if (txtBindings.Text.Trim() == "")
                    throw new Exception("You must specify at least one host header for the webrole");
            }

            private void ValidatePackagesSelectionSettings()
            {
                bool IsChecked=false;
                foreach (ListViewItem li in lstPackages.Items)
                    if (li.Checked)
                    {
                        IsChecked = true;
                        break;
                    }
                if (!IsChecked)
                    throw new Exception("You must select at least one package to upload to Azure Storage");

                if (txtStorageContainer.Text.Trim() == "")
                    throw new Exception("You must specify a blob package container name");
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

                uploadBlockSize = Convert.ToInt32(ConfigurationManager.AppSettings["UploadBlockSize"]);
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

                    ListViewItem li = new ListViewItem(new string[] { doc.SelectSingleNode("/ServicePackage/Name").InnerText, doc.SelectSingleNode("/ServicePackage/Description").InnerText });
                    li.Tag = doc.SelectSingleNode("ServicePackage");
                    li.Checked = true;
                    lstPackages.Items.Add(li);
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

    }
}
