namespace Ionic.Utils.Zip
{
    using System;
    using System.Reflection;
    using System.IO;
    using System.Windows.Forms;


    public partial class WinFormsSelfExtractorStub : Form
    {
        //const string IdString = "DotNetZip Self Extractor, see http://www.codeplex.com/DotNetZip";
        const string DllResourceName = "Ionic.Utils.Zip.dll";



        public WinFormsSelfExtractorStub()
        {
            InitializeComponent();
            txtExtractDirectory.Text = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            try
            {
                
                    if ((zip.Comment != null) && (zip.Comment != ""))
                    {
                        txtComment.Text = zip.Comment;
                    }
                    else
                    {
                        //label2.Text = "";
                        //txtComment.Text = "";
                        label2.Visible= false;
                        txtComment.Visible = false;
                        this.Size = new System.Drawing.Size(this.Width, this.Height - 113);
                    }
                
            }
            catch
            {
                label2.Visible = false;
                txtComment.Visible= false;
                this.Size = new System.Drawing.Size(this.Width, this.Height - 113);
            }
        }

        static WinFormsSelfExtractorStub()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolver);
        }

        static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
        {
            Assembly a1 = Assembly.GetExecutingAssembly();
            Assembly a2 = null;

            Stream s = a1.GetManifestResourceStream(DllResourceName);
            int n = 0;
            int totalBytesRead = 0;
            byte[] bytes = new byte[1024];
            do
            {
                n = s.Read(bytes, 0, bytes.Length);
                totalBytesRead += n;
            }
            while (n > 0);

            byte[] block = new byte[totalBytesRead];
            s.Seek(0, System.IO.SeekOrigin.Begin);
            s.Read(block, 0, block.Length);

            a2 = Assembly.Load(block);

            return a2;
        }



        private void btnDirBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            // Default to the My Documents folder.
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            folderBrowserDialog1.SelectedPath = txtExtractDirectory.Text;
            folderBrowserDialog1.ShowNewFolderButton = true;

            folderBrowserDialog1.Description = "Select the directory for the extracted files.";

            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtExtractDirectory.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            string targetDirectory = txtExtractDirectory.Text;
            bool WantOverwrite = chk_Overwrite.Checked;
            bool extractCancelled = false;
            string currentPassword = "";

            try
            {

                    foreach (global::Ionic.Utils.Zip.ZipEntry entry in zip)
                    {
                        if (entry.Encryption == global::Ionic.Utils.Zip.EncryptionAlgorithm.None)
                            try
                            {
                                entry.Extract(targetDirectory, WantOverwrite);
                            }
                            catch (Exception ex1)
                            {
                                DialogResult result = MessageBox.Show(String.Format("Failed to extract entry {0} -- {1}", entry.FileName, ex1.Message.ToString()),
                                     String.Format("Error Extracting {0}", entry.FileName), MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);

                                if (result == DialogResult.Cancel)
                                {
                                    extractCancelled = true;
                                    break;
                                }
                            }
                        else
                        {
                            if (currentPassword == "")
                            {
                                do
                                {
                                    currentPassword = PromptForPassword(entry.FileName);
                                }
                                while (currentPassword == "");
                            }

                            if (currentPassword == null)
                            {
                                extractCancelled = true;
                                currentPassword = "";
                                break;
                            }
                            else
                            {
                                try
                                {
                                    entry.ExtractWithPassword(targetDirectory, WantOverwrite, currentPassword);
                                }
                                catch (Exception ex2)
                                {
                                    // TODO: probably want a retry here in the case of bad password.
                                    DialogResult result = MessageBox.Show(String.Format("Failed to extract the password-encrypted entry {0} -- {1}", entry.FileName, ex2.Message.ToString()),
                                        String.Format("Error Extracting {0}", entry.FileName), MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);

                                    if (result == DialogResult.Cancel)
                                    {
                                        extractCancelled = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                
            }
            catch (Exception)
            {
                MessageBox.Show("The self-extracting zip file is corrupted.",
                    "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                Application.Exit();
            }

            if (extractCancelled) return;

            btnExtract.Text = "Extracted.";
            btnExtract.Enabled = false;
            btnCancel.Text = "Quit";

            if (chk_OpenExplorer.Checked)
            {
                string w = System.Environment.GetEnvironmentVariable("WINDIR");
                if (w == null) w = "c:\\windows";
                try
                {
                    System.Diagnostics.Process.Start(Path.Combine(w, "explorer.exe"), targetDirectory);
                }
                catch { }
            }
            //Application.Exit();

        }

        private Stream ZipStream
        {
            get
            {
                if (_s != null) return _s;

                // There are only two embedded resources.
                // One of them is the zip dll.  The other is the zip archive.
                // We load the resouce that is NOT the DLL, as the zip archive.
                Assembly a = Assembly.GetExecutingAssembly();
                string[] x = a.GetManifestResourceNames();
                _s = null;
                foreach (string name in x)
                {
                    if ((name != DllResourceName) && (name.EndsWith(".zip")))
                    {
                        _s = a.GetManifestResourceStream(name);
                        break;
                    }
                }

                if (_s == null)
                {
                    MessageBox.Show("No Zip archive found.",
                           "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    Application.Exit();
                }
                return _s;
            }
        }

        private ZipFile zip
        {
            get
            {
                if (_zip == null)
                    _zip = global::Ionic.Utils.Zip.ZipFile.Read(ZipStream);
                return _zip;
            }
        }

        private string PromptForPassword(string entryName)
        {
            PasswordDialog dlg1 = new PasswordDialog();
            dlg1.EntryName = entryName;
            dlg1.ShowDialog();
            return dlg1.Password;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

	// workitem 6413
        private void btnContents_Click(object sender, EventArgs e)
        {
            ZipContentsDialog dlg1 = new ZipContentsDialog();
            dlg1.ZipFile = zip;
            dlg1.ShowDialog();
            return;
        }


        Stream _s;
        global::Ionic.Utils.Zip.ZipFile _zip;

    }



    class WinFormsSelfExtractorStubProgram
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WinFormsSelfExtractorStub());
        }
    }
}
