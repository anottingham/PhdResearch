namespace ZmqInterface
{
    partial class ConnectionForm
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
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbTcpPort = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnGrid = new System.Windows.Forms.Button();
            this.ofdFilterFiles = new System.Windows.Forms.OpenFileDialog();
            this.btnTimeline = new System.Windows.Forms.Button();
            this.btnCompileToFile = new System.Windows.Forms.Button();
            this.ofdCompile = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // tbAddress
            // 
            this.tbAddress.Location = new System.Drawing.Point(15, 25);
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.Size = new System.Drawing.Size(93, 20);
            this.tbAddress.TabIndex = 0;
            this.tbAddress.Text = "127.0.0.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Server Address";
            // 
            // tbTcpPort
            // 
            this.tbTcpPort.Location = new System.Drawing.Point(114, 25);
            this.tbTcpPort.Name = "tbTcpPort";
            this.tbTcpPort.Size = new System.Drawing.Size(50, 20);
            this.tbTcpPort.TabIndex = 2;
            this.tbTcpPort.Text = "5555";
            // 
            // btnConnect
            // 
            this.btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnect.Location = new System.Drawing.Point(16, 51);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(148, 23);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnectClick);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(16, 95);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(148, 28);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Create New Project";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(16, 129);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(148, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // btnGrid
            // 
            this.btnGrid.Location = new System.Drawing.Point(16, 224);
            this.btnGrid.Name = "btnGrid";
            this.btnGrid.Size = new System.Drawing.Size(148, 32);
            this.btnGrid.TabIndex = 7;
            this.btnGrid.Text = "Examine Filter Results";
            this.btnGrid.UseVisualStyleBackColor = true;
            this.btnGrid.Click += new System.EventHandler(this.btnGrid_Click);
            // 
            // btnTimeline
            // 
            this.btnTimeline.Location = new System.Drawing.Point(16, 187);
            this.btnTimeline.Name = "btnTimeline";
            this.btnTimeline.Size = new System.Drawing.Size(148, 31);
            this.btnTimeline.TabIndex = 8;
            this.btnTimeline.Text = "Load Exisiting Project";
            this.btnTimeline.UseVisualStyleBackColor = true;
            this.btnTimeline.Click += new System.EventHandler(this.btnTimeline_Click);
            // 
            // btnCompileToFile
            // 
            this.btnCompileToFile.Location = new System.Drawing.Point(16, 262);
            this.btnCompileToFile.Name = "btnCompileToFile";
            this.btnCompileToFile.Size = new System.Drawing.Size(148, 32);
            this.btnCompileToFile.TabIndex = 9;
            this.btnCompileToFile.Text = "Compile To File";
            this.btnCompileToFile.UseVisualStyleBackColor = true;
            this.btnCompileToFile.Click += new System.EventHandler(this.btnCompileToFile_Click);
            // 
            // ofdCompile
            // 
            this.ofdCompile.FileName = "openFileDialog1";
            // 
            // ConnectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(182, 311);
            this.Controls.Add(this.btnCompileToFile);
            this.Controls.Add(this.btnTimeline);
            this.Controls.Add(this.btnGrid);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.tbTcpPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbAddress);
            this.Name = "ConnectionForm";
            this.Text = "ConnectionForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbAddress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbTcpPort;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnGrid;
        private System.Windows.Forms.OpenFileDialog ofdFilterFiles;
        private System.Windows.Forms.Button btnTimeline;
        private System.Windows.Forms.Button btnCompileToFile;
        private System.Windows.Forms.OpenFileDialog ofdCompile;
    }
}