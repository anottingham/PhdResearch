using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using ClearBufferMask = OpenTK.Graphics.OpenGL.ClearBufferMask;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using GL = OpenTK.Graphics.OpenGL.GL;
using MatrixMode = OpenTK.Graphics.OpenGL.MatrixMode;
using Timer = System.Windows.Forms.Timer;

namespace ViewTimeline.Graphs
{
    public class CaptureGraph
    {
        private const float Interval = 500f;
        private bool _canvasReady;

        private GraphYAxis _yAxis;
        private GraphBackground _background;
        private GraphXAxis _xAxis;

        public GraphCanvas DataVolume { get; private set; }
        public GraphCanvas PacketCount { get; private set; }
        public List<GraphCanvas> MatchingCounts { get; private set; } 

        public CanvasCollection CanvasCollection { get; private set; }

        private Vector2 _positionOffset;
        private GraphScaleData _scaleData;
        private CanvasData _canvasData;
        private Matrix4 _projection;

        private int _transitionState;
        private Timer _transitionTimer;
        private int _transitionCounter;
        private CanvasData _transitionCanvas;

        public GLSLVertexBuffer<Vector2> Border { get; private set; }

        public bool MajorGridLines
        {
            get { return _xAxis.DrawMajorGridlines; }
            set { _xAxis.DrawMajorGridlines = value; }
        }

        public bool MinorGridLines
        {
            get { return _xAxis.DrawMinorGridlines; }
            set { _xAxis.DrawMinorGridlines = value; }
        }

        public MarkList MarkList
        {
            get { return _background.MarkList; }
        }

        //public Vector2 Bounds { get { return new Vector2(CanvasManager.TimeToTransformedOffset(CanvasManager.), 2); } }

        /// <summary>
        /// Returns the viewport transform of the Canvas Manager
        /// </summary>
        public Matrix4 ViewportTransform { get { return CanvasManager.ViewportTransform; } }

        /// <summary>
        /// Constructs an uninitialised CaptureGraph object, which can display multiple graph canvases simultaneously
        /// </summary>
        public CaptureGraph()
        {
            _canvasReady = false;
            _transitionState = 0;

            //set graph start offsets
            _positionOffset = new Vector2(-1, -1);

            //initialise timer
            _transitionTimer = new Timer();
            _transitionTimer.Interval = 40;
            _transitionTimer.Tick += TransitionTimerTick;

            //construct empty canvas collection
            CanvasCollection = new CanvasCollection();
            MatchingCounts = new List<GraphCanvas>();
        }

        /// <summary>
        /// Initialises the capture graph with the default graph collection
        /// </summary>
        public void Initialise()
        {
            //clear the canvas collection and crreate default graphs
            CanvasCollection.Clear();
            DataVolume = new GraphCanvas("Traffic Volume", CanvasManager.SimpleShader2D) { Color = Color4.Firebrick };
            PacketCount = new GraphCanvas("Packet Count", CanvasManager.SimpleShader2D) { Color = Color4.DarkSlateBlue };

            MatchingCounts.Clear();

            var colors = FilterColors(CanvasManager.FileManager.CountCache.FilterFiles.Count);

            for (int k = 0; k < CanvasManager.FileManager.CountCache.FilterFiles.Count; k++)
            {
                var filter = CanvasManager.FileManager.CountCache.FilterFiles[k];
                string name = filter.Filename;
                name = name.Substring(name.LastIndexOf('\\') + 1, name.LastIndexOf('.') - name.LastIndexOf('\\') - 1);
                MatchingCounts.Add(new GraphCanvas(name, CanvasManager.SimpleShader2D)
                {
                    Color = colors[k],
                    ScaleFunction = new GraphScaleConfig(GraphScaleFunction.Maximum, GraphScaleTarget.PacketCount),
                    GraphType = GraphType.SolidGraph
                });
            }

            //set the shaders for the axis and background ui
            _yAxis = new GraphYAxis(CanvasManager.SimpleShader2D);
            _background = new GraphBackground(CanvasManager.SimpleShader2D);
            _xAxis = new GraphXAxis(CanvasManager.SimpleShader2D);

            //initialise the border
            Border = new GLSLVertexBuffer<Vector2>();
            Border.SetAttributeInfo(CanvasManager.SimpleShader2D.AttributeCoord2D, 2);

            //set the default graph types
            DataVolume.GraphType = GraphType.LineGraph;
            PacketCount.GraphType = GraphType.SolidGraph;

            //sett eh default grraph scale funtions
            DataVolume.ScaleFunction = new GraphScaleConfig(GraphScaleFunction.Maximum, GraphScaleTarget.DataVolume);
            PacketCount.ScaleFunction = new GraphScaleConfig(GraphScaleFunction.Maximum, GraphScaleTarget.PacketCount);

            //add the default graphs to the canvas collection
            CanvasCollection.Add(PacketCount);

            foreach (GraphCanvas canvas in MatchingCounts)
            {
                CanvasCollection.Add(canvas);
            }

            CanvasCollection.Add(DataVolume);
        }

