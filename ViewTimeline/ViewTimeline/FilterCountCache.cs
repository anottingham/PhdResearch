using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NetMQ;
using ViewTimeline.Graphs;

namespace ViewTimeline
{

    public class FilterCountCache
    {
        public List<FilterFile> FilterFiles { get; private set; }
        private FrameNodeLevel _rootLayerLevel; //the first level 
        private Thread thread;
        private List<long[]> CountList;

        private CountProcessOptions current;

        private struct CountProcessOptions
        {
            public DateTime Start { get; private set; }
            public DateTime End { get; private set; }
            public FrameNodeLevel RenderUnit { get; private set; }

            public CountProcessOptions(DateTime start, DateTime end, FrameNodeLevel renderUnit) : this()
            {
                this.Start = start;
                this.End = end;
                this.RenderUnit = renderUnit;
            }
        }

        public FilterCountCache(IEnumerable<string> fileNames)
        {
            FilterFiles = new List<FilterFile>();

            foreach (var name in fileNames)
            {
                FilterFiles.Add(new FilterFile(name));
            }
        }

        ~FilterCountCache()
        {
            if (thread != null)
            {
                thread.Abort();
            }
        }

        public FilterStatistics GenerateStatistics(string[] filterNames)
        {
            return CountList != null ? new FilterStatistics(CountList, filterNames) : null;
        }

        public void RequestCounts(DateTime start, DateTime end, FrameNodeLevel renderUnit)
        {
            if (FilterFiles.Count == 0) return;
            if (thread != null)
            {
                MessageBox.Show("Error: Count Request already in progress");
                return;
            }
            current = new CountProcessOptions(start, end, renderUnit);
            thread = new Thread(ProcessFilterFiles);
            thread.Start(current);
        }

        public void CollectCounts(List<FrameElement> elements)
        {
            if (FilterFiles.Count == 0) return;
            if (thread == null)
            {
                MessageBox.Show("Error: Count Request not issued");
                return;
            }

            thread.Join();

            //should be one to one association between frames and count arrays
            Debug.Assert(elements.Count == CountList.Count);

            elements.ForEach(e => e.FilterCounts.Clear());

            for (var k = 0; k < elements.Count; k++)
            {
                elements[k].FilterCounts.AddRange(CountList[k]);
            }
            thread = null;
        }

        private void ProcessFilterFiles(object options)
        {
            if (FilterFiles.Count == 0) return;
            var socket = CanvasManager.ServerSocket.GetSocket(ServerSocketType.Count);
            
            CountList = new List<long[]>();
            var op = (CountProcessOptions) options;
            var curr = op.Start;
            var end = op.End;
            
            var indices = new List<long>();
            indices.Add(CanvasManager.FileManager.TimeFile.GetPacketIndex(curr));

            while (curr < end)
            {
                curr = Increment(curr, op.RenderUnit);
                if (curr > end)
                {
                    curr = end;
                }
                indices.Add(CanvasManager.FileManager.TimeFile.GetPacketIndex(curr));
            }

            var rawList = FilterFiles.Select(filterFile => filterFile.FillBitResultsList(indices)).ToList();
            var segments = rawList[0].Count;
            var max = rawList.SelectMany(r => r).Select(x => x.Length).Max();

            //indicate gpu to use
            socket.Send(BitConverter.GetBytes(CanvasManager.SelectedGpuIndex), sizeof(int));

            //indicate number of segments so server can allocate result memory
            socket.Send(BitConverter.GetBytes(FilterFiles.Count * segments), sizeof(int));

            //indicate max size of segments
            socket.Send(BitConverter.GetBytes(max), sizeof(int));

            for (int k = 0; k < segments; k++)
            {
                for (int j = 0; j < FilterFiles.Count; j++)
                {
                    var msg = rawList[j][k];
                    socket.Send(msg, msg.Length);
                }
            }

            var reply = socket.Receive();
            var size = FilterFiles.Count * sizeof (long);

            for (var k = 0; k < segments; k++)
            {
                var counts = new long[FilterFiles.Count];
                Buffer.BlockCopy(reply, k * size, counts, 0, size);
                CountList.Add(counts);
            }

            CanvasManager.ServerSocket.ReturnSocket(ref socket);
        }

