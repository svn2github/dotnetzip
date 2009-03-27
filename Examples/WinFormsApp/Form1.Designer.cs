namespace Ionic.Zip.WinFormsExample
{
    partial class Form1
    {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tbDirectoryToZip = new System.Windows.Forms.TextBox();
            this.tbZipToCreate = new System.Windows.Forms.TextBox();
            this.btnZipupDirBrowse = new System.Windows.Forms.Button();
            this.btnZipUp = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.comboEncoding = new System.Windows.Forms.ComboBox();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.comboCompression = new System.Windows.Forms.ComboBox();
            this.comboEncryption = new System.Windows.Forms.ComboBox();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.chkHidePassword = new System.Windows.Forms.CheckBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.btnOpenZip = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tbSelectionToExtract = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.chkOverwrite = new System.Windows.Forms.CheckBox();
            this.chkOpenExplorer = new System.Windows.Forms.CheckBox();
            this.btnExtractDirBrowse = new System.Windows.Forms.Button();
            this.tbExtractDir = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btnExtract = new System.Windows.Forms.Button();
            this.btnReadZipBrowse = new System.Windows.Forms.Button();
            this.tbZipToOpen = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboZip64 = new System.Windows.Forms.ComboBox();
            this.comboFlavor = new System.Windows.Forms.ComboBox();
            this.btnCreateZipBrowse = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbDirectoryInArchive = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.tbSelectionToZip = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.btnClearItemsToZip = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.listView2 = new ListViewEx.ListViewEx();
            this.chCheckbox = new System.Windows.Forms.ColumnHeader();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tbDirectoryToZip
            // 
            this.tbDirectoryToZip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDirectoryToZip.Location = new System.Drawing.Point(108, 13);
            this.tbDirectoryToZip.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbDirectoryToZip.Name = "tbDirectoryToZip";
            this.tbDirectoryToZip.Size = new System.Drawing.Size(488, 20);
            this.tbDirectoryToZip.TabIndex = 10;
            this.tbDirectoryToZip.Leave += new System.EventHandler(this.tbDirectoryToZip_Leave);
            // 
            // tbZipToCreate
            // 
            this.tbZipToCreate.AcceptsReturn = true;
            this.tbZipToCreate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbZipToCreate.Location = new System.Drawing.Point(108, 11);
            this.tbZipToCreate.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbZipToCreate.Name = "tbZipToCreate";
            this.tbZipToCreate.Size = new System.Drawing.Size(488, 20);
            this.tbZipToCreate.TabIndex = 20;
            // 
            // btnZipupDirBrowse
            // 
            this.btnZipupDirBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZipupDirBrowse.Location = new System.Drawing.Point(598, 13);
            this.btnZipupDirBrowse.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnZipupDirBrowse.Name = "btnZipupDirBrowse";
            this.btnZipupDirBrowse.Size = new System.Drawing.Size(24, 20);
            this.btnZipupDirBrowse.TabIndex = 11;
            this.btnZipupDirBrowse.Text = "...";
            this.toolTip1.SetToolTip(this.btnZipupDirBrowse, "Browse for a directory to search in");
            this.btnZipupDirBrowse.UseVisualStyleBackColor = true;
            this.btnZipupDirBrowse.Click += new System.EventHandler(this.btnDirBrowse_Click);
            // 
            // btnZipUp
            // 
            this.btnZipUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZipUp.Location = new System.Drawing.Point(568, 331);
            this.btnZipUp.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnZipUp.Name = "btnZipUp";
            this.btnZipUp.Size = new System.Drawing.Size(66, 26);
            this.btnZipUp.TabIndex = 80;
            this.btnZipUp.Text = "Zip All";
            this.btnZipUp.UseVisualStyleBackColor = true;
            this.btnZipUp.Click += new System.EventHandler(this.btnZipup_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(568, 391);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(66, 26);
            this.btnCancel.TabIndex = 90;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(6, 361);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(628, 10);
            this.progressBar1.TabIndex = 4;
            // 
            // progressBar2
            // 
            this.progressBar2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar2.Location = new System.Drawing.Point(6, 377);
            this.progressBar2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(628, 10);
            this.progressBar2.TabIndex = 17;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "directory to add: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 15);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "file to save to:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 39);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "flavor:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 91);
            this.label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "encoding:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 117);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "comment:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(300, 39);
            this.label6.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "ZIP64?:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 65);
            this.label7.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "compression:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(300, 64);
            this.label8.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 91;
            this.label8.Text = "encryption:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(300, 89);
            this.label9.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(55, 13);
            this.label9.TabIndex = 93;
            this.label9.Text = "password:";
            // 
            // comboEncoding
            // 
            this.comboEncoding.FormattingEnabled = true;
            this.comboEncoding.Location = new System.Drawing.Point(108, 85);
            this.comboEncoding.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.comboEncoding.Name = "comboEncoding";
            this.comboEncoding.Size = new System.Drawing.Size(178, 21);
            this.comboEncoding.TabIndex = 50;
            // 
            // tbComment
            // 
            this.tbComment.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbComment.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbComment.Location = new System.Drawing.Point(108, 110);
            this.tbComment.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbComment.Name = "tbComment";
            this.tbComment.Size = new System.Drawing.Size(488, 20);
            this.tbComment.TabIndex = 70;
            this.tbComment.Text = "-zip file comment here-";
            this.tbComment.Leave += new System.EventHandler(this.tbComment_Leave);
            this.tbComment.Enter += new System.EventHandler(this.tbComment_Enter);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 398);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 8;
            // 
            // comboCompression
            // 
            this.comboCompression.FormattingEnabled = true;
            this.comboCompression.Location = new System.Drawing.Point(108, 60);
            this.comboCompression.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.comboCompression.Name = "comboCompression";
            this.comboCompression.Size = new System.Drawing.Size(178, 21);
            this.comboCompression.TabIndex = 40;
            // 
            // comboEncryption
            // 
            this.comboEncryption.FormattingEnabled = true;
            this.comboEncryption.Location = new System.Drawing.Point(366, 60);
            this.comboEncryption.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.comboEncryption.Name = "comboEncryption";
            this.comboEncryption.Size = new System.Drawing.Size(172, 21);
            this.comboEncryption.TabIndex = 55;
            this.comboEncryption.SelectedIndexChanged += new System.EventHandler(this.comboBox3_SelectedIndexChanged);
            // 
            // tbPassword
            // 
            this.tbPassword.AcceptsReturn = true;
            this.tbPassword.Location = new System.Drawing.Point(366, 85);
            this.tbPassword.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.PasswordChar = '*';
            this.tbPassword.Size = new System.Drawing.Size(124, 20);
            this.tbPassword.TabIndex = 58;
            this.tbPassword.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\U.zip";
            this.tbPassword.TextChanged += new System.EventHandler(this.tbPassword_TextChanged);
            // 
            // chkHidePassword
            // 
            this.chkHidePassword.AutoSize = true;
            this.chkHidePassword.Checked = true;
            this.chkHidePassword.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkHidePassword.Location = new System.Drawing.Point(498, 87);
            this.chkHidePassword.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkHidePassword.Name = "chkHidePassword";
            this.chkHidePassword.Size = new System.Drawing.Size(46, 17);
            this.chkHidePassword.TabIndex = 59;
            this.chkHidePassword.Text = "hide";
            this.chkHidePassword.UseVisualStyleBackColor = true;
            this.chkHidePassword.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // listView1
            // 
            this.listView1.AllowDrop = true;
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Location = new System.Drawing.Point(6, 91);
            this.listView1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(628, 238);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.listView1_DragDrop);
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView1_ColumnClick);
            this.listView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.listView_DragEnter);
            // 
            // btnOpenZip
            // 
            this.btnOpenZip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenZip.Location = new System.Drawing.Point(574, 2);
            this.btnOpenZip.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnOpenZip.Name = "btnOpenZip";
            this.btnOpenZip.Size = new System.Drawing.Size(60, 26);
            this.btnOpenZip.TabIndex = 14;
            this.btnOpenZip.Text = "Open";
            this.btnOpenZip.UseVisualStyleBackColor = true;
            this.btnOpenZip.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.AllowDrop = true;
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(648, 448);
            this.tabControl1.TabIndex = 96;
            this.tabControl1.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl1_Selecting);
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tbSelectionToExtract);
            this.tabPage1.Controls.Add(this.label13);
            this.tabPage1.Controls.Add(this.chkOverwrite);
            this.tabPage1.Controls.Add(this.chkOpenExplorer);
            this.tabPage1.Controls.Add(this.btnExtractDirBrowse);
            this.tabPage1.Controls.Add(this.tbExtractDir);
            this.tabPage1.Controls.Add(this.label11);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.btnExtract);
            this.tabPage1.Controls.Add(this.btnReadZipBrowse);
            this.tabPage1.Controls.Add(this.tbZipToOpen);
            this.tabPage1.Controls.Add(this.listView1);
            this.tabPage1.Controls.Add(this.btnOpenZip);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage1.Size = new System.Drawing.Size(640, 422);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Read/Extract";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tbSelectionToExtract
            // 
            this.tbSelectionToExtract.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSelectionToExtract.Location = new System.Drawing.Point(60, 58);
            this.tbSelectionToExtract.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbSelectionToExtract.Name = "tbSelectionToExtract";
            this.tbSelectionToExtract.Size = new System.Drawing.Size(232, 20);
            this.tbSelectionToExtract.TabIndex = 28;
            this.tbSelectionToExtract.Text = "*.*";
            this.toolTip1.SetToolTip(this.tbSelectionToExtract, "Selection criteria.  eg, (name = *.* and size> 1000) etc.  Also use atime/mtime/c" +
                    "time and attributes. (HRSA)");
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 65);
            this.label13.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(52, 13);
            this.label13.TabIndex = 27;
            this.label13.Text = "selection:";
            // 
            // chkOverwrite
            // 
            this.chkOverwrite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkOverwrite.AutoSize = true;
            this.chkOverwrite.Location = new System.Drawing.Point(315, 58);
            this.chkOverwrite.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkOverwrite.Name = "chkOverwrite";
            this.chkOverwrite.Size = new System.Drawing.Size(69, 17);
            this.chkOverwrite.TabIndex = 26;
            this.chkOverwrite.Text = "overwrite";
            this.chkOverwrite.UseVisualStyleBackColor = true;
            // 
            // chkOpenExplorer
            // 
            this.chkOpenExplorer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkOpenExplorer.AutoSize = true;
            this.chkOpenExplorer.Location = new System.Drawing.Point(389, 58);
            this.chkOpenExplorer.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.chkOpenExplorer.Name = "chkOpenExplorer";
            this.chkOpenExplorer.Size = new System.Drawing.Size(91, 17);
            this.chkOpenExplorer.TabIndex = 25;
            this.chkOpenExplorer.Text = "open Explorer";
            this.chkOpenExplorer.UseVisualStyleBackColor = true;
            // 
            // btnExtractDirBrowse
            // 
            this.btnExtractDirBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExtractDirBrowse.Location = new System.Drawing.Point(544, 28);
            this.btnExtractDirBrowse.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnExtractDirBrowse.Name = "btnExtractDirBrowse";
            this.btnExtractDirBrowse.Size = new System.Drawing.Size(24, 26);
            this.btnExtractDirBrowse.TabIndex = 23;
            this.btnExtractDirBrowse.Text = "...";
            this.btnExtractDirBrowse.UseVisualStyleBackColor = true;
            this.btnExtractDirBrowse.Click += new System.EventHandler(this.btnExtractDirBrowse_Click);
            // 
            // tbExtractDir
            // 
            this.tbExtractDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbExtractDir.Location = new System.Drawing.Point(60, 32);
            this.tbExtractDir.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbExtractDir.Name = "tbExtractDir";
            this.tbExtractDir.Size = new System.Drawing.Size(478, 20);
            this.tbExtractDir.TabIndex = 22;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 39);
            this.label11.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(54, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "extract to:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 13);
            this.label10.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 13);
            this.label10.TabIndex = 15;
            this.label10.Text = "archive:";
            // 
            // btnExtract
            // 
            this.btnExtract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExtract.Enabled = false;
            this.btnExtract.Location = new System.Drawing.Point(574, 28);
            this.btnExtract.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(60, 26);
            this.btnExtract.TabIndex = 24;
            this.btnExtract.Text = "Extract";
            this.btnExtract.UseVisualStyleBackColor = true;
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);
            // 
            // btnReadZipBrowse
            // 
            this.btnReadZipBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadZipBrowse.Location = new System.Drawing.Point(544, 2);
            this.btnReadZipBrowse.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnReadZipBrowse.Name = "btnReadZipBrowse";
            this.btnReadZipBrowse.Size = new System.Drawing.Size(24, 26);
            this.btnReadZipBrowse.TabIndex = 13;
            this.btnReadZipBrowse.Text = "...";
            this.btnReadZipBrowse.UseVisualStyleBackColor = true;
            this.btnReadZipBrowse.Click += new System.EventHandler(this.btnZipBrowse_Click);
            // 
            // tbZipToOpen
            // 
            this.tbZipToOpen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbZipToOpen.Location = new System.Drawing.Point(60, 6);
            this.tbZipToOpen.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbZipToOpen.Name = "tbZipToOpen";
            this.tbZipToOpen.Size = new System.Drawing.Size(478, 20);
            this.tbZipToOpen.TabIndex = 12;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.checkBox1);
            this.tabPage2.Controls.Add(this.btnClearItemsToZip);
            this.tabPage2.Controls.Add(this.textBox1);
            this.tabPage2.Controls.Add(this.listView2);
            this.tabPage2.Controls.Add(this.progressBar2);
            this.tabPage2.Controls.Add(this.lblStatus);
            this.tabPage2.Controls.Add(this.btnCancel);
            this.tabPage2.Controls.Add(this.progressBar1);
            this.tabPage2.Controls.Add(this.btnZipUp);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage2.Size = new System.Drawing.Size(640, 422);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Create";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.tbZipToCreate);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.comboEncoding);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.tbComment);
            this.groupBox2.Controls.Add(this.comboZip64);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.comboFlavor);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.btnCreateZipBrowse);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.chkHidePassword);
            this.groupBox2.Controls.Add(this.comboCompression);
            this.groupBox2.Controls.Add(this.tbPassword);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.comboEncryption);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(6, 84);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.groupBox2.Size = new System.Drawing.Size(628, 136);
            this.groupBox2.TabIndex = 104;
            this.groupBox2.TabStop = false;
            // 
            // comboZip64
            // 
            this.comboZip64.FormattingEnabled = true;
            this.comboZip64.Location = new System.Drawing.Point(366, 35);
            this.comboZip64.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.comboZip64.Name = "comboZip64";
            this.comboZip64.Size = new System.Drawing.Size(172, 21);
            this.comboZip64.TabIndex = 97;
            // 
            // comboFlavor
            // 
            this.comboFlavor.FormattingEnabled = true;
            this.comboFlavor.Location = new System.Drawing.Point(108, 35);
            this.comboFlavor.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.comboFlavor.Name = "comboFlavor";
            this.comboFlavor.Size = new System.Drawing.Size(178, 21);
            this.comboFlavor.TabIndex = 96;
            this.comboFlavor.SelectedIndexChanged += new System.EventHandler(this.comboFlavor_SelectedIndexChanged);
            // 
            // btnCreateZipBrowse
            // 
            this.btnCreateZipBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreateZipBrowse.Location = new System.Drawing.Point(598, 10);
            this.btnCreateZipBrowse.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnCreateZipBrowse.Name = "btnCreateZipBrowse";
            this.btnCreateZipBrowse.Size = new System.Drawing.Size(24, 20);
            this.btnCreateZipBrowse.TabIndex = 21;
            this.btnCreateZipBrowse.Text = "...";
            this.toolTip1.SetToolTip(this.btnCreateZipBrowse, "browse for a file to save to");
            this.btnCreateZipBrowse.UseVisualStyleBackColor = true;
            this.btnCreateZipBrowse.Click += new System.EventHandler(this.btnCreateZipBrowse_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.tbDirectoryToZip);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.btnZipupDirBrowse);
            this.groupBox1.Controls.Add(this.tbDirectoryInArchive);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.tbSelectionToZip);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Location = new System.Drawing.Point(6, -2);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.groupBox1.Size = new System.Drawing.Size(628, 87);
            this.groupBox1.TabIndex = 103;
            this.groupBox1.TabStop = false;
            // 
            // tbDirectoryInArchive
            // 
            this.tbDirectoryInArchive.AcceptsReturn = true;
            this.tbDirectoryInArchive.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDirectoryInArchive.Location = new System.Drawing.Point(108, 38);
            this.tbDirectoryInArchive.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbDirectoryInArchive.Name = "tbDirectoryInArchive";
            this.tbDirectoryInArchive.Size = new System.Drawing.Size(488, 20);
            this.tbDirectoryInArchive.TabIndex = 14;
            this.toolTip1.SetToolTip(this.tbDirectoryInArchive, "Selection criteria.  eg, (name = *.* and size> 1000) etc.  Also use atime/mtime/c" +
                    "time and attributes. (HRSA)");
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(598, 63);
            this.button1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(24, 20);
            this.button1.TabIndex = 99;
            this.button1.Text = "+";
            this.toolTip1.SetToolTip(this.button1, "Add Selected files to Zip");
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 42);
            this.label14.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(99, 13);
            this.label14.TabIndex = 16;
            this.label14.Text = "directory in archive:";
            // 
            // tbSelectionToZip
            // 
            this.tbSelectionToZip.AcceptsReturn = true;
            this.tbSelectionToZip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSelectionToZip.Location = new System.Drawing.Point(108, 63);
            this.tbSelectionToZip.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tbSelectionToZip.Name = "tbSelectionToZip";
            this.tbSelectionToZip.Size = new System.Drawing.Size(488, 20);
            this.tbSelectionToZip.TabIndex = 12;
            this.tbSelectionToZip.Text = "*.*";
            this.toolTip1.SetToolTip(this.tbSelectionToZip, "Selection criteria.  eg, (name = *.* and size> 1000) etc.  Also use atime/mtime/c" +
                    "time and attributes. (HRSA)");
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 67);
            this.label12.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(52, 13);
            this.label12.TabIndex = 95;
            this.label12.Text = "selection:";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(12, 228);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(15, 14);
            this.checkBox1.TabIndex = 102;
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged_1);
            // 
            // btnClearItemsToZip
            // 
            this.btnClearItemsToZip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearItemsToZip.Location = new System.Drawing.Point(454, 331);
            this.btnClearItemsToZip.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnClearItemsToZip.Name = "btnClearItemsToZip";
            this.btnClearItemsToZip.Size = new System.Drawing.Size(102, 26);
            this.btnClearItemsToZip.TabIndex = 101;
            this.btnClearItemsToZip.Text = "Remove Checked";
            this.btnClearItemsToZip.UseVisualStyleBackColor = true;
            this.btnClearItemsToZip.Click += new System.EventHandler(this.btnClearItemsToZip_Click);
            // 
            // textBox1
            // 
            this.textBox1.AcceptsReturn = true;
            this.textBox1.Location = new System.Drawing.Point(192, 333);
            this.textBox1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 100;
            this.textBox1.Visible = false;
            this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox1_KeyPress);
            // 
            // listView2
            // 
            this.listView2.AllowColumnReorder = true;
            this.listView2.AllowDrop = true;
            this.listView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView2.CheckBoxes = true;
            this.listView2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chCheckbox,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listView2.DoubleClickActivation = false;
            this.listView2.FullRowSelect = true;
            this.listView2.GridLines = true;
            this.listView2.Location = new System.Drawing.Point(6, 225);
            this.listView2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.listView2.MultiSelect = false;
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(628, 104);
            this.listView2.TabIndex = 98;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.View = System.Windows.Forms.View.Details;
            this.listView2.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listView2_ItemChecked);
            this.listView2.DragDrop += new System.Windows.Forms.DragEventHandler(this.listView2_DragDrop);
            this.listView2.DragEnter += new System.Windows.Forms.DragEventHandler(this.listView_DragEnter);
            this.listView2.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.listView2_BeforeLabelEdit);
            // 
            // chCheckbox
            // 
            this.chCheckbox.Text = "?";
            this.chCheckbox.Width = 24;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File Name";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Directory In Archive";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "File name in Archive";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBox1);
            this.tabPage3.Controls.Add(this.pictureBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.tabPage3.Size = new System.Drawing.Size(640, 422);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "About";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(54, 20);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(508, 312);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBox1_LinkClicked);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::WinFormsExample.Properties.Resources.zippedFile;
            this.pictureBox1.Location = new System.Drawing.Point(6, 20);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(42, 52);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(648, 448);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.MinimumSize = new System.Drawing.Size(556, 458);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "DotNetZip\'s WinForms Zip Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnZipupDirBrowse;
        private System.Windows.Forms.Button btnZipUp;
        private System.Windows.Forms.Button btnOpenZip;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox tbDirectoryToZip;
        private System.Windows.Forms.TextBox tbZipToCreate;
        private System.Windows.Forms.TextBox tbComment;
        private System.Windows.Forms.ComboBox comboEncoding;
        private System.Windows.Forms.ComboBox comboCompression;
        private System.Windows.Forms.ComboBox comboEncryption;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.CheckBox chkHidePassword;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnReadZipBrowse;
        private System.Windows.Forms.TextBox tbZipToOpen;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.Button btnCreateZipBrowse;
        private System.Windows.Forms.Button btnExtractDirBrowse;
        private System.Windows.Forms.TextBox tbExtractDir;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.CheckBox chkOverwrite;
        private System.Windows.Forms.CheckBox chkOpenExplorer;
        private System.Windows.Forms.TextBox tbSelectionToZip;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbSelectionToExtract;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox tbDirectoryInArchive;
        private System.Windows.Forms.Label label14;
        //private System.Windows.Forms.ListViewEx listView2;
        //this.listView2 = new System.Windows.Forms.ListView();
        private ListViewEx.ListViewEx listView2;
        private System.Windows.Forms.ComboBox comboZip64;
        private System.Windows.Forms.ComboBox comboFlavor;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnClearItemsToZip;
        private System.Windows.Forms.ColumnHeader chCheckbox;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

