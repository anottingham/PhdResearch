using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ViewTimeline
{
    public partial class OpenCaptureForm : Form
    {
        ViewForm viewer;
        Process indexer;
        string capture, index, timestamp;

        public OpenCaptureForm(ViewForm viewer)
        {
            InitializeComponent();
            this.viewer = viewer;
            capture = null;
            index = null;
            timestamp = null;
        }

        private void btnLocateCapturefile_Click(object sender, EventArgs e)
        {
            if (ofdOpenCapture.ShowDialog() == DialogResult.OK)
            {
                tbFilename.Text = ofdOpenCapture.FileName;
                btnOpenCaptureFile.Enabled = true;
            }
        }

        //private void btnOpenCapturefile_Click(object sender, EventArgs e)
        //{
        //    capture = tbFilename.Text;
        //    index = capture.Substring(0, capture.LastIndexOf('.')) + ".pidx";
        //    timestamp = capture.Substring(0, capture.LastIndexOf('.')) + ".tidx";
        //    if (cbForceIndex.Checked || !File.Exists(index) || !File.Exists(timestamp))
        //    {
        //        //string indexerInput = capture;
        //        indexer = Process.Start(new ProcessStartInfo("FoundryEngine.exe", "\"" + capture + "\" -i -t" + (cbShowStatistics.Checked ? " -s" : "")));
        //        //indexer.EnableRaisingEvents = true;
        //        //indexer.Exited += new EventHandler(indexer_Exited);
        //        indexer.WaitForExit();
        //    }
        //    // else//all necessary files exist
        //    //{
        //    viewer.Initialise(capture, index, timestamp);
        //    //viewer.Show();
        //    this.Hide();
        //    //}
        //}
    }
}