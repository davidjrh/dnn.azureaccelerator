using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;


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


        private void CmdCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void TxtFriendlyNameTextChanged(object sender, EventArgs e)
        {
            cmdOK.Enabled = txtFriendlyName.Text.Trim() != "";
        }

        private void CmdOkClick(object sender, EventArgs e)
        {
            try
            {
                const string x509Name = "CN=DNN Azure Accelerator";                

                string outputCertificateFile = txtFriendlyName.Text.Trim() + ".cer";

                string filename = System.IO.Path.Combine(Environment.CurrentDirectory, "utils\\makecert.exe");
                string param = "-r -pe -a sha1 -ss My -sky exchange -len 2048 -n \"" + x509Name + "\" " + outputCertificateFile;

                // Run the makecert command.
                string output;
                string error;
                bool success = (ExecuteCommand(filename, param, out output, out error, 30) == 0);
                if (success)
                {
                    // Sets the friendly name
                    var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);

                    // Read the serial number
                    var cert = new X509Certificate2(System.IO.Path.Combine(Environment.CurrentDirectory, outputCertificateFile));
                    SerialNumber = cert.SerialNumber;
                    if (SerialNumber == null)
                        throw new ApplicationException("Certificate error");
                    
                    // Sets the friendly name
                    X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySerialNumber, SerialNumber, false);
                    if (certs.Count > 0) 
                        certs[0].FriendlyName = txtFriendlyName.Text.Trim();                        
                    else
                        throw new Exception("Failed to import the certificate");


                    DialogResult = DialogResult.OK;
                    Close();
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
            var p = new Process
                        {
                            StartInfo =
                                {
                                    FileName = exe,
                                    Arguments = arguments,
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    RedirectStandardError = true,
                                    RedirectStandardOutput = true
                                }
                        };
            p.Start();
            error = p.StandardError.ReadToEnd();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeout);
            int exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

    }
}
