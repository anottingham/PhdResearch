using System;
using System.Collections.Generic;
using OpenTK;

namespace ViewTimeline.Graphs
{
    public enum TimeType
    {
        None = 0,
        SecondStart = 1,
        MinuteStart = 2,
        HourStart = 3,
        DayStart = 4,
        MonthStart = 5,
        YearStart = 6
    }

    public enum GraphScaleFunction
    {
        Maximum,
        AverageOver2
    }

    public enum GraphType
    {
        LineGraph,
        ScatterPlot,
        SolidGraph
    }

    public enum GraphScaleTarget
    {
        DataVolume,
        PacketCount,
        MatchingCount
    }

    public struct GraphScaleConfig
    {
        public GraphScaleFunction Function { get; private set; }
        public GraphScaleTarget Target { get; private set; }
        public int FilterIndex { get; private set; }

        public GraphScaleConfig(GraphScaleFunction function, GraphScaleTarget target, int filterIndex = 0) : this()
        {
            Function = function;
            Target = target;
            FilterIndex = filterIndex;
        }

        public float ScaledHeight(GraphScaleData scaleData)
        {
            switch (Function)
            {
                case GraphScaleFunction.Maximum:
                    switch (Target)
                    {
                        case GraphScaleTarget.DataVolume:
                            return scaleData.MaxData;
                        case GraphScaleTarget.PacketCount:
                            return scaleData.MaxCount;
                        case GraphScaleTarget.MatchingCount:
                            return scaleData.MaxMatchingCount[FilterIndex];
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case GraphScaleFunction.AverageOver2:
                    switch (Target)
                    {
                        case GraphScaleTarget.DataVolume:
                            return scaleData.AvgDataVolume;
                        case GraphScaleTarget.PacketCount:
                            return scaleData.AvgPacketCount;
                        case GraphScaleTarget.MatchingCount:
                            return scaleData.AvgMatchingCount(FilterIndex);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}