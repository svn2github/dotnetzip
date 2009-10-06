// WinFormsSelfExtractorStub.cs
// ------------------------------------------------------------------
//
// Copyright (c)  2008, 2009 Dino Chiesa.  
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-October-06 12:31:28>
//
// ------------------------------------------------------------------
//
// Implements the "stub" of a WinForms self-extracting Zip archive. This
// code is included in all GUI SFX files.  It is included as a resource
// into the DotNetZip DLL, and then is compiled at runtime when a SFX is
// saved.  This code runs when the SFX is run.
//
// ------------------------------------------------------------------

namespace DotNetZip.Examples
{
    // The using statements must be inside the namespace scope, because when the SFX is being 
    // generated, this module gets concatenated with other source code and then compiled.

    using System;
    using System.Reflection;
    using System.IO;
    using System.Windows.Forms;
    using System.Diagnostics;
    using Ionic.Zip;
    
    public partial class WinFormsSelfExtractorStub : Form
    {
        //const string IdString = "DotNetZip Self Extractor, see http://www.codeplex.com/DotNetZip";
        const string DllResourceName = "Ionic.Zip.dll";

        int entryCount;

        delegate void ExtractEntryProgress(ExtractProgressEventArgs e);

        void _SetDefaultExtractLocation()
        {
            // Design Note:
            
            // What follows may look odd.  The textbox is set to a particular value.
            // Then the value is tested, and if the value begins with the first part
            // of the string and ends with the last part, and if it does, then we
            // change the value.  When would that not get replaced?
            //

            // Well, here's the thing.  This module has to compile as it is, as a
            // standalone sample.  But then, inside DotNetZip, when generating an SFX,
            // we do a text.Replace on @@EXTRACTLOCATION and insert a different value.

            // So the effect is, with a straight compile, the value gets
            // SpecialFolder.Personal.  If you replace @@EXTRACTLOCATION with
            // something else, it stays and does not get replaced.

            this.txtExtractDirectory.Text = "@@EXTRACTLOCATION";

            if (this.txtExtractDirectory.Text.StartsWith("@@") && 
                this.txtExtractDirectory.Text.EndsWith("EXTRACTLOCATION"))
            {
                this.txtExtractDirectory.Text = 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                           ZipName);
            }
        }


        
        void _SetPostUnpackCmdLine()
        {
            // See the design note in _SetDefaultExtractLocation() for
            // an explanation of what is going on here.
            
            this.txtPostUnpackCmdLine.Text = "@@POST_UNPACK_CMD_LINE";

            if (this.txtPostUnpackCmdLine.Text.StartsWith("@@") && 
                this.txtPostUnpackCmdLine.Text.EndsWith("POST_UNPACK_CMD_LINE"))
            {
                // If there is nothing set for the CMD to execute after unpack, then
                // disable all the UI associated to that bit.
                this.txtPostUnpackCmdLine.Enabled = false;
                this.txtPostUnpackCmdLine.Visible = false;
                this.chk_ExeAfterUnpack.Enabled = false;
                this.chk_ExeAfterUnpack.Visible = false;

                // adjust the position of all the remaining UI
                int delta = this.txtPostUnpackCmdLine.Height;

                this.MinimumSize = new System.Drawing.Size(this.MinimumSize.Width, this.MinimumSize.Height - (delta + 8));
                
                 MoveDown(this.chk_Overwrite, delta);
                 MoveDown(this.chk_OpenExplorer, delta);
                 MoveDown(this.btnDirBrowse, delta);
                 MoveDown(this.txtExtractDirectory, delta);
                 MoveDown(this.lblExtractDir, delta);
                 //MoveDown(this.txtComment, delta);
                 //MoveDown(this.lblComment, delta);
                          
//                 moveup(this.btnContents, delta);
//                 MoveUp(this.btnCancel, delta);
//                 MoveUp(this.btnExtract, delta);
                
                 // finally, adjust the size of the form
                this.Size = new System.Drawing.Size(this.Width, this.Height - (delta + 8));

                // Add the size to the txtComment, because it is anchored to the bottom.
                // When we shrink the size of the form, the txtComment shrinks also.
                // No need for that.
                this.txtComment.Size = new System.Drawing.Size(this.txtComment.Width,
                                                               this.txtComment.Height + delta);
            }
        }

