using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using OpenTK;

namespace ViewTimeline.Graphs
{
    /// <summary>
    /// Container for graph vertices and axis information
    /// </summary>
    public class CanvasData
    {
        /// <summary>
        /// The level at which the canvas is displayed - default is Root
        /// </summary>
        public FrameNodeLevel DisplayUnit { get; private set; }

        /// <summary>
        /// The time at which the canvas view starts
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// The time at which the canvas view ends
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// the basic rendering unit
        /// </summary>
        public FrameNodeLevel RenderUnit { get; private set; }

        /// <summary>
        /// The string description of the canvas data view
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Sorts all the graphs based on the position of each element on the X axis
        /// Only required if frame elements are added out of order
        /// </summary>
        public void Sort()
        {
            DataVolume.Sort();
            PacketCount.Sort();
        }

        /// <summary>
        /// The graph data representing the packet count over time
        /// </summary>
        public GraphData PacketCount { get; private set; }

        /// <summary>
        /// The graph data representing the data volume over time
        /// </summary>
        public GraphData DataVolume { get; private set; }

        /// <summary>
        /// The graph data for each filter representing the match count over time
        /// </summary>
        public List<GraphData> MatchingCount { get; private set; }
        /// <summary>
        /// The scale data for the graphs
        /// </summary>
        public GraphScaleData ScaleData { get; private set; }

        /// <summary>
        /// Creates a new CanvasData object, based on user specifications
        /// </summary>
        /// <param name="startTime">The time at wwhich the canvas view should start</param>
        /// <param name="endTime">The time at wwhich the canvas view should end</param>
        /// <param name="renderUnit">The basic rendering unit to use</param>
        /// <param name="displayUnit">The display unit to use</param>
        /// <param name="filterCount"></param>
        public CanvasData(DateTime startTime, DateTime endTime, FrameNodeLevel renderUnit, FrameNodeLevel displayUnit)
        {
            DisplayUnit = displayUnit;
            StartTime = startTime;
            EndTime = endTime;
            RenderUnit = renderUnit;

            DataVolume = new GraphData();
            PacketCount = new GraphData();
            MatchingCount = new List<GraphData>();

            for (int k = 0; k < CanvasManager.FileManager.TotalFilters; k++)
            {
                MatchingCount.Add(new GraphData());
            }

            ScaleData = new GraphScaleData(displayUnit, startTime, endTime, CanvasManager.FileManager.TotalFilters);

            switch (displayUnit)
            {
                case FrameNodeLevel.Root:
                    Description = "Global View: " + startTime.ToString(CultureInfo.InvariantCulture) + " - " + endTime.ToString(CultureInfo.InvariantCulture);
                    break;
                case FrameNodeLevel.Year:
                    Description = "Year View: " + startTime.Year;
                    break;
                case FrameNodeLevel.Month:
                    Description = "Month View: " + MonthString(startTime.Month) + " " + startTime.Year;
                    break;
                case FrameNodeLevel.Day:
                    Description = "Day View: " + DayString(startTime.Day) + " " + MonthString(startTime.Month) + " " + startTime.Year;
                    break;
                case FrameNodeLevel.Hour:
                    Description = "Hour View: " + TimeString(startTime.Hour) + " - " + TimeString(endTime.Hour) + " " + DayString(startTime.Day) + " " + MonthString(startTime.Month) + " " + startTime.Year;
                    break;
                case FrameNodeLevel.PartHour:
                    Description = "Part Hour View: " + TimeString(startTime.Hour, startTime.Minute) + " - " + TimeString(endTime.Hour, endTime.Minute) + " " + DayString(startTime.Day) + " " + MonthString(startTime.Month) + " " + startTime.Year;
                    break;
                case FrameNodeLevel.Minute:
                    Description = "Minute View: " + TimeString(startTime.Hour, startTime.Minute) + " - " + TimeString(endTime.Hour, endTime.Minute) + " " + DayString(startTime.Day) + " " + MonthString(startTime.Month) + " " + startTime.Year;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("displayUnit");
            }
        }

        /// <summary>
        /// De-constructs the canvas data
        /// </summary>
        ~CanvasData()
        {
            DataVolume = null;
            PacketCount = null;
            ScaleData = null;
        }

        /// <summary>
        /// Returns the associated month string for a given integer value
        /// </summary>
        /// <param name="month">an interger between 1 and 12</param>
        /// <returns>the month name associated with the input integer</returns>
        private string MonthString(int month)
        {
            switch (month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
            }
            return "";
        }

        /// <summary>
        /// Returns the correct string representation for a given integer day value in a date
        /// </summary>
        /// <param name="day">the day of the month</param>
        /// <returns>the corrrect string representation for that date</returns>
        private string DayString(int day)
        {
            if (day == 1 || day == 21 || day == 31)
                return day + "st";
            if (day == 2 || day == 22) //nd
                return day + "nd";
            if (day == 3 || day == 23) //rd
                return day + "rd";
            return day + "th";
        }

