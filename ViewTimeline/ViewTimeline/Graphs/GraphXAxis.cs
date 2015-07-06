using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using GL = OpenTK.Graphics.OpenGL.GL;

namespace ViewTimeline.Graphs
{
    public class GraphXAxis
    {
        private readonly SimpleShader2D _shader;

        private readonly GLSLVertexBuffer<Vector2> _tickMajorBuffer;
        private readonly GLSLVertexBuffer<Vector2> _tickMinorBuffer;
        private readonly GLSLVertexBuffer<Vector2> _gridMajorBuffer;
        private readonly GLSLVertexBuffer<Vector2> _gridMinorBuffer;
        private readonly int _uniformTransform;
        private readonly int _uniformColor;

        private Vector2[] _majorTick;
        private Vector2[] _majorGrid;
        private Vector2[] _minorTick;
        private Vector2[] _minorGrid;

        public List<float> MajorOffsets { get; private set; }

        public List<float> MinorOffsets { get; private set; }

        public Vector2[] TickMajor { get { return _majorTick; } }

        public Color4 TickColor { get; set; }

        public Color4 GridColor { get; set; }

        public bool DrawMinorTicks { get; set; }

        public float MinorTickScale { get; set; }

        public bool DrawMajorGridlines { get; set; }

        public bool DrawMinorGridlines { get; set; }

        public FrameNodeLevel MinorTickLevel { get; private set; }

        public GraphXAxis(SimpleShader2D shaderProgram)
        {
            _shader = shaderProgram;
            _uniformTransform = shaderProgram.UniformTransform;
            _uniformColor = shaderProgram.UniformColor;

            DrawMinorTicks = true;
            MinorTickScale = 0.5f;

            TickColor = Color4.Black;
            GridColor = Color4.LightGray;

            DrawMajorGridlines = false;
            DrawMinorGridlines = false;

            _tickMajorBuffer = new GLSLVertexBuffer<Vector2>();
            _tickMinorBuffer = new GLSLVertexBuffer<Vector2>();
            _gridMajorBuffer = new GLSLVertexBuffer<Vector2>();
            _gridMinorBuffer = new GLSLVertexBuffer<Vector2>();

            _tickMajorBuffer.SetAttributeInfo(_shader.AttributeCoord2D, 2);
            _tickMinorBuffer.SetAttributeInfo(_shader.AttributeCoord2D, 2);
            _gridMajorBuffer.SetAttributeInfo(_shader.AttributeCoord2D, 2);
            _gridMinorBuffer.SetAttributeInfo(_shader.AttributeCoord2D, 2);

            MajorOffsets = new List<float>();
            MinorOffsets = new List<float>();
        }

        public void Generate(CanvasData data)
        {
            Debug.Assert(data.RenderUnit != FrameNodeLevel.Root); //cannot render at the root level

            var start = data.StartTime;
            var end = data.EndTime;

            MajorOffsets.Clear();
            MinorOffsets.Clear();

            DateTime currMajor, currMinor, tmp;

            switch (data.RenderUnit)
            {
                case FrameNodeLevel.Year:
                case FrameNodeLevel.Month:
                    currMinor = new DateTime(start.Year, 1, 1);
                    tmp = new DateTime(end.Year, 1, 1);
                    if (end != tmp) end = tmp.AddYears(1);

                    while (currMinor <= end)
                    {
                        MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                        currMinor = currMinor.AddYears(1);
                    }
                    MinorTickLevel = FrameNodeLevel.Year;
                    break;

                case FrameNodeLevel.Day:
                    currMajor = new DateTime(start.Year, 1, 1);
                    currMinor = currMajor; // end of first month
                    tmp = new DateTime(end.Year, 1, 1);
                    if (end != tmp) end = tmp.AddYears(1);

                    while (currMajor <= end)
                    {
                        MajorOffsets.Add(CanvasManager.TimeToOffset(currMajor));
                        currMajor = currMajor.AddYears(1);
                        while (currMinor < currMajor)
                        {
                            MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                            currMinor = currMinor.AddMonths(1);
                        }
                    }
                    MinorTickLevel = FrameNodeLevel.Month;
                    break;
                case FrameNodeLevel.Hour:
                    currMajor = new DateTime(start.Year, start.Month, 1);
                    currMinor = currMajor; // 2nd day
                    tmp = new DateTime(end.Year, end.Month, 1);
                    if (end != tmp) end = tmp.AddMonths(1);

                    while (currMajor <= end)
                    {
                        MajorOffsets.Add(CanvasManager.TimeToOffset(currMajor));
                        currMajor = currMajor.AddMonths(1);
                        while (currMinor < currMajor)
                        {
                            MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                            currMinor = currMinor.AddDays(1);
                        }
                    }
                    MinorTickLevel = FrameNodeLevel.Day;
                    break;
                case FrameNodeLevel.PartHour:
                    currMajor = new DateTime(start.Year, start.Month, start.Day);
                    currMinor = currMajor; // 2nd day
                    tmp = new DateTime(end.Year, end.Month, end.Day);
                    if (end != tmp) end = tmp.AddDays(1);

                    while (currMajor <= end)
                    {
                        MajorOffsets.Add(CanvasManager.TimeToOffset(currMajor));
                        currMajor = currMajor.AddDays(1);
                        while (currMinor < currMajor)
                        {
                            MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                            currMinor = currMinor.AddHours(1);
                        }
                    }
                    MinorTickLevel = FrameNodeLevel.Hour;
                    break;

                case FrameNodeLevel.Minute:
                    currMajor = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                    currMinor = currMajor; // 2nd day
                    tmp = new DateTime(end.Year, end.Month, end.Day, start.Hour, 0, 0);
                    if (end != tmp) end = tmp.AddHours(1);

                    while (currMajor <= end)
                    {
                        MajorOffsets.Add(CanvasManager.TimeToOffset(currMajor));
                        currMajor = currMajor.AddHours(1);
                        while (currMinor < currMajor)
                        {
                            MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                            currMinor = currMinor.AddMinutes(10);
                        }
                    }
                    MinorTickLevel = FrameNodeLevel.PartHour;
                    break;
                case FrameNodeLevel.Second:
                    currMajor = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute - (start.Minute%5), 0);
                    currMinor = currMajor; // 2nd day
                    tmp = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute - (start.Minute % 5), 0);
                    if (end != tmp) end = tmp.AddMinutes(5);

