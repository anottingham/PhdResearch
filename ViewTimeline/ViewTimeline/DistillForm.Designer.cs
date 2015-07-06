namespace ViewTimeline
{
    partial class DistillForm
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
            this.tbName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.chbCrop = new System.Windows.Forms.CheckBox();
            this.tbCrop = new System.Windows.Forms.TextBox();
            this.lblBytes = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.btnDistill = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.cbWireshark = new System.Windows.Forms.CheckBox();
            this.cbTemp = new System.Windows.Forms.CheckBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.chbEnableFilter = new System.Windows.Forms.CheckBox();
            this.cbFilter = new System.Windows.Forms.ComboBox();
            this.pbDistillProgress = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(93, 23);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(204, 20);
            this.tbName.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Capture Name";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(260, 50);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(71, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // chbCrop
            // 
            this.chbCrop.Appearance = System.Windows.Forms.Appearance.Button;
            this.chbCrop.AutoSize = true;
            this.chbCrop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chbCrop.Location = new System.Drawing.Point(93, 86);
            this.chbCrop.Name = "chbCrop";
            this.chbCrop.Size = new System.Drawing.Size(81, 23);
            this.chbCrop.TabIndex = 3;
            this.chbCrop.Text = "Crop Packets";
            this.chbCrop.UseVisualStyleBackColor = true;
            this.chbCrop.CheckedChanged += new System.EventHandler(this.chbCrop_CheckedChanged);
            // 
            // tbCrop
            // 
            this.tbCrop.Location = new System.Drawing.Point(205, 88);
            this.tbCrop.Name = "tbCrop";
            this.tbCrop.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.tbCrop.Size = new System.Drawing.Size(92, 20);
            this.tbCrop.TabIndex = 4;
            this.tbCrop.Text = "100";
            this.tbCrop.Visible = false;
            // 
            // lblBytes
            // 
            this.lblBytes.AutoSize = true;
            this.lblBytes.Location = new System.Drawing.Point(303, 91);
            this.lblBytes.Name = "lblBytes";
            this.lblBytes.Size = new System.Drawing.Size(32, 13);
            this.lblBytes.TabIndex = 5;
            this.lblBytes.Text = "bytes";
            this.lblBytes.Visible = false;
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(183, 91);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(16, 13);
            this.lblTo.TabIndex = 6;
            this.lblTo.Text = "to";
            this.lblTo.Visible = false;
            // 
            // btnDistill
            // 
            this.btnDistill.Location = new System.Drawing.Point(93, 194);
            this.btnDistill.Name = "btnDistill";
            this.btnDistill.Size = new System.Drawing.Size(238, 23);
            this.btnDistill.TabIndex = 7;
            this.btnDistill.Text = "Distill Selection";
            this.btnDistill.UseVisualStyleBackColor = true;
            this.btnDistill.Click += new System.EventHandler(this.btnDistill_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(51, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(36, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Folder";
            // 
            // tbFolder
            // 
            this.tbFolder.Enabled = false;
            this.tbFolder.Location = new System.Drawing.Point(93, 52);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.Size = new System.Drawing.Size(161, 20);
            this.tbFolder.TabIndex = 8;
            // 
            // cbWireshark
            // 
            this.cbWireshark.AutoSize = true;
            this.cbWireshark.Location = new System.Drawing.Point(93, 165);
            this.cbWireshark.Name = "cbWireshark";
            this.cbWireshark.Size = new System.Drawing.Size(115, 17);
            this.cbWireshark.TabIndex = 10;
            this.cbWireshark.Text = "Open In Wireshark";
            this.cbWireshark.UseVisualStyleBackColor = true;
            // 
            // cbTemp
            // 
            this.cbTemp.AutoSize = true;
            this.cbTemp.Location = new System.Drawing.Point(205, 165);
            this.cbTemp.Name = "cbTemp";
            this.cbTemp.Size = new System.Drawing.Size(138, 17);
            this.cbTemp.TabIndex = 11;
            this.cbTemp.Text = "Use Temporary Storage";
            this.cbTemp.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(303, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = ".cap";
            // 
            // chbEnableFilter
            // 
            this.chbEnableFilter.AutoSize = true;
            this.chbEnableFilter.Location = new System.Drawing.Point(93, 115);
            this.chbEnableFilter.Name = "chbEnableFilter";
            this.chbEnableFilter.Size = new System.Drawing.Size(48, 17);
            this.chbEnableFilter.TabIndex = 13;
            this.chbEnableFilter.Text = "Filter";
            this.chbEnableFilter.UseVisualStyleBackColor = true;
            this.chbEnableFilter.CheckedChanged += new System.EventHandler(this.chbEnableFilter_CheckedChanged);
            // 
            // cbFilter
            // 
            this.cbFilter.FormattingEnabled = true;
            this.cbFilter.Location = new System.Drawing.Point(93, 138);
            this.cbFilter.Name = "cbFilter";
            this.cbFilter.Size = new System.Drawing.Size(238, 21);
            this.cbFilter.TabIndex = 14;
            this.cbFilter.Visible = false;
            // 
            // pbDistillProgress
            // 
            this.pbDistillProgress.Location = new System.Drawing.Point(93, 224);
            this.pbDistillProgress.Name = "pbDistillProgress";
            this.pbDistillProgress.Size = new System.Drawing.Size(238, 23);
            this.pbDistillProgress.TabIndex = 15;
            // 
            // lblProgress
            // 
            this.lblProgress.Location = new System.Drawing.Point(90, 250);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(241, 23);
            this.lblProgress.TabIndex = 17;
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // DistillForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(354, 293);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.pbDistillProgress);
            this.Controls.Add(this.cbFilter);
            this.Controls.Add(this.chbEnableFilter);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbTemp);
            this.Controls.Add(this.cbWireshark);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbFolder);
            this.Controls.Add(this.btnDistill);
            this.Controls.Add(this.lblTo);
            this.Controls.Add(this.lblBytes);
            this.Controls.Add(this.tbCrop);
            this.Controls.Add(this.chbCrop);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbName);
            this.Name = "DistillForm";
            this.Text = "DistillForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chbCrop;
        private System.Windows.Forms.TextBox tbCrop;
        private System.Windows.Forms.Label lblBytes;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.Button btnDistill;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbFolder;
        private System.Windows.Forms.CheckBox cbWireshark;
        private System.Windows.Forms.CheckBox cbTemp;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chbEnableFilter;
        private System.Windows.Forms.ComboBox cbFilter;
        private System.Windows.Forms.ProgressBar pbDistillProgress;
        private System.Windows.Forms.Label lblProgress;
    }
}