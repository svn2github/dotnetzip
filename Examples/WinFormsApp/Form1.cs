using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;

namespace WinFormsExample
{
    public partial class Form1 : Form
    {
        delegate void ZipProgress(ZipProgressEventArgs e);
        delegate void ButtonClick(object sender, EventArgs e);
        HiResTimer _hrt;

        public Form1()
        {
            InitializeComponent();

            InitEncodingsList();
            InitCompressionLevelList();
            InitEncryptionList();
            FixTitle();
            FillFormFromRegistry();
            AdoptProgressBars();
        }


        // This constructor works to load zips from the command line.
        // It also works to allow "open With..." from Windows Explorer. 
        public Form1(string[] args)
            : this()
        {
            if (args != null && args.Length >= 1 && args[0] != null)
            {
                _initialFileToLoad = args[0];
            }
        }

        private void AdoptProgressBars()
        {
            tabControl1_SelectedIndexChanged(null, null);
        }

        private void InitEncryptionList()
        {
            _EncryptionNames = new List<string>(Enum.GetNames(typeof(Ionic.Zip.EncryptionAlgorithm)));
            foreach (var name in _EncryptionNames)
            {
                comboBox3.Items.Add(name);
            }

            // select the first item: 
            comboBox3.SelectedIndex = 0;
            this.tbPassword.Text = "";
        }

        private void FixTitle()
        {
            this.Text = String.Format("DotNetZip's WinForms Zip Tool v{0}",
                      System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void InitEncodingsList()
        {
            _EncodingNames = new List<string>();
            var e = System.Text.Encoding.GetEncodings();
            foreach (var e1 in e)
            {
                if (!_EncodingNames.Contains(e1.Name))
                    if (!_EncodingNames.Contains(e1.Name.ToUpper()))
                        if (!_EncodingNames.Contains(e1.Name.ToLower()))
                            if (e1.Name != "IBM437" && e1.Name != "utf-8")
                                _EncodingNames.Add(e1.Name);
            }
            _EncodingNames.Sort();
            comboBox1.Items.Add("zip default (IBM437)");
            comboBox1.Items.Add("utf-8");
            foreach (var name in _EncodingNames)
            {
                comboBox1.Items.Add(name);
            }

            // select the first item: 
            comboBox1.SelectedIndex = 0;
        }

        private void InitCompressionLevelList()
        {
            _CompressionLevelNames = new List<string>(Enum.GetNames(typeof(Ionic.Zlib.CompressionLevel)));
            _CompressionLevelNames.Sort();
            foreach (var name in _CompressionLevelNames)
            {
                if (name.StartsWith("LEVEL"))
                {
                    comboBox2.Items.Add(name);
                }
            }

            // select the first item: 
            comboBox2.SelectedIndex = 0;
        }




        private void KickoffZipup()
        {
            if (String.IsNullOrEmpty(this.tbDirectoryToZip.Text)) return;
            if (!System.IO.Directory.Exists(this.tbDirectoryToZip.Text))
            {
                var dlgResult = MessageBox.Show(String.Format("The directory you have specified ({0}) does not exist.", this.tbZipToCreate.Text),
                    "Not gonna happen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (this.tbZipToCreate.Text == null || this.tbZipToCreate.Text == "") return;

            // check for existence of the zip file:
            if (System.IO.File.Exists(this.tbZipToCreate.Text))
            {
                var dlgResult = MessageBox.Show(String.Format("The file you have specified ({0}) already exists.  Do you want to overwrite this file?", this.tbZipToCreate.Text), "Confirmation is Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlgResult != DialogResult.Yes) return;
                System.IO.File.Delete(this.tbZipToCreate.Text);
            }


            // check for a valid zip file name:
            string extension = System.IO.Path.GetExtension(this.tbZipToCreate.Text);
            if ((extension != ".exe" && (this.radioFlavorSfxCmd.Checked || this.radioFlavorSfxGui.Checked)) ||
(extension != ".zip" && this.radioFlavorZip.Checked))
            {
                var dlgResult = MessageBox.Show(String.Format("The file you have specified ({0}) has a non-standard extension ({1}) for this zip flavor.  Do you want to continue anyway?",
                    this.tbZipToCreate.Text, extension), "Hold on there, pardner!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlgResult != DialogResult.Yes) return;
                System.IO.File.Delete(this.tbZipToCreate.Text);
            }



            _hrt = new HiResTimer();
            _hrt.Start();

            _working = true;
            _operationCanceled = false;
            _nFilesCompleted = 0;
            _totalBytesAfterCompress = 0;
            _totalBytesBeforeCompress = 0;
            DisableButtons();
            lblStatus.Text = "Zipping...";

            var options = new SaveWorkerOptions
            {
                ZipName = this.tbZipToCreate.Text,
                Folder = this.tbDirectoryToZip.Text,
                Selection = this.tbSelectionToZip.Text,
                DirInArchive = this.tbDirectoryInArchive.Text,
                Encoding = "ibm437"
            };

            if (this.comboBox1.SelectedIndex != 0)
            {
                options.Encoding = this.comboBox1.SelectedItem.ToString();
            }

            options.Encryption = (Ionic.Zip.EncryptionAlgorithm)Enum.Parse(typeof(Ionic.Zip.EncryptionAlgorithm),
                                       this.comboBox3.SelectedItem.ToString());
            options.Password = this.tbPassword.Text;
            options.CompressionLevel = (Ionic.Zlib.CompressionLevel)Enum.Parse(typeof(Ionic.Zlib.CompressionLevel),
                                           this.comboBox2.SelectedItem.ToString());

            if (this.radioFlavorSfxCmd.Checked)
                options.ZipFlavor = 2;
            else if (this.radioFlavorSfxGui.Checked)
                options.ZipFlavor = 1;
            else options.ZipFlavor = 0;

            if (this.radioZip64AsNecessary.Checked)
                options.Zip64 = Zip64Option.AsNecessary;
            else if (this.radioZip64Always.Checked)
                options.Zip64 = Zip64Option.Always;
            else options.Zip64 = Zip64Option.Never;

            options.Comment = String.Format("Encoding:{0} || Compression:{1} || Encrypt:{2} || ZIP64:{3}\r\nCreated at {4} || {5}\r\n",
                        options.Encoding,
                        options.CompressionLevel.ToString(),
                        (this.tbPassword.Text == "") ? "None" : options.Encryption.ToString(),
                        options.Zip64.ToString(),
                        System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"),
                        this.Text);

            if (this.tbComment.Text != TB_COMMENT_NOTE)
                options.Comment += this.tbComment.Text;

            _workerThread = new Thread(this.DoSave);
            _workerThread.Name = "Zip Saver thread";
            _workerThread.Start(options);
            this.Cursor = Cursors.WaitCursor;
        }


        private string FlavorToString(int p)
        {
            if (p == 2) return "SFX-CMD";
            if (p == 1) return "SFX-GUI";
            return "ZIP";
        }


        private bool _firstFocusInCommentTextBox = true;
        private void tbComment_Enter(object sender, EventArgs e)
        {
            if (_firstFocusInCommentTextBox)
            {
                tbComment.Text = "";
                tbComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tbComment.ForeColor = System.Drawing.SystemColors.WindowText;
                _firstFocusInCommentTextBox = false;
            }
        }


        private void tbComment_Leave(object sender, EventArgs e)
        {
            string TextToFind = tbComment.Text;

            if ((TextToFind == null) || (TextToFind == ""))
            {
                this.tbComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                this.tbComment.ForeColor = System.Drawing.SystemColors.InactiveCaption;
                _firstFocusInCommentTextBox = true;
                this.tbComment.Text = TB_COMMENT_NOTE;
            }
        }


        //delegate void ProgressBarSetup(int count);

        private void SetProgressBars()
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new MethodInvoker(this.SetProgressBars));
                //this.progressBar1.Invoke(new ProgressBarSetup(this.SetProgressBars), new object[] { count });
            }
            else
            {
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = _totalEntriesToProcess;
                this.progressBar1.Minimum = 0;
                this.progressBar1.Step = 1;
                this.progressBar2.Value = 0;
                this.progressBar2.Minimum = 0;
                this.progressBar2.Maximum = 1; // will be set later, for each entry.
                this.progressBar2.Step = 1;
            }
        }


        private void DoSave(Object p)
        {
            SaveWorkerOptions options = p as SaveWorkerOptions;
            try
            {
                using (var zip1 = new ZipFile())
                {
                    zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(options.Encoding);
                    zip1.Comment = options.Comment;
                    zip1.Password = (options.Password != "") ? options.Password : null;
                    zip1.Encryption = options.Encryption;
                    if (!String.IsNullOrEmpty(options.Selection))
                        zip1.AddSelectedFiles(options.Selection, options.Folder, options.DirInArchive, true);
                    else
                        zip1.AddDirectory(options.Folder, options.DirInArchive);
                    _totalEntriesToProcess = zip1.EntryFileNames.Count;
                    SetProgressBars();
                    zip1.TempFileFolder = System.IO.Path.GetDirectoryName(options.ZipName);
                    zip1.SaveProgress += this.zip1_SaveProgress;

                    zip1.UseZip64WhenSaving = options.Zip64;
                    zip1.CompressionLevel = options.CompressionLevel;

                    if (options.ZipFlavor == 1)
                        zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.WinFormsApplication);
                    else if (options.ZipFlavor == 2)
                        zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.ConsoleApplication);
                    else
                        zip1.Save(options.ZipName);
                }
            }
            catch (System.Exception exc1)
            {
                MessageBox.Show(String.Format("Exception while zipping: {0}", exc1.StackTrace.ToString()));
                btnCancel_Click(null, null);
            }
        }



        void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Saving_AfterWriteEntry:
                    StepArchiveProgress(e);
                    break;
                case ZipProgressEventType.Saving_EntryBytesRead:
                    StepEntryProgress(e);
                    break;
                case ZipProgressEventType.Saving_Completed:
                    SaveCompleted();
                    break;
                case ZipProgressEventType.Saving_AfterSaveTempArchive:
                    TempArchiveSaved();
                    break;
            }
            if (_operationCanceled)
                e.Cancel = true;
        }

