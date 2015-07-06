using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grammar;
using NetMQ;

namespace ZmqInterface
{
    public class CaptureProgram
    {
        public string ProjectPath { get; set; }
        public string[] FileNames { get; set; }

        public bool Indexing { get; private set; }
        public bool Filtering { get; private set; }

        public string PacketIndexFile { get; set; }
        public string TimeIndexFile { get; set; }

        public GpfProgram FilterProgram { get; private set; }

        public string GpfOutputFolder { get; set; }
        public int GpuIndex { get; private set; }
        public int GpuBufferSize { get; private set; }
        public int GpuStreams { get; private set; }
    

        public CaptureProgram(string[] fileNames)
        {
            this.FileNames = fileNames;
            this.Indexing = false;
            GpuIndex = 0;
            GpuStreams = 4;
        }

        public void SendProcessRequest(NetMQSocket socket)
        {
            NetMQMessage msg = new NetMQMessage();

            //set file info
            msg.Append(BitConverter.GetBytes((int)ServerCodes.File));
            msg.Append(BitConverter.GetBytes(FileNames.Length));
            for (int k = 0; k < FileNames.Length; k++)
            {
                msg.Append(FileNames[k] + '\0');
            }

            if (Indexing)
            {
                msg.Append(BitConverter.GetBytes((int) ServerCodes.Index));
                msg.Append(PacketIndexFile);
                msg.Append(TimeIndexFile);
            }

            if (Filtering)
            {
                msg.Append(BitConverter.GetBytes((int)ServerCodes.Filter));
                //step 0 - gpu index
                msg.Append(BitConverter.GetBytes(GpuIndex));

                //step 1 - target memsize
                msg.Append(BitConverter.GetBytes(GpuBufferSize));

                //step 2 - streams

                msg.Append(BitConverter.GetBytes(GpuStreams));

                string filename = FileNames[0].Substring(FileNames[0].LastIndexOf('\\') + 1);
                //step 3 - capture name
                msg.Append(filename);

                //step 4 - destination folder
                msg.Append(GpfOutputFolder + '\0');

                //step 5 - constant values
                msg.Append(FilterProgram.StaticMemory.ToArray());

                //step 6 - rule program
                msg.Append(FilterProgram.RuleProgram.ToArray());
                //step 7 - filter_program
                msg.Append(FilterProgram.FilterProgram.ToArray());
                //step 8 - lookup program
                msg.Append(FilterProgram.LookupMemory.ToArray());
                //step 9 - filter names
                foreach (var filter in FilterProgram.FilterNames)
                {
                    msg.Append(filter + '\0');
                }
                //step 10 - integer names

                foreach (var integer in FilterProgram.IntegerNames)
                {
                    msg.Append(integer + '\0');
                }
                
            }

            msg.Append(BitConverter.GetBytes((int) ServerCodes.EndRequest));

            socket.SendMessage(msg);
        }

        public void CreateIndex(string indexFolder, bool forceCreate)
        {
            SetIndexFilePath(indexFolder);
            Indexing = !File.Exists(PacketIndexFile) || !File.Exists(TimeIndexFile) || forceCreate;
        }

        private void SetIndexFilePath(string indexFolder)
        {
            string file = indexFolder + FileNames[0].Substring(FileNames[0].LastIndexOf('\\') + 1);
            PacketIndexFile = file + ".pidx";
            TimeIndexFile = file + ".tidx";
        }

        public void CreateFilter(string programFile, string outputFolder, int gpuIndex, int memSize, int streams)
        {
            Filtering = true;
            FilterProgram = GpfCompiler.CompileProgram(programFile);
            GpfOutputFolder = outputFolder;
            GpuIndex = gpuIndex;
            GpuBufferSize = memSize;
            GpuStreams = streams;

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

        }

        public List<string> GetFilterFiles()
        {
            if (!Filtering) return new List<string>();
            return
                FilterProgram.FilterNames.Select(f => f + ".gpf_filter")
                    .ToList();
        }

        public List<string> GetFieldFiles()
        {
            if (!Filtering) return new List<string>();
            return
                FilterProgram.IntegerNames.Select(f => f + ".gpf_field")
                    .ToList();
        }
    }
}
