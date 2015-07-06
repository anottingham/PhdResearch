using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public partial class DistillForm : Form
    {
        private readonly MarkList _selectedRanges;
        private readonly string _captureFile;
        private readonly string _indexFile;
        private readonly List<string> _filterFiles; 

        private string _captureFolder;
        private string _captureName;

        public DistillForm(MarkList selectedRanges, string captureFile, string indexFile, List<string> filterFiles)
        {
            _filterFiles = filterFiles;
            _selectedRanges = selectedRanges;
            _captureFile = captureFile;
            _indexFile = indexFile;
            _captureFolder = captureFile.Substring(0, captureFile.LastIndexOf("\\") + 1);
            _captureName = captureFile.Substring(captureFile.LastIndexOf("\\") + 1,
                                                 captureFile.LastIndexOf(".") - _captureFolder.Length);


            int k = 1;

            while (File.Exists(_captureFolder + _captureName + k + ".cap"))
            {
                k++;
            }

            InitializeComponent();
            string[] names = filterFiles.Select(s => s.Substring(s.LastIndexOf('\\') + 1, s.LastIndexOf('.') - s.LastIndexOf('\\') - 1)).ToArray();
            cbFilter.Items.AddRange(names);
            tbName.Text = _captureName + k;
            folderBrowserDialog1.SelectedPath = _captureFolder;
            tbFolder.Text = _captureFolder;
        }

        private void chbCrop_CheckedChanged(object sender, EventArgs e)
        {
            lblBytes.Visible = lblTo.Visible = tbCrop.Visible = chbCrop.Checked;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private async void btnDistill_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbName.Text))
            {
                await BeginDistill();
            }
        }

        private async Task BeginDistill()
        {
            var start = DateTime.Now.Ticks;
            string filename = folderBrowserDialog1.SelectedPath + tbName.Text + ".cap";
            if (File.Exists(filename))
            {
                if (MessageBox.Show("Capture file with this name already exists in the specified folder. Do You wish to overwrite this file?", "File already exists", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    File.Delete(filename);
                }
                else return; //do nothing
            }
            _selectedRanges.Marks.Sort();

            //get server socket
            var socket = CanvasManager.ServerSocket.GetSocket(ServerSocketType.Distill);

            NetMQMessage message = new NetMQMessage();
            message.Append(_captureFile);
            message.Append(_indexFile);
            message.Append(filename);

            message.Append(BitConverter.GetBytes(chbEnableFilter.Checked));

            if (chbEnableFilter.Checked)
            {
                message.Append(_filterFiles[cbFilter.SelectedIndex]);
            }
            List<DistillTask> tasks = new List<DistillTask>();

            foreach (var mark in _selectedRanges.Marks)
            {
                tasks.AddRange(DistillTask.GetTasks(mark));
            }

            message.Append(BitConverter.GetBytes(tasks.Count));

            long totalBytes = 0;
            foreach (DistillTask t in tasks)
                totalBytes += t.ByteCount;
            message.Append(BitConverter.GetBytes(totalBytes));
            
            socket.SendMessage(message);

            for (int index = 0; index < tasks.Count; index++)
            {
                tasks[index].Send(socket);
            }

            long progress = 0;
            string total = "  /  " + BytesToMB(totalBytes);

            pbDistillProgress.Value = 0;

            lblProgress.Text = BytesToMB(0) + total;
            while (progress < totalBytes)
            {
                progress = BitConverter.ToInt64(socket.Receive(), 0);
                pbDistillProgress.Value = progress > totalBytes ? 100 : (int)((100 * progress) / totalBytes);
                lblProgress.Text = BytesToMB(progress) + total;
                Refresh();
            }


            var end = DateTime.Now.Ticks;

            var time = TimeSpan.FromTicks(end - start);
            MessageBox.Show("Total Time: " + time.TotalSeconds + "\nMB/s: " + ((totalBytes / 1048576) / time.TotalSeconds),
                "Performance");

            //open in wireshark
            if (cbWireshark.Checked)
            {
                Process.Start(new ProcessStartInfo("wireshark.exe", "\"" + filename + "\""));
            }
        }

        private void chbEnableFilter_CheckedChanged(object sender, EventArgs e)
        {
            cbFilter.Visible = chbEnableFilter.Checked;
            Invalidate();
        }

        private string BytesToMB(long bytes)
        {
            long tmp = bytes / (1024 * 1024);
            string str = "";

            while (tmp >= 1000)
            {
                str = (tmp % 1000) + str;
                str = "," + str;
                tmp = tmp / 1000;
            }
            str = tmp + str + " MB";
            return str;
        }
    }

    public struct DistillTask
    {
        public long IndexStart { get; private set; }
        public int IndexCount { get; private set; }
        public long ByteStart { get; private set; }
        public int ByteCount { get; private set; }

        private const int MAX_BYTES = 64 * 1024 * 1024;
        private const int MAX_INDEX = 16 * 1024 * 1024;

        public static List<DistillTask> GetTasks(CanvasMark mark)
        {
            var tasks = new List<DistillTask>();
            long byteStart, byteEnd;
            CanvasManager.FileManager.IndexFile.GetDataIndices(mark.StartIndex, mark.EndIndex, out byteStart, out byteEnd);

            if (byteEnd - byteStart >= MAX_BYTES || (mark.EndIndex - mark.StartIndex) * sizeof (long) >= MAX_INDEX)
            {
                var marks = mark.Split(byteEnd - byteStart, MAX_BYTES);
                foreach (var m in marks)
                {
                    tasks.AddRange(GetTasks(m));
                }
            }
            else
            {
                tasks.Add(new DistillTask(mark.StartIndex, (int)(mark.EndIndex - mark.StartIndex), byteStart, (int)(byteEnd - byteStart)));
            }
            return tasks;
        }

        public DistillTask(long indexStart, int indexCount, long byteStart, int byteCount) : this()
        {
            IndexStart = indexStart;
            IndexCount = indexCount;
            ByteStart = byteStart;
            ByteCount = byteCount;
        }

        public void Send(NetMQSocket socket)
        {
            NetMQMessage message = new NetMQMessage();
            message.Append(BitConverter.GetBytes(IndexStart));
            message.Append(BitConverter.GetBytes(IndexCount));
            message.Append(BitConverter.GetBytes(ByteStart));
            message.Append(BitConverter.GetBytes(ByteCount));
            socket.SendMessage(message);
        }
    }
}