using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;

namespace ViewTimeline.GLControls
{
    partial class GraphControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            this.renderTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // renderTimer
            // 
            this.renderTimer.Interval = 40;
            this.renderTimer.Tick += new System.EventHandler(this.RenderTimerTick);
            // 
            // GraphControl
            // 
            this.ResumeLayout(false);

        }

        #endregion

        private GLControl glControl;
        private Timer renderTimer;
    }
}
