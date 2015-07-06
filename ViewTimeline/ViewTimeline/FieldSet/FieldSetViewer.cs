using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViewTimeline.FieldSet;

namespace ViewTimeline
{
    public partial class FieldSetViewer : Form
    {
        private FieldSetControl control;
        public FieldSetViewer()
        {
            InitializeComponent();
            control = new FieldSetControl();
            control.Dock = DockStyle.Fill;
            //control.Width = this.Width;
            Controls.Add(control);
        }

        public void Initialise(string filename)
        {
            control.Initialise(filename);

            Text = "Field Viewer - " + filename.Substring(filename.LastIndexOf('\\') + 1);
            Text = Text.Substring(0, Text.LastIndexOf('.'));
            Invalidate();
        }
    }

}
