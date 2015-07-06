using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using ViewTimeline.Graphs;

using NetMQ;
namespace ViewTimeline
{
    //temporary solution to avoid circular dependency
    public interface IGpfProject
    {
        string RootFolder { get; }
        string ProjectName { get; }
        string Path { get; }
        string TimeIndex { get; }
        string PacketIndex { get; }

        List<string> CaptureFiles { get; }
        List<string> FilterFiles { get; }
        List<string> FieldFiles { get; }
    }
    public struct ViewFormSetup
    {
        public IGpfProject Project { get; private set; }
        public NetMQSocket Socket { get; private set; }
        public List<string> Gpus { get; private set; }

        public ViewFormSetup(NetMQSocket socket, IGpfProject project, List<string> gpus) : this()
        {
            Project = project; 
            Socket = socket;
            Gpus = gpus;
        }
    }
    public partial class ViewForm : Form
    {
        private bool _loaded;
        private FrameElement _currentSelection;
        private GraphControl GraphViewport;
        private CanvasList CanvasList;
        private ViewFormSetup _setup;
        private string _captureFile, _indexFile;

        public Size ViewPortSize { get { return GraphViewport.Size; }}
        public ViewForm()
        {
            _loaded = false;
            _currentSelection = null;
            InitializeComponent();
            saveImageDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveImageDialog.Filter = "PNG|*.png|All Files|*.*";
        }

        public void Initialise(ViewFormSetup setup)
        {
            _setup = setup;
            _captureFile = setup.Project.CaptureFiles[0];
            _indexFile = setup.Project.PacketIndex;
            GraphViewport = new GraphControl();
            GraphViewport.Size = GraphPanel.Size;
            CanvasList = new CanvasList();
            GraphViewport.SelectionChanged += GraphViewport_SelectionChanged;
            CanvasManager.GraphChanged += CanvasManager_GraphChanged;
            CanvasManager.Initialise(setup, this);

            GraphViewport.Initialise();//captureFile, indexFile, timeFile);
            CanvasList.Initialise(GraphViewport.CaptureGraph.CanvasCollection, TrimFilenames(setup.Project.FilterFiles));

            GraphViewport.CaptureGraph.MajorGridLines = showMajorGridlinesToolStripMenuItem.Checked;
            GraphViewport.CaptureGraph.MinorGridLines = showMinorGridlinesToolStripMenuItem.Checked;

            GraphPanel.Controls.Add(GraphViewport);
            PropertiesPanel.Controls.Add(CanvasList);

            CreateFieldDistributionMenu(setup.Project.FieldFiles);
            CreateGpuSelectMenu(setup.Gpus);

            GraphViewport.Location = Point.Empty;

            CanvasList.Location = Point.Empty;
            GraphViewport.Focus();
            _loaded = true;
        }

        private void CreateFieldDistributionMenu(List<string> fieldFiles)
        {
            ToolStripMenuItem btnFieldSelect = new ToolStripMenuItem();
            ToolStripDropDown tsddFieldContainer = new ToolStripDropDown();
            btnFieldSelect.Text = "View Field Distribution";
            btnFieldSelect.DropDown = tsddFieldContainer;

            btnFieldSelect.DropDownDirection = ToolStripDropDownDirection.Right;
            foreach (var fieldFile in fieldFiles)
            {
                int first = fieldFile.LastIndexOf('\\') + 1;
                int last = fieldFile.LastIndexOf('.');

                string id = fieldFile.Substring(first, last - first);
                ToolStripButton btn = new ToolStripButton(id);
                
                btn.Click += btn_Click;
                tsddFieldContainer.Items.Add(btn);
            }
            graphToolStripMenuItem.DropDown.Items.Add(btnFieldSelect);
        }

        private void CreateGpuSelectMenu(List<string> gpus)
        {
            foreach (var gpu in gpus)
            {
                var checkbox = new ToolStripMenuItem(gpu);
                checkbox.Click += checkbox_Click;
                //checkbox.CheckOnClick = true;
                configurationToolStripMenuItem.DropDown.Items.Add(checkbox);
            }
            ((ToolStripMenuItem)configurationToolStripMenuItem.DropDown.Items[0]).Checked = true;
        }

        void checkbox_Click(object sender, EventArgs e)
        {
            SuspendLayout();
            var btn = (ToolStripMenuItem)sender;
            btn.Checked = true;
            CanvasManager.SelectedGpuIndex = configurationToolStripMenuItem.DropDown.Items.IndexOf(btn);
            
            for (int index = 0; index < configurationToolStripMenuItem.DropDown.Items.Count; index++)
            {
                if (index != CanvasManager.SelectedGpuIndex)
                {
                    ((ToolStripMenuItem)configurationToolStripMenuItem.DropDown.Items[index]).Checked = false;
                }
            }

            ResumeLayout();
        }

        async void btn_Click(object sender, EventArgs e)
        {
            SuspendLayout();
            ToolStripButton btn = (ToolStripButton)sender;
            
            var frm = new FieldSetViewer();
            //var task = 
                frm.Initialise(_setup.Project.RootFolder + "filter\\" + btn.Text + ".gpf_field");
            frm.Show();
            //await task;
            frm.Closed += frm_Closed;

        }

        void frm_Closed(object sender, EventArgs e)
        {
            ResumeLayout();
        }

        public static List<string> TrimFilenames(List<string> input)
        {
            if (input.Count < 1) return null;
            var tmp = input.Select(i => i.Substring(i.LastIndexOf('\\') + 1));
            return tmp.Select(t => t.Substring(0, t.LastIndexOf('.'))).ToList();
        }

        private void GraphViewport_SelectionChanged(FrameElement newSelection)
        {
            _currentSelection = newSelection;
            Invalidate();
        }

