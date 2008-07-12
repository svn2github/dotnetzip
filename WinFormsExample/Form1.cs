using System;
using System.Threading;
using System.Windows.Forms;
using Ionic.Utils.Zip;

namespace WinFormsExample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void KickoffZipup()
        {
            folderName = textBox1.Text;
            if (folderName != null && folderName != "")
            {
                saveCanceled = false;
                nFilesCompleted = 0;
                this.button2.Enabled = false;
                this.button3.Enabled = true;

                workerThread = new Thread(this.DoSave);
                workerThread.Name = "Zip Saver thread";
                workerThread.Start(new string[] { this.textBox2.Text, folderName });
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
                this.progressBar1.Maximum = entriesToZip;
                this.progressBar1.Minimum = 0;
                this.progressBar1.Step = 1;
            }
        }

        private void DoSave(object p)
        {
            string[] args = p as string[];
            using (var zip1 = new ZipFile(args[0]))
            {
                zip1.AddDirectory(args[1]);
                entriesToZip = zip1.EntryFilenames.Count;
                SetProgressBar();
                zip1.SaveProgress += new SaveProgressEventHandler(this.zip1_SaveProgress);
                zip1.Save();
            }
        }



        void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            StepProgress();
            if (saveCanceled)
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
                if (!saveCanceled)
                {
                    nFilesCompleted++;
                    label3.Text = String.Format("{0} of {1} files...", nFilesCompleted, entriesToZip);
                    this.progressBar1.PerformStep();
                    this.Update();
                }
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {

            folderName = textBox1.Text;
            // Configure open file dialog box
            var dlg1 = new System.Windows.Forms.FolderBrowserDialog();
            //dlg1.RootFolder = "c:\\";
            dlg1.SelectedPath = (System.IO.Directory.Exists(folderName)) ? folderName : "c:\\";
            dlg1.ShowNewFolderButton = false;

            var result = dlg1.ShowDialog();

            if (result == DialogResult.OK)
            {
                folderName = dlg1.SelectedPath;
                textBox1.Text = folderName;
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            KickoffZipup();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveCanceled = true;
            label3.Text = "Canceled...";
            this.button3.Enabled = false;
            this.button2.Enabled = true;
            this.progressBar1.Value = 0;
            if (!workerThread.IsAlive)
                workerThread.Join();
        }


        private string folderName;
        private int entriesToZip;
        private bool saveCanceled;
        private int nFilesCompleted;
        private Thread workerThread;

    }
}