﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DNNAzureWizard
{
    public partial class FrmCertificatePassword : Form
    {
        public FrmCertificatePassword()
        {
            InitializeComponent();
        }

        public string Password { get { return txtPassword.Text; } }

        private void FrmCertificatePassword_Load(object sender, EventArgs e)
        {

        }
    }
}