        private void CanvasManager_GraphChanged()
        {
            Invalidate();
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
            {
                GraphViewport.CaptureImage(saveImageDialog.FileName);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_loaded)
            {
            }
            else
            {
                //    //draw title
                Graphics gfx;// = titlePanel.CreateGraphics();
                //    gfx.Clear(titlePanel.BackColor);
                Font fnt;// = new Font(FontFamily.GenericMonospace, 12f);
                SizeF size; //= gfx.MeasureString(CanvasManager.CurrentCanvasData.Description, fnt);
                //    float x = (titlePanel.Width - size.Width) / 2f;
                //    float y = (titlePanel.Height - size.Height) / 2f;
                //    gfx.DrawString(CanvasManager.CurrentCanvasData.Description, fnt, Brushes.Black,
                //                   new RectangleF(x, y, size.Width, size.Height));

                //draw current selection
                if (_currentSelection != null)
                {
                    gfx = selectionBox.CreateGraphics();
                    gfx.Clear(selectionBox.BackColor);
                    fnt = new Font(FontFamily.GenericMonospace, 10f);

                    string lvl = "";
                    string startTime;
                    switch (_currentSelection.Level)
                    {
                        case FrameNodeLevel.Year:
                            lvl += "Year";
                            startTime = _currentSelection.StartTime.ToLongDateString();
                            break;
                        case FrameNodeLevel.Month:
                            lvl += "Month";
                            startTime = _currentSelection.StartTime.ToLongDateString();
                            break;
                        case FrameNodeLevel.Day:
                            lvl += "Day";
                            startTime = _currentSelection.StartTime.ToLongDateString();
                            break;
                        case FrameNodeLevel.Hour:
                            lvl += "Hour";
                            startTime = _currentSelection.StartTime.ToLongDateString() + " " + _currentSelection.StartTime.ToShortTimeString();
                            break;
                        case FrameNodeLevel.PartHour:
                            lvl += "Part-Hour";
                            startTime = _currentSelection.StartTime.ToLongDateString() + " " + _currentSelection.StartTime.ToShortTimeString();
                            break;
                        case FrameNodeLevel.Minute:
                            lvl += "Minute";
                            startTime = _currentSelection.StartTime.ToLongDateString() + " " + _currentSelection.StartTime.ToShortTimeString();
                            break;
                        case FrameNodeLevel.Second:
                            lvl += "Second";
                            startTime = _currentSelection.StartTime.ToLongDateString() + " " + _currentSelection.StartTime.ToShortTimeString();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    string[] array = new string[]
                                         {
                                             "Selection Start:     " + startTime,
                                             "Selection Span:      1 " + lvl,
                                             "Total Packets:       " +
                                             _currentSelection.TotalPackets.ToString("#,0", CultureInfo.InvariantCulture)
                                             ,
                                             "Total Data:          " +
                                             _currentSelection.TotalData.ToString("#,0", CultureInfo.InvariantCulture) +
                                             " Bytes (" + ShortVolumeString(_currentSelection.TotalData) + ")",
                                             "Average Packet Size: " +
                                             (_currentSelection.TotalPackets == 0
                                                  ? "0"
                                                  : ((float) _currentSelection.TotalData /
                                                     _currentSelection.TotalPackets).ToString("F2",
                                                                                              CultureInfo.
                                                                                                  InvariantCulture)) +
                                             " Bytes",
                                             "Average Packet Rate: " +
                                             (_currentSelection.TotalPackets / _currentSelection.Duration.TotalSeconds).
                                                 ToString("#,#0.00", CultureInfo.InvariantCulture) + " per second",
                                             "                     " +
                                             (_currentSelection.TotalPackets / _currentSelection.Duration.TotalHours).
                                                 ToString("#,0", CultureInfo.InvariantCulture) + " per hour",
                                         };
                    float yPos = 5f;
                    foreach (string str in array)
                    {
                        size = gfx.MeasureString(str, fnt);
                        gfx.DrawString(str, fnt, Brushes.Black, new RectangleF(5f, yPos, size.Width, size.Height));
                        yPos += size.Height + 5f;
                    }
                }
            }
            base.OnPaint(e);
        }

        private string ShortVolumeString(long bytes)
        {
            double b = bytes;
            int count = 0;
            while (b > 1024.0)
            {
                b /= 1024.0;
                count++;
            }

            string str = "" + b.ToString("F2");
            switch (count)
            {
                case 0:
                    str += " B";
                    break;
                case 1:
                    str += " KB";
                    break;
                case 2:
                    str += " MB";
                    break;
                case 3:
                    str += " GB";
                    break;
                case 4:
                    str += " TB";
                    break;
                case 5:
                    str += " PB";
                    break;
            }
            return str;
        }

        private void ViewForm_Load(object sender, EventArgs e)
        {
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenCaptureForm frm = new OpenCaptureForm(this);
            frm.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void showMajorGridlinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GraphViewport.CaptureGraph.MajorGridLines = showMajorGridlinesToolStripMenuItem.Checked;
        }

        private void showMinorGridlinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GraphViewport.CaptureGraph.MinorGridLines = showMinorGridlinesToolStripMenuItem.Checked;
        }

        private void distillCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GraphViewport.SelectionMarks.Marks.Count < 1) return;
            DistillForm frm = new DistillForm(GraphViewport.SelectionMarks, _captureFile, _indexFile, _setup.Project.FilterFiles);
            frm.Show();
        }

        private void GraphPanel_Resize(object sender, EventArgs e)
        {
            SuspendLayout();
            GraphViewport.Size = GraphPanel.Size;
            Invalidate();
            ResumeLayout();
        }

        private void showProtocolStatisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new ProtocolStatistics();
            frm.Show();
        }

    }

}