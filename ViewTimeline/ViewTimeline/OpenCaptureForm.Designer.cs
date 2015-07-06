namespace ViewTimeline
{
    partial class OpenCaptureForm
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
            this.tbFilename = new System.Windows.Forms.TextBox();
            this.btnOpenCaptureFile = new System.Windows.Forms.Button();
            this.btnLocateCapturefile = new System.Windows.Forms.Button();
            this.ofdOpenCapture = new System.Windows.Forms.OpenFileDialog();
            this.cbShowStatistics = new System.Windows.Forms.CheckBox();
            this.cbForceIndex = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // tbFilename
            // 
            this.tbFilename.Location = new System.Drawing.Point(12, 12);
            this.tbFilename.Name = "tbFilename";
            this.tbFilename.ReadOnly = true;
            this.tbFilename.Size = new System.Drawing.Size(167, 20);
            this.tbFilename.TabIndex = 0;
            // 
            // btnOpenCaptureFile
            // 
            this.btnOpenCaptureFile.Enabled = false;
            this.btnOpenCaptureFile.Location = new System.Drawing.Point(158, 90);
            this.btnOpenCaptureFile.Name = "btnOpenCaptureFile";
            this.btnOpenCaptureFile.Size = new System.Drawing.Size(104, 23);
            this.btnOpenCaptureFile.TabIndex = 1;
            this.btnOpenCaptureFile.Text = "Open Capture";
            this.btnOpenCaptureFile.UseVisualStyleBackColor = true;
            // 
            // btnLocateCapturefile
            // 
            this.btnLocateCapturefile.Location = new System.Drawing.Point(187, 9);
            this.btnLocateCapturefile.Name = "btnLocateCapturefile";
            this.btnLocateCapturefile.Size = new System.Drawing.Size(75, 23);
            this.btnLocateCapturefile.TabIndex = 3;
            this.btnLocateCapturefile.Text = "Browse...";
            this.btnLocateCapturefile.UseVisualStyleBackColor = true;
            this.btnLocateCapturefile.Click += new System.EventHandler(this.btnLocateCapturefile_Click);
            // 
            // ofdOpenCapture
            // 
            this.ofdOpenCapture.DefaultExt = "cap";
            this.ofdOpenCapture.Filter = "\"PCAP Files (*.cap, *.pcap)|*.cap;*.pcap|All Files|*.*";
            // 
            // cbShowStatistics
            // 
            this.cbShowStatistics.AutoSize = true;
            this.cbShowStatistics.Location = new System.Drawing.Point(12, 62);
            this.cbShowStatistics.Name = "cbShowStatistics";
            this.cbShowStatistics.Size = new System.Drawing.Size(141, 17);
            this.cbShowStatistics.TabIndex = 5;
            this.cbShowStatistics.Text = "Show Indexing Statistics";
            this.cbShowStatistics.UseVisualStyleBackColor = true;
            // 
            // cbForceIndex
            // 
            this.cbForceIndex.AutoSize = true;
            this.cbForceIndex.Location = new System.Drawing.Point(12, 38);
            this.cbForceIndex.Name = "cbForceIndex";
            this.cbForceIndex.Size = new System.Drawing.Size(130, 17);
            this.cbForceIndex.TabIndex = 4;
            this.cbForceIndex.Text = "Force Index Overwrite";
            this.cbForceIndex.UseVisualStyleBackColor = true;
            // 
            // OpenCaptureForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(274, 125);
            this.Controls.Add(this.cbShowStatistics);
            this.Controls.Add(this.cbForceIndex);
            this.Controls.Add(this.btnLocateCapturefile);
            this.Controls.Add(this.btnOpenCaptureFile);
            this.Controls.Add(this.tbFilename);
            this.Name = "OpenCaptureForm";
            this.Text = "Open Capture";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbFilename;
        private System.Windows.Forms.Button btnOpenCaptureFile;
        private System.Windows.Forms.Button btnLocateCapturefile;
        private System.Windows.Forms.OpenFileDialog ofdOpenCapture;
        private System.Windows.Forms.CheckBox cbShowStatistics;
        private System.Windows.Forms.CheckBox cbForceIndex;
    }
}

