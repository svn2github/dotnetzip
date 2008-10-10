using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Ionic.Utils.Zip;

namespace WinFormsExample
{
    public partial class Form1 : Form
    {
        delegate void SaveCompleted(object sender, SaveEventArgs e);

        public Form1()
        {
            InitializeComponent();

            InitEncodingsList();

            FixTitle();

            FillFormFromRegistry();
        }

        private void FixTitle()
        {
            this.Text = String.Format("WinForms Example for DotNetZip v{0}",
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

            _saveCanceled = false;
            _nFilesCompleted = 0;
            this.btnOk.Enabled = false;
            this.btnOk.Text = "Zipping...";
            this.btnCancel.Enabled = true;
            lblStatus.Text = "Zipping...";

            string comment = "Created at " + System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") +
                "\r\n  by " + this.Text;

            if (this.tbComment.Text != TB_COMMENT_NOTE)
                comment += "\r\n" + this.tbComment.Text;

            string encodingName = "ibm437";
            if (this.comboBox1.SelectedIndex != 0)
            {
                encodingName = this.comboBox1.SelectedItem.ToString();
                comment += "\r\nEncoding used: " + encodingName;
            }
            _workerThread = new Thread(this.DoSave);
            _workerThread.Name = "Zip Saver thread";
            _workerThread.Start(new string[] { this.tbZipName.Text, _folderName, encodingName, comment });
            this.Cursor = Cursors.WaitCursor;

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

        private void SetProgressBar()
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new MethodInvoker(this.SetProgressBar));
            }
            else
            {
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = _entriesToZip;
                this.progressBar1.Minimum = 0;
                this.progressBar1.Step = 1;
            }
        }

        private void DoSave(object p)
        {
            string[] args = p as string[];

            using (var zip1 = new ZipFile(args[0]))
            {
                zip1.Encoding = System.Text.Encoding.GetEncoding(args[2]);
                zip1.Comment = args[3];
                zip1.AddDirectory(args[1]);
                _entriesToZip = zip1.EntryFileNames.Count;
                SetProgressBar();
                zip1.SaveProgress += this.zip1_SaveProgress;
                zip1.SaveCompleted += this.zip1_SaveCompleted;
                zip1.Save();
            }
        }



        void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            StepProgress();
            if (_saveCanceled)
                e.Cancel = true;
        }


        private void StepProgress()
        {
            if (this.progressBar1.InvokeRequired)
            {
                this.progressBar1.Invoke(new MethodInvoker(this.StepProgress));
            }
            else
            {
                if (!_saveCanceled)
                {
                    _nFilesCompleted++;
                    lblStatus.Text = String.Format("{0} of {1} files...", _nFilesCompleted, _entriesToZip);
                    this.progressBar1.PerformStep();

                    // Sleep here just to show the progress bar, when the number of files is small. 
                    // You probably don't want this for actual use!
                    if (_entriesToZip < 10)
                        Thread.Sleep(350);
                    else if (_entriesToZip < 20)
                        Thread.Sleep(200);
                    else if (_entriesToZip < 30)
                        Thread.Sleep(100);
                    else if (_entriesToZip < 45)
                        Thread.Sleep(80);
                    else if (_entriesToZip < 75)
                        Thread.Sleep(40);

                    this.Update();
                }
            }
        }



        private void button1_Click(object sender, EventArgs e)
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


        private void button2_Click(object sender, EventArgs e)
        {
            KickoffZipup();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _saveCanceled = true;
            lblStatus.Text = "Canceled...";
            ResetState();
        }

        private void ResetState()
        {
            this.btnCancel.Enabled = false;
            this.btnOk.Enabled = true;
            this.btnOk.Text = "Zip it!";
            this.progressBar1.Value = 0;
            this.Cursor = Cursors.Default;
            if (!_workerThread.IsAlive)
                _workerThread.Join();
        }

        void zip1_SaveCompleted(object sender, SaveEventArgs e)
        {
            if (this.btnCancel.InvokeRequired)
            {
                this.btnCancel.Invoke(new SaveCompleted(this.zip1_SaveCompleted), new object[] { sender, e });
            }
            else
            {
                lblStatus.Text += "...Done.";
                ResetState();
            }
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
                if (s!=null)
                {
                    for (int i = 0; i < this.comboBox1.Items.Count; i++)
                    {
                        if (this.comboBox1.Items[i].ToString() == s)
                        {
                            this.comboBox1.SelectedIndex = i;
                            break;
                        }
                    }
                }

                AppCuKey.Close();
                AppCuKey = null;
            }
        }


        private void SaveFormToRegistry()
        {
            if (AppCuKey != null)
            {
                AppCuKey.SetValue(_rvn_DirectoryToZip, this.tbDirName.Text);
                AppCuKey.SetValue(_rvn_ZipTarget, this.tbZipName.Text);
                AppCuKey.SetValue(_rvn_Encoding, this.comboBox1.SelectedItem.ToString());

                AppCuKey.SetValue(_rvn_LastRun, System.DateTime.Now.ToString("yyyy MMM dd HH:mm:ss"));
                int x = (Int32)AppCuKey.GetValue(_rvn_Runs, 0);
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
        private int _entriesToZip;
        private bool _saveCanceled;
        private int _nFilesCompleted;
        private Thread _workerThread;
        private static string TB_COMMENT_NOTE = "-zip file comment here-";
        private List<String> _EncodingNames;

        private Microsoft.Win32.RegistryKey _appCuKey;

        private static string _AppRegyPath = "Software\\Dino Chiesa\\DotNetZip Winforms Tool";
        private static string _rvn_DirectoryToZip = "DirectoryToZip";
        private static string _rvn_ZipTarget = "ZipTarget";
        private static string _rvn_Encoding = "Encoding";
        private static string _rvn_LastRun = "LastRun";
        private static string _rvn_Runs = "Runs";

    }
}