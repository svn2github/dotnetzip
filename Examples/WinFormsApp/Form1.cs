using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;

namespace WinFormsExample
{
    public partial class Form1 : Form
    {
        delegate void SaveEntryProgress(SaveProgressEventArgs e);
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
        }

        private void InitEncryptionList()
        {
            _EncryptionNames = new List<string>(Enum.GetNames(typeof(Ionic.Zip.EncryptionAlgorithm)));
            //_EncryptionNames.Sort();
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
            this.Text = String.Format("WinForms Zip Creator Example for DotNetZip v{0}",
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
            _folderName = tbDirName.Text;

            if (_folderName == null || _folderName == "") return;
            if (this.tbZipName.Text == null || this.tbZipName.Text == "") return;

            // check for existence of the zip file:
            if (System.IO.File.Exists(this.tbZipName.Text))
            {
                var dlgResult = MessageBox.Show(String.Format("The file you have specified ({0}) already exists.  Do you want to overwrite this file?", this.tbZipName.Text), "Confirmation is Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlgResult != DialogResult.Yes) return;
                System.IO.File.Delete(this.tbZipName.Text);
            }


            _hrt = new HiResTimer();
            _hrt.Start();

            _saveCanceled = false;
            _nFilesCompleted = 0;
            _totalBytesAfterCompress = 0;
            _totalBytesBeforeCompress = 0;
            this.btnOk.Enabled = false;
            this.btnOk.Text = "Zipping...";
            this.btnCancel.Enabled = true;
            lblStatus.Text = "Zipping...";

            var options = new WorkerOptions
            {
                ZipName = this.tbZipName.Text,
                Folder = _folderName,
                Encoding = "ibm437"
            };

            if (this.comboBox1.SelectedIndex != 0)
            {
                options.Encoding = this.comboBox1.SelectedItem.ToString();
            }

	    options.Encryption = (Ionic.Zip.EncryptionAlgorithm) Enum.Parse(typeof(Ionic.Zip.EncryptionAlgorithm),
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
                        options.Encryption.ToString(),
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


        private void SetProgressBars()
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new MethodInvoker(this.SetProgressBars));
            }
            else
            {
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = _entriesToZip;
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
            WorkerOptions options = p as WorkerOptions;
            try
            {
                using (var zip1 = new ZipFile())
                {
                    zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(options.Encoding);
                    zip1.Comment = options.Comment;
                    zip1.Password = options.Password;
                    zip1.Encryption = options.Encryption;
                    zip1.AddDirectory(options.Folder);
                    _entriesToZip = zip1.EntryFileNames.Count;
                    SetProgressBars();
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
                MessageBox.Show(String.Format("Exception while zipping: {0}", exc1.Message));
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
            if (_saveCanceled)
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
                lblStatus.Text = (this.radioFlavorSfxCmd.Checked || this.radioFlavorSfxGui.Checked)
                    ? "Temp archive saved...compiling SFX..."
                    : "Temp archive saved...";
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
                ResetState();
            }
        }



        private void StepArchiveProgress(SaveProgressEventArgs e)
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new SaveEntryProgress(this.StepArchiveProgress), new object[] { e });
            }
            else
            {
                if (!_saveCanceled)
                {
                    _nFilesCompleted++;
                    this.progressBar1.PerformStep();
                    _totalBytesAfterCompress += e.CurrentEntry.CompressedSize;
                    _totalBytesBeforeCompress += e.CurrentEntry.UncompressedSize;

                    // reset the progress bar for the entry:
                    this.progressBar2.Value = this.progressBar2.Maximum = 1;

                    this.Update();

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

                }
            }
        }


        private void StepEntryProgress(SaveProgressEventArgs e)
        {
            if (this.progressBar2.InvokeRequired)
            {
                this.progressBar2.Invoke(new SaveEntryProgress(this.StepEntryProgress), new object[] { e });
            }
            else
            {
                if (!_saveCanceled)
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
                            _nFilesCompleted + 1, _entriesToZip, e.CurrentEntry.FileName);
                    }

                    // downcast should be safe because we have shifted e.BytesTransferred
                    int xferred = (int) (e.BytesTransferred >> _progress2MaxFactor);

                    this.progressBar2.Value = (xferred >= this.progressBar2.Maximum)
                        ? this.progressBar2.Maximum
                        : xferred;