                    while (currMajor <= end)
                    {
                        MajorOffsets.Add(CanvasManager.TimeToOffset(currMajor));
                        currMajor = data.DisplayUnit == FrameNodeLevel.Minute ? currMajor.AddMinutes(1) : currMajor.AddMinutes(5);
                        while (currMinor < currMajor)
                        {
                            MinorOffsets.Add(CanvasManager.TimeToOffset(currMinor));
                            currMinor = data.DisplayUnit == FrameNodeLevel.Minute ? currMinor.AddSeconds(1) : currMinor.AddMinutes(1);
                        }
                    }
                    MinorTickLevel = FrameNodeLevel.Minute;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("axisMajorLevel");
            }

            //generate major axis
            if (MajorOffsets.Count > 0)
            {
                _majorTick = new Vector2[MajorOffsets.Count * 2];
                _majorGrid = new Vector2[MajorOffsets.Count * 2];

                for (int k = 0; k < MajorOffsets.Count; k++)
                {
                    _majorTick[k * 2] = new Vector2(MajorOffsets[k], 0);
                    _majorGrid[k * 2] = new Vector2(MajorOffsets[k], 0);

                    _majorTick[k * 2 + 1] = new Vector2(MajorOffsets[k], -CanvasManager.TickSize * 2 * CanvasManager.PixelSize.X);
                    _majorGrid[k * 2 + 1] = new Vector2(MajorOffsets[k], 2);
                }

                _tickMajorBuffer.BindData(_majorTick);
                _gridMajorBuffer.BindData(_majorGrid);
            }
            else
            {
                _majorTick = null;
            }

            //generate minor axis if possible
            if (MinorOffsets.Count > 0)
            {
                _minorTick = new Vector2[MinorOffsets.Count * 2];
                _minorGrid = new Vector2[MinorOffsets.Count * 2];

                for (int k = 0; k < MinorOffsets.Count; k++)
                {
                    _minorTick[k * 2] = new Vector2(MinorOffsets[k], 0);
                    _minorGrid[k * 2] = new Vector2(MinorOffsets[k], 0);

                    _minorTick[k * 2 + 1] = new Vector2(MinorOffsets[k],
                                                        -(CanvasManager.TickSize * 2) * MinorTickScale * CanvasManager.PixelSize.X);
                    _minorGrid[k * 2 + 1] = new Vector2(MinorOffsets[k], 2);
                }
                _tickMinorBuffer.BindData(_minorTick);
                _gridMinorBuffer.BindData(_minorGrid);
            }
            else _minorTick = null;
        }

        public void Draw(float xShift, float baseScale)
        {
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor((int)(CanvasManager.CanvasLocationX - 1), 0,
                       (int)(CanvasManager.CanvasWidth + 2),
                       CanvasManager.ControlHeight);

            double scale = 2.0 / (double)baseScale;
            Matrix4 transform = Matrix4.CreateTranslation(xShift, 0, 0) * Matrix4.Scale((float)scale/*2f / baseScale*/, 1f, 1f) * Matrix4.CreateTranslation(-1f, -1f, 0) * CanvasManager.ViewportTransform;

            GL.UniformMatrix4(_uniformTransform, false, ref transform);
            GL.Uniform4(_uniformColor, GridColor);

            if (DrawMajorGridlines)
            {
                GL.LineWidth(1.25f);
                _gridMajorBuffer.BeginDraw();
                _gridMajorBuffer.Draw(BeginMode.Lines);
                _gridMajorBuffer.EndDraw();
            }
            if (DrawMinorGridlines && _minorGrid != null)
            {
                GL.LineWidth(0.75f);
                _gridMinorBuffer.BeginDraw();
                _gridMinorBuffer.Draw(BeginMode.Lines);
                _gridMinorBuffer.EndDraw();
            }

            if (_majorTick != null)
            {
                GL.LineWidth(2f);
                GL.Uniform4(_uniformColor, TickColor);

                _tickMajorBuffer.BeginDraw();
                _tickMajorBuffer.Draw(BeginMode.Lines);
                _tickMajorBuffer.EndDraw();
            }

            if (DrawMinorTicks && _minorTick != null)
            {
                //transform = Matrix4.Scale(1, MinorTickScale, 1) * transform; //shift x down for ticks
                GL.UniformMatrix4(_uniformTransform, false, ref transform);

                GL.LineWidth(1.5f);
                _tickMinorBuffer.BeginDraw();
                _tickMinorBuffer.Draw(BeginMode.Lines);
                _tickMinorBuffer.EndDraw();
            }
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}