        private DateTime Increment(DateTime time, FrameNodeLevel renderUnit)
        {
            DateTime tmp;
            switch (renderUnit)
            {
                case FrameNodeLevel.Year:
                    tmp = new DateTime(time.Year + 1, 1, 1);
                    break;
                case FrameNodeLevel.Month:
                    tmp = new DateTime(time.Year, time.Month, 1).AddMonths(1);
                    break;
                case FrameNodeLevel.Day:
                    tmp = new DateTime(time.Year, time.Month, time.Day).AddDays(1);
                    break;
                case FrameNodeLevel.Hour:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0).AddHours(1);
                    break;
                case FrameNodeLevel.PartHour:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute - (time.Minute % 5), 0).AddMinutes(5);
                    break;
                case FrameNodeLevel.Minute:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0).AddMinutes(1);
                    break;
                case FrameNodeLevel.Second:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second).AddSeconds(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("renderUnit invalid: Root cannot be rendered.");
            }
            return tmp;
        }

    }

    public class FilterFile
    {
        private const int Magic = 0x1234ABCD;
        public string Filename { get; private set; }
        public long Records { get; private set; }
        public string Capture { get; private set; }

        public bool Enabled { get; private set; }

        public FilterFile(string filterFile)
        {
            Enabled = false;
            using (var view = new FileStream(filterFile, FileMode.Open, FileAccess.Read))
            {
                Filename = filterFile;
                byte[] buffer = new byte[272];
                view.Seek(0, SeekOrigin.Begin);
                view.Read(buffer, 0, 272);
                Records = Convert.ToInt64(BitConverter.ToInt64(buffer, 8));
                
                Capture = Encoding.Default.GetString(buffer, 144, 128);
                Capture = Capture.Substring(0, Capture.IndexOf('\0'));

            }
        }

        //public byte[] GetBitResults(long startIndex, long endIndex)
        //{
        //    using (var view = new BufferedStream(new FileStream(Filename, FileMode.Open, FileAccess.Read)))
        //    {
        //        view.Seek(272, SeekOrigin.Begin); //skip header

        //        int startByte = (int) (startIndex / 8);
        //        int endByte = (int) (endIndex / 8 + (endIndex % 8 == 0 ? 0 : 1));

        //        if (endByte == startByte)return new byte[4];

        //        int bytes = endByte - startByte;
        //        view.Seek(startByte, SeekOrigin.Current);
        //        byte[] buffer = new byte[bytes];
        //        view.Read(buffer, startByte, bytes);

        //        //mask edges
        //        byte mask = (byte) (0xFF >> (((int)startIndex) % 8));
        //        buffer[0] &= mask;

        //        mask = (byte) (0xFF << (7 - ((int)endIndex) % 8));
        //        buffer[buffer.Length - 1] &= mask;

        //        return buffer;
        //    }
        //}

        public List<byte[]> FillBitResultsList(List<long> indices)
        {
            
            List<byte[]> results = new List<byte[]>();

            using (var view = new BufferedStream(new FileStream(Filename, FileMode.Open, FileAccess.Read)))
            {

                for (int k = 0; k < indices.Count - 1; k++)
                {
                    if (indices[k] == indices[k + 1])
                    {
                        results.Add(new byte[4]);
                        continue;
                    }

                    long currByte = 272 + (indices[k] / 8);
                    long nextByte = 272 + (indices[k + 1] / 8 + (indices[k + 1] % 8 == 0 ? 0 : 1));


                    int bytes = (int)(nextByte - currByte);
                    int ints = (int)Math.Ceiling(bytes / 4.0);

                    view.Seek(currByte, SeekOrigin.Begin);

                    byte[] buffer = new byte[ints * sizeof(int)]; //allocate on integer boundries
                    view.Read(buffer, 0, bytes);

                    //mask edges
                    byte mask = (byte)(0xFF >> ((int)(indices[k]) % 8));
                    buffer[0] &= mask;

                    mask = (byte)(0xFF & ~(0xFF >> ((int)(indices[k + 1]) % 8)));
                    buffer[bytes - 1] &= mask;

                    results.Add(buffer);
                }
            }
            return results;
        }

        public override string ToString()
        {
            return Filename + " ( " + Capture + " )";
        }
    }

    public class FilterStatistics
    {
        public int ProtocolCount { get; private set; }
        public long PacketCount { get; private set; }
        public string[] ProtocolNames { get; private set; }
        public long[] MatchingCount { get; private set; }
        public float[] MatchingRatio { get; private set; }

        public FilterStatistics( List<long[]> Counts, string[] filterNames)
        {
            PacketCount = CanvasManager.TimeTree.TotalPackets;
            ProtocolNames = filterNames;
            ProtocolCount = filterNames.Count();
            MatchingCount = new long[ProtocolCount];
            MatchingRatio = new float[ProtocolCount];

            foreach (var array in Counts)
            {
                for (var k = 0; k < ProtocolCount; k++)
                {
                        MatchingCount[k] += array[k];
                }
            }
            for (var k = 0; k < ProtocolCount; k++)
            {
                MatchingRatio[k] = ((float)MatchingCount[k] / PacketCount) * 100f;
            }
        }

    }
}
