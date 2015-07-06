namespace ZmqInterface
{
    partial class Grid
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
            this.resultGrid1 = new ViewReader.ResultGrid();
            this.SuspendLayout();
            // 
            // resultGrid1
            // 
            this.resultGrid1.AutoScroll = true;
            this.resultGrid1.BackColor = System.Drawing.Color.White;
            this.resultGrid1.Location = new System.Drawing.Point(12, 12);
            this.resultGrid1.Name = "resultGrid1";
            this.resultGrid1.Size = new System.Drawing.Size(1412, 746);
            this.resultGrid1.TabIndex = 0;
            // 
            // Grid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1422, 760);
            this.Controls.Add(this.resultGrid1);
            this.Name = "Grid";
            this.Text = "Grid";
            this.ResumeLayout(false);

        }

        #endregion

        private ViewReader.ResultGrid resultGrid1;
    }
}