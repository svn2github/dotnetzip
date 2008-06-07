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
            textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
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
            folderBrowserDialog1.SelectedPath = textBox1.Text;
            folderBrowserDialog1.ShowNewFolderButton = true;

            folderBrowserDialog1.Description = "Select the directory for the extracted files.";

            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }


        private void btnExtract_Click(object sender, EventArgs e)
        {
            string targetDirectory = textBox1.Text;
            bool WantOverwrite = checkBox1.Checked;

            // There are only two embedded resources.
            // One of them is the zip dll.  The other is the zip archive.
            // We load the resouce that is NOT the DLL, as the zip archive.
            Assembly a = Assembly.GetExecutingAssembly();
            string[] x = a.GetManifestResourceNames();
            Stream s = null;
            foreach (string name in x)
            {
                if ((name != DllResourceName) && (name.EndsWith(".zip")))
                {
                    s = a.GetManifestResourceStream(name);
                    break;
                }
            }

            string currentPassword = null;
            if (s == null)
            {
                MessageBox.Show("No Zip archive found.",
                       "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                Application.Exit();
            }
            
                try
                {
                    using (global::Ionic.Utils.Zip.ZipFile zip = global::Ionic.Utils.Zip.ZipFile.Read(s))
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
                                    MessageBox.Show(String.Format("Failed to extract entry {0} -- {1}", entry.FileName, ex1.ToString()),
                                        "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                                }
                            else
                            {
                                while ((currentPassword == null) || (currentPassword == ""))
                                {
                                    currentPassword = PromptForPassword(entry.FileName);
                                }

                                try
                                {
                                    entry.ExtractWithPassword(currentPassword, WantOverwrite, targetDirectory);
                                }
                                catch (Exception ex2)
                                {
                                    // probably want a retry here in the case of bad password.
                                    MessageBox.Show(String.Format("Failed to extract entry {0} -- {1}", entry.FileName, ex2.ToString()),
                                        "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
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

                btnExtract.Enabled = false;
                if (checkBox2.Checked)
                {
                    string w = System.Environment.GetEnvironmentVariable("WINDIR");
                    if (w == null) w = "c:\\windows";
                    try
                    {
                        System.Diagnostics.Process.Start(Path.Combine(w, "explorer.exe"), targetDirectory);
                    }
                    catch { }
                    Application.Exit();
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
