using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public class FileManager
    {
        public TimeIndexFile TimeFile { get; private set; }
        public PacketIndexFile IndexFile { get; private set; }
        public PacketCaptureFile CaptureFile { get; private set; }

        public FilterCountCache CountCache { get; private set; }
 
        public DateTime StartTime { get { return TimeFile.StartTime; } }
        public DateTime EndTime { get { return TimeFile.EndTime; } }
        public TimeSpan Duration { get { return TimeFile.Duration; } }

        public long TotalPackets { get { return IndexFile.TotalPackets; } }

        public long TotalData { get { return CaptureFile.TotalData; } }
        public string CaptureFileName { get { return CaptureFile.CaptureFileName; } }

        public int TotalFilters { get { return CountCache.FilterFiles.Count; } }
        //private CacheManager _cache;
        

        public FileManager(IGpfProject setup)
        {
            CaptureFile = new PacketCaptureFile(setup.CaptureFiles[0]);
            IndexFile = new PacketIndexFile(setup.PacketIndex);
            TimeFile = new TimeIndexFile(setup.TimeIndex);

             CountCache = new FilterCountCache(setup.FilterFiles);
        }
        
        /// <summary>
        /// Gets one or more packets from the capture file, beginning at the specified packet index
        /// </summary>
        /// <param name="index">the index of the first record in the packet index file</param>
        /// <param name="count">the number of packets to collect</param>
        /// <returns>a new packet collection object</returns>
        public PacketCollection GetPackets(long index, int count = 1)
        {
            return CaptureFile.GetPackets(IndexFile.GetDataIndex(index), count);
        }

        /// <summary>
        /// Gets one or more packets from the capture file, beginning at the specified time
        /// </summary>
        /// <param name="time">the time of the first record in the time index file</param>
        /// <param name="count">the number of packets to collect</param>
        /// <returns>a new packet collection object</returns>
        public PacketCollection GetPackets(DateTime time, int count = 1)
        {
            return GetPackets(TimeFile.GetPacketIndex(time), count);
        }

        /// <summary>
        /// Gets all packets between time1 and time2 from the capture file.
        /// </summary>
        /// <param name="time">the inclusive time of the first record in the time index file</param>
        /// <param name="count">the exclusive time at which to stop collecting</param>
        /// <returns>a new packet collection object</returns>
        public PacketCollection GetPackets(DateTime time1, DateTime time2)
        {
            long packetIndex1;
            long packetIndex2;

            TimeFile.GetPacketIndices(time1, time2, out packetIndex1, out packetIndex2);

            int count = (int)(packetIndex2 - packetIndex1);

            return GetPackets(packetIndex1, count);
        }

        /// <summary>
        /// Gets the amount of elapsed time between packets at index1 and index2
        /// </summary>
        /// <param name="index1">the index of the packet starting the time span</param>
        /// <param name="index2">the index of the packet concluding the time span</param>
        /// <returns>the time span as a TimeSpan object</returns>
        public TimeSpan GetTimeSpan(long index1, long index2)
        {
            PacketCollection packets = GetPackets(index1);
            packets.Merge(GetPackets(index2));

            return packets.Collection[1].ArrivalTime.Subtract(packets.Collection[0].ArrivalTime);
        }

        /// <summary>
        /// Gets the number of packets contained between time1 and time2
        /// </summary>
        /// <param name="time1">the start time (inclusive)</param>
        /// <param name="time2">the end time (exclusive)</param>
        /// <returns>the packet count</returns>
        public long GetIndexSpan(DateTime time1, DateTime time2)
        {
            long start, end;
            TimeFile.GetPacketIndices(time1, time2, out start, out end);
            return end - start;
        }

        /// <summary>
        /// Gets the number of bytes in the packet capture between time1 and time2 in the timestamp file
        /// </summary>
        /// <param name="time1">the start time (inclusive)</param>
        /// <param name="time2">the end time (exclusive)</param>
        /// <returns>the packet count</returns>
        public long GetDataSpan(DateTime time1, DateTime time2)
        {
            long packetStartIndex, packetEndIndex;
            TimeFile.GetPacketIndices(time1, time2, out packetStartIndex, out packetEndIndex);
            return GetDataSpan(packetStartIndex, packetEndIndex);
        }

        /// <summary>
        /// Gets the number of bytes in the packet capture between index1 and index2 in the index file
        /// </summary>
        /// <param name="index1">the start index (includive)</param>
        /// <param name="index2">the end index (exclusive)</param>
        /// <returns>the packet count</returns>
        public long GetDataSpan(long index1, long index2)
        {
            long start, end;
            IndexFile.GetDataIndices(index1, index2, out start, out end);
            return end - start;
        }
        
        private static DateTime Increment(DateTime time, FrameElement parent)
        {
            DateTime tmp;
            switch (parent.Level)
            {
                case FrameNodeLevel.Root:
                    tmp = new DateTime(time.Year + 1, 1, 1);
                    break;
                case FrameNodeLevel.Year:
                    tmp = new DateTime(time.Year, time.Month, 1).AddMonths(1);
                    break;
                case FrameNodeLevel.Month:
                    tmp = new DateTime(time.Year, time.Month, time.Day).AddDays(1);
                    break;
                case FrameNodeLevel.Day:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0).AddHours(1);
                    break;
                case FrameNodeLevel.Hour:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute - (time.Minute % 5), 0).AddMinutes(5);
                    break;
                case FrameNodeLevel.PartHour:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0).AddMinutes(1);
                    break;
                case FrameNodeLevel.Minute:
                    tmp = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second- (time.Second % 6)).AddSeconds(10);
                    break; 
                default:
                    throw new ArgumentOutOfRangeException("parentLevel invalid: Seconds cannot be subdivided.");
            }
            return tmp > parent.EndTime ? parent.EndTime : tmp;
        }

        /// <summary>
        /// Fills the parent element, and generates the canvas data
        /// </summary>
        /// <param name="time1">The time to start the canvas</param>
        /// <param name="time2">The time to end the canvas</param>
        /// <param name="parentLevel">the parent nodes level</param>
        /// <returns>The CanvasData for the specified range and resolution</returns>
        public void FillFrameElement(FrameElement frame)
        {
            //Debug.Assert(frame.Level != FrameNodeLevel.Second); //seconds cannot be subdivided
            if (frame.Level == FrameNodeLevel.Second) return;

            long packetIndex1, packetIndex2;
            long startOffset, endOffset;


            //first time record
            packetIndex1 = TimeFile.GetPacketIndex(frame.StartTime);
            //first index record
            startOffset = IndexFile.GetDataIndex(packetIndex1);
            //second element
            DateTime curr = frame.StartTime;
            DateTime next = Increment(curr, frame); // Increment(frame.StartTime, frame.Level);

            frame.Children.Clear();
            
            while (curr < frame.EndTime)
            {
                packetIndex2 = TimeFile.GetPacketIndex(next);


                endOffset = IndexFile.GetDataIndex(packetIndex2);
                
                
                frame.CreateChildElement(curr, next.Subtract(curr), packetIndex2 - packetIndex1, endOffset - startOffset);

                curr = next;
                next = Increment(curr, frame);
                packetIndex1 = packetIndex2;
                startOffset = endOffset;
            }


            //if (FilterFiles.Count < 1) return;

            //List<FilterData> filters = new List<FilterData>();

            ////use collected filter byte segments to generate packet counts per filter
            //foreach (var filter in FilterFiles)
            //{
            //    filters.Add(new FilterData(FilterFiles.IndexOf(filter), filter.FillBitResultsList(filterIndeces)));
            //}

            //if (CountFilters != null) CountFilters(filters);
            ////filters now contains the correct coutn information

            //for (int k = 0; k < filters.Count; k++)
            //{
            //    for (int j = 0; j < filters[k].Segments.Count; j++)
            //    {
            //        //copy the jth segment count in filter k to the kth filter index in frame child j
            //        frame.Children[j].FilterCounts.Add(filters[k].Counts[j]);
            //    }
            //}
            
        }
    }

    public class FilterData
    {
        private int index;
        public List<byte[]> Segments { get; private set; }
        public List<int> Counts { get; private set; }
         
        public FilterData(int index, List<byte[]> segments)
        {
            this.index = index;
            this.Segments = segments;
            Counts = new List<int>();
        }

    }

    public abstract class GpfVisualiserFile
    {
        protected byte[] _readBuffer;
        protected string _filepath;
        protected BufferedStream _filestream;

        public GpfVisualiserFile(string filepath)
        {
            _filepath = filepath;
            _readBuffer = new byte[16];
            _filestream = new BufferedStream(new FileStream(filepath, FileMode.Open, FileAccess.Read), ReadBufferSize);
        }

        ~GpfVisualiserFile()
        {
            if (_filestream != null) _filestream.Dispose();
        }

        private const int ReadBufferSize = 512 * 1024;

    }

    public class TimeIndexFile : GpfVisualiserFile
    {
        private const int HeaderSize = 16;

        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public TimeSpan Duration { get { return EndTime.Subtract(StartTime); } }

        //because time data size is determined by time and not by number of packets (under 250MB per year)
        //safe to load whole file - a 10 year capture will contain under 2.5 GB, which is acceptable for high end x64 machine
        //saves alot of disk reads
        public long[] timeData;
        public TimeIndexFile(string filepath) : base(filepath)
        {
            byte[] tmp = new byte[_filestream.Length - HeaderSize];
            _filestream.Seek(0, SeekOrigin.Begin);
            _filestream.Read(_readBuffer, 0, 16);
            long startSec = BitConverter.ToInt64(_readBuffer, 0);
            long dur = BitConverter.ToInt64(_readBuffer, 8);

            StartTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(startSec);
            EndTime = StartTime.AddSeconds(dur);

            _filestream.Seek(HeaderSize, SeekOrigin.Begin);
            _filestream.Read(tmp, 0, tmp.Length);

            timeData = new long[dur];
            Buffer.BlockCopy(tmp, 0, timeData, 0, tmp.Length);
        }

        public long GetPacketIndex(DateTime target)
        {
            int val = (int) target.Subtract(StartTime).TotalSeconds;

            if (val == timeData.Length)
            {
                val--;
                return timeData[val];
            }
    
            while (target < EndTime && val + 1 < timeData.Length && timeData[val] == timeData[val + 1])
                val--; //index is a forward reference only, so packet is outside of span - last vali
            
            return timeData[val];
        }

        public void GetPacketIndices(DateTime time1, DateTime time2, out long packetStartIndex, out long packetEndIndex)
        {
            packetStartIndex = GetPacketIndex(time1);
            packetEndIndex = GetPacketIndex(time2);
        }
    }

    public class PacketIndexFile : GpfVisualiserFile
    {
        private const int HeaderSize = 8;
        public long TotalPackets { get; private set; }

        public PacketIndexFile(string filepath)
            : base(filepath)
        {
            _filestream.Seek(0, SeekOrigin.Begin);
            _filestream.Read(_readBuffer, 0, 8);
            TotalPackets = BitConverter.ToInt64(_readBuffer, 0);
        }

        /// <summary>
        /// Gets the number of bytes in the packet capture between index1 and index2 in the index file
        /// </summary>
        /// <param name="index1">the start index (includive)</param>
        /// <param name="index2">the end index (exclusive)</param>
        /// <returns>the packet count</returns>
        public void GetDataIndices(long packetIndex1, long packetIndex2, out long dataStartIndex, out long dataEndIndex)
        {
            if (packetIndex1 > packetIndex2)
                throw new ArgumentException("packet index 1 cannot be greater than packet index 2");
            dataStartIndex = GetDataIndex(packetIndex1);
            dataEndIndex = GetDataIndex(packetIndex2);
        }


        public long GetDataIndex(long packetIndex)
        {
            if (packetIndex > TotalPackets)
                packetIndex = TotalPackets;
            _filestream.Seek(HeaderSize + (packetIndex * 8), SeekOrigin.Begin);
            _filestream.Read(_readBuffer, 0, 8);
            return BitConverter.ToInt64(_readBuffer, 0);
        }
    }

    public class PacketCaptureFile : GpfVisualiserFile
    {
        public long TotalData { get; private set; }

        public string CaptureFileName
        {
            get { return _filepath.Substring(_filepath.LastIndexOf('\\') + 1); }
        }

        public PacketCaptureFile(string filepath) : base(filepath)
        {
            TotalData = _filestream.Length;
        }

        /// <summary>
        /// Gets one or more packets from the capture file, beginning at the specified packet index
        /// </summary>
        /// <param name="index">the index of the first record in the packet index file</param>
        /// <param name="count">the number of packets to collect</param>
        /// <returns>a new packet collection object</returns>
        public PacketCollection GetPackets(long index, int count = 1)
        {
            PacketCollection collection = new PacketCollection();

            long curr = index;

            for (int k = 0; k < count; k++)
            {
                _filestream.Seek(curr, SeekOrigin.Begin);
                byte[] header = new byte[16];
                _filestream.Read(header, 0, 16);

                DateTime time = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(BitConverter.ToInt32(header, 0));
                time = time.AddMilliseconds(BitConverter.ToInt32(header, 4));
                int incl = BitConverter.ToInt32(header, 8);
                int orig = BitConverter.ToInt32(header, 12);

                byte[] payload = new byte[incl];
                _filestream.Read(payload, 0, incl);
                collection.AddPacket(new Packet(time, orig, payload));

                curr += incl + 16;
            }
            
            return collection;
        }

    }
}