        private void TempArchiveSaved()
        {
            if (this.lblStatus.InvokeRequired)
            {
                this.lblStatus.Invoke(new MethodInvoker(this.TempArchiveSaved));
            }
            else
            {
                System.TimeSpan ts = new System.TimeSpan(0, 0, (int)_hrt.Seconds);

                lblStatus.Text = String.Format("Temp archive saved ({0})...{1}...",
                    ts.ToString(),
                    (this.radioFlavorSfxCmd.Checked || this.radioFlavorSfxGui.Checked)
                    ? "compiling SFX"
                    : "finishing");
            }
        }



        private void SaveCompleted()
        {
            if (this.lblStatus.InvokeRequired)
            {
                this.lblStatus.Invoke(new MethodInvoker(this.SaveCompleted));
            }
            else
            {
                _hrt.Stop();
                System.TimeSpan ts = new System.TimeSpan(0, 0, (int)_hrt.Seconds);
                lblStatus.Text = String.Format("Done, Compressed {0} files, {1:N0}% of original, time: {2}",
                           _nFilesCompleted, (100.00 * _totalBytesAfterCompress) / _totalBytesBeforeCompress,
                           ts.ToString());
                ResetUiState();
            }
        }



        private void StepArchiveProgress(ZipProgressEventArgs e)
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new ZipProgress(this.StepArchiveProgress), new object[] { e });
            }
            else
            {
                if (!_operationCanceled)
                {
                    _nFilesCompleted++;
                    this.progressBar1.PerformStep();
                    _totalBytesAfterCompress += e.CurrentEntry.CompressedSize;
                    _totalBytesBeforeCompress += e.CurrentEntry.UncompressedSize;

                    // reset the progress bar for the entry:
                    this.progressBar2.Value = this.progressBar2.Maximum = 1;

                    this.Update();

#if NOT_SPEEDY
			// Sleep here just to show the progress bar, when the number of files is small,
			// or when all done. 
			// You may not want this for actual use!
			if (this.progressBar2.Value == this.progressBar2.Maximum)
			    Thread.Sleep(350);
			else if (_entriesToZip < 10)
			    Thread.Sleep(350);
			else if (_entriesToZip < 20)
			    Thread.Sleep(200);
			else if (_entriesToZip < 30)
			    Thread.Sleep(100);
			else if (_entriesToZip < 45)
			    Thread.Sleep(80);
			else if (_entriesToZip < 75)
			    Thread.Sleep(40);
                    // more than 75 entries, don't sleep at all.
#endif

                }
            }
        }


        private void StepEntryProgress(ZipProgressEventArgs e)
        {
            if (this.progressBar2.InvokeRequired)
            {
                this.progressBar2.Invoke(new ZipProgress(this.StepEntryProgress), new object[] { e });
            }
            else
            {
                if (!_operationCanceled)
                {
                    if (this.progressBar2.Maximum == 1)
                    {
                        // reset
                        Int64 entryMax = e.TotalBytesToTransfer;
                        Int64 absoluteMax = System.Int32.MaxValue;
                        _progress2MaxFactor = 0;
                        while (entryMax > absoluteMax)
                        {
                            entryMax /= 2;
                            _progress2MaxFactor++;
                        }
                        if ((int)entryMax < 0) entryMax *= -1;
                        this.progressBar2.Maximum = (int)entryMax;
                        lblStatus.Text = String.Format("{0} of {1} files...({2})",
                               _nFilesCompleted + 1, _totalEntriesToProcess, e.CurrentEntry.FileName);
                    }

                    // downcast is safe here because we have shifted e.BytesTransferred
                    int xferred = (int)(e.BytesTransferred >> _progress2MaxFactor);

                    this.progressBar2.Value = (xferred >= this.progressBar2.Maximum)
                        ? this.progressBar2.Maximum
                        : xferred;

                    this.Update();
                }
            }
        }



        private void btnDirBrowse_Click(object sender, EventArgs e)
        {
            var dlg1 = new Ionic.Utils.FolderBrowserDialogEx
            {
                Description = "Select a folder to zip up:",
                ShowNewFolderButton = false,
                ShowEditBox = true,
                //NewStyle = false,
                SelectedPath = this.tbDirectoryToZip.Text,
                ShowFullPathInEditBox = true,
            };
            dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            var result = dlg1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.tbDirectoryToZip.Text = dlg1.SelectedPath;
                this.tbDirectoryInArchive.Text = System.IO.Path.GetFileName(this.tbDirectoryToZip.Text);
            }
        }

        private void btnCreateZipBrowse_Click(object sender, EventArgs e)
        {
            var dlg1 = new SaveFileDialog
            {
                FileName = System.IO.Path.GetFileName(this.tbZipToCreate.Text),
                InitialDirectory = System.IO.Path.GetDirectoryName(this.tbZipToCreate.Text),
                OverwritePrompt = false,
                Title = "Where would you like to save the generated Zip file?",
                Filter = "ZIP files|*.zip",
            };

            var result = dlg1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.tbZipToCreate.Text = dlg1.FileName;
            }
        }


        private void btnZipup_Click(object sender, EventArgs e)
        {
            KickoffZipup();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (this.lblStatus.InvokeRequired)
            {
                this.lblStatus.Invoke(new ButtonClick(this.btnCancel_Click), new object[] { sender, e });
            }
            else
            {
                _operationCanceled = true;
                lblStatus.Text = "Canceled...";
                ResetUiState();
            }
        }

        private void radioFlavorSfx_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioFlavorSfxGui.Checked || this.radioFlavorSfxCmd.Checked)
            {
                // intelligently change the name of the thing to create
                if (this.tbZipToCreate.Text.ToUpper().EndsWith(".ZIP"))
                {
                    tbZipToCreate.Text = System.Text.RegularExpressions.Regex.Replace(tbZipToCreate.Text, "(?i:)\\.zip$", ".exe");
                }

#if TOO_SMART
                // Always use UTF-8 when creating a self-extractor.
                // A zip created with UTF-8 encoding is foolproof when 
                // extracted with the DotNetZip library.  The only reason you wouldn't
                // want to use UTF-8 is when your extractor doesn't support it. 
                // But DotNetZip supports it, and DotNetZip is the extractor on a SFX, 
                // so .. we'll use UTF-8 when these checkboxes are checked.  

                // but we will cache the current setting, so if the user clicks back
                // to the traditional zip, then he will get his favorite flavor of encoding restored. 
                if (_mostRecentEncoding == null)
                    _mostRecentEncoding = this.comboBox1.SelectedItem.ToString();
                this.comboBox1.SelectedIndex = 1; // UTF-8
                this.comboBox1.Enabled = false;

                // We also do the same thing with the ZIP64 setting, for the same reason. 
                // Extracting from a ZIP64 is foolproof when DotNetZip is the extractor. 

                if (_mostRecentZip64 == null)
                {
                    Zip64Option x =
            (this.radioZip64AsNecessary.Checked)
            ? Zip64Option.AsNecessary
            : (this.radioZip64Always.Checked)
            ? Zip64Option.Always
            : Zip64Option.Never;
                    _mostRecentZip64 = new Nullable<Zip64Option>(x);
                }
                this.radioZip64Always.Checked = true;
                this.radioZip64Always.Enabled = false;
                this.radioZip64AsNecessary.Enabled = false;
                this.radioZip64Never.Enabled = false;
#endif
            }
        }

        private void radioFlavorZip_CheckedChanged(object sender, EventArgs e)
        {

            if (this.radioFlavorZip.Checked)
            {
                // intelligently change the name of the thing to create
                if (this.tbZipToCreate.Text.ToUpper().EndsWith(".EXE"))
                {
                    tbZipToCreate.Text = System.Text.RegularExpressions.Regex.Replace(tbZipToCreate.Text, "(?i:)\\.exe$", ".zip");
                }
#if TOO_SMART
                // re-enable the encoding, and set it to what it was most recently
                this.comboBox1.Enabled = true;
                if (_mostRecentEncoding != null)
                {
                    this.SelectNamedEncoding(_mostRecentEncoding);
                    _mostRecentEncoding = null;
                }

                // re-enable the zip64 setting, too.
                this.radioZip64Always.Enabled = true;
                this.radioZip64AsNecessary.Enabled = true;
                this.radioZip64Never.Enabled = true;
                if (_mostRecentZip64 != null)
                {
                    if (_mostRecentZip64.Value == Zip64Option.Always)
                        this.radioZip64Always.Checked = true;
                    if (_mostRecentZip64.Value == Zip64Option.AsNecessary)
                        this.radioZip64AsNecessary.Checked = true;
                    if (_mostRecentZip64.Value == Zip64Option.Never)
                        this.radioZip64Never.Checked = true;
                    _mostRecentZip64 = null;
                }
#endif
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            //this.tbPassword.Enabled = (this.comboBox3.SelectedItem.ToString() != "None");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.tbPassword.PasswordChar = (this.chkHidePassword.Checked) ? '*' : '\0';
        }

        private void ResetUiState()
        {
            this.btnZipUp.Text = "Zip it!";
            this.btnZipUp.Enabled = true;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Enabled = false;
            this.btnExtract.Text = "Extract";
            this.btnExtractDirBrowse.Enabled = true;
            this.btnZipupDirBrowse.Enabled = true;
            this.btnReadZipBrowse.Enabled = true;

            this.progressBar1.Value = 0;
            this.progressBar2.Value = 0;
            this.Cursor = Cursors.Default;
            if (!_workerThread.IsAlive)
                _workerThread.Join();

            _working = false;
        }


        /// This app uses the windows registry to store config data for itself. 
        ///     - creates a key for this DotNetZip Winforms app, if one does not exist
        ///     - stores and retrieves the most recent settings.
        ///     - this is done on a per user basis. (HKEY_CURRENT_USER)
        private void FillFormFromRegistry()
        {
            if (AppCuKey != null)
            {
                var s = (string)AppCuKey.GetValue(_rvn_DirectoryToZip);
                if (s != null)
                {
                    this.tbDirectoryToZip.Text = s;
                    this.tbDirectoryInArchive.Text = System.IO.Path.GetFileName(this.tbDirectoryToZip.Text);
                }

                s = (string)AppCuKey.GetValue(_rvn_SelectionToZip);
                if (s != null) this.tbSelectionToZip.Text = s;

                s = (string)AppCuKey.GetValue(_rvn_SelectionToExtract);
                if (s != null) this.tbSelectionToExtract.Text = s;

                s = (string)AppCuKey.GetValue(_rvn_ZipTarget);
                if (s != null) this.tbZipToCreate.Text = s;

                s = (string)AppCuKey.GetValue(_rvn_ZipToOpen);
                if (s != null) this.tbZipToOpen.Text = s;

                s = (string)AppCuKey.GetValue(_rvn_ExtractLoc);
                if (s != null) this.tbExtractDir.Text = s;
                else
                    this.tbExtractDir.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                s = (string)AppCuKey.GetValue(_rvn_Encoding);
                if (s != null)
                {
                    SelectNamedEncoding(s);
                }

                s = (string)AppCuKey.GetValue(_rvn_Compression);
                if (s != null)
                {
                    SelectNamedCompressionLevel(s);
                }
                else SelectNamedCompressionLevel("LEVEL6_DEFAULT");

                s = (string)AppCuKey.GetValue(_rvn_Encryption);
                if (s != null)
                {
                    SelectNamedEncryption(s);
                    this.tbPassword.Text = "";
                }


                int x = (Int32)AppCuKey.GetValue(_rvn_ZipFlavor, 0);
                if (x == 2)
                    this.radioFlavorSfxCmd.Checked = true;
                else if (x == 1)
                    this.radioFlavorSfxGui.Checked = true;
                else
                    this.radioFlavorZip.Checked = true;

                x = (Int32)AppCuKey.GetValue(_rvn_Zip64Option, 0);
                if (x == 1)
                    this.radioZip64AsNecessary.Checked = true;
                else if (x == 2)
                    this.radioZip64Always.Checked = true;
                else
                    this.radioZip64Never.Checked = true;


                x = (Int32)AppCuKey.GetValue(_rvn_FormTab, 1);
                if (x == 0 || x == 1)
                    this.tabControl1.SelectedIndex = x;

                x = (Int32)AppCuKey.GetValue(_rvn_HidePassword, 1);
                this.chkHidePassword.Checked = (x != 0);

                x = (Int32)AppCuKey.GetValue(_rvn_Overwrite, 1);
                this.chkOverwrite.Checked = (x != 0);

                x = (Int32)AppCuKey.GetValue(_rvn_OpenExplorer, 1);
                this.chkOpenExplorer.Checked = (x != 0);

                // set the geometry of the form
                s = (string)AppCuKey.GetValue(_rvn_Geometry);
                if (!String.IsNullOrEmpty(s))
                {
                    int[] p = Array.ConvertAll<string, int>(s.Split(','),
                                     new Converter<string, int>((t) => { return Int32.Parse(t); }));
                    if (p != null && p.Length == 5)
                    {
                        this.Bounds = ConstrainToScreen(new System.Drawing.Rectangle(p[0], p[1], p[2], p[3]));

                        // Starting a window minimized is confusing...
                        //this.WindowState = (FormWindowState)p[4];
                    }
                }

                AppCuKey.Close();
                AppCuKey = null;

                tbPassword_TextChanged(null, null);
            }
        }



        private void SaveFormToRegistry()
        {
            if (AppCuKey != null)
            {
                AppCuKey.SetValue(_rvn_DirectoryToZip, this.tbDirectoryToZip.Text);
                AppCuKey.SetValue(_rvn_SelectionToZip, this.tbSelectionToZip.Text);
                AppCuKey.SetValue(_rvn_SelectionToExtract, this.tbSelectionToExtract.Text);
                AppCuKey.SetValue(_rvn_ZipTarget, this.tbZipToCreate.Text);
                AppCuKey.SetValue(_rvn_ZipToOpen, this.tbZipToOpen.Text);
                AppCuKey.SetValue(_rvn_Encoding, this.comboBox1.SelectedItem.ToString());
                AppCuKey.SetValue(_rvn_Compression, this.comboBox2.SelectedItem.ToString());
                if (this.tbPassword.Text == "")
                {
                    if (!String.IsNullOrEmpty(_mostRecentEncryption))
                        AppCuKey.SetValue(_rvn_Encryption, _mostRecentEncryption);
                }
                else
                    AppCuKey.SetValue(_rvn_Encryption, this.comboBox3.SelectedItem.ToString());

                AppCuKey.SetValue(_rvn_ExtractLoc, this.tbExtractDir.Text);

                int x = 0;
                if (this.radioFlavorSfxCmd.Checked)
                    x = 2;
                else if (this.radioFlavorSfxGui.Checked)
                    x = 1;
                AppCuKey.SetValue(_rvn_ZipFlavor, x);

                x = 0;
                if (this.radioZip64AsNecessary.Checked)
                    x = 1;
                else if (this.radioZip64Always.Checked)
                    x = 2;
                AppCuKey.SetValue(_rvn_Zip64Option, x);

                AppCuKey.SetValue(_rvn_FormTab, this.tabControl1.SelectedIndex);

                AppCuKey.SetValue(_rvn_LastRun, System.DateTime.Now.ToString("yyyy MMM dd HH:mm:ss"));
                x = (Int32)AppCuKey.GetValue(_rvn_Runs, 0);
                x++;
                AppCuKey.SetValue(_rvn_Runs, x);

                AppCuKey.SetValue(_rvn_HidePassword, this.chkHidePassword.Checked ? 1 : 0);
                AppCuKey.SetValue(_rvn_Overwrite, this.chkOverwrite.Checked ? 1 : 0);
                AppCuKey.SetValue(_rvn_OpenExplorer, this.chkOpenExplorer.Checked ? 1 : 0);

                // store the size of the form
                int w = 0, h = 0, left = 0, top = 0;
                if (this.Bounds.Width < this.MinimumSize.Width || this.Bounds.Height < this.MinimumSize.Height)
                {
                    // RestoreBounds is the size of the window prior to last minimize action.
                    // But the form may have been resized since then!
                    w = this.RestoreBounds.Width;
                    h = this.RestoreBounds.Height;
                    left = this.RestoreBounds.Location.X;
                    top = this.RestoreBounds.Location.Y;
                }
                else
                {
                    w = this.Bounds.Width;
                    h = this.Bounds.Height;
                    left = this.Location.X;
                    top = this.Location.Y;
                }
                AppCuKey.SetValue(_rvn_Geometry,
                  String.Format("{0},{1},{2},{3},{4}",
                        left, top, w, h, (int)this.WindowState));

                AppCuKey.Close();
            }
        }


        private System.Drawing.Rectangle ConstrainToScreen(System.Drawing.Rectangle bounds)
        {
            Screen screen = Screen.FromRectangle(bounds);
            System.Drawing.Rectangle workingArea = screen.WorkingArea;
            int width = Math.Min(bounds.Width, workingArea.Width);
            int height = Math.Min(bounds.Height, workingArea.Height);
            // mmm....minimax            
            int left = Math.Min(workingArea.Right - width, Math.Max(bounds.Left, workingArea.Left));
            int top = Math.Min(workingArea.Bottom - height, Math.Max(bounds.Top, workingArea.Top));
            return new System.Drawing.Rectangle(left, top, width, height);
        }

        private void SelectNamedEncoding(string s)
        {
            _SelectComboBoxItem(this.comboBox2, s);
        }

        private void SelectNamedCompressionLevel(string s)
        {
            _SelectComboBoxItem(this.comboBox2, s);
        }

        private void SelectNamedEncryption(string s)
        {
            _SelectComboBoxItem(this.comboBox3, s);
            //tbPassword.Text = "";
            comboBox3_SelectedIndexChanged(null, null);
        }

        private void _SelectComboBoxItem(ComboBox c, string s)
        {
            for (int i = 0; i < c.Items.Count; i++)
            {
                if (c.Items[i].ToString() == s)
                {
                    c.SelectedIndex = i;
                    break;
                }
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveFormToRegistry();
        }



        private void btnZipBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = (System.IO.File.Exists(this.tbZipToOpen.Text) ? System.IO.Path.GetDirectoryName(this.tbZipToOpen.Text) : this.tbZipToOpen.Text);
            openFileDialog1.Filter = "zip files|*.zip|EXE files|*.exe|All Files|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.tbZipToOpen.Text = openFileDialog1.FileName;
                if (System.IO.File.Exists(this.tbZipToOpen.Text))
                    btnOpen_Click(sender, e);
            }
        }



        string _DisplayedZip = null;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(this.tbZipToOpen.Text)) return;

            _DisplayedZip = this.tbZipToOpen.Text;

            listView1.Clear();
            listView1.BeginUpdate();

            string[] columnHeaders = new string[] { "n", "name", "lastmod", "original", "ratio", "compressed", "enc?", "CRC" };
            foreach (string label in columnHeaders)
            {
                SortableColumnHeader ch = new SortableColumnHeader(label);
                if (label != "name" && label != "lastmod")
                    ch.TextAlign = HorizontalAlignment.Right;
                listView1.Columns.Add(ch);
            }

            int n = 1;
            using (ZipFile zip = ZipFile.Read(_DisplayedZip))
            {
                foreach (ZipEntry entry in zip)
                {
                    ListViewItem item = new ListViewItem(n.ToString());
                    n++;
                    string[] subitems = new string[] {
                        entry.FileName.Replace("/","\\"),
                        entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                        entry.UncompressedSize.ToString(),
                        String.Format("{0,5:F0}%", entry.CompressionRatio),
                        entry.CompressedSize.ToString(),
                        (entry.UsesEncryption) ? "Y" : "N",
                        String.Format("{0:X8}", entry.Crc32)};

                    foreach (String s in subitems)
                    {
                        ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                        subitem.Text = s;
                        item.SubItems.Add(subitem);
                    }

                    this.listView1.Items.Add(item);
                }
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.listView1.EndUpdate();
            this.btnExtract.Enabled = true;
        }


        private void btnExtractDirBrowse_Click(object sender, EventArgs e)
        {
            // pop a dialog to ask where to extract
            // Configure the "select folder" dialog box
            //_folderName = tbDirName.Text;
            //_folderName = (System.IO.Directory.Exists(_folderName)) ? _folderName : "";
            var dlg1 = new Ionic.Utils.FolderBrowserDialogEx
            {
                Description = "Select a folder to extract to:",
                ShowNewFolderButton = true,
                ShowEditBox = true,
                //NewStyle = false,
                SelectedPath = tbExtractDir.Text,
                ShowFullPathInEditBox = true,
            };
            dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            var result = dlg1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.tbExtractDir.Text = dlg1.SelectedPath;
                // actually extract the files
            }


        }



        private void btnExtract_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(_DisplayedZip)) return;
            KickoffExtract();
        }


        private void KickoffExtract()
        {
            if (String.IsNullOrEmpty(this.tbExtractDir.Text)) return;

            _hrt = new HiResTimer();
            _hrt.Start();

            _working = true;
            _operationCanceled = false;
            _nFilesCompleted = 0;
            _totalBytesAfterCompress = 0;
            _totalBytesBeforeCompress = 0;
            DisableButtons();
            lblStatus.Text = "Extracting...";

            var options = new ExtractWorkerOptions
            {
                ExtractLocation = this.tbExtractDir.Text,
                Selection = this.tbSelectionToExtract.Text,
                OpenExplorer = this.chkOpenExplorer.Checked,
                ExtractExisting = (this.chkOverwrite.Checked)
                    ? ExtractExistingFileAction.OverwriteSilently
                    : ExtractExistingFileAction.DontOverwrite,
            };

            _workerThread = new Thread(this.DoExtract);
            _workerThread.Name = "Zip Extractor thread";
            _workerThread.Start(options);
            this.Cursor = Cursors.WaitCursor;
        }


        private void DisableButtons()
        {
            // this set for Zipping
            this.btnZipUp.Text = "Zipping...";
            this.btnZipUp.Enabled = false;
            this.btnZipupDirBrowse.Enabled = false;
            this.btnCreateZipBrowse.Enabled = false;
            this.btnCancel.Enabled = true;

            // this for Extract
            this.btnExtract.Enabled = false;
            this.btnExtract.Text = "working...";
            this.btnExtractDirBrowse.Enabled = false;
            this.btnCancel.Enabled = true;
            this.btnReadZipBrowse.Enabled = false;
        }


        private void zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
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


        private void OnExtractDone()
        {
            if (this.lblStatus.InvokeRequired)
            {
                this.lblStatus.Invoke(new MethodInvoker(this.OnExtractDone));
            }
            else
            {
                _hrt.Stop();
                System.TimeSpan ts = new System.TimeSpan(0, 0, (int)_hrt.Seconds);
                lblStatus.Text = String.Format("Done, Extracted {0} files, time: {1}",
                           _nFilesCompleted,
                           ts.ToString());
                ResetUiState();
            }
        }


        bool _setCancel = false;
        private void DoExtract(object p)
        {
            ExtractWorkerOptions options = p as ExtractWorkerOptions;

            bool extractCancelled = false;
            _setCancel = false;
            string currentPassword = "";

            try
            {
                using (var zip = ZipFile.Read(_DisplayedZip))
                {
                    System.Collections.Generic.ICollection<ZipEntry> collection = null;
                    if (String.IsNullOrEmpty(options.Selection))
                        collection = zip.Entries;
                    else
                        collection = zip.SelectEntries(options.Selection);

                    _totalEntriesToProcess = collection.Count;
                    zip.ExtractProgress += zip_ExtractProgress;
                    SetProgressBars();
                    foreach (global::Ionic.Zip.ZipEntry entry in collection)
                    {
                        if (_setCancel) { extractCancelled = true; break; }
                        if (entry.Encryption == global::Ionic.Zip.EncryptionAlgorithm.None)
                        {
                            try
                            {
                                entry.Extract(options.ExtractLocation, options.ExtractExisting);
                            }
                            catch (Exception ex1)
                            {
                                string msg = String.Format("Faisled to extract entry {0} -- {1}",
                                               entry.FileName,
                                               ex1.Message.ToString());
                                DialogResult result =
                                    MessageBox.Show(msg,
                                            String.Format("Error Extracting {0}", entry.FileName),
                                            MessageBoxButtons.OKCancel,
                                            MessageBoxIcon.Exclamation,
                                            MessageBoxDefaultButton.Button1);

                                if (result == DialogResult.Cancel)
                                {
                                    _setCancel = true;
                                    extractCancelled = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bool done = false;
                            while (!done)
                            {
                                if (currentPassword == "")
                                {
                                    string t = PromptForPassword(entry.FileName);
                                    if (t == "")
                                    {
                                        done = true; // escape ExtractWithPassword loop
                                        continue;
                                    }
                                    currentPassword = t;
                                }

                                if (currentPassword == null) // cancel all 
                                {
                                    _setCancel = true;
                                    currentPassword = "";
                                    break;
                                }

                                try
                                {
                                    entry.ExtractWithPassword(options.ExtractLocation, options.ExtractExisting, currentPassword);
                                    done = true;
                                }
                                catch (Exception ex2)
                                {
                                    // Retry here in the case of bad password.
                                    if (ex2 as Ionic.Zip.BadPasswordException != null)
                                    {
                                        currentPassword = "";
                                        continue; // loop around, ask for password again
                                    }
                                    else
                                    {
                                        string msg =
                                            String.Format("Failed to extract the password-encrypted entry {0} -- {1}",
                                                  entry.FileName, ex2.Message.ToString());
                                        DialogResult result =
                                            MessageBox.Show(msg,
                                                    String.Format("Error Extracting {0}",
                                                          entry.FileName),
                                                    MessageBoxButtons.OKCancel,
                                                    MessageBoxIcon.Exclamation,
                                                    MessageBoxDefaultButton.Button1);

                                        done = true; // done with this entry
                                        if (result == DialogResult.Cancel)
                                        {
                                            _setCancel = true;
                                            extractCancelled = true;
                                            break;
                                        }
                                    }
                                }
                            } // while
                        } // else (encryption)
                    } // foreach
                } // using
            }
            catch (Exception ex1)
            {
                MessageBox.Show(String.Format("There's been a problem extracting that zip file.  {0}", ex1.Message),
                "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                Application.Exit();
            }

            OnExtractDone();

            if (extractCancelled) return;

            if (options.OpenExplorer)
            {
                string w = System.IO.Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.System));
                if (w == null) w = "c:\\windows";
                try
                {
                    System.Diagnostics.Process.Start(System.IO.Path.Combine(w, "explorer.exe"), options.ExtractLocation);
                }
                catch { }
            }
        }


        private string PromptForPassword(string entryName)
        {
            PasswordDialog dlg1 = new PasswordDialog();
            dlg1.EntryName = entryName;

            // ask for password in a loop until user enters a proper one, 
            // or clicks skip or cancel.
            bool done = false;
            do
            {
                dlg1.ShowDialog();
                done = (dlg1.Result != PasswordDialog.PasswordDialogResult.OK ||
                    dlg1.Password != "");
            } while (!done);

            if (dlg1.Result == PasswordDialog.PasswordDialogResult.OK)
                return dlg1.Password;

            else if (dlg1.Result == PasswordDialog.PasswordDialogResult.Skip)
                return "";

            // cancel
            return null;
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Recycle the progress bars the cancel button, and the status textbox.  
            // An alternative way to accomplish a similar thing is to visually place 
            // the progress bars OFF the tabs.
            if (this.tabControl1.SelectedIndex == 0)
            {
                if (this.tabPage2.Controls.Contains(this.progressBar1))
                    this.tabPage2.Controls.Remove(this.progressBar1);
                if (this.tabPage2.Controls.Contains(this.progressBar2))
                    this.tabPage2.Controls.Remove(this.progressBar2);
                if (this.tabPage2.Controls.Contains(this.btnCancel))
                    this.tabPage2.Controls.Remove(this.btnCancel);
                if (this.tabPage2.Controls.Contains(this.lblStatus))
                    this.tabPage2.Controls.Remove(this.lblStatus);

                if (!this.tabPage1.Controls.Contains(this.progressBar1))
                    this.tabPage1.Controls.Add(this.progressBar1);
                if (!this.tabPage1.Controls.Contains(this.progressBar2))
                    this.tabPage1.Controls.Add(this.progressBar2);
                if (!this.tabPage1.Controls.Contains(this.btnCancel))
                    this.tabPage1.Controls.Add(this.btnCancel);
                if (!this.tabPage1.Controls.Contains(this.lblStatus))
                    this.tabPage1.Controls.Add(this.lblStatus);
            }
            else if (this.tabControl1.SelectedIndex == 1)
            {
                if (this.tabPage1.Controls.Contains(this.progressBar1))
                    this.tabPage1.Controls.Remove(this.progressBar1);
                if (this.tabPage1.Controls.Contains(this.progressBar2))
                    this.tabPage1.Controls.Remove(this.progressBar2);
                if (this.tabPage1.Controls.Contains(this.btnCancel))
                    this.tabPage1.Controls.Remove(this.btnCancel);
                if (this.tabPage1.Controls.Contains(this.lblStatus))
                    this.tabPage1.Controls.Remove(this.lblStatus);

                if (!this.tabPage2.Controls.Contains(this.progressBar1))
                    this.tabPage2.Controls.Add(this.progressBar1);
                if (!this.tabPage2.Controls.Contains(this.progressBar2))
                    this.tabPage2.Controls.Add(this.progressBar2);
                if (!this.tabPage2.Controls.Contains(this.btnCancel))
                    this.tabPage2.Controls.Add(this.btnCancel);
                if (!this.tabPage2.Controls.Contains(this.lblStatus))
                    this.tabPage2.Controls.Add(this.lblStatus);
            }
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // prevent switching TABs if working
            if (_working) e.Cancel = true;
        }


        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Create an instance of the ColHeader class.
            SortableColumnHeader clickedCol = (SortableColumnHeader)this.listView1.Columns[e.Column];

            // Set the ascending property to sort in the opposite order.
            clickedCol.SortAscending = !clickedCol.SortAscending;

            // Get the number of items in the list.
            int numItems = this.listView1.Items.Count;

            // Turn off display while data is repoplulated.
            this.listView1.BeginUpdate();

            // Populate an ArrayList with a SortWrapper of each list item.
            List<ItemWrapper> list = new List<ItemWrapper>();
            for (int i = 0; i < numItems; i++)
            {
                list.Add(new ItemWrapper(this.listView1.Items[i], e.Column));
            }

            if (e.Column == 0 || e.Column == 3 || e.Column == 5)
                list.Sort(new ItemWrapper.NumericComparer(clickedCol.SortAscending));
            else
                list.Sort(new ItemWrapper.StringComparer(clickedCol.SortAscending));

            // Clear the list, and repopulate with the sorted items.
            this.listView1.Items.Clear();
            for (int i = 0; i < numItems; i++)
                this.listView1.Items.Add(list[i].Item);

            // Turn display back on.
            this.listView1.EndUpdate();
        }


        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            if (this.tbPassword.Text == "")
            {
                if (_mostRecentEncryption == null && this.comboBox3.SelectedItem.ToString() != "None")
                {
                    _mostRecentEncryption = this.comboBox3.SelectedItem.ToString();
                    SelectNamedEncryption("None");
                }
            }
            else
            {
                if (_mostRecentEncryption != null && this.comboBox3.SelectedItem.ToString() == "None")
                {
                    SelectNamedEncryption(_mostRecentEncryption);
                }
                _mostRecentEncryption = null;
            }
        }


        public Microsoft.Win32.RegistryKey AppCuKey
        {
            get
            {
                if (_appCuKey == null)
                {
                    _appCuKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_AppRegyPath, true);
                    if (_appCuKey == null)
                        _appCuKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_AppRegyPath);
                }
                return _appCuKey;
            }
            set { _appCuKey = null; }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (_initialFileToLoad != null)
            {
                // select the page that opens zip files.
                this.tabControl1.SelectedIndex = 0;
                //this.tabPage1.Select(); 
                this.tbZipToOpen.Text = _initialFileToLoad;
                btnOpen_Click(null, null);
            }
        }

        private void tbDirectoryToZip_Leave(object sender, EventArgs e)
        {
            this.tbDirectoryInArchive.Text = System.IO.Path.GetFileName(this.tbDirectoryToZip.Text);
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }


        //private string _folderName;
        //private int _priorLeft, _priorTop;
        private int _progress2MaxFactor;
        private int _totalEntriesToProcess;
        private bool _working;
        private bool _operationCanceled;
        private int _nFilesCompleted;
        private long _totalBytesBeforeCompress;
        private long _totalBytesAfterCompress;
        private Thread _workerThread;
        private static string TB_COMMENT_NOTE = "-zip file comment here-";
        private List<String> _EncodingNames;
        private List<String> _CompressionLevelNames;
        private List<String> _EncryptionNames;
        //private string _mostRecentEncoding;
        //private Nullable<Zip64Option> _mostRecentZip64;
        private String _mostRecentEncryption;

        private Microsoft.Win32.RegistryKey _appCuKey;
        private static string _AppRegyPath = "Software\\Dino Chiesa\\DotNetZip Winforms Tool";
        private static string _rvn_FormTab = "FormTab";
        private static string _rvn_Geometry = "Geometry";
        private static string _rvn_HidePassword = "HidePassword";
        private static string _rvn_Overwrite = "Overwrite";
        private static string _rvn_OpenExplorer = "OpenExplorer";
        private static string _rvn_ExtractLoc = "ExtractLoc";
        private static string _rvn_DirectoryToZip = "DirectoryToZip";
        private static string _rvn_SelectionToZip = "SelectionToZip";
        private static string _rvn_SelectionToExtract = "SelectionToExtract";
        private static string _rvn_ZipTarget = "ZipTarget";
        private static string _rvn_ZipToOpen = "ZipToOpen";
        private static string _rvn_Encoding = "Encoding";
        private static string _rvn_Compression = "Compression";
        private static string _rvn_Encryption = "Encryption";
        private static string _rvn_ZipFlavor = "ZipFlavor";
        private static string _rvn_Zip64Option = "Zip64Option";
        private static string _rvn_LastRun = "LastRun";
        private static string _rvn_Runs = "Runs";
        private string _initialFileToLoad;

    }


    // The ColHeader class is a ColumnHeader object with an
    // added property for determining an ascending or descending sort.
    // True specifies an ascending order, false specifies a descending order.
    public class SortableColumnHeader : ColumnHeader
    {
        public bool SortAscending;
        public SortableColumnHeader(string text)
        {
            this.Text = text;
            this.SortAscending = true;
        }
    }


    // An instance of the SortWrapper class is created for
    // each item and added to the ArrayList for sorting.
    public class ItemWrapper
    {
        internal ListViewItem Item;
        internal int Column;

        // A SortWrapper requires the item and the index of the clicked column.
        public ItemWrapper(ListViewItem item, int column)
        {
            Item = item;
            Column = column;
        }

        // Text property for getting the text of an item.
        public string Text
        {
            get { return Item.SubItems[Column].Text; }
        }

        // Implementation of the IComparer
        public class StringComparer : IComparer<ItemWrapper>
        {
            bool ascending;

            // Constructor requires the sort order;
            // true if ascending, otherwise descending.
            public StringComparer(bool asc)
            {
                this.ascending = asc;
            }

            // Implemnentation of the IComparer:Compare
            // method for comparing two objects.
            public int Compare(ItemWrapper xItem, ItemWrapper yItem)
            {
                string xText = xItem.Item.SubItems[xItem.Column].Text;
                string yText = yItem.Item.SubItems[yItem.Column].Text;
                return xText.CompareTo(yText) * (this.ascending ? 1 : -1);
            }
        }

        public class NumericComparer : IComparer<ItemWrapper>
        {
            bool ascending;

            // Constructor requires the sort order;
            // true if ascending, otherwise descending.
            public NumericComparer(bool asc)
            {
                this.ascending = asc;
            }

            // Implementation of the IComparer:Compare
            // method for comparing two objects.
            public int Compare(ItemWrapper xItem, ItemWrapper yItem)
            {
                int x = 0, y = 0;
                try
                {
                    x = Int32.Parse(xItem.Item.SubItems[xItem.Column].Text);
                    y = Int32.Parse(yItem.Item.SubItems[yItem.Column].Text);
                }
                catch
                {
                    try
                    {
                        // lop off one char for %
                        String trimmed = null;
                        trimmed = xItem.Item.SubItems[xItem.Column].Text;
                        trimmed = trimmed.Substring(0, trimmed.Length - 1);
                        x = Int32.Parse(trimmed);
                        trimmed = xItem.Item.SubItems[yItem.Column].Text;
                        trimmed = trimmed.Substring(0, trimmed.Length - 1);
                        y = Int32.Parse(trimmed);
                    }
                    catch { }
                }
                return (x - y) * (this.ascending ? 1 : -1);
            }
        }
    }
    public class ExtractWorkerOptions
    {
        public string ExtractLocation;
        public Ionic.Zip.ExtractExistingFileAction ExtractExisting;
        public bool OpenExplorer;
        public String Selection;
    }
    public class SaveWorkerOptions
    {
        public string ZipName;
        public string Folder;
        public string Selection;
        public String DirInArchive;
        public string Encoding;
        public string Comment;
        public string Password;
        public int ZipFlavor;
        public Ionic.Zlib.CompressionLevel CompressionLevel;
        public Ionic.Zip.EncryptionAlgorithm Encryption;
        public Zip64Option Zip64;
    }


    internal class HiResTimer
    {
        // usage: 
        // 
        //  hrt= new HiResTimer();
        //  hrt.Start();
        //     ... do work ... 
        //  hrt.Stop();
        //  System.Console.WriteLine("elapsed time: {0:N4}", hrt.Seconds);
        //

        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(ref long lpPerformanceCount);

        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        private static extern bool QueryPerformanceFrequency(ref long lpFrequency);

        private long m_TickCountAtStart = 0;
        private long m_TickCountAtStop = 0;
        private long m_ElapsedTicks = 0;

        public HiResTimer()
        {
            m_Frequency = 0;
            QueryPerformanceFrequency(ref m_Frequency);
        }

        public void Start()
        {
            m_TickCountAtStart = 0;
            QueryPerformanceCounter(ref m_TickCountAtStart);
        }

        public void Stop()
        {
            m_TickCountAtStop = 0;
            QueryPerformanceCounter(ref m_TickCountAtStop);
            m_ElapsedTicks = m_TickCountAtStop - m_TickCountAtStart;
        }

        public void Reset()
        {
            m_TickCountAtStart = 0;
            m_TickCountAtStop = 0;
            m_ElapsedTicks = 0;
        }

        public long Elapsed
        {
            get { return m_ElapsedTicks; }
        }

        public float Seconds
        {
            get { return ((float)m_ElapsedTicks / (float)m_Frequency); }
        }

        private long m_Frequency = 0;
        public long Frequency
        {
            get { return m_Frequency; }
        }

    }

}