                    this.Update();
                }
            }
        }



        private void btnDirBrowse_Click(object sender, EventArgs e)
        {
            _folderName = tbDirName.Text;
            // Configure open file dialog box
            var dlg1 = new System.Windows.Forms.FolderBrowserDialog();
            //dlg1.RootFolder = "c:\\";
            dlg1.SelectedPath = (System.IO.Directory.Exists(_folderName)) ? _folderName : "c:\\";
            dlg1.ShowNewFolderButton = false;

            var result = dlg1.ShowDialog();

            if (result == DialogResult.OK)
            {
                _folderName = dlg1.SelectedPath;
                tbDirName.Text = _folderName;
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
                _saveCanceled = true;
                lblStatus.Text = "Canceled...";
                ResetState();
            }
        }

        private void radioFlavorSfx_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioFlavorSfxGui.Checked || this.radioFlavorSfxCmd.Checked)
            {
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

                // intelligently change the name of the thing to create
                if (this.tbZipName.Text.ToUpper().EndsWith(".ZIP"))
                {
                    tbZipName.Text = System.Text.RegularExpressions.Regex.Replace(tbZipName.Text, "(?i:)\\.zip$", ".exe");
                }

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
            }
        }

        private void radioFlavorZip_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioFlavorZip.Checked)
            {
                // re-enable the encoding, and set it to what it was most recently
                this.comboBox1.Enabled = true;
                if (_mostRecentEncoding != null)
                {
                    this.SelectNamedEncoding(_mostRecentEncoding);
                    _mostRecentEncoding = null;
                }

                // intelligently change the name of the thing to create
                if (this.tbZipName.Text.ToUpper().EndsWith(".EXE"))
                {
                    tbZipName.Text = System.Text.RegularExpressions.Regex.Replace(tbZipName.Text, "(?i:)\\.exe$", ".zip");
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
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.tbPassword.Enabled = (this.comboBox3.SelectedItem.ToString() != "None"); 
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.tbPassword.PasswordChar = (this.checkBox1.Checked) ? '*' : '\0';
        }

        private void ResetState()
        {
            this.btnCancel.Enabled = false;
            this.btnOk.Enabled = true;
            this.btnOk.Text = "Zip it!";
            this.progressBar1.Value = 0;
            this.progressBar2.Value = 0;
            this.Cursor = Cursors.Default;
            if (!_workerThread.IsAlive)
                _workerThread.Join();
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
                if (s != null) this.tbDirName.Text = s;

                s = (string)AppCuKey.GetValue(_rvn_ZipTarget);
                if (s != null) this.tbZipName.Text = s;

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

                s = (string)AppCuKey.GetValue(_rvn_Encryption);
                if (s != null)
                {
                    SelectNamedEncryption(s);
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

                AppCuKey.Close();
                AppCuKey = null;
            }
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
            tbPassword.Text = "";
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



        private void SaveFormToRegistry()
        {
            if (AppCuKey != null)
            {
                AppCuKey.SetValue(_rvn_DirectoryToZip, this.tbDirName.Text);
                AppCuKey.SetValue(_rvn_ZipTarget, this.tbZipName.Text);
                AppCuKey.SetValue(_rvn_Encoding, this.comboBox1.SelectedItem.ToString());
                AppCuKey.SetValue(_rvn_Compression, this.comboBox2.SelectedItem.ToString());
                AppCuKey.SetValue(_rvn_Encryption, this.comboBox3.SelectedItem.ToString());

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

                AppCuKey.SetValue(_rvn_LastRun, System.DateTime.Now.ToString("yyyy MMM dd HH:mm:ss"));
                x = (Int32)AppCuKey.GetValue(_rvn_Runs, 0);
                x++;
                AppCuKey.SetValue(_rvn_Runs, x);

                AppCuKey.Close();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveFormToRegistry();
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

        private string _folderName;
        private int _progress2MaxFactor;
        private int _entriesToZip;
        private bool _saveCanceled;
        private int _nFilesCompleted;
        private long _totalBytesBeforeCompress;
        private long _totalBytesAfterCompress;
        private Thread _workerThread;
        private static string TB_COMMENT_NOTE = "-zip file comment here-";
        private List<String> _EncodingNames;
        private List<String> _CompressionLevelNames;
        private List<String> _EncryptionNames;
        private string _mostRecentEncoding;
        private Nullable<Zip64Option> _mostRecentZip64;

        private Microsoft.Win32.RegistryKey _appCuKey;
        private static string _AppRegyPath = "Software\\Dino Chiesa\\DotNetZip Winforms Tool";
        private static string _rvn_DirectoryToZip = "DirectoryToZip";
        private static string _rvn_ZipTarget = "ZipTarget";
        private static string _rvn_Encoding = "Encoding";
        private static string _rvn_Compression = "Compression";
        private static string _rvn_Encryption = "Encryption";
        private static string _rvn_ZipFlavor = "ZipFlavor";
        private static string _rvn_Zip64Option = "Zip64Option";
        private static string _rvn_LastRun = "LastRun";
        private static string _rvn_Runs = "Runs";

    }

    public class WorkerOptions
    {
        public string ZipName;
        public string Folder;
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
