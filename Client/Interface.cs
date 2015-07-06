using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;

using Grammar;
using ViewTimeline;

namespace ZmqInterface
{
    public partial class Interface : Form
    {
        private NetMQContext context;

        public event EventHandler<CaptureProgram> OnSendComplete;

        public Interface(NetMQContext context, List<string> gpus)
        {
            this.context = context;
            InitializeComponent();

            if (gpus.Count == 0)
            {
                cbGpu.Items.Add("No CUDA GPUs detected");
                cbGpu.Enabled = false;
            }
            else
            {
                foreach (var t in gpus)
                {
                    cbGpu.Items.Add(t);
                }
                cbGpu.SelectedIndex = 0;
            }
            cbBufferSize.SelectedIndex = 5;
            cbStreams.SelectedIndex = 2;
            
            this.Invalidate();
        }

        private void btnAddSource_Click(object sender, EventArgs e)
        {
            ofdPacketCaptue.DefaultExt = "cap";
            ofdPacketCaptue.Filter = "PCAP File (*.cap;*.pcap)|*.cap;*.pcap|All File (*.*)|*.*";
            if (ofdPacketCaptue.ShowDialog() == DialogResult.OK)
            {
                lbSources.Items.Add(ofdPacketCaptue.FileName);
            }
        }

        private void btnRemoveSource_Click(object sender, EventArgs e)
        {
            if (lbSources.Items.Count > 0 && lbSources.SelectedIndex >= 0)
            {
                lbSources.Items.RemoveAt(lbSources.SelectedIndex);
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (lbSources.Items.Count < 1)
            {
                MessageBox.Show("No capture sources specified.");
                return;
            }

            string[] files = new string[lbSources.Items.Count];
            for (int k = 0; k < files.Length; k++)
            {
                files[k] = lbSources.Items[k].ToString();
            }

            CaptureProgram program = new CaptureProgram(files);
            string outputFolder = tbProjectFolder.Text + tbProjectName.Text + "\\";
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            program.CreateIndex(outputFolder, chIndexing.Checked);

            if (chFilter.Checked)
            {
                if (!cbGpu.Enabled)
                {
                    MessageBox.Show("Filtering requires a CUDA capable GPU with a compute capability of 3.5 or greater.");
                }
                else
                {
                    program.CreateFilter(tbFilterProgram.Text, outputFolder + "filter\\", cbGpu.SelectedIndex, Convert.ToInt32(cbBufferSize.SelectedItem.ToString()), Convert.ToInt32(cbStreams.SelectedItem.ToString()));
                }
            }

            GpfProjectFile file = new GpfProjectFile(program, outputFolder, tbProjectName.Text);
            if (GpfProjectFile.Serialize(file) != true) return;
            

            this.Close();
            
            if (OnSendComplete != null) OnSendComplete(this, program);
        }

        /// <summary>
        /// Locates and opens a specific filter program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadFilter_Click(object sender, EventArgs e)
        {
            ofdGpfFilter.DefaultExt = "gpf";
            ofdGpfFilter.Filter = "GPF Program (*.gpf)|*.gpf|Text Document (*.txt)|*txt|All File (*.*)|*.*";

            if (ofdGpfFilter.ShowDialog() == DialogResult.OK)
            {
                tbFilterProgram.Text = ofdGpfFilter.FileName;
            }

        }

        /// <summary>
        /// Selects an output folder for project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProjectFolder_Click(object sender, EventArgs e)
        {
            fbdProjectFolder.RootFolder = Environment.SpecialFolder.MyComputer;
            if (fbdProjectFolder.ShowDialog() == DialogResult.OK)
            {
                tbProjectFolder.Text = fbdProjectFolder.SelectedPath;
            }
        }

        private void DragDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void btnDragDrop_DragDrop(object sender, DragEventArgs e)
        {
            lbSources.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
        }

        private void tbFilterProgram_DragDrop(object sender, DragEventArgs e)
        {
            tbFilterProgram.Text = ((string[]) e.Data.GetData(DataFormats.FileDrop))[0];
        }

        private void tbProjectFolder_DragDrop(object sender, DragEventArgs e)
        {
            var tmp = ((string[]) e.Data.GetData(DataFormats.FileDrop))[0];
            if (File.Exists(tmp))
            {
                tbProjectFolder.Text = tmp.Substring(0, tmp.LastIndexOf('\\') + 1);
            }
            else if (tmp.EndsWith(":\\")) tbProjectFolder.Text = tmp;
            else tbProjectFolder.Text = tmp + "\\";
        }
    }

