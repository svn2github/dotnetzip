namespace Ionic.Utils.Zip
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    public partial class PasswordDialog : Form
    {
        public PasswordDialog()
        {
            InitializeComponent();
        }
        private bool wasCanceled = false;
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
                if (wasCanceled) return null;
                return textBox1.Text;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            wasCanceled = true;
            this.Close();
        }

    }
}
