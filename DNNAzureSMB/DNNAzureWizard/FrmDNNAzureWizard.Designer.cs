using System;
using System.ComponentModel;

namespace DNNAzureWizard
{
    partial class FrmDNNAzureWizard
    {
        /// <summary>
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben eliminar; false en caso contrario, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmDNNAzureWizard));
            this.split = new System.Windows.Forms.SplitContainer();
            this.pnl = new System.Windows.Forms.Panel();
            this.pnl8 = new System.Windows.Forms.Panel();
            this.pictureBox14 = new System.Windows.Forms.PictureBox();
            this.pictureBox15 = new System.Windows.Forms.PictureBox();
            this.panel6 = new System.Windows.Forms.Panel();
            this.pnlSSL = new System.Windows.Forms.Panel();
            this.cmdViewSSL = new System.Windows.Forms.Button();
            this.cmdOpenSSL = new System.Windows.Forms.Button();
            this.cmdRemoveSSL = new System.Windows.Forms.Button();
            this.cmdAddSSL = new System.Windows.Forms.Button();
            this.lstCASSLCertificates = new System.Windows.Forms.ListView();
            this.Cert = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Thumbprint = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label64 = new System.Windows.Forms.Label();
            this.txtSSLCertificate = new System.Windows.Forms.TextBox();
            this.label63 = new System.Windows.Forms.Label();
            this.chkEnableSSL = new System.Windows.Forms.CheckBox();
            this.label65 = new System.Windows.Forms.Label();
            this.label66 = new System.Windows.Forms.Label();
            this.pnl2 = new System.Windows.Forms.Panel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox10 = new System.Windows.Forms.PictureBox();
            this.label43 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.label61 = new System.Windows.Forms.Label();
            this.label44 = new System.Windows.Forms.Label();
            this.cboSubscriptions = new System.Windows.Forms.ComboBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.optSubscription = new System.Windows.Forms.RadioButton();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label45 = new System.Windows.Forms.Label();
            this.label46 = new System.Windows.Forms.Label();
            this.pnl1 = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pnl11 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lstTasks = new System.Windows.Forms.ListView();
            this.TaskDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TaskStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.txtLOG = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.pnl3 = new System.Windows.Forms.Panel();
            this.pnlHostingServices = new System.Windows.Forms.Panel();
            this.txtPackagesContainer = new System.Windows.Forms.TextBox();
            this.label58 = new System.Windows.Forms.Label();
            this.pictureBox12 = new System.Windows.Forms.PictureBox();
            this.txtVHDDriveSize = new System.Windows.Forms.TextBox();
            this.label57 = new System.Windows.Forms.Label();
            this.txtVHDName = new System.Windows.Forms.TextBox();
            this.label56 = new System.Windows.Forms.Label();
            this.label55 = new System.Windows.Forms.Label();
            this.cboStorage = new System.Windows.Forms.ComboBox();
            this.label54 = new System.Windows.Forms.Label();
            this.label51 = new System.Windows.Forms.Label();
            this.cboEnvironment = new System.Windows.Forms.ComboBox();
            this.label53 = new System.Windows.Forms.Label();
            this.label50 = new System.Windows.Forms.Label();
            this.cboHostingService = new System.Windows.Forms.ComboBox();
            this.label52 = new System.Windows.Forms.Label();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.pictureBox11 = new System.Windows.Forms.PictureBox();
            this.label49 = new System.Windows.Forms.Label();
            this.pnl12 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txtLogFinal = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.lblSuccess = new System.Windows.Forms.Label();
            this.pnl5 = new System.Windows.Forms.Panel();
            this.pictureBox13 = new System.Windows.Forms.PictureBox();
            this.DBSettings = new System.Windows.Forms.Panel();
            this.cboDatabase = new System.Windows.Forms.ComboBox();
            this.label24 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.txtDBRePassword = new System.Windows.Forms.TextBox();
            this.btnTestDB = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.txtDBPassword = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtDBUser = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtDBAdminPassword = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtDBAdminUser = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtDBName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtDBServer = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.pnl9 = new System.Windows.Forms.Panel();
            this.PackageSettings = new System.Windows.Forms.Panel();
            this.lnkMorePackages = new System.Windows.Forms.LinkLabel();
            this.label62 = new System.Windows.Forms.Label();
            this.txtDNNUrl = new System.Windows.Forms.TextBox();
            this.lblCustomUrl = new System.Windows.Forms.Label();
            this.cboDNNVersion = new System.Windows.Forms.ComboBox();
            this.label60 = new System.Windows.Forms.Label();
            this.label59 = new System.Windows.Forms.Label();
            this.chkAutoInstall = new System.Windows.Forms.CheckBox();
            this.lstPackages = new System.Windows.Forms.ListView();
            this.packageName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.packageDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label36 = new System.Windows.Forms.Label();
            this.label37 = new System.Windows.Forms.Label();
            this.pnl6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.chkEnableRemoteMgmt = new System.Windows.Forms.CheckBox();
            this.pnlRDP = new System.Windows.Forms.Panel();
            this.chkEnableFTP = new System.Windows.Forms.CheckBox();
            this.chkEnableRDP = new System.Windows.Forms.CheckBox();
            this.chkWebDeploy = new System.Windows.Forms.CheckBox();
            this.cboRDPExpirationDate = new System.Windows.Forms.DateTimePicker();
            this.label32 = new System.Windows.Forms.Label();
            this.label33 = new System.Windows.Forms.Label();
            this.txtRDPConfirmPassword = new System.Windows.Forms.TextBox();
            this.label40 = new System.Windows.Forms.Label();
            this.txtRDPPassword = new System.Windows.Forms.TextBox();
            this.label42 = new System.Windows.Forms.Label();
            this.txtRDPUser = new System.Windows.Forms.TextBox();
            this.cboCertificates = new System.Windows.Forms.ComboBox();
            this.lblRDPCredentialsInfo = new System.Windows.Forms.Label();
            this.cmdViewCertificate = new System.Windows.Forms.Button();
            this.lblRDPInfo = new System.Windows.Forms.Label();
            this.label38 = new System.Windows.Forms.Label();
            this.label39 = new System.Windows.Forms.Label();
            this.pnl7 = new System.Windows.Forms.Panel();
            this.pnlAzureConnect = new System.Windows.Forms.Panel();
            this.lnkConnectHelp = new System.Windows.Forms.LinkLabel();
            this.label41 = new System.Windows.Forms.Label();
            this.txtConnectActivationToken = new System.Windows.Forms.TextBox();
            this.label35 = new System.Windows.Forms.Label();
            this.chkAzureConnect = new System.Windows.Forms.CheckBox();
            this.label47 = new System.Windows.Forms.Label();
            this.label48 = new System.Windows.Forms.Label();
            this.pnl4 = new System.Windows.Forms.Panel();
            this.AzureSettings = new System.Windows.Forms.Panel();
            this.label34 = new System.Windows.Forms.Label();
            this.txtVHDSize = new System.Windows.Forms.TextBox();
            this.txtVHDBlobName = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.txtStorageContainer = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.txtBindings = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.lblStTest = new System.Windows.Forms.Label();
            this.btnTestStorage = new System.Windows.Forms.Button();
            this.chkStorageHTTPS = new System.Windows.Forms.CheckBox();
            this.txtStorageKey = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.txtStorageName = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.pnl10 = new System.Windows.Forms.Panel();
            this.pnlConfig = new System.Windows.Forms.Panel();
            this.txtConfig = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.errProv = new System.Windows.Forms.ErrorProvider(this.components);
            this.dlgFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.dlgFile = new System.Windows.Forms.OpenFileDialog();
            this.dlgSSLFile = new System.Windows.Forms.OpenFileDialog();
            this.pictureBox8 = new System.Windows.Forms.PictureBox();
            this.pictureBox16 = new System.Windows.Forms.PictureBox();
            this.pictureBox9 = new System.Windows.Forms.PictureBox();
            this.pictureBox17 = new System.Windows.Forms.PictureBox();
            this.pictureBox7 = new System.Windows.Forms.PictureBox();
            this.pictureBox18 = new System.Windows.Forms.PictureBox();
            this.pictureBox5 = new System.Windows.Forms.PictureBox();
            this.pictureBox19 = new System.Windows.Forms.PictureBox();
            this.pictureBox20 = new System.Windows.Forms.PictureBox();
            this.pictureBox21 = new System.Windows.Forms.PictureBox();
            this.pictureBox22 = new System.Windows.Forms.PictureBox();
            this.pictureBox23 = new System.Windows.Forms.PictureBox();
            this.pictureBox24 = new System.Windows.Forms.PictureBox();
            this.pictureBox25 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.pnl.SuspendLayout();
            this.pnl8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox14)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox15)).BeginInit();
            this.panel6.SuspendLayout();
            this.pnlSSL.SuspendLayout();
            this.pnl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox10)).BeginInit();
            this.panel1.SuspendLayout();
            this.pnl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.pnl11.SuspendLayout();
            this.panel2.SuspendLayout();
            this.pnl3.SuspendLayout();
            this.pnlHostingServices.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox12)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox11)).BeginInit();
            this.pnl12.SuspendLayout();
            this.panel3.SuspendLayout();
            this.pnl5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox13)).BeginInit();
            this.DBSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.pnl9.SuspendLayout();
            this.PackageSettings.SuspendLayout();
            this.pnl6.SuspendLayout();
            this.panel5.SuspendLayout();
            this.pnlRDP.SuspendLayout();
            this.pnl7.SuspendLayout();
            this.pnlAzureConnect.SuspendLayout();
            this.pnl4.SuspendLayout();
            this.AzureSettings.SuspendLayout();
            this.pnl10.SuspendLayout();
            this.pnlConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errProv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox16)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox17)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox18)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox19)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox20)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox22)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox23)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox24)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox25)).BeginInit();
            this.SuspendLayout();
            // 
            // split
            // 
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.IsSplitterFixed = true;
            this.split.Location = new System.Drawing.Point(0, 0);
            this.split.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // split.Panel1
            // 
            this.split.Panel1.Controls.Add(this.pnl);
            // 
            // split.Panel2
            // 
            this.split.Panel2.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.split.Panel2.Controls.Add(this.btnBack);
            this.split.Panel2.Controls.Add(this.btnCancel);
            this.split.Panel2.Controls.Add(this.btnOK);
            this.split.Size = new System.Drawing.Size(1356, 741);
            this.split.SplitterDistance = 648;
            this.split.SplitterWidth = 1;
            this.split.TabIndex = 0;
            // 
            // pnl
            // 
            this.pnl.Controls.Add(this.pnl6);
            this.pnl.Controls.Add(this.pnl7);
            this.pnl.Controls.Add(this.pnl1);
            this.pnl.Controls.Add(this.pnl4);
            this.pnl.Controls.Add(this.pnl9);
            this.pnl.Controls.Add(this.pnl10);
            this.pnl.Controls.Add(this.pnl11);
            this.pnl.Controls.Add(this.pnl12);
            this.pnl.Controls.Add(this.pnl5);
            this.pnl.Controls.Add(this.pnl8);
            this.pnl.Controls.Add(this.pnl2);
            this.pnl.Controls.Add(this.pnl3);
            this.pnl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl.Location = new System.Drawing.Point(0, 0);
            this.pnl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl.Name = "pnl";
            this.pnl.Size = new System.Drawing.Size(1356, 648);
            this.pnl.TabIndex = 0;
            // 
            // pnl8
            // 
            this.pnl8.Controls.Add(this.pictureBox14);
            this.pnl8.Controls.Add(this.pictureBox15);
            this.pnl8.Controls.Add(this.panel6);
            this.pnl8.Controls.Add(this.label66);
            this.pnl8.Location = new System.Drawing.Point(295, 56);
            this.pnl8.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl8.Name = "pnl8";
            this.pnl8.Size = new System.Drawing.Size(682, 399);
            this.pnl8.TabIndex = 13;
            // 
            // pictureBox14
            // 
            this.pictureBox14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox14.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox14.Image")));
            this.pictureBox14.Location = new System.Drawing.Point(621, 6);
            this.pictureBox14.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox14.Name = "pictureBox14";
            this.pictureBox14.Size = new System.Drawing.Size(50, 50);
            this.pictureBox14.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox14.TabIndex = 7;
            this.pictureBox14.TabStop = false;
            // 
            // pictureBox15
            // 
            this.pictureBox15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox15.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox15.Image")));
            this.pictureBox15.Location = new System.Drawing.Point(502, 22);
            this.pictureBox15.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox15.Name = "pictureBox15";
            this.pictureBox15.Size = new System.Drawing.Size(114, 23);
            this.pictureBox15.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox15.TabIndex = 6;
            this.pictureBox15.TabStop = false;
            // 
            // panel6
            // 
            this.panel6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel6.BackColor = System.Drawing.SystemColors.Control;
            this.panel6.Controls.Add(this.pnlSSL);
            this.panel6.Controls.Add(this.chkEnableSSL);
            this.panel6.Controls.Add(this.label65);
            this.panel6.Location = new System.Drawing.Point(0, 64);
            this.panel6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(681, 339);
            this.panel6.TabIndex = 0;
            // 
            // pnlSSL
            // 
            this.pnlSSL.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSSL.Controls.Add(this.cmdViewSSL);
            this.pnlSSL.Controls.Add(this.cmdOpenSSL);
            this.pnlSSL.Controls.Add(this.cmdRemoveSSL);
            this.pnlSSL.Controls.Add(this.cmdAddSSL);
            this.pnlSSL.Controls.Add(this.lstCASSLCertificates);
            this.pnlSSL.Controls.Add(this.label64);
            this.pnlSSL.Controls.Add(this.txtSSLCertificate);
            this.pnlSSL.Controls.Add(this.label63);
            this.pnlSSL.Enabled = false;
            this.pnlSSL.Location = new System.Drawing.Point(14, 93);
            this.pnlSSL.Name = "pnlSSL";
            this.pnlSSL.Size = new System.Drawing.Size(656, 235);
            this.pnlSSL.TabIndex = 2;
            // 
            // cmdViewSSL
            // 
            this.cmdViewSSL.Location = new System.Drawing.Point(585, 10);
            this.cmdViewSSL.Name = "cmdViewSSL";
            this.cmdViewSSL.Size = new System.Drawing.Size(59, 23);
            this.cmdViewSSL.TabIndex = 6;
            this.cmdViewSSL.Text = "&View";
            this.cmdViewSSL.UseVisualStyleBackColor = true;
            this.cmdViewSSL.Click += new System.EventHandler(this.cmdViewSSL_Click);
            // 
            // cmdOpenSSL
            // 
            this.cmdOpenSSL.Location = new System.Drawing.Point(550, 10);
            this.cmdOpenSSL.Name = "cmdOpenSSL";
            this.cmdOpenSSL.Size = new System.Drawing.Size(28, 23);
            this.cmdOpenSSL.TabIndex = 5;
            this.cmdOpenSSL.Text = "...";
            this.cmdOpenSSL.UseVisualStyleBackColor = true;
            this.cmdOpenSSL.Click += new System.EventHandler(this.cmdOpenSSL_Click);
            // 
            // cmdRemoveSSL
            // 
            this.cmdRemoveSSL.Location = new System.Drawing.Point(98, 205);
            this.cmdRemoveSSL.Name = "cmdRemoveSSL";
            this.cmdRemoveSSL.Size = new System.Drawing.Size(75, 23);
            this.cmdRemoveSSL.TabIndex = 10;
            this.cmdRemoveSSL.Text = "&Remove";
            this.cmdRemoveSSL.UseVisualStyleBackColor = true;
            this.cmdRemoveSSL.Click += new System.EventHandler(this.cmdRemoveSSL_Click);
            // 
            // cmdAddSSL
            // 
            this.cmdAddSSL.Location = new System.Drawing.Point(17, 205);
            this.cmdAddSSL.Name = "cmdAddSSL";
            this.cmdAddSSL.Size = new System.Drawing.Size(75, 23);
            this.cmdAddSSL.TabIndex = 9;
            this.cmdAddSSL.Text = "&Add";
            this.cmdAddSSL.UseVisualStyleBackColor = true;
            this.cmdAddSSL.Click += new System.EventHandler(this.cmdAddSSL_Click);
            // 
            // lstCASSLCertificates
            // 
            this.lstCASSLCertificates.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Cert,
            this.Thumbprint});
            this.lstCASSLCertificates.FullRowSelect = true;
            this.lstCASSLCertificates.HideSelection = false;
            this.lstCASSLCertificates.Location = new System.Drawing.Point(17, 64);
            this.lstCASSLCertificates.MultiSelect = false;
            this.lstCASSLCertificates.Name = "lstCASSLCertificates";
            this.lstCASSLCertificates.Size = new System.Drawing.Size(626, 135);
            this.lstCASSLCertificates.TabIndex = 8;
            this.lstCASSLCertificates.UseCompatibleStateImageBehavior = false;
            this.lstCASSLCertificates.View = System.Windows.Forms.View.Details;
            this.lstCASSLCertificates.DoubleClick += new System.EventHandler(this.lstCASSLCertificates_DoubleClick);
            // 
            // Cert
            // 
            this.Cert.Text = "Certificate";
            this.Cert.Width = 350;
            // 
            // Thumbprint
            // 
            this.Thumbprint.Text = "Thumbprint";
            this.Thumbprint.Width = 200;
            // 
            // label64
            // 
            this.label64.AutoSize = true;
            this.label64.Location = new System.Drawing.Point(16, 42);
            this.label64.Name = "label64";
            this.label64.Size = new System.Drawing.Size(378, 15);
            this.label64.TabIndex = 7;
            this.label64.Text = "A&dd all the other certificates in the Certification Path (CA\'s certificates)";
            // 
            // txtSSLCertificate
            // 
            this.txtSSLCertificate.Location = new System.Drawing.Point(103, 11);
            this.txtSSLCertificate.Name = "txtSSLCertificate";
            this.txtSSLCertificate.ReadOnly = true;
            this.txtSSLCertificate.Size = new System.Drawing.Size(425, 23);
            this.txtSSLCertificate.TabIndex = 4;
            this.txtSSLCertificate.Validating += new System.ComponentModel.CancelEventHandler(this.txtSSLCertificateValidating);
            // 
            // label63
            // 
            this.label63.AutoSize = true;
            this.label63.Location = new System.Drawing.Point(16, 14);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(81, 15);
            this.label63.TabIndex = 3;
            this.label63.Text = "&Site certificate";
            // 
            // chkEnableSSL
            // 
            this.chkEnableSSL.AutoSize = true;
            this.chkEnableSSL.Location = new System.Drawing.Point(13, 68);
            this.chkEnableSSL.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkEnableSSL.Name = "chkEnableSSL";
            this.chkEnableSSL.Size = new System.Drawing.Size(177, 19);
            this.chkEnableSSL.TabIndex = 1;
            this.chkEnableSSL.Text = "&Enable SSL (HTTPS protocol)";
            this.chkEnableSSL.UseVisualStyleBackColor = true;
            this.chkEnableSSL.CheckedChanged += new System.EventHandler(this.chkEnableSSL_CheckedChanged);
            // 
            // label65
            // 
            this.label65.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label65.Location = new System.Drawing.Point(10, 11);
            this.label65.Name = "label65";
            this.label65.Size = new System.Drawing.Size(661, 51);
            this.label65.TabIndex = 1;
            this.label65.Text = resources.GetString("label65.Text");
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label66.Location = new System.Drawing.Point(10, 18);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(107, 25);
            this.label66.TabIndex = 2;
            this.label66.Text = "SSL Settings";
            // 
            // pnl2
            // 
            this.pnl2.Controls.Add(this.pictureBox3);
            this.pnl2.Controls.Add(this.pictureBox10);
            this.pnl2.Controls.Add(this.label43);
            this.pnl2.Controls.Add(this.panel1);
            this.pnl2.Location = new System.Drawing.Point(152, 103);
            this.pnl2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl2.Name = "pnl2";
            this.pnl2.Size = new System.Drawing.Size(682, 399);
            this.pnl2.TabIndex = 13;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(625, 6);
            this.pictureBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(50, 50);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox3.TabIndex = 5;
            this.pictureBox3.TabStop = false;
            // 
            // pictureBox10
            // 
            this.pictureBox10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox10.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox10.Image")));
            this.pictureBox10.Location = new System.Drawing.Point(506, 22);
            this.pictureBox10.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox10.Name = "pictureBox10";
            this.pictureBox10.Size = new System.Drawing.Size(114, 23);
            this.pictureBox10.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox10.TabIndex = 3;
            this.pictureBox10.TabStop = false;
            // 
            // label43
            // 
            this.label43.AutoSize = true;
            this.label43.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label43.Location = new System.Drawing.Point(10, 18);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(223, 25);
            this.label43.TabIndex = 2;
            this.label43.Text = "Deployment Type Settings";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.linkLabel2);
            this.panel1.Controls.Add(this.label61);
            this.panel1.Controls.Add(this.label44);
            this.panel1.Controls.Add(this.cboSubscriptions);
            this.panel1.Controls.Add(this.radioButton1);
            this.panel1.Controls.Add(this.optSubscription);
            this.panel1.Controls.Add(this.linkLabel1);
            this.panel1.Controls.Add(this.label45);
            this.panel1.Controls.Add(this.label46);
            this.panel1.Location = new System.Drawing.Point(0, 64);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(681, 339);
            this.panel1.TabIndex = 4;
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(341, 130);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(167, 15);
            this.linkLabel2.TabIndex = 16;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Download Publish Settings file";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
            // 
            // label61
            // 
            this.label61.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label61.Location = new System.Drawing.Point(59, 130);
            this.label61.Name = "label61";
            this.label61.Size = new System.Drawing.Size(289, 22);
            this.label61.TabIndex = 15;
            this.label61.Text = "Don\'t know how to obtain your publish settings file?";
            // 
            // label44
            // 
            this.label44.AutoSize = true;
            this.label44.Location = new System.Drawing.Point(62, 97);
            this.label44.Name = "label44";
            this.label44.Size = new System.Drawing.Size(73, 15);
            this.label44.TabIndex = 14;
            this.label44.Text = "Subscription";
            // 
            // cboSubscriptions
            // 
            this.cboSubscriptions.DisplayMember = "Name";
            this.cboSubscriptions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSubscriptions.FormattingEnabled = true;
            this.cboSubscriptions.Items.AddRange(new object[] {
            "<Import publish settings file...>"});
            this.cboSubscriptions.Location = new System.Drawing.Point(141, 94);
            this.cboSubscriptions.Name = "cboSubscriptions";
            this.cboSubscriptions.Size = new System.Drawing.Size(315, 23);
            this.cboSubscriptions.TabIndex = 13;
            this.cboSubscriptions.ValueMember = "SubscriptionId";
            this.cboSubscriptions.SelectedIndexChanged += new System.EventHandler(this.CboSubscriptionsSelectedIndexChanged);
            this.cboSubscriptions.Validating += new System.ComponentModel.CancelEventHandler(this.CboSubscriptionsValidating);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(31, 233);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(554, 19);
            this.radioButton1.TabIndex = 12;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Create a configured set of package files and export them to local file system for" +
    " manual deployment.";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // optSubscription
            // 
            this.optSubscription.AutoSize = true;
            this.optSubscription.Checked = true;
            this.optSubscription.Location = new System.Drawing.Point(31, 64);
            this.optSubscription.Name = "optSubscription";
            this.optSubscription.Size = new System.Drawing.Size(308, 19);
            this.optSubscription.TabIndex = 11;
            this.optSubscription.TabStop = true;
            this.optSubscription.Text = "Deploy DotNetNuke on Windows Azure automatically";
            this.optSubscription.UseVisualStyleBackColor = true;
            this.optSubscription.CheckedChanged += new System.EventHandler(this.OptSubscriptionCheckedChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(60, 180);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(262, 15);
            this.linkLabel1.TabIndex = 10;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "How to obtain a Windows Azure 90-day free trial";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1LinkClicked1);
            // 
            // label45
            // 
            this.label45.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label45.Location = new System.Drawing.Point(60, 158);
            this.label45.Name = "label45";
            this.label45.Size = new System.Drawing.Size(482, 22);
            this.label45.TabIndex = 9;
            this.label45.Text = "For more information about creating a Windows Azure subscription, see";
            // 
            // label46
            // 
            this.label46.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label46.Location = new System.Drawing.Point(10, 11);
            this.label46.Name = "label46";
            this.label46.Size = new System.Drawing.Size(661, 37);
            this.label46.TabIndex = 1;
            this.label46.Text = "To create your DotNetNuke site on Windows Azure, the first step is to select the " +
    "subscription where you want to deploy on.  Please select the subscription from t" +
    "he list below.";
            // 
            // pnl1
            // 
            this.pnl1.Controls.Add(this.pictureBox2);
            this.pnl1.Controls.Add(this.label2);
            this.pnl1.Controls.Add(this.label1);
            this.pnl1.Controls.Add(this.pictureBox1);
            this.pnl1.Location = new System.Drawing.Point(891, 9);
            this.pnl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl1.Name = "pnl1";
            this.pnl1.Size = new System.Drawing.Size(695, 383);
            this.pnl1.TabIndex = 3;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(123, 313);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(120, 28);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 19;
            this.pictureBox2.TabStop = false;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(269, 127);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(409, 240);
            this.label2.TabIndex = 17;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Font = new System.Drawing.Font("Segoe UI Light", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(268, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(411, 76);
            this.label1.TabIndex = 16;
            this.label1.Text = "Welcome to the DotNetNuke Azure Accelerator Wizard";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(27, 85);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(216, 216);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 13;
            this.pictureBox1.TabStop = false;
            // 
            // pnl11
            // 
            this.pnl11.Controls.Add(this.pictureBox24);
            this.pnl11.Controls.Add(this.pictureBox25);
            this.pnl11.Controls.Add(this.panel2);
            this.pnl11.Controls.Add(this.label28);
            this.pnl11.Location = new System.Drawing.Point(613, 224);
            this.pnl11.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl11.Name = "pnl11";
            this.pnl11.Size = new System.Drawing.Size(708, 397);
            this.pnl11.TabIndex = 8;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Controls.Add(this.lstTasks);
            this.panel2.Controls.Add(this.txtLOG);
            this.panel2.Controls.Add(this.label27);
            this.panel2.Location = new System.Drawing.Point(0, 66);
            this.panel2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(706, 330);
            this.panel2.TabIndex = 3;
            // 
            // lstTasks
            // 
            this.lstTasks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstTasks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.TaskDescription,
            this.TaskStatus});
            this.lstTasks.Location = new System.Drawing.Point(16, 56);
            this.lstTasks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lstTasks.Name = "lstTasks";
            this.lstTasks.Size = new System.Drawing.Size(675, 253);
            this.lstTasks.TabIndex = 2;
            this.lstTasks.UseCompatibleStateImageBehavior = false;
            this.lstTasks.View = System.Windows.Forms.View.Details;
            // 
            // TaskDescription
            // 
            this.TaskDescription.Text = "Task description";
            this.TaskDescription.Width = 450;
            // 
            // TaskStatus
            // 
            this.TaskStatus.Text = "Status";
            this.TaskStatus.Width = 200;
            // 
            // txtLOG
            // 
            this.txtLOG.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLOG.BackColor = System.Drawing.Color.MidnightBlue;
            this.txtLOG.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLOG.ForeColor = System.Drawing.Color.Silver;
            this.txtLOG.Location = new System.Drawing.Point(617, 21);
            this.txtLOG.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtLOG.Multiline = true;
            this.txtLOG.Name = "txtLOG";
            this.txtLOG.ReadOnly = true;
            this.txtLOG.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLOG.Size = new System.Drawing.Size(80, 20);
            this.txtLOG.TabIndex = 1;
            this.txtLOG.Visible = false;
            // 
            // label27
            // 
            this.label27.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label27.Location = new System.Drawing.Point(16, 19);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(665, 24);
            this.label27.TabIndex = 0;
            this.label27.Text = "Check the log window in order to review the process of deploying DotNetNuke on Wi" +
    "ndows Azure";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label28.Location = new System.Drawing.Point(16, 19);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(356, 25);
            this.label28.TabIndex = 2;
            this.label28.Text = "Deploying DotNetNuke on Windows Azure";
            // 
            // pnl3
            // 
            this.pnl3.Controls.Add(this.pnlHostingServices);
            this.pnl3.Controls.Add(this.pictureBox6);
            this.pnl3.Controls.Add(this.pictureBox11);
            this.pnl3.Controls.Add(this.label49);
            this.pnl3.Location = new System.Drawing.Point(80, 289);
            this.pnl3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl3.Name = "pnl3";
            this.pnl3.Size = new System.Drawing.Size(682, 399);
            this.pnl3.TabIndex = 14;
            // 
            // pnlHostingServices
            // 
            this.pnlHostingServices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlHostingServices.BackColor = System.Drawing.SystemColors.Control;
            this.pnlHostingServices.Controls.Add(this.txtPackagesContainer);
            this.pnlHostingServices.Controls.Add(this.label58);
            this.pnlHostingServices.Controls.Add(this.pictureBox12);
            this.pnlHostingServices.Controls.Add(this.txtVHDDriveSize);
            this.pnlHostingServices.Controls.Add(this.label57);
            this.pnlHostingServices.Controls.Add(this.txtVHDName);
            this.pnlHostingServices.Controls.Add(this.label56);
            this.pnlHostingServices.Controls.Add(this.label55);
            this.pnlHostingServices.Controls.Add(this.cboStorage);
            this.pnlHostingServices.Controls.Add(this.label54);
            this.pnlHostingServices.Controls.Add(this.label51);
            this.pnlHostingServices.Controls.Add(this.cboEnvironment);
            this.pnlHostingServices.Controls.Add(this.label53);
            this.pnlHostingServices.Controls.Add(this.label50);
            this.pnlHostingServices.Controls.Add(this.cboHostingService);
            this.pnlHostingServices.Controls.Add(this.label52);
            this.pnlHostingServices.Location = new System.Drawing.Point(0, 64);
            this.pnlHostingServices.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlHostingServices.Name = "pnlHostingServices";
            this.pnlHostingServices.Size = new System.Drawing.Size(681, 339);
            this.pnlHostingServices.TabIndex = 4;
            // 
            // txtPackagesContainer
            // 
            this.txtPackagesContainer.Location = new System.Drawing.Point(180, 185);
            this.txtPackagesContainer.MaxLength = 63;
            this.txtPackagesContainer.Name = "txtPackagesContainer";
            this.txtPackagesContainer.Size = new System.Drawing.Size(245, 23);
            this.txtPackagesContainer.TabIndex = 16;
            // 
            // label58
            // 
            this.label58.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label58.Location = new System.Drawing.Point(49, 276);
            this.label58.Name = "label58";
            this.label58.Size = new System.Drawing.Size(601, 37);
            this.label58.TabIndex = 29;
            this.label58.Text = "Be sure to locate your hosting service and storage account in the same location. " +
    "This will result in better performance and avoid extra transfer charges between " +
    "different Azure datacenters";
            // 
            // pictureBox12
            // 
            this.pictureBox12.Image = global::DNNAzureWizard.Properties.Resources.icon_warning;
            this.pictureBox12.Location = new System.Drawing.Point(28, 276);
            this.pictureBox12.Name = "pictureBox12";
            this.pictureBox12.Size = new System.Drawing.Size(27, 37);
            this.pictureBox12.TabIndex = 28;
            this.pictureBox12.TabStop = false;
            // 
            // txtVHDDriveSize
            // 
            this.txtVHDDriveSize.Location = new System.Drawing.Point(180, 246);
            this.txtVHDDriveSize.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtVHDDriveSize.Name = "txtVHDDriveSize";
            this.txtVHDDriveSize.Size = new System.Drawing.Size(91, 23);
            this.txtVHDDriveSize.TabIndex = 18;
            // 
            // label57
            // 
            this.label57.AutoSize = true;
            this.label57.Location = new System.Drawing.Point(63, 249);
            this.label57.Name = "label57";
            this.label57.Size = new System.Drawing.Size(111, 15);
            this.label57.TabIndex = 26;
            this.label57.Text = "VHD drive size (Mb)";
            // 
            // txtVHDName
            // 
            this.txtVHDName.Location = new System.Drawing.Point(180, 216);
            this.txtVHDName.Name = "txtVHDName";
            this.txtVHDName.Size = new System.Drawing.Size(245, 23);
            this.txtVHDName.TabIndex = 17;
            // 
            // label56
            // 
            this.label56.AutoSize = true;
            this.label56.Location = new System.Drawing.Point(25, 219);
            this.label56.Name = "label56";
            this.label56.Size = new System.Drawing.Size(149, 15);
            this.label56.TabIndex = 24;
            this.label56.Text = "VHD drive page blob name";
            // 
            // label55
            // 
            this.label55.AutoSize = true;
            this.label55.Location = new System.Drawing.Point(41, 188);
            this.label55.Name = "label55";
            this.label55.Size = new System.Drawing.Size(133, 15);
            this.label55.TabIndex = 22;
            this.label55.Text = "Storage container name";
            // 
            // cboStorage
            // 
            this.cboStorage.DisplayMember = "Name";
            this.cboStorage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStorage.FormattingEnabled = true;
            this.cboStorage.Items.AddRange(new object[] {
            "<Create new...>",
            "<Refresh...>"});
            this.cboStorage.Location = new System.Drawing.Point(180, 156);
            this.cboStorage.Name = "cboStorage";
            this.cboStorage.Size = new System.Drawing.Size(245, 23);
            this.cboStorage.TabIndex = 15;
            this.cboStorage.ValueMember = "SubscriptionId";
            this.cboStorage.SelectedIndexChanged += new System.EventHandler(this.CboStorageSelectedIndexChanged);
            // 
            // label54
            // 
            this.label54.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label54.Location = new System.Drawing.Point(10, 130);
            this.label54.Name = "label54";
            this.label54.Size = new System.Drawing.Size(661, 29);
            this.label54.TabIndex = 19;
            this.label54.Text = "Now select the storage account where to store blobs and cloud drives for this ins" +
    "tance:";
            // 
            // label51
            // 
            this.label51.AutoSize = true;
            this.label51.Location = new System.Drawing.Point(79, 159);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(95, 15);
            this.label51.TabIndex = 18;
            this.label51.Text = "Storage Account";
            // 
            // cboEnvironment
            // 
            this.cboEnvironment.DisplayMember = "Name";
            this.cboEnvironment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEnvironment.FormattingEnabled = true;
            this.cboEnvironment.Items.AddRange(new object[] {
            "Production",
            "Staging"});
            this.cboEnvironment.Location = new System.Drawing.Point(180, 66);
            this.cboEnvironment.Name = "cboEnvironment";
            this.cboEnvironment.Size = new System.Drawing.Size(245, 23);
            this.cboEnvironment.TabIndex = 14;
            this.cboEnvironment.ValueMember = "SubscriptionId";
            // 
            // label53
            // 
            this.label53.AutoSize = true;
            this.label53.Location = new System.Drawing.Point(99, 69);
            this.label53.Name = "label53";
            this.label53.Size = new System.Drawing.Size(75, 15);
            this.label53.TabIndex = 16;
            this.label53.Text = "Environment";
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Location = new System.Drawing.Point(89, 42);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(85, 15);
            this.label50.TabIndex = 14;
            this.label50.Text = "Hosted Service";
            // 
            // cboHostingService
            // 
            this.cboHostingService.DisplayMember = "Name";
            this.cboHostingService.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHostingService.FormattingEnabled = true;
            this.cboHostingService.Items.AddRange(new object[] {
            "<Create new...>",
            "<Refresh...>"});
            this.cboHostingService.Location = new System.Drawing.Point(180, 39);
            this.cboHostingService.Name = "cboHostingService";
            this.cboHostingService.Size = new System.Drawing.Size(245, 23);
            this.cboHostingService.TabIndex = 13;
            this.cboHostingService.ValueMember = "SubscriptionId";
            this.cboHostingService.SelectedIndexChanged += new System.EventHandler(this.CboHostingServiceSelectedIndexChanged);
            // 
            // label52
            // 
            this.label52.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label52.Location = new System.Drawing.Point(10, 11);
            this.label52.Name = "label52";
            this.label52.Size = new System.Drawing.Size(661, 37);
            this.label52.TabIndex = 1;
            this.label52.Text = "Select from the lists below the Hosted Service where to deploy the new DotNetNuke" +
    " instance:";
            // 
            // pictureBox6
            // 
            this.pictureBox6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox6.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox6.Image")));
            this.pictureBox6.Location = new System.Drawing.Point(625, 6);
            this.pictureBox6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(50, 50);
            this.pictureBox6.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox6.TabIndex = 5;
            this.pictureBox6.TabStop = false;
            // 
            // pictureBox11
            // 
            this.pictureBox11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox11.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox11.Image")));
            this.pictureBox11.Location = new System.Drawing.Point(506, 21);
            this.pictureBox11.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox11.Name = "pictureBox11";
            this.pictureBox11.Size = new System.Drawing.Size(114, 23);
            this.pictureBox11.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox11.TabIndex = 3;
            this.pictureBox11.TabStop = false;
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label49.Location = new System.Drawing.Point(10, 18);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(296, 25);
            this.label49.TabIndex = 2;
            this.label49.Text = "Hosting && Storage Services Settings";
            // 
            // pnl12
            // 
            this.pnl12.Controls.Add(this.pictureBox22);
            this.pnl12.Controls.Add(this.pictureBox23);
            this.pnl12.Controls.Add(this.panel3);
            this.pnl12.Controls.Add(this.lblSuccess);
            this.pnl12.Location = new System.Drawing.Point(628, 136);
            this.pnl12.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl12.Name = "pnl12";
            this.pnl12.Size = new System.Drawing.Size(680, 366);
            this.pnl12.TabIndex = 9;
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.BackColor = System.Drawing.SystemColors.Control;
            this.panel3.Controls.Add(this.txtLogFinal);
            this.panel3.Controls.Add(this.label29);
            this.panel3.Location = new System.Drawing.Point(0, 66);
            this.panel3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(678, 299);
            this.panel3.TabIndex = 3;
            // 
            // txtLogFinal
            // 
            this.txtLogFinal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogFinal.BackColor = System.Drawing.SystemColors.Control;
            this.txtLogFinal.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLogFinal.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtLogFinal.Location = new System.Drawing.Point(15, 48);
            this.txtLogFinal.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtLogFinal.Multiline = true;
            this.txtLogFinal.Name = "txtLogFinal";
            this.txtLogFinal.ReadOnly = true;
            this.txtLogFinal.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLogFinal.Size = new System.Drawing.Size(644, 263);
            this.txtLogFinal.TabIndex = 1;
            // 
            // label29
            // 
            this.label29.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label29.Location = new System.Drawing.Point(16, 19);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(637, 24);
            this.label29.TabIndex = 0;
            this.label29.Text = "Check the log window in order to review the process of uploading DotNetNuke to Wi" +
    "ndows Azure";
            // 
            // lblSuccess
            // 
            this.lblSuccess.AutoSize = true;
            this.lblSuccess.Font = new System.Drawing.Font("Segoe UI Light", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSuccess.Location = new System.Drawing.Point(16, 19);
            this.lblSuccess.Name = "lblSuccess";
            this.lblSuccess.Size = new System.Drawing.Size(76, 25);
            this.lblSuccess.TabIndex = 2;
            this.lblSuccess.Text = "Success!";
            // 
            // pnl5
            // 
            this.pnl5.Controls.Add(this.pictureBox13);
            this.pnl5.Controls.Add(this.DBSettings);
            this.pnl5.Controls.Add(this.label3);
            this.pnl5.Controls.Add(this.pictureBox4);
            this.pnl5.Location = new System.Drawing.Point(745, 70);
            this.pnl5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl5.Name = "pnl5";
            this.pnl5.Size = new System.Drawing.Size(684, 432);
            this.pnl5.TabIndex = 4;
            // 
            // pictureBox13
            // 
            this.pictureBox13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox13.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox13.Image")));
            this.pictureBox13.Location = new System.Drawing.Point(624, 11);
            this.pictureBox13.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox13.Name = "pictureBox13";
            this.pictureBox13.Size = new System.Drawing.Size(50, 50);
            this.pictureBox13.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox13.TabIndex = 6;
            this.pictureBox13.TabStop = false;
            // 
            // DBSettings
            // 
            this.DBSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DBSettings.BackColor = System.Drawing.SystemColors.Control;
            this.DBSettings.Controls.Add(this.cboDatabase);
            this.DBSettings.Controls.Add(this.label24);
            this.DBSettings.Controls.Add(this.label23);
            this.DBSettings.Controls.Add(this.txtDBRePassword);
            this.DBSettings.Controls.Add(this.btnTestDB);
            this.DBSettings.Controls.Add(this.label16);
            this.DBSettings.Controls.Add(this.label15);
            this.DBSettings.Controls.Add(this.txtDBPassword);
            this.DBSettings.Controls.Add(this.label14);
            this.DBSettings.Controls.Add(this.label13);
            this.DBSettings.Controls.Add(this.txtDBUser);
            this.DBSettings.Controls.Add(this.label12);
            this.DBSettings.Controls.Add(this.txtDBAdminPassword);
            this.DBSettings.Controls.Add(this.label11);
            this.DBSettings.Controls.Add(this.label10);
            this.DBSettings.Controls.Add(this.txtDBAdminUser);
            this.DBSettings.Controls.Add(this.label9);
            this.DBSettings.Controls.Add(this.label8);
            this.DBSettings.Controls.Add(this.txtDBName);
            this.DBSettings.Controls.Add(this.label7);
            this.DBSettings.Controls.Add(this.label6);
            this.DBSettings.Controls.Add(this.txtDBServer);
            this.DBSettings.Controls.Add(this.label5);
            this.DBSettings.Controls.Add(this.label4);
            this.DBSettings.Location = new System.Drawing.Point(0, 69);
            this.DBSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DBSettings.Name = "DBSettings";
            this.DBSettings.Size = new System.Drawing.Size(682, 362);
            this.DBSettings.TabIndex = 2;
            // 
            // cboDatabase
            // 
            this.cboDatabase.DisplayMember = "Name";
            this.cboDatabase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDatabase.FormattingEnabled = true;
            this.cboDatabase.Items.AddRange(new object[] {
            "<Create new...>",
            "<Refresh...>"});
            this.cboDatabase.Location = new System.Drawing.Point(133, 48);
            this.cboDatabase.Name = "cboDatabase";
            this.cboDatabase.Size = new System.Drawing.Size(116, 23);
            this.cboDatabase.TabIndex = 22;
            this.cboDatabase.ValueMember = "SubscriptionId";
            this.cboDatabase.SelectedIndexChanged += new System.EventHandler(this.CboDatabaseSelectedIndexChanged);
            // 
            // label24
            // 
            this.label24.Location = new System.Drawing.Point(275, 299);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(385, 34);
            this.label24.TabIndex = 21;
            this.label24.Text = "Confirm the DB password.";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(17, 299);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(104, 15);
            this.label23.TabIndex = 20;
            this.label23.Text = "Confirm Password";
            // 
            // txtDBRePassword
            // 
            this.txtDBRePassword.Location = new System.Drawing.Point(133, 296);
            this.txtDBRePassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBRePassword.Name = "txtDBRePassword";
            this.txtDBRePassword.Size = new System.Drawing.Size(116, 23);
            this.txtDBRePassword.TabIndex = 19;
            this.txtDBRePassword.UseSystemPasswordChar = true;
            this.txtDBRePassword.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBRePasswordValidating);
            // 
            // btnTestDB
            // 
            this.btnTestDB.Location = new System.Drawing.Point(133, 147);
            this.btnTestDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnTestDB.Name = "btnTestDB";
            this.btnTestDB.Size = new System.Drawing.Size(116, 26);
            this.btnTestDB.TabIndex = 3;
            this.btnTestDB.Text = "Test Connection";
            this.btnTestDB.UseVisualStyleBackColor = true;
            this.btnTestDB.Click += new System.EventHandler(this.BtnTestDBClick);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(275, 266);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(239, 15);
            this.label16.TabIndex = 18;
            this.label16.Text = "Password for the DotNetNuke database user";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(17, 266);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(75, 15);
            this.label15.TabIndex = 17;
            this.label15.Text = "DB Password";
            // 
            // txtDBPassword
            // 
            this.txtDBPassword.Location = new System.Drawing.Point(133, 263);
            this.txtDBPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBPassword.Name = "txtDBPassword";
            this.txtDBPassword.Size = new System.Drawing.Size(116, 23);
            this.txtDBPassword.TabIndex = 6;
            this.txtDBPassword.UseSystemPasswordChar = true;
            this.txtDBPassword.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBPasswordValidating);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(275, 234);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(265, 15);
            this.label14.TabIndex = 15;
            this.label14.Text = "User name that will be created during installation";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(17, 234);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(83, 15);
            this.label13.TabIndex = 14;
            this.label13.Text = "DB User Name";
            // 
            // txtDBUser
            // 
            this.txtDBUser.Location = new System.Drawing.Point(133, 231);
            this.txtDBUser.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBUser.Name = "txtDBUser";
            this.txtDBUser.Size = new System.Drawing.Size(116, 23);
            this.txtDBUser.TabIndex = 5;
            this.txtDBUser.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBUserValidating);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(275, 117);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(157, 15);
            this.label12.TabIndex = 12;
            this.label12.Text = "Password for the admin user";
            // 
            // txtDBAdminPassword
            // 
            this.txtDBAdminPassword.Location = new System.Drawing.Point(133, 114);
            this.txtDBAdminPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBAdminPassword.Name = "txtDBAdminPassword";
            this.txtDBAdminPassword.Size = new System.Drawing.Size(116, 23);
            this.txtDBAdminPassword.TabIndex = 2;
            this.txtDBAdminPassword.UseSystemPasswordChar = true;
            this.txtDBAdminPassword.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBAdminPasswordValidating);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(17, 121);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(96, 15);
            this.label11.TabIndex = 10;
            this.label11.Text = "Admin Password";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(275, 86);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(245, 15);
            this.label10.TabIndex = 9;
            this.label10.Text = "User with admin rights to create the database";
            // 
            // txtDBAdminUser
            // 
            this.txtDBAdminUser.Location = new System.Drawing.Point(133, 82);
            this.txtDBAdminUser.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBAdminUser.Name = "txtDBAdminUser";
            this.txtDBAdminUser.Size = new System.Drawing.Size(116, 23);
            this.txtDBAdminUser.TabIndex = 1;
            this.txtDBAdminUser.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBAdminUserValidating);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(17, 86);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(104, 15);
            this.label9.TabIndex = 7;
            this.label9.Text = "Admin User Name";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(275, 202);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(337, 15);
            this.label8.TabIndex = 6;
            this.label8.Text = "The database will be created during installation (1Gb database)";
            // 
            // txtDBName
            // 
            this.txtDBName.Location = new System.Drawing.Point(133, 199);
            this.txtDBName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBName.Name = "txtDBName";
            this.txtDBName.Size = new System.Drawing.Size(116, 23);
            this.txtDBName.TabIndex = 4;
            this.txtDBName.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBNameValidating);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(17, 203);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 15);
            this.label7.TabIndex = 4;
            this.label7.Text = "Database Name";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(271, 53);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(127, 15);
            this.label6.TabIndex = 3;
            this.label6.Text = ".database.windows.net";
            // 
            // txtDBServer
            // 
            this.txtDBServer.Location = new System.Drawing.Point(133, 49);
            this.txtDBServer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDBServer.Name = "txtDBServer";
            this.txtDBServer.Size = new System.Drawing.Size(116, 23);
            this.txtDBServer.TabIndex = 0;
            this.txtDBServer.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDBServerValidating);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 53);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 15);
            this.label5.TabIndex = 1;
            this.label5.Text = "SQL Azure Server";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(13, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(661, 37);
            this.label4.TabIndex = 0;
            this.label4.Text = "Please, complete all the SQL Azure related fields where the DotNetNuke database w" +
    "ill be deployed. Note that the database will be created during the installation " +
    "process.";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(255, 25);
            this.label3.TabIndex = 1;
            this.label3.Text = "SQL Azure connection settings";
            // 
            // pictureBox4
            // 
            this.pictureBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox4.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox4.Image")));
            this.pictureBox4.Location = new System.Drawing.Point(543, 36);
            this.pictureBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(75, 23);
            this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox4.TabIndex = 0;
            this.pictureBox4.TabStop = false;
            // 
            // pnl9
            // 
            this.pnl9.Controls.Add(this.pictureBox7);
            this.pnl9.Controls.Add(this.pictureBox18);
            this.pnl9.Controls.Add(this.PackageSettings);
            this.pnl9.Controls.Add(this.label37);
            this.pnl9.Location = new System.Drawing.Point(466, 46);
            this.pnl9.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl9.Name = "pnl9";
            this.pnl9.Size = new System.Drawing.Size(682, 372);
            this.pnl9.TabIndex = 10;
            // 
            // PackageSettings
            // 
            this.PackageSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PackageSettings.BackColor = System.Drawing.SystemColors.Control;
            this.PackageSettings.Controls.Add(this.lnkMorePackages);
            this.PackageSettings.Controls.Add(this.label62);
            this.PackageSettings.Controls.Add(this.txtDNNUrl);
            this.PackageSettings.Controls.Add(this.lblCustomUrl);
            this.PackageSettings.Controls.Add(this.cboDNNVersion);
            this.PackageSettings.Controls.Add(this.label60);
            this.PackageSettings.Controls.Add(this.label59);
            this.PackageSettings.Controls.Add(this.chkAutoInstall);
            this.PackageSettings.Controls.Add(this.lstPackages);
            this.PackageSettings.Controls.Add(this.label36);
            this.PackageSettings.Location = new System.Drawing.Point(0, 64);
            this.PackageSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PackageSettings.Name = "PackageSettings";
            this.PackageSettings.Size = new System.Drawing.Size(681, 312);
            this.PackageSettings.TabIndex = 4;
            // 
            // lnkMorePackages
            // 
            this.lnkMorePackages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkMorePackages.AutoSize = true;
            this.lnkMorePackages.Location = new System.Drawing.Point(508, 37);
            this.lnkMorePackages.Name = "lnkMorePackages";
            this.lnkMorePackages.Size = new System.Drawing.Size(158, 15);
            this.lnkMorePackages.TabIndex = 38;
            this.lnkMorePackages.TabStop = true;
            this.lnkMorePackages.Text = "Reload from packages folder";
            this.lnkMorePackages.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkMorePackages_LinkClicked);
            // 
            // label62
            // 
            this.label62.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label62.Location = new System.Drawing.Point(12, 135);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(650, 46);
            this.label62.TabIndex = 37;
            this.label62.Text = resources.GetString("label62.Text");
            // 
            // txtDNNUrl
            // 
            this.txtDNNUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDNNUrl.Location = new System.Drawing.Point(186, 211);
            this.txtDNNUrl.Name = "txtDNNUrl";
            this.txtDNNUrl.Size = new System.Drawing.Size(476, 23);
            this.txtDNNUrl.TabIndex = 35;
            this.txtDNNUrl.Validating += new System.ComponentModel.CancelEventHandler(this.TxtDNNUrlValidating);
            // 
            // lblCustomUrl
            // 
            this.lblCustomUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCustomUrl.Location = new System.Drawing.Point(13, 214);
            this.lblCustomUrl.Name = "lblCustomUrl";
            this.lblCustomUrl.Size = new System.Drawing.Size(167, 15);
            this.lblCustomUrl.TabIndex = 36;
            this.lblCustomUrl.Text = "Custom Url";
            // 
            // cboDNNVersion
            // 
            this.cboDNNVersion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboDNNVersion.DisplayMember = "Description";
            this.cboDNNVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDNNVersion.FormattingEnabled = true;
            this.cboDNNVersion.Items.AddRange(new object[] {
            "<Create new...>",
            "<Refresh...>"});
            this.cboDNNVersion.Location = new System.Drawing.Point(186, 184);
            this.cboDNNVersion.Name = "cboDNNVersion";
            this.cboDNNVersion.Size = new System.Drawing.Size(476, 23);
            this.cboDNNVersion.TabIndex = 34;
            this.cboDNNVersion.ValueMember = "SubscriptionId";
            this.cboDNNVersion.SelectedIndexChanged += new System.EventHandler(this.cboDNNVersion_SelectedIndexChanged);
            // 
            // label60
            // 
            this.label60.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label60.Location = new System.Drawing.Point(12, 187);
            this.label60.Name = "label60";
            this.label60.Size = new System.Drawing.Size(192, 15);
            this.label60.TabIndex = 33;
            this.label60.Text = "DotNetNuke Version to deploy";
            // 
            // label59
            // 
            this.label59.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label59.Location = new System.Drawing.Point(10, 243);
            this.label59.Name = "label59";
            this.label59.Size = new System.Drawing.Size(661, 37);
            this.label59.TabIndex = 32;
            this.label59.Text = "If you want that the wizard install DotNetNuke using the default parameters you c" +
    "an check the box below. This will give you a DotNetNuke site running as the resu" +
    "lt of this wizard.";
            // 
            // chkAutoInstall
            // 
            this.chkAutoInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoInstall.AutoSize = true;
            this.chkAutoInstall.Checked = true;
            this.chkAutoInstall.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoInstall.Location = new System.Drawing.Point(13, 283);
            this.chkAutoInstall.Name = "chkAutoInstall";
            this.chkAutoInstall.Size = new System.Drawing.Size(401, 19);
            this.chkAutoInstall.TabIndex = 31;
            this.chkAutoInstall.Text = "Auto-install the DotNetNuke hosting instance using default parameters";
            this.chkAutoInstall.UseVisualStyleBackColor = true;
            // 
            // lstPackages
            // 
            this.lstPackages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstPackages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.packageName,
            this.packageDescription});
            this.lstPackages.FullRowSelect = true;
            this.lstPackages.HideSelection = false;
            this.lstPackages.Location = new System.Drawing.Point(14, 56);
            this.lstPackages.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lstPackages.MultiSelect = false;
            this.lstPackages.Name = "lstPackages";
            this.lstPackages.Size = new System.Drawing.Size(648, 65);
            this.lstPackages.TabIndex = 2;
            this.lstPackages.UseCompatibleStateImageBehavior = false;
            this.lstPackages.View = System.Windows.Forms.View.Details;
            this.lstPackages.Validating += new System.ComponentModel.CancelEventHandler(this.LstPackagesValidating);
            // 
            // packageName
            // 
            this.packageName.Text = "Package name";
            this.packageName.Width = 200;
            // 
            // packageDescription
            // 
            this.packageDescription.Text = "Description";
            this.packageDescription.Width = 350;
            // 
            // label36
            // 
            this.label36.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label36.Location = new System.Drawing.Point(10, 11);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(661, 37);
            this.label36.TabIndex = 1;
            this.label36.Text = "Please, select the package that you want to use to deploy. Each package can defin" +
    "e a different deployment architecture and would include different behaviours whe" +
    "n running on Azure environment.";
            // 
            // label37
            // 
            this.label37.AutoSize = true;
            this.label37.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label37.Location = new System.Drawing.Point(10, 23);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(283, 25);
            this.label37.TabIndex = 2;
            this.label37.Text = "Accelerator deployment packages";
            // 
            // pnl6
            // 
            this.pnl6.Controls.Add(this.pictureBox8);
            this.pnl6.Controls.Add(this.pictureBox16);
            this.pnl6.Controls.Add(this.panel5);
            this.pnl6.Controls.Add(this.label39);
            this.pnl6.Location = new System.Drawing.Point(12, 14);
            this.pnl6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl6.Name = "pnl6";
            this.pnl6.Size = new System.Drawing.Size(682, 408);
            this.pnl6.TabIndex = 11;
            // 
            // panel5
            // 
            this.panel5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel5.BackColor = System.Drawing.SystemColors.Control;
            this.panel5.Controls.Add(this.chkEnableRemoteMgmt);
            this.panel5.Controls.Add(this.pnlRDP);
            this.panel5.Controls.Add(this.label38);
            this.panel5.Location = new System.Drawing.Point(0, 64);
            this.panel5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(681, 348);
            this.panel5.TabIndex = 4;
            // 
            // chkEnableRemoteMgmt
            // 
            this.chkEnableRemoteMgmt.AutoSize = true;
            this.chkEnableRemoteMgmt.Checked = true;
            this.chkEnableRemoteMgmt.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableRemoteMgmt.Location = new System.Drawing.Point(12, 50);
            this.chkEnableRemoteMgmt.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkEnableRemoteMgmt.Name = "chkEnableRemoteMgmt";
            this.chkEnableRemoteMgmt.Size = new System.Drawing.Size(222, 19);
            this.chkEnableRemoteMgmt.TabIndex = 1;
            this.chkEnableRemoteMgmt.Text = "Enable Remote Management options";
            this.chkEnableRemoteMgmt.UseVisualStyleBackColor = true;
            this.chkEnableRemoteMgmt.CheckedChanged += new System.EventHandler(this.chkEnableRemoteMgmt_CheckedChanged);
            // 
            // pnlRDP
            // 
            this.pnlRDP.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlRDP.Controls.Add(this.chkEnableFTP);
            this.pnlRDP.Controls.Add(this.chkEnableRDP);
            this.pnlRDP.Controls.Add(this.chkWebDeploy);
            this.pnlRDP.Controls.Add(this.cboRDPExpirationDate);
            this.pnlRDP.Controls.Add(this.label32);
            this.pnlRDP.Controls.Add(this.label33);
            this.pnlRDP.Controls.Add(this.txtRDPConfirmPassword);
            this.pnlRDP.Controls.Add(this.label40);
            this.pnlRDP.Controls.Add(this.txtRDPPassword);
            this.pnlRDP.Controls.Add(this.label42);
            this.pnlRDP.Controls.Add(this.txtRDPUser);
            this.pnlRDP.Controls.Add(this.cboCertificates);
            this.pnlRDP.Controls.Add(this.lblRDPCredentialsInfo);
            this.pnlRDP.Controls.Add(this.cmdViewCertificate);
            this.pnlRDP.Controls.Add(this.lblRDPInfo);
            this.pnlRDP.Enabled = false;
            this.pnlRDP.Location = new System.Drawing.Point(6, 78);
            this.pnlRDP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlRDP.Name = "pnlRDP";
            this.pnlRDP.Size = new System.Drawing.Size(662, 251);
            this.pnlRDP.TabIndex = 2;
            // 
            // chkEnableFTP
            // 
            this.chkEnableFTP.AutoSize = true;
            this.chkEnableFTP.Location = new System.Drawing.Point(461, 151);
            this.chkEnableFTP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkEnableFTP.Name = "chkEnableFTP";
            this.chkEnableFTP.Size = new System.Drawing.Size(84, 19);
            this.chkEnableFTP.TabIndex = 17;
            this.chkEnableFTP.Text = "Enable FTP";
            this.chkEnableFTP.UseVisualStyleBackColor = true;
            // 
            // chkEnableRDP
            // 
            this.chkEnableRDP.AutoSize = true;
            this.chkEnableRDP.Checked = true;
            this.chkEnableRDP.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableRDP.Location = new System.Drawing.Point(461, 99);
            this.chkEnableRDP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkEnableRDP.Name = "chkEnableRDP";
            this.chkEnableRDP.Size = new System.Drawing.Size(184, 19);
            this.chkEnableRDP.TabIndex = 15;
            this.chkEnableRDP.Text = "Enable Remote Desktop (RDP)";
            this.chkEnableRDP.UseVisualStyleBackColor = true;
            this.chkEnableRDP.CheckedChanged += new System.EventHandler(this.ChkEnableRDPCheckedChanged);
            // 
            // chkWebDeploy
            // 
            this.chkWebDeploy.AutoSize = true;
            this.chkWebDeploy.Location = new System.Drawing.Point(461, 126);
            this.chkWebDeploy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkWebDeploy.Name = "chkWebDeploy";
            this.chkWebDeploy.Size = new System.Drawing.Size(128, 19);
            this.chkWebDeploy.TabIndex = 16;
            this.chkWebDeploy.Text = "Enable Web Deploy";
            this.chkWebDeploy.UseVisualStyleBackColor = true;
            // 
            // cboRDPExpirationDate
            // 
            this.cboRDPExpirationDate.Location = new System.Drawing.Point(153, 192);
            this.cboRDPExpirationDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cboRDPExpirationDate.Name = "cboRDPExpirationDate";
            this.cboRDPExpirationDate.Size = new System.Drawing.Size(263, 23);
            this.cboRDPExpirationDate.TabIndex = 14;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(12, 197);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(134, 15);
            this.label32.TabIndex = 13;
            this.label32.Text = "Account Expiration Date";
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(11, 168);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(104, 15);
            this.label33.TabIndex = 11;
            this.label33.Text = "Confirm Password";
            // 
            // txtRDPConfirmPassword
            // 
            this.txtRDPConfirmPassword.Location = new System.Drawing.Point(153, 161);
            this.txtRDPConfirmPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtRDPConfirmPassword.Name = "txtRDPConfirmPassword";
            this.txtRDPConfirmPassword.Size = new System.Drawing.Size(116, 23);
            this.txtRDPConfirmPassword.TabIndex = 12;
            this.txtRDPConfirmPassword.UseSystemPasswordChar = true;
            this.txtRDPConfirmPassword.Validating += new System.ComponentModel.CancelEventHandler(this.TxtRDPConfirmPasswordValidating);
            // 
            // label40
            // 
            this.label40.AutoSize = true;
            this.label40.Location = new System.Drawing.Point(12, 136);
            this.label40.Name = "label40";
            this.label40.Size = new System.Drawing.Size(57, 15);
            this.label40.TabIndex = 9;
            this.label40.Text = "Password";
            // 
            // txtRDPPassword
            // 
            this.txtRDPPassword.Location = new System.Drawing.Point(153, 130);
            this.txtRDPPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtRDPPassword.Name = "txtRDPPassword";
            this.txtRDPPassword.Size = new System.Drawing.Size(116, 23);
            this.txtRDPPassword.TabIndex = 10;
            this.txtRDPPassword.UseSystemPasswordChar = true;
            this.txtRDPPassword.Validating += new System.ComponentModel.CancelEventHandler(this.TxtRDPPasswordValidating);
            // 
            // label42
            // 
            this.label42.AutoSize = true;
            this.label42.Location = new System.Drawing.Point(12, 104);
            this.label42.Name = "label42";
            this.label42.Size = new System.Drawing.Size(65, 15);
            this.label42.TabIndex = 7;
            this.label42.Text = "User Name";
            // 
            // txtRDPUser
            // 
            this.txtRDPUser.Location = new System.Drawing.Point(153, 99);
            this.txtRDPUser.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtRDPUser.Name = "txtRDPUser";
            this.txtRDPUser.Size = new System.Drawing.Size(116, 23);
            this.txtRDPUser.TabIndex = 8;
            this.txtRDPUser.Validating += new System.ComponentModel.CancelEventHandler(this.TxtRDPUserValidating);
            // 
            // cboCertificates
            // 
            this.cboCertificates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCertificates.FormattingEnabled = true;
            this.cboCertificates.ItemHeight = 15;
            this.cboCertificates.Location = new System.Drawing.Point(14, 26);
            this.cboCertificates.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cboCertificates.Name = "cboCertificates";
            this.cboCertificates.Size = new System.Drawing.Size(500, 23);
            this.cboCertificates.TabIndex = 4;
            this.cboCertificates.SelectedIndexChanged += new System.EventHandler(this.CboCertificatesSelectedIndexChanged);
            this.cboCertificates.Validating += new System.ComponentModel.CancelEventHandler(this.CboCertificatesValidating);
            // 
            // lblRDPCredentialsInfo
            // 
            this.lblRDPCredentialsInfo.Location = new System.Drawing.Point(10, 61);
            this.lblRDPCredentialsInfo.Name = "lblRDPCredentialsInfo";
            this.lblRDPCredentialsInfo.Size = new System.Drawing.Size(617, 34);
            this.lblRDPCredentialsInfo.TabIndex = 6;
            this.lblRDPCredentialsInfo.Text = "Specify the user credentials that will be used to connect remotely. A Windows loc" +
    "al account will be created on each role with these credentials. ";
            // 
            // cmdViewCertificate
            // 
            this.cmdViewCertificate.Location = new System.Drawing.Point(541, 24);
            this.cmdViewCertificate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmdViewCertificate.Name = "cmdViewCertificate";
            this.cmdViewCertificate.Size = new System.Drawing.Size(70, 26);
            this.cmdViewCertificate.TabIndex = 5;
            this.cmdViewCertificate.Text = "View...";
            this.cmdViewCertificate.UseVisualStyleBackColor = true;
            this.cmdViewCertificate.Click += new System.EventHandler(this.CmdViewCertificateClick);
            // 
            // lblRDPInfo
            // 
            this.lblRDPInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRDPInfo.Location = new System.Drawing.Point(10, 6);
            this.lblRDPInfo.Name = "lblRDPInfo";
            this.lblRDPInfo.Size = new System.Drawing.Size(626, 41);
            this.lblRDPInfo.TabIndex = 3;
            this.lblRDPInfo.Text = "Create or select a certificate to encrypt the user credentials. ";
            // 
            // label38
            // 
            this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label38.Location = new System.Drawing.Point(10, 11);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(661, 37);
            this.label38.TabIndex = 0;
            this.label38.Text = resources.GetString("label38.Text");
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label39.Location = new System.Drawing.Point(10, 18);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(256, 25);
            this.label39.TabIndex = 2;
            this.label39.Text = "Remote Management Settings";
            // 
            // pnl7
            // 
            this.pnl7.Controls.Add(this.pictureBox9);
            this.pnl7.Controls.Add(this.pictureBox17);
            this.pnl7.Controls.Add(this.pnlAzureConnect);
            this.pnl7.Controls.Add(this.label48);
            this.pnl7.Location = new System.Drawing.Point(-2, 174);
            this.pnl7.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl7.Name = "pnl7";
            this.pnl7.Size = new System.Drawing.Size(682, 399);
            this.pnl7.TabIndex = 12;
            // 
            // pnlAzureConnect
            // 
            this.pnlAzureConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAzureConnect.BackColor = System.Drawing.SystemColors.Control;
            this.pnlAzureConnect.Controls.Add(this.lnkConnectHelp);
            this.pnlAzureConnect.Controls.Add(this.label41);
            this.pnlAzureConnect.Controls.Add(this.txtConnectActivationToken);
            this.pnlAzureConnect.Controls.Add(this.label35);
            this.pnlAzureConnect.Controls.Add(this.chkAzureConnect);
            this.pnlAzureConnect.Controls.Add(this.label47);
            this.pnlAzureConnect.Location = new System.Drawing.Point(0, 64);
            this.pnlAzureConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlAzureConnect.Name = "pnlAzureConnect";
            this.pnlAzureConnect.Size = new System.Drawing.Size(681, 339);
            this.pnlAzureConnect.TabIndex = 4;
            // 
            // lnkConnectHelp
            // 
            this.lnkConnectHelp.AutoSize = true;
            this.lnkConnectHelp.Location = new System.Drawing.Point(37, 157);
            this.lnkConnectHelp.Name = "lnkConnectHelp";
            this.lnkConnectHelp.Size = new System.Drawing.Size(224, 15);
            this.lnkConnectHelp.TabIndex = 10;
            this.lnkConnectHelp.TabStop = true;
            this.lnkConnectHelp.Text = "Online Help for Windows Azure Connect.";
            this.lnkConnectHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1LinkClicked);
            // 
            // label41
            // 
            this.label41.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label41.Location = new System.Drawing.Point(35, 136);
            this.label41.Name = "label41";
            this.label41.Size = new System.Drawing.Size(630, 22);
            this.label41.TabIndex = 9;
            this.label41.Text = "For more information about joining a Windows Azure role to a local domain, see:";
            // 
            // txtConnectActivationToken
            // 
            this.txtConnectActivationToken.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnectActivationToken.Enabled = false;
            this.txtConnectActivationToken.Location = new System.Drawing.Point(37, 107);
            this.txtConnectActivationToken.Name = "txtConnectActivationToken";
            this.txtConnectActivationToken.Size = new System.Drawing.Size(602, 23);
            this.txtConnectActivationToken.TabIndex = 8;
            this.txtConnectActivationToken.Validating += new System.ComponentModel.CancelEventHandler(this.TxtConnectActivationTokenValidating);
            // 
            // label35
            // 
            this.label35.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label35.Location = new System.Drawing.Point(37, 72);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(630, 37);
            this.label35.TabIndex = 7;
            this.label35.Text = "To activate Windows Azure Connect on all the roles so that you can connect them t" +
    "o local computers, get an activation token from the Windows Azure Portal and pas" +
    "te it here:";
            // 
            // chkAzureConnect
            // 
            this.chkAzureConnect.AutoSize = true;
            this.chkAzureConnect.Location = new System.Drawing.Point(19, 51);
            this.chkAzureConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkAzureConnect.Name = "chkAzureConnect";
            this.chkAzureConnect.Size = new System.Drawing.Size(202, 19);
            this.chkAzureConnect.TabIndex = 6;
            this.chkAzureConnect.Text = "Activate Windows Azure Connect";
            this.chkAzureConnect.UseVisualStyleBackColor = true;
            this.chkAzureConnect.CheckedChanged += new System.EventHandler(this.ChkAzureConnectCheckedChanged);
            // 
            // label47
            // 
            this.label47.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label47.Location = new System.Drawing.Point(10, 11);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(661, 37);
            this.label47.TabIndex = 1;
            this.label47.Text = resources.GetString("label47.Text");
            // 
            // label48
            // 
            this.label48.AutoSize = true;
            this.label48.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label48.Location = new System.Drawing.Point(10, 18);
            this.label48.Name = "label48";
            this.label48.Size = new System.Drawing.Size(203, 25);
            this.label48.TabIndex = 2;
            this.label48.Text = "Virtual Network Settings";
            // 
            // pnl4
            // 
            this.pnl4.Controls.Add(this.pictureBox5);
            this.pnl4.Controls.Add(this.pictureBox19);
            this.pnl4.Controls.Add(this.AzureSettings);
            this.pnl4.Controls.Add(this.label17);
            this.pnl4.Location = new System.Drawing.Point(6, 4);
            this.pnl4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl4.Name = "pnl4";
            this.pnl4.Size = new System.Drawing.Size(668, 370);
            this.pnl4.TabIndex = 6;
            // 
            // AzureSettings
            // 
            this.AzureSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AzureSettings.BackColor = System.Drawing.SystemColors.Control;
            this.AzureSettings.Controls.Add(this.label34);
            this.AzureSettings.Controls.Add(this.txtVHDSize);
            this.AzureSettings.Controls.Add(this.txtVHDBlobName);
            this.AzureSettings.Controls.Add(this.label30);
            this.AzureSettings.Controls.Add(this.label31);
            this.AzureSettings.Controls.Add(this.txtStorageContainer);
            this.AzureSettings.Controls.Add(this.label22);
            this.AzureSettings.Controls.Add(this.txtBindings);
            this.AzureSettings.Controls.Add(this.label21);
            this.AzureSettings.Controls.Add(this.lblStTest);
            this.AzureSettings.Controls.Add(this.btnTestStorage);
            this.AzureSettings.Controls.Add(this.chkStorageHTTPS);
            this.AzureSettings.Controls.Add(this.txtStorageKey);
            this.AzureSettings.Controls.Add(this.label20);
            this.AzureSettings.Controls.Add(this.txtStorageName);
            this.AzureSettings.Controls.Add(this.label19);
            this.AzureSettings.Controls.Add(this.label18);
            this.AzureSettings.Location = new System.Drawing.Point(0, 64);
            this.AzureSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.AzureSettings.Name = "AzureSettings";
            this.AzureSettings.Size = new System.Drawing.Size(667, 306);
            this.AzureSettings.TabIndex = 4;
            // 
            // label34
            // 
            this.label34.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label34.AutoSize = true;
            this.label34.Location = new System.Drawing.Point(50, 283);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(111, 15);
            this.label34.TabIndex = 17;
            this.label34.Text = "VHD drive size (Mb)";
            // 
            // txtVHDSize
            // 
            this.txtVHDSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtVHDSize.Location = new System.Drawing.Point(167, 280);
            this.txtVHDSize.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtVHDSize.Name = "txtVHDSize";
            this.txtVHDSize.Size = new System.Drawing.Size(265, 23);
            this.txtVHDSize.TabIndex = 16;
            // 
            // txtVHDBlobName
            // 
            this.txtVHDBlobName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtVHDBlobName.Location = new System.Drawing.Point(167, 249);
            this.txtVHDBlobName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtVHDBlobName.Name = "txtVHDBlobName";
            this.txtVHDBlobName.Size = new System.Drawing.Size(563, 23);
            this.txtVHDBlobName.TabIndex = 15;
            // 
            // label30
            // 
            this.label30.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(41, 253);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(120, 15);
            this.label30.TabIndex = 14;
            this.label30.Text = "VHD drive blob name";
            // 
            // label31
            // 
            this.label31.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(28, 222);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(133, 15);
            this.label31.TabIndex = 13;
            this.label31.Text = "Storage container name";
            // 
            // txtStorageContainer
            // 
            this.txtStorageContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtStorageContainer.Location = new System.Drawing.Point(167, 218);
            this.txtStorageContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtStorageContainer.Name = "txtStorageContainer";
            this.txtStorageContainer.Size = new System.Drawing.Size(563, 23);
            this.txtStorageContainer.TabIndex = 12;
            // 
            // label22
            // 
            this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label22.Location = new System.Drawing.Point(14, 194);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(633, 60);
            this.label22.TabIndex = 11;
            this.label22.Text = "Introduce the parameters for the cloud drive:";
            // 
            // txtBindings
            // 
            this.txtBindings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBindings.Location = new System.Drawing.Point(164, 172);
            this.txtBindings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtBindings.Name = "txtBindings";
            this.txtBindings.Size = new System.Drawing.Size(452, 23);
            this.txtBindings.TabIndex = 10;
            this.txtBindings.Visible = false;
            this.txtBindings.Validating += new System.ComponentModel.CancelEventHandler(this.TxtBindingsValidating);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(14, 175);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(53, 15);
            this.label21.TabIndex = 9;
            this.label21.Text = "Bindings";
            this.label21.Visible = false;
            // 
            // lblStTest
            // 
            this.lblStTest.AutoSize = true;
            this.lblStTest.Location = new System.Drawing.Point(307, 146);
            this.lblStTest.Name = "lblStTest";
            this.lblStTest.Size = new System.Drawing.Size(281, 15);
            this.lblStTest.TabIndex = 8;
            this.lblStTest.Text = "Test to http://AccountName.blob.windows.core.net";
            // 
            // btnTestStorage
            // 
            this.btnTestStorage.Location = new System.Drawing.Point(164, 141);
            this.btnTestStorage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnTestStorage.Name = "btnTestStorage";
            this.btnTestStorage.Size = new System.Drawing.Size(135, 26);
            this.btnTestStorage.TabIndex = 7;
            this.btnTestStorage.Text = "Test Credentials";
            this.btnTestStorage.UseVisualStyleBackColor = true;
            this.btnTestStorage.Click += new System.EventHandler(this.BtnTestStorageClick);
            // 
            // chkStorageHTTPS
            // 
            this.chkStorageHTTPS.AutoSize = true;
            this.chkStorageHTTPS.Checked = true;
            this.chkStorageHTTPS.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStorageHTTPS.Location = new System.Drawing.Point(164, 117);
            this.chkStorageHTTPS.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkStorageHTTPS.Name = "chkStorageHTTPS";
            this.chkStorageHTTPS.Size = new System.Drawing.Size(84, 19);
            this.chkStorageHTTPS.TabIndex = 6;
            this.chkStorageHTTPS.Text = "Use HTTPS";
            this.chkStorageHTTPS.UseVisualStyleBackColor = true;
            this.chkStorageHTTPS.CheckedChanged += new System.EventHandler(this.ChkStorageHttpsCheckedChanged);
            // 
            // txtStorageKey
            // 
            this.txtStorageKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtStorageKey.Location = new System.Drawing.Point(164, 84);
            this.txtStorageKey.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtStorageKey.Name = "txtStorageKey";
            this.txtStorageKey.Size = new System.Drawing.Size(452, 23);
            this.txtStorageKey.TabIndex = 5;
            this.txtStorageKey.Validating += new System.ComponentModel.CancelEventHandler(this.TxtStorageKeyValidating);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(15, 87);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(114, 15);
            this.label20.TabIndex = 4;
            this.label20.Text = "Storage account key";
            // 
            // txtStorageName
            // 
            this.txtStorageName.Location = new System.Drawing.Point(164, 52);
            this.txtStorageName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtStorageName.Name = "txtStorageName";
            this.txtStorageName.Size = new System.Drawing.Size(135, 23);
            this.txtStorageName.TabIndex = 3;
            this.txtStorageName.TextChanged += new System.EventHandler(this.TxtStorageNameTextChanged);
            this.txtStorageName.Validating += new System.ComponentModel.CancelEventHandler(this.TxtStorageNameValidating);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(15, 56);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(126, 15);
            this.label19.TabIndex = 2;
            this.label19.Text = "Storage account name";
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.Location = new System.Drawing.Point(10, 11);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(647, 37);
            this.label18.TabIndex = 1;
            this.label18.Text = "Please, complete all the Windows Azure related fields. The packages will be uploa" +
    "ded to the Storage Account specified.";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(10, 23);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(296, 25);
            this.label17.TabIndex = 2;
            this.label17.Text = "Windows Azure connection settings";
            // 
            // pnl10
            // 
            this.pnl10.Controls.Add(this.pictureBox20);
            this.pnl10.Controls.Add(this.pictureBox21);
            this.pnl10.Controls.Add(this.pnlConfig);
            this.pnl10.Controls.Add(this.label25);
            this.pnl10.Location = new System.Drawing.Point(17, 22);
            this.pnl10.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnl10.Name = "pnl10";
            this.pnl10.Size = new System.Drawing.Size(685, 391);
            this.pnl10.TabIndex = 7;
            // 
            // pnlConfig
            // 
            this.pnlConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlConfig.BackColor = System.Drawing.SystemColors.Control;
            this.pnlConfig.Controls.Add(this.txtConfig);
            this.pnlConfig.Controls.Add(this.label26);
            this.pnlConfig.Location = new System.Drawing.Point(0, 66);
            this.pnlConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlConfig.Name = "pnlConfig";
            this.pnlConfig.Size = new System.Drawing.Size(682, 324);
            this.pnlConfig.TabIndex = 3;
            // 
            // txtConfig
            // 
            this.txtConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConfig.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConfig.Location = new System.Drawing.Point(21, 72);
            this.txtConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtConfig.Multiline = true;
            this.txtConfig.Name = "txtConfig";
            this.txtConfig.ReadOnly = true;
            this.txtConfig.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtConfig.Size = new System.Drawing.Size(636, 238);
            this.txtConfig.TabIndex = 1;
            this.txtConfig.WordWrap = false;
            // 
            // label26
            // 
            this.label26.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label26.Location = new System.Drawing.Point(16, 19);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(642, 49);
            this.label26.TabIndex = 0;
            this.label26.Text = "This is the Summary of Settings in order to deploy DotNetNuke on Windows Azure. P" +
    "lease, review all the settings in order to ensure that all parameters are correc" +
    "t.";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Font = new System.Drawing.Font("Segoe UI Light", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label25.Location = new System.Drawing.Point(16, 19);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(156, 25);
            this.label25.TabIndex = 2;
            this.label25.Text = "Settings Summary";
            // 
            // btnBack
            // 
            this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBack.CausesValidation = false;
            this.btnBack.Enabled = false;
            this.btnBack.Location = new System.Drawing.Point(999, 14);
            this.btnBack.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(114, 26);
            this.btnBack.TabIndex = 2;
            this.btnBack.Text = "< Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.BtnBackClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.CausesValidation = false;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(1242, 14);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 26);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(1120, 14);
            this.btnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(114, 26);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "Next >";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            // 
            // errProv
            // 
            this.errProv.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errProv.ContainerControl = this;
            // 
            // dlgFolder
            // 
            this.dlgFolder.Description = "Select the folder to export the service configuration and package file:";
            // 
            // dlgFile
            // 
            this.dlgFile.Filter = "Publish Settings files|*.publishsettings|All files|*.*";
            this.dlgFile.Title = "Import publish settings file";
            // 
            // dlgSSLFile
            // 
            this.dlgSSLFile.Filter = "X.509 Certificate|*.cer|Personal Information Exchange|*.pfx|All files|*.*";
            // 
            // pictureBox8
            // 
            this.pictureBox8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox8.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox8.Image")));
            this.pictureBox8.Location = new System.Drawing.Point(618, 6);
            this.pictureBox8.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox8.Name = "pictureBox8";
            this.pictureBox8.Size = new System.Drawing.Size(50, 50);
            this.pictureBox8.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox8.TabIndex = 7;
            this.pictureBox8.TabStop = false;
            // 
            // pictureBox16
            // 
            this.pictureBox16.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox16.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox16.Image")));
            this.pictureBox16.Location = new System.Drawing.Point(499, 22);
            this.pictureBox16.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox16.Name = "pictureBox16";
            this.pictureBox16.Size = new System.Drawing.Size(114, 23);
            this.pictureBox16.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox16.TabIndex = 6;
            this.pictureBox16.TabStop = false;
            // 
            // pictureBox9
            // 
            this.pictureBox9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox9.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox9.Image")));
            this.pictureBox9.Location = new System.Drawing.Point(622, 7);
            this.pictureBox9.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox9.Name = "pictureBox9";
            this.pictureBox9.Size = new System.Drawing.Size(50, 50);
            this.pictureBox9.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox9.TabIndex = 9;
            this.pictureBox9.TabStop = false;
            // 
            // pictureBox17
            // 
            this.pictureBox17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox17.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox17.Image")));
            this.pictureBox17.Location = new System.Drawing.Point(503, 23);
            this.pictureBox17.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox17.Name = "pictureBox17";
            this.pictureBox17.Size = new System.Drawing.Size(114, 23);
            this.pictureBox17.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox17.TabIndex = 8;
            this.pictureBox17.TabStop = false;
            // 
            // pictureBox7
            // 
            this.pictureBox7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox7.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox7.Image")));
            this.pictureBox7.Location = new System.Drawing.Point(622, 6);
            this.pictureBox7.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox7.Name = "pictureBox7";
            this.pictureBox7.Size = new System.Drawing.Size(50, 50);
            this.pictureBox7.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox7.TabIndex = 11;
            this.pictureBox7.TabStop = false;
            // 
            // pictureBox18
            // 
            this.pictureBox18.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox18.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox18.Image")));
            this.pictureBox18.Location = new System.Drawing.Point(503, 22);
            this.pictureBox18.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox18.Name = "pictureBox18";
            this.pictureBox18.Size = new System.Drawing.Size(114, 23);
            this.pictureBox18.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox18.TabIndex = 10;
            this.pictureBox18.TabStop = false;
            // 
            // pictureBox5
            // 
            this.pictureBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox5.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox5.Image")));
            this.pictureBox5.Location = new System.Drawing.Point(604, 6);
            this.pictureBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new System.Drawing.Size(50, 50);
            this.pictureBox5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox5.TabIndex = 13;
            this.pictureBox5.TabStop = false;
            // 
            // pictureBox19
            // 
            this.pictureBox19.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox19.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox19.Image")));
            this.pictureBox19.Location = new System.Drawing.Point(485, 22);
            this.pictureBox19.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox19.Name = "pictureBox19";
            this.pictureBox19.Size = new System.Drawing.Size(114, 23);
            this.pictureBox19.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox19.TabIndex = 12;
            this.pictureBox19.TabStop = false;
            // 
            // pictureBox20
            // 
            this.pictureBox20.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox20.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox20.Image")));
            this.pictureBox20.Location = new System.Drawing.Point(627, 8);
            this.pictureBox20.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox20.Name = "pictureBox20";
            this.pictureBox20.Size = new System.Drawing.Size(50, 50);
            this.pictureBox20.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox20.TabIndex = 13;
            this.pictureBox20.TabStop = false;
            // 
            // pictureBox21
            // 
            this.pictureBox21.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox21.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox21.Image")));
            this.pictureBox21.Location = new System.Drawing.Point(508, 24);
            this.pictureBox21.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox21.Name = "pictureBox21";
            this.pictureBox21.Size = new System.Drawing.Size(114, 23);
            this.pictureBox21.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox21.TabIndex = 12;
            this.pictureBox21.TabStop = false;
            // 
            // pictureBox22
            // 
            this.pictureBox22.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox22.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox22.Image")));
            this.pictureBox22.Location = new System.Drawing.Point(621, 8);
            this.pictureBox22.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox22.Name = "pictureBox22";
            this.pictureBox22.Size = new System.Drawing.Size(50, 50);
            this.pictureBox22.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox22.TabIndex = 13;
            this.pictureBox22.TabStop = false;
            // 
            // pictureBox23
            // 
            this.pictureBox23.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox23.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox23.Image")));
            this.pictureBox23.Location = new System.Drawing.Point(502, 24);
            this.pictureBox23.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox23.Name = "pictureBox23";
            this.pictureBox23.Size = new System.Drawing.Size(114, 23);
            this.pictureBox23.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox23.TabIndex = 12;
            this.pictureBox23.TabStop = false;
            // 
            // pictureBox24
            // 
            this.pictureBox24.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox24.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox24.Image")));
            this.pictureBox24.Location = new System.Drawing.Point(647, 8);
            this.pictureBox24.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox24.Name = "pictureBox24";
            this.pictureBox24.Size = new System.Drawing.Size(50, 50);
            this.pictureBox24.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox24.TabIndex = 13;
            this.pictureBox24.TabStop = false;
            // 
            // pictureBox25
            // 
            this.pictureBox25.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox25.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox25.Image")));
            this.pictureBox25.Location = new System.Drawing.Point(528, 24);
            this.pictureBox25.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox25.Name = "pictureBox25";
            this.pictureBox25.Size = new System.Drawing.Size(114, 23);
            this.pictureBox25.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox25.TabIndex = 12;
            this.pictureBox25.TabStop = false;
            // 
            // FrmDNNAzureWizard
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1356, 741);
            this.Controls.Add(this.split);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmDNNAzureWizard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DotNetNuke Azure Accelerator Wizard";
            this.Load += new System.EventHandler(this.FrmDNNAzureWizardLoad);
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.pnl.ResumeLayout(false);
            this.pnl8.ResumeLayout(false);
            this.pnl8.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox14)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox15)).EndInit();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.pnlSSL.ResumeLayout(false);
            this.pnlSSL.PerformLayout();
            this.pnl2.ResumeLayout(false);
            this.pnl2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox10)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.pnl1.ResumeLayout(false);
            this.pnl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.pnl11.ResumeLayout(false);
            this.pnl11.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.pnl3.ResumeLayout(false);
            this.pnl3.PerformLayout();
            this.pnlHostingServices.ResumeLayout(false);
            this.pnlHostingServices.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox12)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox11)).EndInit();
            this.pnl12.ResumeLayout(false);
            this.pnl12.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.pnl5.ResumeLayout(false);
            this.pnl5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox13)).EndInit();
            this.DBSettings.ResumeLayout(false);
            this.DBSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.pnl9.ResumeLayout(false);
            this.pnl9.PerformLayout();
            this.PackageSettings.ResumeLayout(false);
            this.PackageSettings.PerformLayout();
            this.pnl6.ResumeLayout(false);
            this.pnl6.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.pnlRDP.ResumeLayout(false);
            this.pnlRDP.PerformLayout();
            this.pnl7.ResumeLayout(false);
            this.pnl7.PerformLayout();
            this.pnlAzureConnect.ResumeLayout(false);
            this.pnlAzureConnect.PerformLayout();
            this.pnl4.ResumeLayout(false);
            this.pnl4.PerformLayout();
            this.AzureSettings.ResumeLayout(false);
            this.AzureSettings.PerformLayout();
            this.pnl10.ResumeLayout(false);
            this.pnl10.PerformLayout();
            this.pnlConfig.ResumeLayout(false);
            this.pnlConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errProv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox16)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox9)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox17)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox18)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox19)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox20)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox22)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox23)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox24)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox25)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Panel pnl;
        private System.Windows.Forms.Panel pnl1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl5;
        private System.Windows.Forms.PictureBox pictureBox4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel DBSettings;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtDBServer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtDBName;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtDBAdminUser;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtDBAdminPassword;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtDBUser;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtDBPassword;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Button btnTestDB;
        private System.Windows.Forms.Panel pnl4;
        private System.Windows.Forms.Panel AzureSettings;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox txtStorageName;
        private System.Windows.Forms.TextBox txtStorageKey;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.CheckBox chkStorageHTTPS;
        private System.Windows.Forms.Button btnTestStorage;
        private System.Windows.Forms.Label lblStTest;
        private System.Windows.Forms.TextBox txtBindings;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox txtDBRePassword;
        private System.Windows.Forms.Panel pnl10;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Panel pnlConfig;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TextBox txtConfig;
        private System.Windows.Forms.Panel pnl11;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Panel pnl12;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TextBox txtLogFinal;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label lblSuccess;
        private System.Windows.Forms.Panel pnl9;
        private System.Windows.Forms.Panel PackageSettings;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.Label label37;
        private System.Windows.Forms.ListView lstPackages;
        private System.Windows.Forms.ColumnHeader packageName;
        private System.Windows.Forms.ColumnHeader packageDescription;
        private System.Windows.Forms.TextBox txtLOG;
        private System.Windows.Forms.ListView lstTasks;
        private System.Windows.Forms.ColumnHeader TaskDescription;
        private System.Windows.Forms.ColumnHeader TaskStatus;
        private System.Windows.Forms.Panel pnl6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label38;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.Panel pnlRDP;
        private System.Windows.Forms.DateTimePicker cboRDPExpirationDate;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.TextBox txtRDPConfirmPassword;
        private System.Windows.Forms.Label label40;
        private System.Windows.Forms.TextBox txtRDPPassword;
        private System.Windows.Forms.Label label42;
        private System.Windows.Forms.TextBox txtRDPUser;
        private System.Windows.Forms.ComboBox cboCertificates;
        private System.Windows.Forms.Label lblRDPCredentialsInfo;
        private System.Windows.Forms.Button cmdViewCertificate;
        private System.Windows.Forms.Label lblRDPInfo;
        private System.Windows.Forms.ErrorProvider errProv;
        private System.Windows.Forms.Panel pnl7;
        private System.Windows.Forms.Panel pnlAzureConnect;
        private System.Windows.Forms.CheckBox chkAzureConnect;
        private System.Windows.Forms.Label label47;
        private System.Windows.Forms.Label label48;
        private System.Windows.Forms.Label label35;
        private System.Windows.Forms.TextBox txtConnectActivationToken;
        private System.Windows.Forms.Label label41;
        private System.Windows.Forms.LinkLabel lnkConnectHelp;
        private System.Windows.Forms.Panel pnl2;
        private System.Windows.Forms.PictureBox pictureBox10;
        private System.Windows.Forms.Label label43;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label44;
        private System.Windows.Forms.ComboBox cboSubscriptions;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton optSubscription;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label45;
        private System.Windows.Forms.Label label46;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Panel pnl3;
        private System.Windows.Forms.PictureBox pictureBox6;
        private System.Windows.Forms.PictureBox pictureBox11;
        private System.Windows.Forms.Label label49;
        private System.Windows.Forms.Panel pnlHostingServices;
        private System.Windows.Forms.Label label50;
        private System.Windows.Forms.ComboBox cboHostingService;
        private System.Windows.Forms.Label label52;
        private System.Windows.Forms.ComboBox cboEnvironment;
        private System.Windows.Forms.Label label53;
        private System.Windows.Forms.Label label54;
        private System.Windows.Forms.Label label51;
        private System.Windows.Forms.Label label55;
        private System.Windows.Forms.TextBox txtVHDName;
        private System.Windows.Forms.Label label56;
        private System.Windows.Forms.TextBox txtVHDDriveSize;
        private System.Windows.Forms.Label label57;
        private System.Windows.Forms.PictureBox pictureBox12;
        private System.Windows.Forms.Label label58;
        internal System.Windows.Forms.ComboBox cboStorage;
        private System.Windows.Forms.TextBox txtPackagesContainer;
        private System.Windows.Forms.PictureBox pictureBox13;
        private System.Windows.Forms.ComboBox cboDatabase;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.TextBox txtVHDSize;
        private System.Windows.Forms.TextBox txtVHDBlobName;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.TextBox txtStorageContainer;
        private System.Windows.Forms.CheckBox chkAutoInstall;
        private System.Windows.Forms.Label label59;
        private System.Windows.Forms.FolderBrowserDialog dlgFolder;
        private System.Windows.Forms.ComboBox cboDNNVersion;
        private System.Windows.Forms.Label label60;
        private System.Windows.Forms.TextBox txtDNNUrl;
        private System.Windows.Forms.Label lblCustomUrl;
        private System.Windows.Forms.Label label62;
        private System.Windows.Forms.LinkLabel lnkMorePackages;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.Label label61;
        private System.Windows.Forms.OpenFileDialog dlgFile;
        private System.Windows.Forms.CheckBox chkWebDeploy;
        private System.Windows.Forms.CheckBox chkEnableRemoteMgmt;
        private System.Windows.Forms.CheckBox chkEnableRDP;
        private System.Windows.Forms.CheckBox chkEnableFTP;
        private System.Windows.Forms.Panel pnl8;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label65;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.CheckBox chkEnableSSL;
        private System.Windows.Forms.Panel pnlSSL;
        private System.Windows.Forms.Label label63;
        private System.Windows.Forms.TextBox txtSSLCertificate;
        private System.Windows.Forms.Label label64;
        private System.Windows.Forms.ListView lstCASSLCertificates;
        private System.Windows.Forms.Button cmdAddSSL;
        private System.Windows.Forms.Button cmdRemoveSSL;
        private System.Windows.Forms.ColumnHeader Cert;
        private System.Windows.Forms.ColumnHeader Thumbprint;
        private System.Windows.Forms.Button cmdOpenSSL;
        private System.Windows.Forms.Button cmdViewSSL;
        private System.Windows.Forms.OpenFileDialog dlgSSLFile;
        private System.Windows.Forms.PictureBox pictureBox14;
        private System.Windows.Forms.PictureBox pictureBox15;
        private System.Windows.Forms.PictureBox pictureBox8;
        private System.Windows.Forms.PictureBox pictureBox16;
        private System.Windows.Forms.PictureBox pictureBox9;
        private System.Windows.Forms.PictureBox pictureBox17;
        private System.Windows.Forms.PictureBox pictureBox7;
        private System.Windows.Forms.PictureBox pictureBox18;
        private System.Windows.Forms.PictureBox pictureBox24;
        private System.Windows.Forms.PictureBox pictureBox25;
        private System.Windows.Forms.PictureBox pictureBox22;
        private System.Windows.Forms.PictureBox pictureBox23;
        private System.Windows.Forms.PictureBox pictureBox5;
        private System.Windows.Forms.PictureBox pictureBox19;
        private System.Windows.Forms.PictureBox pictureBox20;
        private System.Windows.Forms.PictureBox pictureBox21;
        
    }
}