        private void MoveDown (System.Windows.Forms.Control c, int delta)
        {
            c.Location = new System.Drawing.Point(c.Location.X, c.Location.Y + delta);
        }
        
        private void FixTitle()
        {
            this.Text = String.Format("DotNetZip v{0} Self-extractor (www.codeplex.com/DotNetZip)",
                                      Ionic.Zip.ZipFile.LibraryVersion.ToString());
        }

        public WinFormsSelfExtractorStub()
        {
            InitializeComponent();
            FixTitle();
            _setCancel = true;
            entryCount= 0;
            _SetDefaultExtractLocation();
            _SetPostUnpackCmdLine();
            
            try
            {
                if ((zip.Comment != null) && (zip.Comment != ""))
                {
                    txtComment.Text = zip.Comment;
                }
                else
                {
                    lblComment.Visible = false;
                    txtComment.Visible = false;
                    
                    this.MinimumSize = new System.Drawing.Size(this.MinimumSize.Width, this.MinimumSize.Height -
                                                               (this.txtComment.Height+this.lblComment.Height+5));
                    
                    this.MaximumSize = new System.Drawing.Size(this.MaximumSize.Width, this.MinimumSize.Height);
                    
                    this.Size = new System.Drawing.Size(this.Width, this.MinimumSize.Height);
                    
                    //this.lblStatus.Text = String.Format("size: ({0}, {1})", this.Width, this.Height);
                }
            }
            catch (Exception e1)
            {
                this.lblStatus.Text = "exception while resetting size: " + e1.ToString();
                
                // why would this ever fail?  Not sure. 
                lblComment.Visible = false;
                txtComment.Visible = false;
                this.MinimumSize = new System.Drawing.Size(this.MinimumSize.Width, this.MinimumSize.Height -
                                                           (this.txtComment.Height+this.lblComment.Height+5));
                
                this.Size = new System.Drawing.Size(this.Width, this.MinimumSize.Height);
            }
        }


