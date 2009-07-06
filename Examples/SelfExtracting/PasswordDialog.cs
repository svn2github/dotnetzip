namespace Ionic.Zip
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    public partial class PasswordDialog : Form
    {
        public enum PasswordDialogResult { OK, Skip, Cancel };
        
        public PasswordDialog()
        {
            InitializeComponent();
            this.textBox1.Focus();
        }

        public PasswordDialogResult Result
        {
            get
            {
                return _result;
            }
        }
        
        public string EntryName
        {
            set
            {
                prompt.Text = "Enter the password for " + value;
            }
        }
        public string Password
        {
            get
            {
                return textBox1.Text;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            _result = PasswordDialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _result = PasswordDialogResult.Cancel;
            this.Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            _result = PasswordDialogResult.Skip;
            this.Close();
        }


        private PasswordDialogResult _result;


    }
}
