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
            this.label1 = new System.Windows.Forms.Label();
            this.tbDirName = new System.Windows.Forms.TextBox();
            this.btnDirBrowse = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tbZipName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.radioFlavorZip = new System.Windows.Forms.RadioButton();
            this.radioFlavorSfxGui = new System.Windows.Forms.RadioButton();
            this.radioFlavorSfxCmd = new System.Windows.Forms.RadioButton();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.radioZip64Never = new System.Windows.Forms.RadioButton();
            this.radioZip64Always = new System.Windows.Forms.RadioButton();
            this.radioZip64AsNecessary = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "directory to zip: ";
            // 
            // tbDirName
            // 
            this.tbDirName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDirName.Location = new System.Drawing.Point(99, 20);
            this.tbDirName.Name = "tbDirName";
            this.tbDirName.Size = new System.Drawing.Size(496, 20);
            this.tbDirName.TabIndex = 10;
            this.tbDirName.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\UnicodeTestCases\\A\\den f¢rste hjemmeside i rækken [" +
                "DK]";
            // 
            // btnDirBrowse
            // 
            this.btnDirBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDirBrowse.Location = new System.Drawing.Point(601, 20);
            this.btnDirBrowse.Name = "btnDirBrowse";
            this.btnDirBrowse.Size = new System.Drawing.Size(24, 20);
            this.btnDirBrowse.TabIndex = 11;
            this.btnDirBrowse.Text = "...";
            this.btnDirBrowse.UseVisualStyleBackColor = true;
            this.btnDirBrowse.Click += new System.EventHandler(this.btnDirBrowse_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(525, 311);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(101, 29);
            this.btnOk.TabIndex = 80;
            this.btnOk.Text = "Zip It!";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnZipup_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 347);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(613, 14);
            this.progressBar1.TabIndex = 4;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(525, 388);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(101, 29);
            this.btnCancel.TabIndex = 90;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tbZipName
            // 
            this.tbZipName.AcceptsReturn = true;
            this.tbZipName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbZipName.Location = new System.Drawing.Point(99, 48);
            this.tbZipName.Name = "tbZipName";
            this.tbZipName.Size = new System.Drawing.Size(496, 20);
            this.tbZipName.TabIndex = 20;
            this.tbZipName.Text = "c:\\dinoch\\dev\\dotnet\\zip\\test\\U.zip";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "file to save to:";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 388);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 143);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "encoding:";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(99, 139);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 21);
            this.comboBox1.TabIndex = 50;
            // 
            // tbComment
            // 
            this.tbComment.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbComment.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbComment.Location = new System.Drawing.Point(99, 201);
            this.tbComment.Multiline = true;
            this.tbComment.Name = "tbComment";
            this.tbComment.Size = new System.Drawing.Size(526, 104);
            this.tbComment.TabIndex = 70;
            this.tbComment.Text = "-zip file comment here-";
            this.tbComment.Leave += new System.EventHandler(this.tbComment_Leave);
            this.tbComment.Enter += new System.EventHandler(this.tbComment_Enter);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 201);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "comment:";
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
            // progressBar2
            // 
            this.progressBar2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar2.Location = new System.Drawing.Point(12, 367);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(613, 14);
            this.progressBar2.TabIndex = 17;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 76);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "flavor:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 169);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "ZIP64?:";
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
            this.groupBox1.Location = new System.Drawing.Point(99, 68);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(386, 28);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioZip64Never);
            this.groupBox2.Controls.Add(this.radioZip64Always);
            this.groupBox2.Controls.Add(this.radioZip64AsNecessary);
            this.groupBox2.Location = new System.Drawing.Point(99, 161);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(386, 28);
            this.groupBox2.TabIndex = 60;
            this.groupBox2.TabStop = false;
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(99, 109);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(200, 21);
            this.comboBox2.TabIndex = 40;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 113);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "compression:";
            // 
            // Form1
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(637, 427);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.tbComment);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.tbZipName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnDirBrowse);
            this.Controls.Add(this.tbDirName);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "DotNetZip\'s WinForms Zip Creator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbDirName;
        private System.Windows.Forms.Button btnDirBrowse;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox tbZipName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox tbComment;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton radioFlavorZip;
        private System.Windows.Forms.RadioButton radioFlavorSfxGui;
        private System.Windows.Forms.RadioButton radioFlavorSfxCmd;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton radioZip64Never;
        private System.Windows.Forms.RadioButton radioZip64Always;
        private System.Windows.Forms.RadioButton radioZip64AsNecessary;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label7;
    }
}

