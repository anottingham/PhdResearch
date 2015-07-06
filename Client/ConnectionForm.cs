using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grammar;
using NetMQ;
using ViewReader;
using ViewTimeline.Graphs;
using Timer = System.Windows.Forms.Timer;
using ViewTimeline;

namespace ZmqInterface
{
    public enum ServerCodes
    {
        Reset = -1,
        EndRequest = 0,
        Index = 1,
        Filter = 2,
        File = 3,
        Poll = 4,
        Util = 5,
        Connect = 1234
    }

    public partial class ConnectionForm : Form
    {
        private readonly NetMQContext _context;
        private readonly NetMQSocket _socket;

        private CaptureProgram CaptureProgram;
        private Timer pollTimer;

        private long total;
        private long curr;

        private Interface form;
        private List<string> Gpus;

        public ConnectionForm(NetMQContext context, NetMQSocket socket)
        {
            _context = context;
            _socket = socket;
            InitializeComponent();
            progressBar1.Text = "Hello";
            btnStart.Enabled = false;
            btnTimeline.Enabled = false;

            pollTimer = new Timer {Interval = 200};
            pollTimer.Tick += pollTimer_Tick;

            ofdFilterFiles.InitialDirectory = "H:\\gpf\\";
            ofdFilterFiles.Filter = "GPF Filter File|*.gpf_filter|All Files|*.*";

            Gpus = new List<string>();
        }


        private void btnConnectClick(object sender, EventArgs e)
        {
            string addr = "tcp://" + tbAddress.Text + ":" + tbTcpPort.Text;
            btnConnect.Text = "Connecting...";
            btnConnect.BackColor = Color.DarkOrange;
            btnConnect.Refresh();
            
            try
            {
                _socket.Connect(addr);
            }
            catch (NetMQException exception)
            {
                MessageBox.Show(exception.ToString(), "Connect Error");
                btnConnect.Text = exception.Message;
                btnConnect.BackColor = SystemColors.Control;
                return;
            }
            int k = 1234;
            byte[] magic = BitConverter.GetBytes(k);

            _socket.Send(magic, magic.Length);

            k = BitConverter.ToInt32(_socket.Receive(), 0);

            if (k == 1234)
            {
                int count = BitConverter.ToInt32(_socket.Receive(), 0);
                
                for (int j = 0; j < count; j++)
                {
                    Gpus.Add(System.Text.Encoding.Default.GetString(_socket.Receive()));
                }

                if (count == BitConverter.ToInt32(_socket.Receive(), 0))
                {


                    _socket.Send(BitConverter.GetBytes(count), sizeof (int));

                    k = BitConverter.ToInt32(_socket.Receive(), 0);

                    if (k == 1234)
                    {
                        btnConnect.Text = "Connected";
                        btnConnect.BackColor = Color.LawnGreen;
                        btnStart.Enabled = true;
                        btnTimeline.Enabled = true;
                        return;
                    }
                }
            }
            btnConnect.Text = "Error";
            btnConnect.BackColor = Color.DarkRed;
            btnStart.Enabled = false;
            btnTimeline.Enabled = false;


        }

        private void form_OnSendComplete(object sender, CaptureProgram captureProgram)
        {
            CaptureProgram = captureProgram;
            captureProgram.SendProcessRequest(_socket);
            total = BitConverter.ToInt64(_socket.Receive(), 0);
            curr = 0;

            pollTimer.Start();
        }

        void pollTimer_Tick(object sender, EventArgs e)
        {
            
            _socket.Send(BitConverter.GetBytes((int) ServerCodes.Poll));
            curr = BitConverter.ToInt64(_socket.Receive(), 0);
            progressBar1.Value = curr > total ? 100 : (int) ((100 * curr) / total);
            progressBar1.Refresh();
            
            if (curr >= total)
            {
                pollTimer.Stop();

                progressBar1.Value = 0;
                progressBar1.Refresh();

                CompleteFilter();
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            form = new Interface(_context, Gpus);
            form.OnSendComplete += form_OnSendComplete;
            form.Show();
        }

        private void btnGrid_Click(object sender, EventArgs e)
        {
            if (ofdFilterFiles.ShowDialog() == DialogResult.OK)
            {
                Grid frm = new Grid();
                frm.SetFile(ofdFilterFiles.FileName);
                frm.Show();
            }
        }

        private void btnTimeline_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "GPF Project File (*.gpf_project)|*.gpf_project";
            dlg.DefaultExt = "gpf_project";

            if (dlg.ShowDialog() != DialogResult.OK) return;

            GpfProjectFile project = GpfProjectFile.Deserialize(dlg.FileName);
            LaunchViewForm(project);
        }
        

        private void CompleteFilter()
        {
            _socket.Send(BitConverter.GetBytes(1234));

            int response = BitConverter.ToInt32(_socket.Receive(), 0);
            if (response != 1234)
            {
                throw new Exception("Error getting completeion response from server: output writers reported failure.");
            }
            GpfProjectFile project = GpfProjectFile.Deserialize(CaptureProgram.ProjectPath);

            LaunchViewForm(project);
        }

        private void LaunchViewForm(GpfProjectFile project)
        {
            _socket.Send(BitConverter.GetBytes((int)ServerCodes.Util));
            int reply = BitConverter.ToInt32(_socket.Receive(), 0);

            if (reply != (int)ServerCodes.Util) throw new Exception("Count funtion failure. Server response was invalid.");
            
            int port = Convert.ToInt32(tbTcpPort.Text) + 1;
            _socket.Send(BitConverter.GetBytes(port));

            reply = BitConverter.ToInt32(_socket.Receive(), 0);
            if (reply != port) throw new Exception("Count funtion failure. Server response was invalid.");

            ViewForm frm = new ViewForm();

            NetMQSocket countSocket = _context.CreatePairSocket();
            countSocket.Connect("tcp://" + tbAddress.Text + ":" + port);


            frm.Initialise(new ViewFormSetup(countSocket, project, Gpus));

            this.Hide();
            frm.Show();

            
        }

        private void btnCompileToFile_Click(object sender, EventArgs e)
        {
            ofdCompile.DefaultExt = "gpf";
            ofdCompile.Filter = "GPF Program File (*.gpf) |*.gpf";
            if (ofdCompile.ShowDialog() == DialogResult.OK)
            {
                string filename = ofdCompile.FileName;
                var program = GpfCompiler.CompileProgram(filename);
                program.ToFile(filename + "_c");
                MessageBox.Show("Compilation Successful");
            }
        }

    }
}