        public List<Color4> FilterColors(int count)
        {
            List<Color4> colors = new List<Color4>();

            colors.Add(Color4.LightSeaGreen);
            colors.Add(Color4.MediumSeaGreen);
            colors.Add(Color4.DarkOrange);
            colors.Add(Color4.LightCoral);
            colors.Add(Color4.DarkRed);
            colors.Add(Color4.DarkOrchid);
            colors.Add(Color4.SlateBlue);
            colors.Add(Color4.CadetBlue);

            for (int k = 8; k < count; k++)
            {
                colors.Add(colors[k - 8]);
            }

            return colors;
        }
        

        public void SetCanvasData(CanvasData canvasData)
        {
            Border.BindData(new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) });
            DataVolume.BindData(canvasData.DataVolume);
            PacketCount.BindData(canvasData.PacketCount);

            for (var k = 0; k < MatchingCounts.Count; k++)
            {
                MatchingCounts[k].BindData(canvasData.MatchingCount[k]);
            }

            _canvasReady = true;

            if (_canvasData != null)
            {
                _transitionCanvas = canvasData;
                StartTransition();
            }
            else
            {
                _xAxis.Generate(canvasData);
                _yAxis.GenerateAxis(canvasData.ScaleData.MaxData);
                _scaleData = canvasData.ScaleData;
                _canvasData = canvasData;
            }
        }

        public void StartTransition()
        {
            if (_transitionCanvas.StartTime == _canvasData.StartTime && _transitionCanvas.EndTime == _canvasData.EndTime)
            {
                _transitionState = 2;
            }
            else _transitionState = 1;
            HideUI();
            _transitionCounter = 0;
            _transitionTimer.Start();
            SetMousePosition(Vector2.Zero);
        }

        private void TransitionTimerTick(object sender, EventArgs e)
        {
            _transitionCounter += _transitionTimer.Interval;
            if (_transitionCounter >= Interval)
            {
                _transitionCounter = 0;
                _transitionState++;
                if (_transitionState == 2)
                {
                    _xAxis.Generate(_transitionCanvas);
                    _yAxis.GenerateAxis(_transitionCanvas.ScaleData.MaxData);
                }
                if (_transitionState == 3)
                {
                    //reset timer info
                    _transitionTimer.Stop();

                    //switch canvas data
                    _canvasData = _transitionCanvas;
                    _scaleData = _canvasData.ScaleData;
                    _transitionCanvas = null;

                    ////swap buffers in graphs
                    CanvasCollection.SwapBuffers();
                    ////reset alpha blend
                    CanvasCollection.AlphaBlend(1f);
                    _transitionState = 0;
                    ShowUI();
                }
            }
        }

        public void Clear()
        {
            _canvasReady = false;
        }

        public void MarkSelection(FrameElement selection)
        {
            //DateTime start = new DateTime(selection.StartTime.Ticks, DateTimeKind.Utc);
            //Everything is UTC - change to allow for different time zones
            _background.AddSelectionMark(selection.StartTime, selection.EndTime);
        }

        public void ClearSelectionMarks()
        {
            _background.ClearSelectionMarks();
        }

        public void UndoClearSelectionMarks()
        {
            _background.UndoClearSelectionMarks();
        }

        public FrameElement SetMousePosition(Vector2 mousePosition)
        {
            var boundaries = _xAxis.MinorOffsets;
            var location =
                CanvasManager.UnProject(
                Matrix4.CreateTranslation(_scaleData.XShift, 0, 0) *
                    Matrix4.Scale(2f / _scaleData.BaseScale, 1f, 1f) * Matrix4.CreateTranslation(-1f, -1f, 0) *
                    CanvasManager.ViewportTransform, mousePosition);

            float index = -1;
            if (location.Y <= 2f && location.Y >= 0f)
            {
                for (int k = 1; k < boundaries.Count; k++)
                {
                    if (boundaries[k] > 0 && boundaries[k] > location.X)
                    {
                        _background.SelectRegion(boundaries[0], boundaries[k - 1], boundaries[k]);

                        return CanvasManager.FindFrameElement(boundaries[k - 1], _xAxis.MinorTickLevel);
                    }
                }
            }
            return null;
        }

        public void DrillLocation()
        {
            CanvasManager.Drill(_background.StartPosition, _background.EndPosition);
            SetCanvasData(CanvasManager.CurrentCanvasData);
        }

        //public void AdjustPosition(Vector2 adjustment)
        //{
        //    _positionOffset.X += adjustment.X * 1f / _scale.X;
        //    _positionOffset.Y += adjustment.Y * 1f / _scale.Y;
        //    _background.ShiftPosition(adjustment.X / _scale.X);
        //}

        //public void AdjustScale(Vector2 scalePercent)
        //{
        //    _scale.X *= scalePercent.X;
        //    _scale.Y *= scalePercent.Y;
        //}

        //public void ResetView()
        //{
        //    _scale = Vector2.One;
        //    _positionOffset = new Vector2(-1, -1);
        //}

        /// <summary>
        /// Deactivates User Interface elements (cursor animations etc) in order to produce graph image files.
        /// </summary>
        public void HideUI()
        {
            _background.Enabled = false;
        }

        /// <summary>
        /// Reactivates User Interface after taking a screen capture.
        /// </summary>
        public void ShowUI()
        {
            _background.Enabled = true;
        }

        public void DrawGraph()
        {
            if (!_canvasReady) return;

            CanvasManager.SimpleShader2D.UseProgram();
            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Viewport((int)CanvasManager.CanvasLocationX, (int)CanvasManager.CanvasLocationY, (int)CanvasManager.CanvasWidth, (int)CanvasManager.CanvasHeight);
            GL.Scissor((int)CanvasManager.CanvasLocationX, (int)CanvasManager.CanvasLocationY, (int)CanvasManager.CanvasWidth, (int)CanvasManager.CanvasHeight);
            GL.Enable(EnableCap.ScissorTest);

            var scale = new GraphScaleData(_scaleData);

            if (_transitionState == 1) //reposition
            {
                float factor = _transitionCounter / Interval;
                //x difference
                scale.XShift = _scaleData.XShift + (_transitionCanvas.ScaleData.XShift - _scaleData.XShift) * factor;
                //scale difference
                scale.BaseScale = _scaleData.BaseScale + (_transitionCanvas.ScaleData.BaseScale - _scaleData.BaseScale) * factor;
            }
            else if (_transitionState == 2)
            {
                scale.XShift = _transitionCanvas.ScaleData.XShift;
                scale.BaseScale = _transitionCanvas.ScaleData.BaseScale;
            }
            else
            {
                scale.XShift = _scaleData.XShift;
                scale.BaseScale = _scaleData.BaseScale;
            }

            if (_transitionState == 2) //blend colors
            {
                //DataVolume.AlphaBlend = 1 - _transitionCounter / Interval;
                //PacketCount.AlphaBlend = 1 - _transitionCounter / Interval;
                CanvasCollection.AlphaBlend(1 - _transitionCounter / Interval);
            }

            _background.Draw(scale.XShift);

            ////draw data volume
            //DataVolume.Draw(scale);
            ////draw packet count
            //PacketCount.Draw(scale);
            //
            CanvasCollection.DrawCollection(scale);

            if (_transitionState == 2)
            {
                //DataVolume.DrawBackBuffer(scale.BaseScale, _transitionCanvas.ScaleData.MaxData, scale.XShift);
                //PacketCount.DrawBackBuffer(scale.BaseScale, _transitionCanvas.ScaleData.MaxCount, scale.XShift);
                CanvasCollection.DrawCollectionBackBuffer(scale);
            }

            //////reset viewport
            GL.Viewport(0, 0, CanvasManager.ControlWidth, CanvasManager.ControlHeight);
            GL.Disable(EnableCap.ScissorTest);

            Matrix4 transform = CanvasManager.ViewportTransform;
            GL.UniformMatrix4(CanvasManager.SimpleShader2D.UniformTransform, false, ref transform);
            GL.Uniform4(CanvasManager.SimpleShader2D.UniformColor, Color4.Black);
            GL.LineWidth(2f);

            Border.BeginDraw();
            Border.Draw(BeginMode.LineLoop);
            Border.EndDraw();

            //transform.Invert();
            //float left = -(transform.M11 - transform.M41);
            //float right = transform.M11 + transform.M41;
            _yAxis.Draw(_scaleData.MaxData);
            _xAxis.Draw(scale.XShift, scale.BaseScale);
        }

        public event GraphControl.SelectionChangedEventHandler SelectionChanged;
    }
}