        static WinFormsSelfExtractorStub()
        {
            // This is important to resolve the Ionic.Zip.dll inside the extractor. 
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolver);
        }


        #if ORIG
            static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
            {
                Assembly a1 = Assembly.GetExecutingAssembly();
                Stream s = a1.GetManifestResourceStream(DllResourceName);
                byte[] block = new byte[s.Length];
                s.Read(block, 0, block.Length);
                Assembly a2 = Assembly.Load(block);
                return a2;
            }
        #else
            
            static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
            {
                // super defensive
                Assembly a1 = Assembly.GetExecutingAssembly();
                if (a1==null)
                    throw new Exception("GetExecutingAssembly returns null.");

                string[] tokens = args.Name.Split(',');
            
                String[] names = a1.GetManifestResourceNames();
            
                if (names==null)
                    throw new Exception("GetManifestResourceNames returns null.");

                // workitem 7978
                Stream s = null;
                foreach (string n in names)
                {
                    string root = n.Substring(0,n.Length-4);
                    string ext = n.Substring(n.Length-3);
                    if (root.Equals(tokens[0])  && ext.ToLower().Equals("dll"))
                    {
                        s= a1.GetManifestResourceStream(n);
                        if (s!=null) break;
                    }
                }
            
                if (s==null)
                    throw new Exception(String.Format("GetManifestResourceStream returns null. Available resources: [{0}]",
                                                      String.Join("|", names)));

                byte[] block = new byte[s.Length];
            
                if (block==null)
                    throw new Exception(String.Format("Cannot allocated buffer of length({0}).", s.Length));

                s.Read(block, 0, block.Length);
                Assembly a2 = Assembly.Load(block);
                if (a2==null)
                    throw new Exception("Assembly.Load(block) returns null");
            
                return a2;
            }
            
        #endif


        private void btnDirBrowse_Click(object sender, EventArgs e)
        {
            
            Ionic.Utils.FolderBrowserDialogEx dlg1 = new Ionic.Utils.FolderBrowserDialogEx();
            dlg1.Description = "Select a folder for the extracted files:";
            dlg1.ShowNewFolderButton = true;
            dlg1.ShowEditBox = true;
            //dlg1.NewStyle = false;
            if (Directory.Exists(txtExtractDirectory.Text))
                dlg1.SelectedPath = txtExtractDirectory.Text;
            else
            {
                string d = txtExtractDirectory.Text;
                while (d.Length > 2 && !Directory.Exists(d))
                {
                    d = Path.GetDirectoryName(d);
                }
                if (d.Length < 2)
                    dlg1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                else
                    dlg1.SelectedPath = d;
            }
                
            dlg1.ShowFullPathInEditBox = true;
                
            //dlg1.RootFolder = System.Environment.SpecialFolder.MyComputer;

            // Show the FolderBrowserDialog.
            DialogResult result = dlg1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtExtractDirectory.Text = dlg1.SelectedPath;
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
            this.chk_ExeAfterUnpack.Enabled = false;
            this.txtExtractDirectory.Enabled = false;
            this.txtPostUnpackCmdLine.Enabled = false;
            this.btnDirBrowse.Enabled = false;
            this.btnExtract.Text = "Extracting...";
            System.Threading.Thread _workerThread = new System.Threading.Thread(this.DoExtract);
            _workerThread.Name = "Zip Extractor thread";
            _workerThread.Start(null);
            this.Cursor = Cursors.WaitCursor;
        }


        private void DoExtract(Object obj)
        {
            string targetDirectory = txtExtractDirectory.Text;
            global::Ionic.Zip.ExtractExistingFileAction WantOverwrite = chk_Overwrite.Checked
                ? global::Ionic.Zip.ExtractExistingFileAction.OverwriteSilently
                : global::Ionic.Zip.ExtractExistingFileAction.Throw;
            bool extractCancelled = false;
            System.Collections.Generic.List<String> didNotOverwrite =
                new System.Collections.Generic.List<String>();
            _setCancel = false;
            string currentPassword = "";
            SetProgressBars();

            try
            {
                // zip has already been set, when opening the exe.
                
                zip.ExtractProgress += ExtractProgress;
                foreach (global::Ionic.Zip.ZipEntry entry in zip)
                {
                    if (_setCancel) { extractCancelled = true; break; }
                    if (entry.Encryption == global::Ionic.Zip.EncryptionAlgorithm.None)
                    {
                        try
                        {
                            entry.Extract(targetDirectory, WantOverwrite);
                            entryCount++;
                        }
                        catch (Exception ex1)
                        {
                            if (WantOverwrite != global::Ionic.Zip.ExtractExistingFileAction.OverwriteSilently 
                                && ex1.Message.Contains("already exists."))
                            {
                                // The file exists, but the user did not ask for overwrite.
                                didNotOverwrite.Add("    " + entry.FileName);
                            }
                            else if (WantOverwrite == global::Ionic.Zip.ExtractExistingFileAction.OverwriteSilently ||
                                     ex1.Message.Contains("already exists."))
                            {
                                DialogResult result = MessageBox.Show(String.Format("Failed to extract entry {0} -- {1}", entry.FileName, ex1.Message),
                                                                      String.Format("Error Extracting {0}", entry.FileName), MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);

                                if (result == DialogResult.Cancel)
                                {
                                    _setCancel = true;
                                    break;
                                }
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
                                entry.ExtractWithPassword(targetDirectory, WantOverwrite, currentPassword);
                                entryCount++;
                                done= true;
                            }
                            catch (Exception ex2)
                            {
                                // Retry here in the case of bad password.
                                if (ex2 as Ionic.Zip.BadPasswordException != null)
                                {
                                    currentPassword = "";
                                    continue; // loop around, ask for password again
                                }
                                else if (WantOverwrite != global::Ionic.Zip.ExtractExistingFileAction.OverwriteSilently 
                                        && ex2.Message.Contains("already exists."))
                                {
                                    // The file exists, but the user did not ask for overwrite.
                                    didNotOverwrite.Add("    " + entry.FileName);
                                    done = true;
                                }
                                else if (WantOverwrite == global::Ionic.Zip.ExtractExistingFileAction.OverwriteSilently 
                                        && !ex2.Message.Contains("already exists."))
                                {
                                    DialogResult result = MessageBox.Show(String.Format("Failed to extract the password-encrypted entry {0} -- {1}", entry.FileName, ex2.Message.ToString()),
                                                                          String.Format("Error Extracting {0}", entry.FileName), MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                                    
                                    done= true;
                                    if (result == DialogResult.Cancel)
                                    {
                                        _setCancel = true;
                                        break;
                                    }
                                }
                            } // catch
                        } // while
                    } // else (encryption)
                } // foreach

            }
            catch (Exception)
            {
                MessageBox.Show("The self-extracting zip file is corrupted.",
                                "Error Extracting", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                Application.Exit();
            }

            if (didNotOverwrite.Count > 0)
            {
                UnzipStatusReport f = new UnzipStatusReport();
                if (didNotOverwrite.Count == 1)
                    f.Header = "This file was not extracted because the target file already exists:";
                else
                    f.Header = String.Format("These {0} files were not extracted because the target files already exist:",
                                             didNotOverwrite.Count);
                f.Message = String.Join("\r\n", didNotOverwrite.ToArray());
                f.ShowDialog();
            }

            SetUiDone();

            if (extractCancelled) return;

            
            // optionally open explorer
            if (chk_OpenExplorer.Checked)
            {
                string w = System.Environment.GetEnvironmentVariable("WINDIR");
                if (w == null) w = "c:\\windows";
                try
                {
                    Process.Start(Path.Combine(w, "explorer.exe"), targetDirectory);
                }
                catch { }
            }

            // optionally execute a command
            if (this.chk_ExeAfterUnpack.Visible && this.chk_ExeAfterUnpack.Checked)
            {
                try
                {
                    string[] args = this.txtPostUnpackCmdLine.Text.Split( new char[] {' '}, 2);
                    if (args.Length > 0)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo(args[0]);
                        startInfo.WorkingDirectory = targetDirectory;
                        if (args.Length > 1) startInfo.Arguments = args[1];

                        // Process is IDisposable
                        using (Process p = Process.Start(startInfo)) { }
                    }
                    // else, nothing.
                }
                catch {  }
            }
        }
        

        private void SetUiDone()
        {
            if (this.btnExtract.InvokeRequired)
            {
                this.btnExtract.Invoke(new MethodInvoker(this.SetUiDone));
            }
            else
            {
                this.lblStatus.Text = String.Format("Finished extracting {0} entries.", entryCount);
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

                int xferred = (int)(e.BytesTransferred >> _progress2MaxFactor);

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
                return Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }

        private Stream ZipStream
        {
            get
            {
                if (_s != null) return _s;
                Assembly a = Assembly.GetExecutingAssembly();

                // workitem 7067
                _s= File.OpenRead(a.Location);

                return _s;
            }
        }

        private ZipFile zip
        {
            get
            {
                if (_zip == null)
                    _zip = global::Ionic.Zip.ZipFile.Read(ZipStream);
                return _zip;
            }
        }

        private string PromptForPassword(string entryName)
        {
            PasswordDialog dlg1 = new PasswordDialog();
            dlg1.EntryName = entryName;

            // ask for password in a loop until user enters a proper one, 
            // or clicks skip or cancel.
            bool done= false;
            do {
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
        global::Ionic.Zip.ZipFile _zip;

    }
    

    
    public class UnzipStatusReport : Form
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.Button btnOK;

        public UnzipStatusReport()
        {
            InitializeComponent();
        }

        
        private void UnzipStatusReport_Load(object sender, EventArgs e)
        {
            this.Text = "DotNetZip: Unzip status report...";
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
        
        public string Message
        {
            set
            {
                this.tbMessage.Text = value;
                this.tbMessage.Select(0,0);
            }
            get
            {
                return this.tbMessage.Text;
            }
        }

        public string Header
        {
            set
            {
                this.label1.Text = value;   
            }
            get
            {
                return this.label1.Text;
            }
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Status";
            // 
            // tbMessage
            // 
            this.tbMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)
                        | System.Windows.Forms.AnchorStyles.Bottom)));
            this.tbMessage.Location = new System.Drawing.Point(20, 31);
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.Multiline = true;
            this.tbMessage.ScrollBars = ScrollBars.Vertical;
            this.tbMessage.ReadOnly = true;
            this.tbMessage.Size = new System.Drawing.Size(340, 110);
            this.tbMessage.TabIndex = 10;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(290, 156);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(82, 24);
            this.btnOK.TabIndex = 20;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // UnzipStatusReport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 190);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbMessage);
            this.Controls.Add(this.btnOK);
            this.Name = "UnzipStatusReport";
            this.Text = "Not Unzipped";
            this.Load += new System.EventHandler(this.UnzipStatusReport_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
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
