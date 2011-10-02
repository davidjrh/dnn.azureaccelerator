using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;


namespace DNNAzureWizard
{
    public partial class FrmCreateCertificate : Form
    {
        public FrmCreateCertificate()
        {
            InitializeComponent();
        }

        string _serialNumber = "";
        public string SerialNumber { get { return _serialNumber; } set { _serialNumber = value; } }


        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtFriendlyName_TextChanged(object sender, EventArgs e)
        {
            cmdOK.Enabled = txtFriendlyName.Text.Trim() != "";
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            try
            {
                string x509Name = "CN=DNN Azure Accelerator";                

                string outputCertificateFile = txtFriendlyName.Text.Trim() + ".cer";

                string filename = System.IO.Path.Combine(Environment.CurrentDirectory, "utils\\makecert.exe");
                string param = "-r -pe -a sha1 -ss My -sky exchange -len 2048 -n \"" + x509Name + "\" " + outputCertificateFile;

                // Run the makecert command.
                string output = "";
                string error = "";
                bool success = (ExecuteCommand(filename, param, out output, out error, 30) == 0);
                if (success)
                {
                    // Sets the friendly name
                    X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);

                    // Read the serial number
                    X509Certificate2 cert = new X509Certificate2(System.IO.Path.Combine(Environment.CurrentDirectory, outputCertificateFile));
                    SerialNumber = cert.SerialNumber;
                    
                    // Sets the friendly name
                    X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySerialNumber, cert.SerialNumber, false);
                    if (certs.Count > 0) 
                        certs[0].FriendlyName = txtFriendlyName.Text.Trim();                        
                    else
                        throw new Exception("Failed to import the certificate");


                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                    this.Close();
                }
                else
                    throw new Exception(error);                   
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error ocurred while creating the certificate: " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="output">Output of the command</param>
        /// <param name="error">Contents of the error results if fails</param>
        /// <param name="timeout">Timeout for executing the command in milliseconds</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string output, out string error, int timeout)
        {
            Process p = new Process();
            int exitCode;
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            error = p.StandardError.ReadToEnd();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeout);
            exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

    }
}