        /// <summary>
        /// Returns the correct time string for an integer hour value and an optional minute value
        /// </summary>
        /// <param name="hour">the hour component of the time</param>
        /// <param name="minute">the minute component of the time - defaults to 0</param>
        /// <returns>the corrrect time string</returns>
        private string TimeString(int hour, int minute = 0)
        {
            string str = "";
            if (hour < 10) str += "0";
            str += hour + ":";
            if (minute < 10) str += "0";
            str += minute;
            return str;
        }

        /// <summary>
        /// Adds an individual frame element of the graph tree to the current graph vertex collections as a series of vertices
        /// </summary>
        /// <param name="element">the element to add to the canvas vertex data</param>
        public void AddFrameElement(FrameElement element)
        {
            //Add the vertex to the graphs
            DataVolume.VertexData.Add(new Vector2(element.Offset, element.TotalData));
            PacketCount.VertexData.Add(new Vector2(element.Offset, element.TotalPackets));

            for (int k = 0; k < MatchingCount.Count; k++)
            {
                MatchingCount[k].VertexData.Add(new Vector2(element.Offset, element.FilterCounts[k]));
            }

            //update the graph scale object to reflect the new vertices
            ScaleData.Update(element.TotalPackets, element.TotalData, element.FilterCounts);

            //Add axis zeros. These are necessary for filled graphs, and are skipped over when drawing line graphs.
            //This allows graph types to be swapped by simply modifying the shader attributes of the vertex buffer.
            //Shader attributes are automatically adjusted when the graph type attribute is changed.
            DataVolume.VertexData.Add(new Vector2(element.Offset, 0));
            PacketCount.VertexData.Add(new Vector2(element.Offset, 0));
            
            for (int k = 0; k < MatchingCount.Count; k++)
            {
                MatchingCount[k].VertexData.Add(new Vector2(element.Offset, 0));
            }
        }
    }

    public class GraphScaleData
    {
        private bool _empty; //indicates whether the scale data is empty

        /// <summary>
        /// The level at which the canvas is displayed - default is Root
        /// </summary>
        public FrameNodeLevel DisplayUnit { get; private set; }

        /// <summary>
        /// the total amount of data represented in the canvas
        /// </summary>
        public long TotalData { get; set; }

        /// <summary>
        /// The total number of packets represented in the canvas
        /// </summary>
        public long TotalPackets { get; set; }


        /// <summary>
        /// The total number of packets matching a particular filter
        /// </summary>
        public List<long> TotalMatchingPackets { get; private set; } 

        /// <summary>
        /// The smallest amount of data represented by a vertex
        /// </summary>
        public long MinData { get; set; }

        /// <summary>
        /// The largest amount of data represented by a vertex
        /// </summary>
        public long MaxData { get; set; }

        /// <summary>
        /// The smallest number of packets represented by a vertex
        /// </summary>
        public long MinCount { get; set; }

        /// <summary>
        /// The largest number of packets represented by a vertex
        /// </summary>
        public long MaxCount { get; set; }

        /// <summary>
        /// The smallest number of matching packets for a particular filter represented by a vertex
        /// </summary>
        public List<long> MinMatchingCount { get; private set; }

        /// <summary>
        /// The largest number of matching packets for a particular filter represented by a vertex
        /// </summary>
        public List<long> MaxMatchingCount { get; private set; }

        public int VertexCount { get; private set; }

        public float AvgDataVolume { get { return ((float)TotalData / VertexCount) * 2f; } }

        public float AvgPacketCount { get { return ((float)TotalPackets / VertexCount) * 2f; } }

        public float AvgMatchingCount(int filter) { return ((float)TotalMatchingPackets[filter] / VertexCount) * 2f; }

        /// <summary>
        /// The number of seconds covered by the viewport
        /// </summary>
        public float BaseScale { get; set; }

        /// <summary>
        /// The amount of shift from the origin applied to vertices to maintain a temporal structure.
        /// </summary>
        public float XShift { get; set; }

        /// <summary>
        /// Creates a replica of the passed GraphScaleData
        /// </summary>
        /// <param name="data">the scale data to replicate</param>
        public GraphScaleData(GraphScaleData data)
        {
            DisplayUnit = data.DisplayUnit;
            XShift = data.XShift;
            BaseScale = data.BaseScale;

            TotalData = data.TotalData;
            TotalPackets = data.TotalPackets;
            VertexCount = data.VertexCount;

            MinCount = data.MinCount;
            MaxCount = data.MaxCount;
            MinData = data.MinData;
            MaxData = data.MaxData;

            MinMatchingCount = new List<long>(data.MinMatchingCount);
            MaxMatchingCount = new List<long>(data.MaxMatchingCount);

        }

