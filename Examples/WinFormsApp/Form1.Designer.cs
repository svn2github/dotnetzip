namespace WinFormsExample
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tbDirName = new System.Windows.Forms.TextBox();
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.radioFlavorZip = new System.Windows.Forms.RadioButton();
            this.radioFlavorSfxGui = new System.Windows.Forms.RadioButton();
            this.radioFlavorSfxCmd = new System.Windows.Forms.RadioButton();
            this.lblStatus = new System.Windows.Forms.Label();
            this.radioZip64Never = new System.Windows.Forms.RadioButton();
            this.radioZip64Always = new System.Windows.Forms.RadioButton();
            this.radioZip64AsNecessary = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.chkHidePassword = new System.Windows.Forms.CheckBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.btnOpenZip = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnExtractDirBrowse = new System.Windows.Forms.Button();
            this.tbExtractDir = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btnExtract = new System.Windows.Forms.Button();
            this.btnReadZipBrowse = new System.Windows.Forms.Button();
            this.tbZipToOpen = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnCreateZipBrowse = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tbDirName
            // 
            this.tbDirName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDirName.Location = new System.Drawing.Point(91, 23);
            this.tbDirName.Name = "tbDirName";
            this.tbDirName.Size = new System.Drawing.Size(448, 20);
            this.tbDirName.TabIndex = 10;
            this.tbDirName.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\UnicodeTestCases\\A\\den f¢rste hjemmeside i rækken [" +
                "DK]";
            // 
            // tbZipToCreate
            // 
            this.tbZipToCreate.AcceptsReturn = true;
            this.tbZipToCreate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbZipToCreate.Location = new System.Drawing.Point(90, 51);
            this.tbZipToCreate.Name = "tbZipToCreate";
            this.tbZipToCreate.Size = new System.Drawing.Size(449, 20);
            this.tbZipToCreate.TabIndex = 20;
            this.tbZipToCreate.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\U.zip";
            // 
            // btnZipupDirBrowse
            // 
            this.btnZipupDirBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZipupDirBrowse.Location = new System.Drawing.Point(545, 23);
            this.btnZipupDirBrowse.Name = "btnZipupDirBrowse";
            this.btnZipupDirBrowse.Size = new System.Drawing.Size(24, 20);
            this.btnZipupDirBrowse.TabIndex = 11;
            this.btnZipupDirBrowse.Text = "...";
            this.btnZipupDirBrowse.UseVisualStyleBackColor = true;
            this.btnZipupDirBrowse.Click += new System.EventHandler(this.btnDirBrowse_Click);
            // 
            // btnZipUp
            // 
            this.btnZipUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZipUp.Location = new System.Drawing.Point(468, 248);
            this.btnZipUp.Name = "btnZipUp";
            this.btnZipUp.Size = new System.Drawing.Size(101, 29);
            this.btnZipUp.TabIndex = 80;
            this.btnZipUp.Text = "Zip It!";
            this.btnZipUp.UseVisualStyleBackColor = true;
            this.btnZipUp.Click += new System.EventHandler(this.btnZipup_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(468, 323);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(101, 29);
            this.btnCancel.TabIndex = 90;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 283);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(557, 14);
            this.progressBar1.TabIndex = 4;
            // 
            // progressBar2
            // 
            this.progressBar2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar2.Location = new System.Drawing.Point(12, 301);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(557, 14);
            this.progressBar2.TabIndex = 17;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "directory to zip: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "file to save to:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "flavor:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 146);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "encoding:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 204);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "comment:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 176);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "ZIP64?:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 116);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "compression:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(282, 116);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 91;
            this.label8.Text = "encryption:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(282, 146);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(55, 13);
            this.label9.TabIndex = 93;
            this.label9.Text = "password:";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(90, 139);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(175, 21);
            this.comboBox1.TabIndex = 50;
            // 
            // tbComment
            // 
            this.tbComment.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbComment.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbComment.Location = new System.Drawing.Point(90, 204);
            this.tbComment.Multiline = true;
            this.tbComment.Name = "tbComment";
            this.tbComment.Size = new System.Drawing.Size(479, 38);
            this.tbComment.TabIndex = 70;
            this.tbComment.Text = "-zip file comment here-";
            this.tbComment.Leave += new System.EventHandler(this.tbComment_Leave);
            this.tbComment.Enter += new System.EventHandler(this.tbComment_Enter);
            // 
            // radioFlavorZip
            // 
            this.radioFlavorZip.AutoSize = true;
            this.radioFlavorZip.Location = new System.Drawing.Point(13, 8);
            this.radioFlavorZip.Name = "radioFlavorZip";
            this.radioFlavorZip.Size = new System.Drawing.Size(86, 17);
            this.radioFlavorZip.TabIndex = 31;
            this.radioFlavorZip.Text = "traditional zip";
            this.radioFlavorZip.UseVisualStyleBackColor = true;
            this.radioFlavorZip.CheckedChanged += new System.EventHandler(this.radioFlavorZip_CheckedChanged);
            // 
            // radioFlavorSfxGui
            // 
            this.radioFlavorSfxGui.AutoSize = true;
            this.radioFlavorSfxGui.Location = new System.Drawing.Point(112, 8);
            this.radioFlavorSfxGui.Name = "radioFlavorSfxGui";
            this.radioFlavorSfxGui.Size = new System.Drawing.Size(113, 17);
            this.radioFlavorSfxGui.TabIndex = 32;
            this.radioFlavorSfxGui.Text = "self-extractor (GUI)";
            this.radioFlavorSfxGui.UseVisualStyleBackColor = true;
            this.radioFlavorSfxGui.CheckedChanged += new System.EventHandler(this.radioFlavorSfx_CheckedChanged);
            // 
            // radioFlavorSfxCmd
            // 
            this.radioFlavorSfxCmd.AutoSize = true;
            this.radioFlavorSfxCmd.Location = new System.Drawing.Point(238, 8);
            this.radioFlavorSfxCmd.Name = "radioFlavorSfxCmd";
            this.radioFlavorSfxCmd.Size = new System.Drawing.Size(118, 17);
            this.radioFlavorSfxCmd.TabIndex = 33;
            this.radioFlavorSfxCmd.Text = "self-extractor (CMD)";
            this.radioFlavorSfxCmd.UseVisualStyleBackColor = true;
            this.radioFlavorSfxCmd.CheckedChanged += new System.EventHandler(this.radioFlavorSfx_CheckedChanged);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(15, 331);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 8;
            // 
            // radioZip64Never
            // 
            this.radioZip64Never.AutoSize = true;
            this.radioZip64Never.Location = new System.Drawing.Point(14, 8);
            this.radioZip64Never.Name = "radioZip64Never";
            this.radioZip64Never.Size = new System.Drawing.Size(54, 17);
            this.radioZip64Never.TabIndex = 61;
            this.radioZip64Never.Text = "Never";
            this.radioZip64Never.UseVisualStyleBackColor = true;
            // 
            // radioZip64Always
            // 
            this.radioZip64Always.AutoSize = true;
            this.radioZip64Always.Location = new System.Drawing.Point(85, 8);
            this.radioZip64Always.Name = "radioZip64Always";
            this.radioZip64Always.Size = new System.Drawing.Size(58, 17);
            this.radioZip64Always.TabIndex = 62;
            this.radioZip64Always.Text = "Always";
            this.radioZip64Always.UseVisualStyleBackColor = true;
            // 
            // radioZip64AsNecessary
            // 
            this.radioZip64AsNecessary.AutoSize = true;
            this.radioZip64AsNecessary.Location = new System.Drawing.Point(160, 8);
            this.radioZip64AsNecessary.Name = "radioZip64AsNecessary";
            this.radioZip64AsNecessary.Size = new System.Drawing.Size(90, 17);
            this.radioZip64AsNecessary.TabIndex = 63;
            this.radioZip64AsNecessary.Text = "As Necessary";
            this.radioZip64AsNecessary.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioFlavorSfxGui);
            this.groupBox1.Controls.Add(this.radioFlavorZip);
            this.groupBox1.Controls.Add(this.radioFlavorSfxCmd);
            this.groupBox1.Location = new System.Drawing.Point(90, 74);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(370, 28);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioZip64Never);
            this.groupBox2.Controls.Add(this.radioZip64Always);
            this.groupBox2.Controls.Add(this.radioZip64AsNecessary);
            this.groupBox2.Location = new System.Drawing.Point(90, 164);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(294, 28);
            this.groupBox2.TabIndex = 60;
            this.groupBox2.TabStop = false;
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(90, 109);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(175, 21);
            this.comboBox2.TabIndex = 40;
            // 
            // comboBox3
            // 
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(347, 109);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(124, 21);
            this.comboBox3.TabIndex = 55;
            this.comboBox3.SelectedIndexChanged += new System.EventHandler(this.comboBox3_SelectedIndexChanged);
            // 
            // tbPassword
            // 
            this.tbPassword.AcceptsReturn = true;
            this.tbPassword.Location = new System.Drawing.Point(347, 142);
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
            this.chkHidePassword.Location = new System.Drawing.Point(480, 146);
            this.chkHidePassword.Name = "chkHidePassword";
            this.chkHidePassword.Size = new System.Drawing.Size(46, 17);
            this.chkHidePassword.TabIndex = 59;
            this.chkHidePassword.Text = "hide";
            this.chkHidePassword.UseVisualStyleBackColor = true;
            this.chkHidePassword.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Location = new System.Drawing.Point(6, 64);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(563, 210);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView1_ColumnClick);
            // 
            // btnOpenZip
            // 
            this.btnOpenZip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenZip.Location = new System.Drawing.Point(510, 4);
            this.btnOpenZip.Name = "btnOpenZip";
            this.btnOpenZip.Size = new System.Drawing.Size(59, 23);
            this.btnOpenZip.TabIndex = 14;
            this.btnOpenZip.Text = "Open";
            this.btnOpenZip.UseVisualStyleBackColor = true;
            this.btnOpenZip.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(585, 386);
            this.tabControl1.TabIndex = 96;
            this.tabControl1.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl1_Selecting);
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
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
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(577, 360);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Read/Extract";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnExtractDirBrowse
            // 
            this.btnExtractDirBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExtractDirBrowse.Location = new System.Drawing.Point(480, 33);
            this.btnExtractDirBrowse.Name = "btnExtractDirBrowse";
            this.btnExtractDirBrowse.Size = new System.Drawing.Size(24, 23);
            this.btnExtractDirBrowse.TabIndex = 23;
            this.btnExtractDirBrowse.Text = "...";
            this.btnExtractDirBrowse.UseVisualStyleBackColor = true;
            this.btnExtractDirBrowse.Click += new System.EventHandler(this.btnExtractDirBrowse_Click);
            // 
            // tbExtractDir
            // 
            this.tbExtractDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbExtractDir.Location = new System.Drawing.Point(59, 34);
            this.tbExtractDir.Name = "tbExtractDir";
            this.tbExtractDir.Size = new System.Drawing.Size(415, 20);
            this.tbExtractDir.TabIndex = 22;
            this.tbExtractDir.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\UnicodeTestCases\\A\\den f¢rste hjemmeside i rækken [" +
                "DK]";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(8, 37);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(54, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "extract to:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 10);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 13);
            this.label10.TabIndex = 15;
            this.label10.Text = "archive:";
            // 
            // btnExtract
            // 
            this.btnExtract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExtract.Enabled = false;
            this.btnExtract.Location = new System.Drawing.Point(510, 33);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(59, 23);
            this.btnExtract.TabIndex = 24;
            this.btnExtract.Text = "Extract";
            this.btnExtract.UseVisualStyleBackColor = true;
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);
            // 
            // btnReadZipBrowse
            // 
            this.btnReadZipBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadZipBrowse.Location = new System.Drawing.Point(480, 6);
            this.btnReadZipBrowse.Name = "btnReadZipBrowse";
            this.btnReadZipBrowse.Size = new System.Drawing.Size(24, 23);
            this.btnReadZipBrowse.TabIndex = 13;
            this.btnReadZipBrowse.Text = "...";
            this.btnReadZipBrowse.UseVisualStyleBackColor = true;
            this.btnReadZipBrowse.Click += new System.EventHandler(this.btnZipBrowse_Click);
            // 
            // tbZipToOpen
            // 
            this.tbZipToOpen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbZipToOpen.Location = new System.Drawing.Point(59, 7);
            this.tbZipToOpen.Name = "tbZipToOpen";
            this.tbZipToOpen.Size = new System.Drawing.Size(415, 20);
            this.tbZipToOpen.TabIndex = 12;
            this.tbZipToOpen.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\UnicodeTestCases\\A\\den f¢rste hjemmeside i rækken [" +
                "DK]";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnCreateZipBrowse);
            this.tabPage2.Controls.Add(this.chkHidePassword);
            this.tabPage2.Controls.Add(this.tbPassword);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.comboBox3);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.comboBox2);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.progressBar2);
            this.tabPage2.Controls.Add(this.tbComment);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.comboBox1);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.lblStatus);
            this.tabPage2.Controls.Add(this.btnCancel);
            this.tabPage2.Controls.Add(this.progressBar1);
            this.tabPage2.Controls.Add(this.btnZipUp);
            this.tabPage2.Controls.Add(this.tbZipToCreate);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.btnZipupDirBrowse);
            this.tabPage2.Controls.Add(this.tbDirName);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(577, 360);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Create";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnCreateZipBrowse
            // 
            this.btnCreateZipBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreateZipBrowse.Location = new System.Drawing.Point(545, 51);
            this.btnCreateZipBrowse.Name = "btnCreateZipBrowse";
            this.btnCreateZipBrowse.Size = new System.Drawing.Size(24, 20);
            this.btnCreateZipBrowse.TabIndex = 21;
            this.btnCreateZipBrowse.Text = "...";
            this.btnCreateZipBrowse.UseVisualStyleBackColor = true;
            this.btnCreateZipBrowse.Click += new System.EventHandler(this.btnCreateZipBrowse_Click);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBox1);
            this.tabPage3.Controls.Add(this.pictureBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(577, 360);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "About";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::WinFormsExample.Properties.Resources.zippedFile;
            this.pictureBox1.Location = new System.Drawing.Point(8, 19);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(42, 51);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(56, 19);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(510, 325);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // Form1
            // 
            this.AcceptButton = this.btnZipUp;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(585, 386);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(558, 422);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "DotNetZip\'s WinForms Zip Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
        private System.Windows.Forms.TextBox tbDirName;
        private System.Windows.Forms.TextBox tbZipToCreate;
        private System.Windows.Forms.TextBox tbComment;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.RadioButton radioFlavorZip;
        private System.Windows.Forms.RadioButton radioFlavorSfxGui;
        private System.Windows.Forms.RadioButton radioFlavorSfxCmd;
        private System.Windows.Forms.RadioButton radioZip64Never;
        private System.Windows.Forms.RadioButton radioZip64Always;
        private System.Windows.Forms.RadioButton radioZip64AsNecessary;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.ComboBox comboBox3;
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
    }
}

