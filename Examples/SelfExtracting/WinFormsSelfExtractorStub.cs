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

        delegate void ExtractEntryProgress(ExtractProgressEventArgs e);

        public WinFormsSelfExtractorStub()
        {
            InitializeComponent();
            _setCancel = true;

            txtExtractDirectory.Text =
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ZipName);

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
                    label2.Visible = false;
                    txtComment.Visible = false;
                    this.Size = new System.Drawing.Size(this.Width, this.Height - 113);
                }

            }
            catch
            {
                label2.Visible = false;
                txtComment.Visible = false;
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
            KickoffExtract();
        }


        private void KickoffExtract()
        {
            // disable most of the UI: 
            this.btnContents.Enabled = false;
            this.btnExtract.Enabled = false;
            this.chk_OpenExplorer.Enabled = false;
            this.chk_Overwrite.Enabled = false;
            this.txtExtractDirectory.Enabled = false;
            this.btnDirBrowse.Enabled = false;
            this.btnExtract.Text = "Extracting...";
            System.Threading.Thread _workerThread = new System.Threading.Thread(this.DoExtract);
            _workerThread.Name = "Zip Extractor thread";
            _workerThread.Start(null);
            this.Cursor = Cursors.WaitCursor;
        }


        private void DoExtract(Object p)
        {
            string targetDirectory = txtExtractDirectory.Text;
            bool WantOverwrite = chk_Overwrite.Checked;
            bool extractCancelled = false;
            _setCancel = false;
            string currentPassword = "";
            SetProgressBars();

            try
            {
                zip.ExtractProgress += ExtractProgress;
                foreach (global::Ionic.Utils.Zip.ZipEntry entry in zip)
                {
                    if (_setCancel) { extractCancelled = true; break; }
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


            SetUiDone();

            if (extractCancelled) return;

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

        private void SetUiDone()
        {
            if (this.btnExtract.InvokeRequired)
            {
                this.btnExtract.Invoke(new MethodInvoker(this.SetUiDone));
            }
            else
            {
                this.lblStatus.Text = "Done.";
                btnExtract.Text = "Extracted.";
                btnExtract.Enabled = false;
                btnCancel.Text = "Quit";
                _setCancel = true;
                this.Cursor = Cursors.Default;
            }
        }

        private void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
            {
                StepEntryProgress(e);
            }

            else if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
            {
                StepArchiveProgress(e);
            }
            if (_setCancel)
                e.Cancel = true;
        }

        private void StepArchiveProgress(ExtractProgressEventArgs e)
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar2.Invoke(new ExtractEntryProgress(this.StepArchiveProgress), new object[] { e });
            }
            else
            {
                this.progressBar1.PerformStep();

                // reset the progress bar for the entry:
                this.progressBar2.Value = this.progressBar2.Maximum = 1;
                this.lblStatus.Text = "";
                this.Update();
            }
        }

        private void StepEntryProgress(ExtractProgressEventArgs e)
        {
            if (this.progressBar2.InvokeRequired)
            {
                this.progressBar2.Invoke(new ExtractEntryProgress(this.StepEntryProgress), new object[] { e });
            }
            else
            {
                if (this.progressBar2.Maximum == 1)
                {
                        // reset
                        Int64 max = e.TotalBytesToTransfer;
                        _progress2MaxFactor = 0;
                        while (max > System.Int32.MaxValue)
                        {
                            max /= 2;
                            _progress2MaxFactor++;
                        }
                        this.progressBar2.Maximum = (int)max;
                    this.lblStatus.Text = String.Format("Extracting {0}/{1}: {2} ...",
                        this.progressBar1.Value, zip.Entries.Count, e.CurrentEntry.FileName);
                }

                    int xferred = e.BytesTransferred >> _progress2MaxFactor;

                    this.progressBar2.Value = (xferred >= this.progressBar2.Maximum)
                        ? this.progressBar2.Maximum
                        : xferred;

                this.Update();
            }
        }

        private void SetProgressBars()
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new MethodInvoker(this.SetProgressBars));
            }
            else
            {
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = zip.Entries.Count;
                this.progressBar1.Minimum = 0;
                this.progressBar1.Step = 1;
                this.progressBar2.Value = 0;
                this.progressBar2.Minimum = 0;
                this.progressBar2.Maximum = 1; // will be set later, for each entry.
                this.progressBar2.Step = 1;
            }
        }

        private String ZipName
        {
            get
            {
               return System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            }
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
            if (_setCancel == false)
                _setCancel = true;
            else
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


        private int _progress2MaxFactor;
        private bool _setCancel;
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
