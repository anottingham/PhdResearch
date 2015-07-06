namespace ViewTimeline
{
    partial class ViewForm
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
            this.saveImageDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.graphToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showMajorGridlinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showMinorGridlinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.saveImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.distillCaptureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectionBox = new System.Windows.Forms.Panel();
            this.PropertiesPanel = new System.Windows.Forms.Panel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.GraphPanel = new System.Windows.Forms.Panel();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslCaptureName = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslCurrentView = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslTotalPackets = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslTotalData = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslMarkedPackets = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslMarkedData = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslMarkedDistillSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.showProtocolStatisticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.graphToolStripMenuItem,
            this.configurationToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1246, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "viewFormMenuStrip";
            // 
            // graphToolStripMenuItem
            // 
            this.graphToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showMajorGridlinesToolStripMenuItem,
            this.showMinorGridlinesToolStripMenuItem,
            this.toolStripMenuItem2,
            this.saveImageToolStripMenuItem,
            this.toolStripMenuItem3,
            this.distillCaptureToolStripMenuItem,
            this.toolStripMenuItem1,
            this.showProtocolStatisticsToolStripMenuItem});
            this.graphToolStripMenuItem.Name = "graphToolStripMenuItem";
            this.graphToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.graphToolStripMenuItem.Text = "Graph";
            // 
            // showMajorGridlinesToolStripMenuItem
            // 
            this.showMajorGridlinesToolStripMenuItem.Checked = true;
            this.showMajorGridlinesToolStripMenuItem.CheckOnClick = true;
            this.showMajorGridlinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showMajorGridlinesToolStripMenuItem.Name = "showMajorGridlinesToolStripMenuItem";
            this.showMajorGridlinesToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.showMajorGridlinesToolStripMenuItem.Text = "Show Major Gridlines";
            this.showMajorGridlinesToolStripMenuItem.Click += new System.EventHandler(this.showMajorGridlinesToolStripMenuItem_Click);
            // 
            // showMinorGridlinesToolStripMenuItem
            // 
            this.showMinorGridlinesToolStripMenuItem.CheckOnClick = true;
            this.showMinorGridlinesToolStripMenuItem.Name = "showMinorGridlinesToolStripMenuItem";
            this.showMinorGridlinesToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.showMinorGridlinesToolStripMenuItem.Text = "Show Minor Gridlines";
            this.showMinorGridlinesToolStripMenuItem.Click += new System.EventHandler(this.showMinorGridlinesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(227, 6);
            // 
            // saveImageToolStripMenuItem
            // 
            this.saveImageToolStripMenuItem.Name = "saveImageToolStripMenuItem";
            this.saveImageToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.saveImageToolStripMenuItem.Text = "Save Image";
            this.saveImageToolStripMenuItem.Click += new System.EventHandler(this.btnSaveImage_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(227, 6);
            // 
            // distillCaptureToolStripMenuItem
            // 
            this.distillCaptureToolStripMenuItem.Name = "distillCaptureToolStripMenuItem";
            this.distillCaptureToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.distillCaptureToolStripMenuItem.Text = "Distill Capture From Selection";
            this.distillCaptureToolStripMenuItem.Click += new System.EventHandler(this.distillCaptureToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(227, 6);
            // 
            // configurationToolStripMenuItem
            // 
            this.configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
            this.configurationToolStripMenuItem.Size = new System.Drawing.Size(119, 20);
            this.configurationToolStripMenuItem.Text = "GPU Configuration";
            // 
            // selectionBox
            // 
            this.selectionBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectionBox.Location = new System.Drawing.Point(0, 0);
            this.selectionBox.Name = "selectionBox";
            this.selectionBox.Size = new System.Drawing.Size(722, 242);
            this.selectionBox.TabIndex = 4;
            this.selectionBox.Text = "Current Selection";
            // 
            // PropertiesPanel
            // 
            this.PropertiesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertiesPanel.Location = new System.Drawing.Point(0, 0);
            this.PropertiesPanel.Name = "PropertiesPanel";
            this.PropertiesPanel.Size = new System.Drawing.Size(520, 242);
            this.PropertiesPanel.TabIndex = 6;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.GraphPanel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1246, 706);
            this.splitContainer1.SplitterDistance = 460;
            this.splitContainer1.TabIndex = 7;
            // 
            // GraphPanel
            // 
            this.GraphPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.GraphPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GraphPanel.Location = new System.Drawing.Point(0, 0);
            this.GraphPanel.Name = "GraphPanel";
            this.GraphPanel.Size = new System.Drawing.Size(1246, 460);
            this.GraphPanel.TabIndex = 8;
            this.GraphPanel.Resize += new System.EventHandler(this.GraphPanel_Resize);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.PropertiesPanel);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.selectionBox);
            this.splitContainer2.Size = new System.Drawing.Size(1246, 242);
            this.splitContainer2.SplitterDistance = 520;
            this.splitContainer2.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslCaptureName,
            this.tsslCurrentView,
            this.tsslTotalPackets,
            this.tsslTotalData,
            this.tsslMarkedPackets,
            this.tsslMarkedData,
            this.tsslMarkedDistillSize});
            this.statusStrip1.Location = new System.Drawing.Point(0, 708);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1246, 22);
            this.statusStrip1.TabIndex = 8;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslCaptureName
            // 
            this.tsslCaptureName.Name = "tsslCaptureName";
            this.tsslCaptureName.Size = new System.Drawing.Size(98, 17);
            this.tsslCaptureName.Text = "tsslCaptureName";
            // 
            // tsslCurrentView
            // 
            this.tsslCurrentView.Name = "tsslCurrentView";
            this.tsslCurrentView.Size = new System.Drawing.Size(89, 17);
            this.tsslCurrentView.Text = "tsslCurrentView";
            // 
            // tsslTotalPackets
            // 
            this.tsslTotalPackets.Name = "tsslTotalPackets";
            this.tsslTotalPackets.Size = new System.Drawing.Size(91, 17);
            this.tsslTotalPackets.Text = "tsslTotalPackets";
            // 
            // tsslTotalData
            // 
            this.tsslTotalData.Name = "tsslTotalData";
            this.tsslTotalData.Size = new System.Drawing.Size(75, 17);
            this.tsslTotalData.Text = "tsslTotalData";
            // 
            // tsslMarkedPackets
            // 
            this.tsslMarkedPackets.Name = "tsslMarkedPackets";
            this.tsslMarkedPackets.Size = new System.Drawing.Size(104, 17);
            this.tsslMarkedPackets.Text = "tsslMarkedPackets";
            // 
            // tsslMarkedData
            // 
            this.tsslMarkedData.Name = "tsslMarkedData";
            this.tsslMarkedData.Size = new System.Drawing.Size(88, 17);
            this.tsslMarkedData.Text = "tsslMarkedData";
            // 
            // tsslMarkedDistillSize
            // 
            this.tsslMarkedDistillSize.Name = "tsslMarkedDistillSize";
            this.tsslMarkedDistillSize.Size = new System.Drawing.Size(113, 17);
            this.tsslMarkedDistillSize.Text = "tsslMarkedDistillSize";
            // 
            // showProtocolStatisticsToolStripMenuItem
            // 
            this.showProtocolStatisticsToolStripMenuItem.Name = "showProtocolStatisticsToolStripMenuItem";
            this.showProtocolStatisticsToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.showProtocolStatisticsToolStripMenuItem.Text = "Show Protocol Statistics";
            this.showProtocolStatisticsToolStripMenuItem.Click += new System.EventHandler(this.showProtocolStatisticsToolStripMenuItem_Click);
            // 
            // ViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1246, 730);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "ViewForm";
            this.Text = "ViewForm";
            this.Load += new System.EventHandler(this.ViewForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SaveFileDialog saveImageDialog;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem graphToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveImageToolStripMenuItem;
        private System.Windows.Forms.Panel selectionBox;
        private System.Windows.Forms.ToolStripMenuItem showMajorGridlinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMinorGridlinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.Panel PropertiesPanel;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem distillCaptureToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel GraphPanel;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsslCaptureName;
        private System.Windows.Forms.ToolStripStatusLabel tsslCurrentView;
        private System.Windows.Forms.ToolStripStatusLabel tsslTotalPackets;
        private System.Windows.Forms.ToolStripStatusLabel tsslTotalData;
        private System.Windows.Forms.ToolStripStatusLabel tsslMarkedPackets;
        private System.Windows.Forms.ToolStripStatusLabel tsslMarkedData;
        private System.Windows.Forms.ToolStripStatusLabel tsslMarkedDistillSize;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem configurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showProtocolStatisticsToolStripMenuItem;
    }
}