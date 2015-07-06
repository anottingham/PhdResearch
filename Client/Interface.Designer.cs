namespace ZmqInterface
{
    partial class Interface
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
            this.lbSources = new System.Windows.Forms.ListBox();
            this.btnAddSource = new System.Windows.Forms.Button();
            this.btnRemoveSource = new System.Windows.Forms.Button();
            this.fbdProjectFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.ofdPacketCaptue = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.chIndexing = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbProjectFolder = new System.Windows.Forms.TextBox();
            this.chFilter = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbFilterProgram = new System.Windows.Forms.TextBox();
            this.btnLoadFilter = new System.Windows.Forms.Button();
            this.ofdGpfFilter = new System.Windows.Forms.OpenFileDialog();
            this.btnProjectFolder = new System.Windows.Forms.Button();
            this.cbGpu = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cbBufferSize = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbStreams = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbProjectName = new System.Windows.Forms.TextBox();
            this.sfdProject = new System.Windows.Forms.SaveFileDialog();
            this.btnClear = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbSources
            // 
            this.lbSources.AllowDrop = true;
            this.lbSources.FormattingEnabled = true;
            this.lbSources.Location = new System.Drawing.Point(107, 29);
            this.lbSources.Name = "lbSources";
            this.lbSources.Size = new System.Drawing.Size(225, 95);
            this.lbSources.TabIndex = 0;
            this.lbSources.DragDrop += new System.Windows.Forms.DragEventHandler(this.btnDragDrop_DragDrop);
            this.lbSources.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragDrop_DragEnter);
            // 
            // btnAddSource
            // 
            this.btnAddSource.Location = new System.Drawing.Point(339, 29);
            this.btnAddSource.Name = "btnAddSource";
            this.btnAddSource.Size = new System.Drawing.Size(89, 25);
            this.btnAddSource.TabIndex = 4;
            this.btnAddSource.Text = "Add";
            this.btnAddSource.UseVisualStyleBackColor = true;
            this.btnAddSource.Click += new System.EventHandler(this.btnAddSource_Click);
            // 
            // btnRemoveSource
            // 
            this.btnRemoveSource.Location = new System.Drawing.Point(339, 60);
            this.btnRemoveSource.Name = "btnRemoveSource";
            this.btnRemoveSource.Size = new System.Drawing.Size(89, 25);
            this.btnRemoveSource.TabIndex = 5;
            this.btnRemoveSource.Text = "Remove";
            this.btnRemoveSource.UseVisualStyleBackColor = true;
            this.btnRemoveSource.Click += new System.EventHandler(this.btnRemoveSource_Click);
            // 
            // ofdPacketCaptue
            // 
            this.ofdPacketCaptue.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(31, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Source List";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(339, 330);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(89, 23);
            this.btnStart.TabIndex = 11;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // chIndexing
            // 
            this.chIndexing.AutoSize = true;
            this.chIndexing.Checked = true;
            this.chIndexing.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chIndexing.Location = new System.Drawing.Point(107, 141);
            this.chIndexing.Name = "chIndexing";
            this.chIndexing.Size = new System.Drawing.Size(102, 17);
            this.chIndexing.TabIndex = 14;
            this.chIndexing.Text = "Enable Indexing";
            this.chIndexing.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(15, 309);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 19;
            this.label3.Text = "Project Folder";
            // 
            // tbProjectFolder
            // 
            this.tbProjectFolder.AllowDrop = true;
            this.tbProjectFolder.ForeColor = System.Drawing.SystemColors.WindowText;
            this.tbProjectFolder.Location = new System.Drawing.Point(107, 306);
            this.tbProjectFolder.Name = "tbProjectFolder";
            this.tbProjectFolder.Size = new System.Drawing.Size(229, 20);
            this.tbProjectFolder.TabIndex = 18;
            this.tbProjectFolder.DragDrop += new System.Windows.Forms.DragEventHandler(this.tbProjectFolder_DragDrop);
            this.tbProjectFolder.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragDrop_DragEnter);
            // 
            // chFilter
            // 
            this.chFilter.AutoSize = true;
            this.chFilter.Checked = true;
            this.chFilter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chFilter.Location = new System.Drawing.Point(234, 141);
            this.chFilter.Name = "chFilter";
            this.chFilter.Size = new System.Drawing.Size(98, 17);
            this.chFilter.TabIndex = 17;
            this.chFilter.Text = "Enable Filtering";
            this.chFilter.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(16, 185);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 21;
            this.label4.Text = "Filter Program";
            // 
            // tbFilterProgram
            // 
            this.tbFilterProgram.AllowDrop = true;
            this.tbFilterProgram.Location = new System.Drawing.Point(107, 182);
            this.tbFilterProgram.Name = "tbFilterProgram";
            this.tbFilterProgram.Size = new System.Drawing.Size(225, 20);
            this.tbFilterProgram.TabIndex = 20;
            this.tbFilterProgram.DragDrop += new System.Windows.Forms.DragEventHandler(this.tbFilterProgram_DragDrop);
            this.tbFilterProgram.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragDrop_DragEnter);
            // 
            // btnLoadFilter
            // 
            this.btnLoadFilter.Location = new System.Drawing.Point(339, 180);
            this.btnLoadFilter.Name = "btnLoadFilter";
            this.btnLoadFilter.Size = new System.Drawing.Size(89, 23);
            this.btnLoadFilter.TabIndex = 22;
            this.btnLoadFilter.Text = "Open";
            this.btnLoadFilter.UseVisualStyleBackColor = true;
            this.btnLoadFilter.Click += new System.EventHandler(this.btnLoadFilter_Click);
            // 
            // btnProjectFolder
            // 
            this.btnProjectFolder.Location = new System.Drawing.Point(339, 304);
            this.btnProjectFolder.Name = "btnProjectFolder";
            this.btnProjectFolder.Size = new System.Drawing.Size(89, 23);
            this.btnProjectFolder.TabIndex = 23;
            this.btnProjectFolder.Text = "Select";
            this.btnProjectFolder.UseVisualStyleBackColor = true;
            this.btnProjectFolder.Click += new System.EventHandler(this.btnProjectFolder_Click);
            // 
            // cbGpu
            // 
            this.cbGpu.FormattingEnabled = true;
            this.cbGpu.Location = new System.Drawing.Point(107, 208);
            this.cbGpu.Name = "cbGpu";
            this.cbGpu.Size = new System.Drawing.Size(225, 21);
            this.cbGpu.TabIndex = 25;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(71, 211);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(30, 13);
            this.label5.TabIndex = 26;
            this.label5.Text = "GPU";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 238);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 13);
            this.label6.TabIndex = 28;
            this.label6.Text = "Buffer Size (MB)";
            // 
            // cbBufferSize
            // 
            this.cbBufferSize.FormattingEnabled = true;
            this.cbBufferSize.Items.AddRange(new object[] {
            "8",
            "16",
            "32",
            "64",
            "128",
            "256",
            "512"});
            this.cbBufferSize.Location = new System.Drawing.Point(107, 235);
            this.cbBufferSize.Name = "cbBufferSize";
            this.cbBufferSize.Size = new System.Drawing.Size(75, 21);
            this.cbBufferSize.TabIndex = 27;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(30, 265);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "Stream Count";
            // 
            // cbStreams
            // 
            this.cbStreams.FormattingEnabled = true;
            this.cbStreams.Items.AddRange(new object[] {
            "1",
            "2",
            "4",
            "8",
            "12",
            "16",
            "24",
            "32"});
            this.cbStreams.Location = new System.Drawing.Point(107, 262);
            this.cbStreams.Name = "cbStreams";
            this.cbStreams.Size = new System.Drawing.Size(75, 21);
            this.cbStreams.TabIndex = 30;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(30, 335);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 13);
            this.label8.TabIndex = 32;
            this.label8.Text = "Project Name";
            // 
            // tbProjectName
            // 
            this.tbProjectName.Location = new System.Drawing.Point(107, 332);
            this.tbProjectName.Name = "tbProjectName";
            this.tbProjectName.Size = new System.Drawing.Size(229, 20);
            this.tbProjectName.TabIndex = 31;
            this.tbProjectName.Text = "Project1";
            // 
            // sfdProject
            // 
            this.sfdProject.DefaultExt = "gpf_project";
            this.sfdProject.Filter = "GPF Project File (*.gpf_project)|*.gpf_project";
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(339, 91);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(89, 25);
            this.btnClear.TabIndex = 33;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(307, 377);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 13);
            this.label2.TabIndex = 34;
            this.label2.Text = "(Drag Drop Support)";
            // 
            // Interface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 399);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tbProjectName);
            this.Controls.Add(this.cbStreams);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cbBufferSize);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbGpu);
            this.Controls.Add(this.btnProjectFolder);
            this.Controls.Add(this.btnLoadFilter);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbFilterProgram);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbProjectFolder);
            this.Controls.Add(this.chFilter);
            this.Controls.Add(this.chIndexing);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRemoveSource);
            this.Controls.Add(this.btnAddSource);
            this.Controls.Add(this.lbSources);
            this.Name = "Interface";
            this.Text = "Processing Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbSources;
        private System.Windows.Forms.Button btnAddSource;
        private System.Windows.Forms.Button btnRemoveSource;
        private System.Windows.Forms.FolderBrowserDialog fbdProjectFolder;
        private System.Windows.Forms.OpenFileDialog ofdPacketCaptue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.CheckBox chIndexing;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbProjectFolder;
        private System.Windows.Forms.CheckBox chFilter;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbFilterProgram;
        private System.Windows.Forms.Button btnLoadFilter;
        private System.Windows.Forms.OpenFileDialog ofdGpfFilter;
        private System.Windows.Forms.Button btnProjectFolder;
        private System.Windows.Forms.ComboBox cbGpu;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbBufferSize;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbStreams;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbProjectName;
        private System.Windows.Forms.SaveFileDialog sfdProject;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label label2;
    }
}

