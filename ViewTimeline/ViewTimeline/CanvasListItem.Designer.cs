namespace ViewTimeline
{
    partial class CanvasListItem
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CanvasListItem));
            this.btnColor = new System.Windows.Forms.Button();
            this.cbxEnableCanvas = new System.Windows.Forms.CheckBox();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnColor
            // 
            this.btnColor.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnColor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnColor.Location = new System.Drawing.Point(3, 3);
            this.btnColor.Name = "btnColor";
            this.btnColor.Size = new System.Drawing.Size(29, 29);
            this.btnColor.TabIndex = 9;
            this.btnColor.UseVisualStyleBackColor = true;
            this.btnColor.Click += new System.EventHandler(this.btnColor_Click);
            // 
            // cbxEnableCanvas
            // 
            this.cbxEnableCanvas.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbxEnableCanvas.Checked = true;
            this.cbxEnableCanvas.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxEnableCanvas.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightSteelBlue;
            this.cbxEnableCanvas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbxEnableCanvas.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbxEnableCanvas.Location = new System.Drawing.Point(38, 3);
            this.cbxEnableCanvas.Name = "cbxEnableCanvas";
            this.cbxEnableCanvas.Size = new System.Drawing.Size(241, 29);
            this.cbxEnableCanvas.TabIndex = 8;
            this.cbxEnableCanvas.Text = "Canvas Name";
            this.cbxEnableCanvas.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbxEnableCanvas.UseVisualStyleBackColor = true;
            this.cbxEnableCanvas.CheckedChanged += new System.EventHandler(this.cbxEnableCanvas_CheckedChanged);
            // 
            // btnSettings
            // 
            this.btnSettings.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Image = global::ViewTimeline.Properties.Resources.Settings;
            this.btnSettings.Location = new System.Drawing.Point(285, 3);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(31, 29);
            this.btnSettings.TabIndex = 13;
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveDown.Image = ((System.Drawing.Image)(resources.GetObject("btnMoveDown.Image")));
            this.btnMoveDown.Location = new System.Drawing.Point(361, 3);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(33, 29);
            this.btnMoveDown.TabIndex = 12;
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveUp.Image = ((System.Drawing.Image)(resources.GetObject("btnMoveUp.Image")));
            this.btnMoveUp.Location = new System.Drawing.Point(322, 3);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(33, 29);
            this.btnMoveUp.TabIndex = 11;
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // CanvasListItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.btnColor);
            this.Controls.Add(this.cbxEnableCanvas);
            this.Name = "CanvasListItem";
            this.Size = new System.Drawing.Size(399, 35);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnColor;
        private System.Windows.Forms.CheckBox cbxEnableCanvas;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnSettings;
    }
}
