using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public enum FrameNodeLevel
    {
        Root = 0,
        Year = 1,
        Month = 2,
        Day = 3,
        Hour = 4,
        PartHour = 5,
        Minute = 6,
        Second = 7
    };

    public abstract class FrameElement : IComparable<FrameElement>
    {
        private long _totalData = -1;
        private long _totalPackets = -1;
        private int _totalFilters = -1;

        public List<long> FilterCounts { get; private set; }

        public TimeSpan Duration { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime
        {
            get { return StartTime.Add(Duration); }
        }

        public float Offset { get; private set; }

        public List<FrameElement> Children { get; private set; }

        public abstract FrameNodeLevel Level { get; }

        public virtual FrameNodeLevel MajorTickLevel
        {
            get
            {
                if (Level != FrameNodeLevel.Second) return (FrameNodeLevel)((int)(Level) + 1);
                return FrameNodeLevel.Second; //cannot go deeper
            }
        }

        protected FileManager FileManager;

        public bool Filled { get; protected set; }

        public FrameElement Parent { get; private set; }

        protected FrameElement(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement parent)
        {
            StartTime = time;
            Offset = CanvasManager.TimeToOffset(StartTime);
            Duration = duration;
            FileManager = fileManager;
            Parent = parent;
            Children = new List<FrameElement>();
            Filled = false;
            _totalData = FileManager.TotalData;
            _totalPackets = FileManager.TotalPackets;
            _totalFilters = FileManager.TotalFilters;
            FilterCounts = new List<long>(_totalFilters);
        }

        public void CreateChildElement(DateTime time, TimeSpan duration, long totalPackets, long totalData)
        {
            FrameElement tmp;
            switch (Level)
            {
                case FrameNodeLevel.Root:
                    tmp = new FrameNodeYear(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.Year:
                    tmp = new FrameNodeMonth(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.Month:
                    tmp = new FrameNodeDay(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.Day:
                    tmp = new FrameNodeHour(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.Hour:
                    tmp = new FrameNodePartHour(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.PartHour:
                    tmp = new FrameNodeMinute(time, duration, FileManager, this);
                    break;
                case FrameNodeLevel.Minute:
                    tmp = new FrameNodeSecond(time, FileManager, this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Level", Level, "FrameElement->CreateChildElement: Argument out of range. Seconds cannot be subdivided.");
            }
            tmp._totalData = totalData - 16 * totalPackets;
            tmp._totalPackets = totalPackets;
            tmp._totalFilters = _totalFilters;

            tmp.FilterCounts = new List<long>();
            Children.Add(tmp);
        }


        ~FrameElement()
        {
            Clear();
        }

        public void Fill()
        {
            //if no children,
            if (Filled || Children.Count != 0) return;
            FileManager.FillFrameElement(this);
            Filled = true;
        }

        /// <summary>
        /// Internal recursive method to build a graph canvas over a specified period at the the specified depth
        /// </summary>
        /// <param name="start">the start of the period to draw</param>
        /// <param name="end">the end of the period to draw</param>
        /// <param name="renderUnit">the depth at which to render</param>
        /// <param name="data">the canvas to fill</param>
        protected internal List<FrameElement> GenerateCanvasElements(DateTime start, DateTime end, FrameNodeLevel renderUnit)
        {
            Debug.Assert(Level != FrameNodeLevel.Second);

            //if definitely overlaps to some degree
            if (end <= StartTime || start >= EndTime) return new List<FrameElement>();

            if (Children.Count == 0) 
                FileManager.FillFrameElement(this);

            var valid = Children.Where(c => end > c.StartTime && start < c.EndTime).ToList();
            
            return Children[0].Level == renderUnit
                ? valid
                : valid.SelectMany(v => v.GenerateCanvasElements(start, end, renderUnit)).ToList();
        }


        protected internal bool FindFrameElement(DateTime start, DateTime end, FrameNodeLevel level, out FrameElement element)
        {
            element = null;
            if (start >= EndTime || end <= StartTime) return false;

            if (level == Level)
            {
                bool test;
                switch (level)
                {
                    case FrameNodeLevel.Year:
                        test = start.Year == StartTime.Year;
                        break;
                    case FrameNodeLevel.Month:
                        test = start.Year == StartTime.Year && start.Month == StartTime.Month;
                        break;
                    case FrameNodeLevel.Day:
                        test = start.Date == StartTime.Date;
                        break;
                    case FrameNodeLevel.Hour:
                        test = start.Date == StartTime.Date && start.Hour == StartTime.Hour;
                        break;
                    case FrameNodeLevel.PartHour:
                        test = start.Date == StartTime.Date && start.Hour == StartTime.Hour && start.Minute - (start.Minute % 5) == StartTime.Minute - (StartTime.Minute % 5);
                        break;
                    case FrameNodeLevel.Minute:
                        test = start.Date == StartTime.Date && start.Hour == StartTime.Hour && start.Minute == StartTime.Minute;
                        break;
                    case FrameNodeLevel.Second:
                        test = start == StartTime;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("level");
                }
                if (test)
                {
                    element = this;
                    return true;
                }
                return false;
            }

            if ((int)level > (int)Level && start <= EndTime && end >= StartTime) //check children
            {
                foreach (FrameElement child in Children)
                {
                    if (child.FindFrameElement(start, end, level, out element))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public virtual void Clear()
        {
            foreach (var node in Children)
            {
                node.Clear();
            }
            Children.Clear();
            Filled = false;
        }

        public virtual PacketCollection PacketCollection
        {
            get { return FileManager.GetPackets(StartTime, StartTime.Add(Duration)); }
        }

        public virtual long TotalData
        {
            get
            {
                if (_totalData == -1)
                    _totalData = FileManager.GetDataSpan(StartTime, StartTime.Add(Duration)); //store value for later
                return _totalData;
            }
        }

        public virtual long TotalPackets
        {
            get
            {
                if (_totalPackets == -1)
                    _totalPackets = FileManager.GetIndexSpan(StartTime, StartTime.Add(Duration));
                //store value for later
                return _totalPackets;
            }
        }

        public int CompareTo(FrameElement other)
        {
            if (StartTime.Equals(other.StartTime)) return Duration.CompareTo(other.Duration);
            return StartTime.CompareTo(other.StartTime);
        }
    }

    public class TimeFrameTree : FrameElement
    {
        private readonly int _filters;

        public TimeFrameTree(FileManager manager, int filters)
            : base(manager.StartTime, manager.Duration, manager, null)
        {
            _filters = filters;
        }

        /// <summary>
        /// Recursively scans tree for CanvasData at the specified render depth which intersects with the specified time period.
        /// Canvas Data is not pruned. All canvas data will be returned for a particular element, even if some of that data falls outside the time window.
        /// </summary>
        /// <param name="start">the start of the time period to scan</param>
        /// <param name="end">the end time of the periodd to scan</param>
        /// <param name="renderUnit">the unit size at which to render elements</param>
        /// <returns></returns>
        public CanvasData GetCanvasData(DateTime start, DateTime end, FrameNodeLevel renderUnit, FrameNodeLevel displayUnit)
        {
            Debug.Assert(renderUnit != FrameNodeLevel.Root); //ensure in range

            if (start < StartTime) start = StartTime;
            if (end > EndTime) end = EndTime;
            FileManager.CountCache.RequestCounts(start, end, renderUnit);

            List<FrameElement> elements = GenerateCanvasElements(start, end, renderUnit); //fill canvas with relevant elements

            FileManager.CountCache.CollectCounts(elements);

            var data = new CanvasData(start, end, renderUnit, displayUnit); //create a new, empty canvas

            foreach (var element in elements)
            {
                data.AddFrameElement(element);
            }

            return data;
        }

        public FrameElement GetFrameElement(DateTime start, FrameNodeLevel level)
        {
            FrameElement element;
            DateTime end;
            switch (level)
            {
                case FrameNodeLevel.Year:
                    if (SafeEquals(start, new DateTime(start.Year, 1, 1), level))
                    {
                        start = new DateTime(start.Year, 1, 1);
                    }
                    else start = new DateTime(start.Year + 1, 1, 1); //if error caused year value to decrease
                    end = start.AddYears(1);
                    break;
                case FrameNodeLevel.Month:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, 1), level))
                    {
                        start = new DateTime(start.Year, start.Month, 1);
                    }
                    else start = new DateTime(start.Year, start.Month, 1).AddMonths(1); //if error caused year value to decrease
                    end = start.AddMonths(1);
                    break;
                case FrameNodeLevel.Day:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, start.Day), level))
                    {
                        start = new DateTime(start.Year, start.Month, start.Day);
                    }
                    else start = new DateTime(start.Year, start.Month, start.Day).AddMonths(1); //if error caused year value to decrease
                    end = start.AddDays(1);
                    break;
                case FrameNodeLevel.Hour:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0), level))
                    {
                        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                    }
                    else start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0).AddHours(1); //if error caused year value to decrease
                    end = start.AddHours(1);
                    break;
                case FrameNodeLevel.PartHour:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute - (start.Minute%5), 0), level))
                    {
                        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute - (start.Minute % 5), 0);
                    }
                    else start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute - (start.Minute%5), 0).AddMinutes(5); //if error caused year value to decrease
                    end = start.AddMinutes(10);
                    break;
                case FrameNodeLevel.Minute:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0), level))
                    {
                        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
                    }
                    else start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0).AddMinutes(1); //if error caused year value to decrease
                    end = start.AddMinutes(1);
                    break;
                case FrameNodeLevel.Second:
                    if (SafeEquals(start, new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second), level))
                    {
                        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
                    }
                    else start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second).AddSeconds(1); //if error caused year value to decrease
                    end = start.AddSeconds(1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return FindFrameElement(start, end, level, out element) ? element : null;
        }


        private bool SafeEquals(DateTime one, DateTime two, FrameNodeLevel unitDifference)
        {
            long errorTolerance;
            switch (unitDifference)
            {
                case FrameNodeLevel.Year:
                case FrameNodeLevel.Month:
                    errorTolerance = new TimeSpan(1, 0, 0, 0).Ticks;
                    break;
                case FrameNodeLevel.Day:
                    errorTolerance = new TimeSpan(0, 1, 0, 0).Ticks;
                    break;
                case FrameNodeLevel.Hour:
                    errorTolerance = new TimeSpan(0, 0, 1, 0).Ticks;
                    break;
                case FrameNodeLevel.PartHour:
                    errorTolerance = new TimeSpan(0, 0, 0, 10).Ticks;
                    break;
                case FrameNodeLevel.Minute:
                    errorTolerance = new TimeSpan(0, 0, 0, 1).Ticks;
                    break;
                case FrameNodeLevel.Second:
                    errorTolerance = new TimeSpan(0, 0, 0, 0, 100).Ticks;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unitDifference");
            }

            return (Math.Abs(one.Ticks - two.Ticks) < errorTolerance);
        }

        /// <summary>
        /// Returns every packet in the capture as a list of packet items.
        /// Warning: Expensive (or "priceless") on large captures. Only use if capture is small enough to easily fit in host memory.
        /// </summary>
        public override PacketCollection PacketCollection
        {
            get { return FileManager.GetPackets(0, (int)FileManager.TotalPackets); }
        }

        /// <summary>
        /// The total number of packets in the capture
        /// </summary>
        public override long TotalPackets
        {
            get { return FileManager.TotalPackets; }
        }

        /// <summary>
        /// the total amount of packet data in the capture (does not include PCAP headers)
        /// </summary>
        public override long TotalData
        {
            get { return FileManager.TotalData; }
        }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Root; }
        }
    }

    public class FrameNodeYear : FrameElement
    {
        public FrameNodeYear(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement root)
            : base(time, duration, fileManager, root)
        {
        }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Year; }
        }
    }

    public class FrameNodeMonth : FrameElement
    {
        public FrameNodeMonth(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement yearNode)
            : base(time, duration, fileManager, yearNode)
        {
        }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Month; }
        }
    }

    public class FrameNodeDay : FrameElement
    {
        public FrameNodeDay(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement monthNode) : base(time, duration, fileManager, monthNode) { }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Day; }
        }
    }

    public class FrameNodeHour : FrameElement
    {
        public FrameNodeHour(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement dayNode) : base(time, duration, fileManager, dayNode) { }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Hour; }
        }
    }

    public class FrameNodePartHour : FrameElement
    {
        public FrameNodePartHour(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement hourNode)
            : base(time, duration, fileManager, hourNode)
        {
        }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.PartHour; }
        }
    }

    public class FrameNodeMinute : FrameElement
    {
        public FrameNodeMinute(DateTime time, TimeSpan duration, FileManager fileManager, FrameElement hourNode) : base(time, duration, fileManager, hourNode) { }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Minute; }
        }
    }
    
    public class FrameNodeSecond : FrameElement
    {
        public FrameNodeSecond(DateTime time, FileManager fileManager, FrameElement minuteNode)
            : base(time, new TimeSpan(0, 0, 0, 1), fileManager, minuteNode)
        {
        }

        public override FrameNodeLevel Level
        {
            get { return FrameNodeLevel.Second; }
        }
    }
}