    [Serializable]
    public class GpfProjectFile : ISerializable, ViewTimeline.IGpfProject
    {
        public string RootFolder { get; private set; }
        public string ProjectName { get; private set; }
        public string Path { get { return RootFolder + ProjectName + ".gpf_project"; } }
        public string TimeIndex { get { return RootFolder + _timeIndex; } }
        public string PacketIndex { get { return RootFolder + _packetIndex; } }

        public List<string> CaptureFiles { get { return _captureFiles; } }
        public List<string> FilterFiles { get { return _filterFiles.Select(f => RootFolder + "filter\\" + f).ToList(); } }
        public List<string> FieldFiles { get { return _fieldFiles.Select(f => RootFolder + "filter\\" + f).ToList(); } }

        private readonly string _timeIndex;
        private readonly string _packetIndex;
        private readonly List<string> _captureFiles;
        private readonly List<string> _filterFiles;
        private readonly List<string> _fieldFiles;

        public GpfProjectFile(CaptureProgram program, string projectFolder, string projectName)
        {
            _captureFiles = program.FileNames.ToList();
            _timeIndex = program.TimeIndexFile.Substring(projectFolder.Length);
            _packetIndex = program.PacketIndexFile.Substring(projectFolder.Length); ;

            _filterFiles = program.GetFilterFiles();
            _fieldFiles = program.GetFieldFiles();
            RootFolder = projectFolder; //reset each time capture is deserialised to the folder in which the project file sits
            ProjectName = projectName;


            program.ProjectPath = Path;
        }

        public static bool Serialize(GpfProjectFile file)
        {
            if (!Directory.Exists(file.RootFolder)) Directory.CreateDirectory(file.RootFolder);

            if (File.Exists(file.Path) && MessageBox.Show("The solution already exists - Overwrite?", 
                "Overwrite Existing Project", MessageBoxButtons.OKCancel) != DialogResult.OK) return false;

            var s = new FileStream(file.Path, FileMode.Create, FileAccess.Write);
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(s, file);
            s.Close();
            return true;
        }

        public static GpfProjectFile Deserialize(string projectPath)
        {
            var s = new FileStream(projectPath, FileMode.Open, FileAccess.Read);
            IFormatter formatter = new BinaryFormatter();
            GpfProjectFile file = (GpfProjectFile)formatter.Deserialize(s);

            file.RootFolder = projectPath.Substring(0, projectPath.LastIndexOf("\\") + 1);
            s.Close();
            return file;
        }

        private GpfProjectFile(SerializationInfo info, StreamingContext context)
        {
            int count = info.GetInt32("captureCount");
            _captureFiles = new List<string>();
            for (int k = 0; k < count; k++)
            {
                CaptureFiles.Add(info.GetString("capture" + k));
            }

            _timeIndex = info.GetString("time");
            _packetIndex = info.GetString("packet");

            count = info.GetInt32("filterCount");
            _filterFiles = new List<string>();
            for (int k = 0; k < count; k++)
            {
                _filterFiles.Add(info.GetString("filter" + k));
            }
            count = info.GetInt32("fieldCount");
            _fieldFiles = new List<string>();
            for (int k = 0; k < count; k++)
            {
                _fieldFiles.Add(info.GetString("field" + k));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("captureCount",_captureFiles.Count);
            for (int k = 0; k < _captureFiles.Count; k++)
            {
                info.AddValue("capture" + k, _captureFiles[k]);
            }
            info.AddValue("time", _timeIndex);
            info.AddValue("packet", _packetIndex);

            info.AddValue("filterCount", _filterFiles.Count);
            for (int k = 0; k < _filterFiles.Count; k++)
            {
                info.AddValue("filter" + k, _filterFiles[k]);
            }

            info.AddValue("fieldCount", _fieldFiles.Count);
            for (int k = 0; k < _fieldFiles.Count; k++)
            {
                info.AddValue("field" + k, _fieldFiles[k]);
            }
        }
    }
}
