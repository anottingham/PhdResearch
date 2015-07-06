namespace ViewTimeline
{
    partial class CanvasRenderProperties
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
            this.cbGraphType = new System.Windows.Forms.ComboBox();
            this.cbScaleFunction = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbScaleTarget = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbGraphType
            // 
            this.cbGraphType.FormattingEnabled = true;
            this.cbGraphType.Location = new System.Drawing.Point(99, 21);
            this.cbGraphType.Name = "cbGraphType";
            this.cbGraphType.Size = new System.Drawing.Size(156, 21);
            this.cbGraphType.TabIndex = 0;
            this.cbGraphType.SelectedIndexChanged += new System.EventHandler(this.cbGraphType_SelectedIndexChanged);
            // 
            // cbScaleFunction
            // 
            this.cbScaleFunction.FormattingEnabled = true;
            this.cbScaleFunction.Location = new System.Drawing.Point(99, 48);
            this.cbScaleFunction.Name = "cbScaleFunction";
            this.cbScaleFunction.Size = new System.Drawing.Size(156, 21);
            this.cbScaleFunction.TabIndex = 1;
            this.cbScaleFunction.SelectedIndexChanged += new System.EventHandler(this.cbScaleFunction_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Graph Type";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Scale Function";
            // 
            // cbScaleTarget
            // 
            this.cbScaleTarget.FormattingEnabled = true;
            this.cbScaleTarget.Location = new System.Drawing.Point(99, 75);
            this.cbScaleTarget.Name = "cbScaleTarget";
            this.cbScaleTarget.Size = new System.Drawing.Size(156, 21);
            this.cbScaleTarget.TabIndex = 4;
            this.cbScaleTarget.SelectedIndexChanged += new System.EventHandler(this.cbScaleTarget_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Scale Target";
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(99, 102);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 6;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(180, 102);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // CanvasRenderProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(278, 140);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbScaleTarget);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbScaleFunction);
            this.Controls.Add(this.cbGraphType);
            this.Name = "CanvasRenderProperties";
            this.Text = "Properties";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbGraphType;
        private System.Windows.Forms.ComboBox cbScaleFunction;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbScaleTarget;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;

    }
}