        /// <summary>
        /// Generates and stores the default scaling information for the current canvas
        /// </summary>
        /// <param name="displayUnit">the unit of time covered by the viewport at one time</param>
        /// <param name="startTime">the time at which the canvas starts</param>
        /// <param name="endTime">the time at which the canvas ends</param>
        public GraphScaleData(FrameNodeLevel displayUnit, DateTime startTime, DateTime endTime, int totalFilters = 0)
        {
            DisplayUnit = displayUnit;
            Debug.Assert(displayUnit != FrameNodeLevel.Second);
            DateTime spanStart;

            _empty = true; //graph is initially empty
            TimeSpan span;
            switch (displayUnit)
            {
                case FrameNodeLevel.Root:
                    span = endTime.Subtract(startTime);
                    spanStart = CanvasManager.FileManager.StartTime;
                    break;
                case FrameNodeLevel.Year:
                    spanStart = new DateTime(startTime.Year, 1, 1);
                    span = new DateTime(startTime.Year + 1, 1, 1).Subtract(spanStart);

                    break;
                case FrameNodeLevel.Month:
                    spanStart = new DateTime(startTime.Year, startTime.Month, 1);
                    span = spanStart.AddMonths(1).Subtract(spanStart);
                    break;
                case FrameNodeLevel.Day:
                    spanStart = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                    span = spanStart.AddDays(1).Subtract(spanStart);
                    break;
                case FrameNodeLevel.Hour:
                    spanStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);
                    span = spanStart.AddHours(1).Subtract(spanStart);
                    break;
                case FrameNodeLevel.PartHour:
                    spanStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour,
                                             startTime.Minute - (startTime.Minute%5), 0);
                    span = spanStart.AddMinutes(5).Subtract(spanStart);
                    break;
                case FrameNodeLevel.Minute:
                    spanStart = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour,
                                             startTime.Minute, 0);
                    span = spanStart.AddMinutes(1).Subtract(spanStart);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("displayUnit", displayUnit, "GraphScaleData Constructor: RenderUnit out of range. Seconds cannot be subdivided, and are not valid.");
            }
            XShift = (float)startTime.Subtract(spanStart).TotalSeconds;
            XShift -= CanvasManager.TimeToOffset(spanStart);
            BaseScale = (float)(span.TotalSeconds);
            TotalPackets = 0;
            TotalData = 0;
            VertexCount = 0;

            TotalMatchingPackets = new List<long>(totalFilters);
            MinMatchingCount = new List<long>(totalFilters);
            MaxMatchingCount = new List<long>(totalFilters);
            for (int k = 0; k < totalFilters; k++)
            {
                TotalMatchingPackets.Add(0);
                MinMatchingCount.Add(0);
                MaxMatchingCount.Add(0);
            }
        }

        /// <summary>
        /// Updates the scale data with additional vertex information derived from a frame element.
        /// </summary>
        /// <param name="count">the number of packets in the frame element</param>
        /// <param name="volume">the amount of data in the frame element</param>
        public void Update(long count, long volume, List<long> filterCounts )
        {
            VertexCount++;
            TotalPackets += count;
            TotalData += volume;

            for (int k = 0; k < filterCounts.Count; k++)
            {
                if (!_empty)
                {
                    TotalMatchingPackets[k] += filterCounts[k];
                    if (filterCounts[k] < MinMatchingCount[k]) MinMatchingCount[k] = filterCounts[k];
                    else if (filterCounts[k] > MaxMatchingCount[k]) MaxMatchingCount[k] = filterCounts[k];
                }
                else
                {
                    MinMatchingCount[k] = MaxMatchingCount[k] = filterCounts[k];
                    //no need to set _empty as will be reset by following method before loop reiterates
                }
            }

            if (!_empty)
            {
                if (count < MinCount) MinCount = count;
                else if (count > MaxCount) MaxCount = count;

                if (volume < MinData) MinData = volume;
                else if (volume > MaxData) MaxData = volume;
            }
            else //if scale data is empty, initialise
            {
                MinData = MaxData = volume;
                MinCount = MaxCount = count;
                _empty = false;
            }
        }
    }

    /// <summary>
    /// Contains the vertex data for a particular graph
    /// </summary>
    public class GraphData
    {
        /// <summary>
        /// creates an empty GraphData object
        /// </summary>
        public GraphData()
        {
            VertexData = new List<Vector2>();
        }

        /// <summary>
        /// Clears and destroys the object
        /// </summary>
        ~GraphData()
        {
            VertexData.Clear();
            VertexData = null;
        }

        /// <summary>
        /// Returns the number of vertices in the current graphdata object
        /// </summary>
        public long Total { get { return VertexData.Count; } }

        /// <summary>
        /// Returns the vertex data of the graph as a List of Vector2 objects
        /// </summary>
        public List<Vector2> VertexData { get; private set; }

        /// <summary>
        /// Sorts elements based on their position on the X axis.
        /// Only required if elements are added to the graph out of order.
        /// </summary>
        public void Sort()
        {
            VertexData.Sort(CompareXAxisPosition);
        }

        /// <summary>
        /// Compares two elements to indicate their relative position on the x axis
        /// </summary>
        /// <param name="a">the first elements position</param>
        /// <param name="b">the second elements position</param>
        /// <returns>an integer value indicating the elements relative position on the x axis</returns>
        private static int CompareXAxisPosition(Vector2 a, Vector2 b)
        {
            return a.X.CompareTo(b.X);
        }
    }
}