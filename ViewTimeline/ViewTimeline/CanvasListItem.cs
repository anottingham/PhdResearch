using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK.Graphics;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public partial class CanvasListItem : UserControl
    {
        public delegate void PositionChangedEventHandler(GraphCanvas sender, bool up);

        public event PositionChangedEventHandler PositionChanged;
        private GraphCanvas _canvas;
        private List<string> _filters;

        public CanvasListItem()
        {
            InitializeComponent();
        }

        public void Initialise(GraphCanvas canvas, List<string> filtersList)
        {
            _canvas = canvas;
            _filters = filtersList;
            cbxEnableCanvas.Text = _canvas.Name;

            btnColor.BackColor = Color.FromArgb(_canvas.Color.ToArgb());
            cbxEnableCanvas.Checked = true;
            _canvas.Hide = false;
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Color c = colorDialog1.Color;
                btnColor.BackColor = c;
                _canvas.Color = new Color4(c.R, c.G, c.B, c.A);
            }
        }
        
        private void cbxEnableCanvas_CheckedChanged(object sender, EventArgs e)
        {
            _canvas.Hide = !cbxEnableCanvas.Checked;
        }
        
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (PositionChanged != null) PositionChanged(_canvas, true);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (PositionChanged != null) PositionChanged(_canvas, false);
        }

        public void EnableMoveButtons(bool up, bool down)
        {
            btnMoveUp.Enabled = up;
            btnMoveDown.Enabled = down;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            CanvasRenderProperties frm = new CanvasRenderProperties();
            frm.Initialise(_canvas, _filters);
            frm.Show();
        }
    }
}