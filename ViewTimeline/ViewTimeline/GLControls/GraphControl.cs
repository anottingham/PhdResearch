using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.Graphs;
using BlendingFactorDest = OpenTK.Graphics.OpenGL.BlendingFactorDest;
using BlendingFactorSrc = OpenTK.Graphics.OpenGL.BlendingFactorSrc;
using ClearBufferMask = OpenTK.Graphics.OpenGL.ClearBufferMask;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using GL = OpenTK.Graphics.OpenGL.GL;
using HintMode = OpenTK.Graphics.OpenGL.HintMode;
using HintTarget = OpenTK.Graphics.OpenGL.HintTarget;
using MatrixMode = OpenTK.Graphics.OpenGL.MatrixMode;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL.PixelType;
using ShadingModel = OpenTK.Graphics.OpenGL.ShadingModel;

namespace ViewTimeline.GLControls
{
    public partial class GraphControl : UserControl
    {
        public delegate void SelectionChangedEventHandler(FrameElement newSelection);
        public event SelectionChangedEventHandler SelectionChanged;

        public FrameElement _currentSelection;

        private bool _loaded;
        private bool _bound;
        private Matrix4 _projection = Matrix4.Identity;

        private CaptureGraph _graph;
        private bool _screenCap;
        private string _screenCapFile;
        private bool _sleep;
        public CaptureGraph CaptureGraph { get { return _graph; } }

        public GraphControl()
        {
            _loaded = false;
            _bound = false;
            _sleep = false;
            this.Resize += GraphControlResize;
            SuspendLayout();
            this.glControl = new OpenTK.GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, 16));
            glControl.Load += GlControlLoad;
            glControl.Paint += GlControlPaint;
            glControl.PreviewKeyDown += GlControlPreviewKeyDown;
            glControl.MouseMove += new MouseEventHandler(GlControlMouseMove);
            glControl.DoubleClick += new EventHandler(GlControlDoubleClick);
            glControl.Size = this.Size;
            glControl.Location = Point.Empty;

            this.Controls.Add(glControl);
            glControl.MakeCurrent();
            ResumeLayout();

            InitializeComponent();
            _screenCap = false;
            _currentSelection = null;
        }

        private void GlControlLoad(object sender, EventArgs e)
        {
            GL.ClearColor(1, 1, 1, 1);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            _graph = new CaptureGraph();
            _loaded = true;
        }

        public void Initialise()//string captureFile, string indexFile, string timeFile)
        {

            _graph.Initialise();
            _graph.SetCanvasData(CanvasManager.CurrentCanvasData);
            _bound = true;
            renderTimer.Start();
            glControl.Invalidate();
        }

        private void GlControlPaint(object sender, PaintEventArgs e)
        {
            if (!_loaded)
            {
                GL.ClearColor(Color4.LightSteelBlue);
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }

            else if (!_bound)
            {
                GL.ClearColor(Color4.SteelBlue);
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }

            else if (_screenCap)
            {
                _screenCap = false;
                SuspendLayout();

                _graph.HideUI();
                Size tmp = glControl.Size;
                glControl.Size = new Size(2000, 750);
                CanvasManager.ChangeViewportSize(glControl.Size);

                GL.ClearColor(Color4.White);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                _graph.DrawGraph();

                Bitmap bmp = new Bitmap(glControl.Width, this.glControl.Height);
                BitmapData data =
                    bmp.LockBits(glControl.ClientRectangle, ImageLockMode.WriteOnly,
                                 System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                GL.Finish();
                GL.ReadPixels(0, 0, this.glControl.Width, this.glControl.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bmp.Save(_screenCapFile, ImageFormat.Png);

                glControl.Size = tmp;
                CanvasManager.ChangeViewportSize(glControl.Size);
                _graph.ShowUI();

                ResumeLayout();
            }
            else
            {
                GL.ClearColor(Color4.White);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                _graph.DrawGraph();
            }
            glControl.SwapBuffers();
        }

        public void CaptureImage(string filename)
        {
            _screenCap = true;
            _screenCapFile = filename;
        }

        private void RenderTimerTick(object sender, EventArgs e)
        {
            glControl.Invalidate();
        }

        private void GlControlPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Add)
            {
                CanvasManager.IncreaseDrawDepth();
                _graph.SetCanvasData(CanvasManager.CurrentCanvasData);
            }
            if (e.KeyCode == Keys.Subtract)
            {
                CanvasManager.ReduceDrawDepth();
                _graph.SetCanvasData(CanvasManager.CurrentCanvasData);
            }
            if (e.KeyCode == Keys.Back)
            {
                CanvasManager.UndoDrill();
                _graph.SetCanvasData(CanvasManager.CurrentCanvasData);
            }
            if (e.KeyCode == Keys.M)
            {
                _graph.MarkSelection(_currentSelection);
            }
            if (e.KeyCode == Keys.Escape)
            {
                _graph.ClearSelectionMarks();
            }
            if (e.KeyCode == Keys.Z && e.Control)
            {
                _graph.UndoClearSelectionMarks();
            }
            if (e.KeyCode == Keys.F1)
            {
                string str = "";
                str += "Mouse\n--------\n";
                str += "Double Click\t-\tExpand highighted region to fill screen\n\n";
                str += "Keyboard Keys\n----------------\n";
                str += "BackSpace\t-\tContract screen back one layer\n";
                str += "'+'\t\t-\tIncrease Draw Depth\n";
                str += "'-'\t\t-\tReduce Draw Depth\n";
                str += "'m'\t\t-\tMark highlighted region for capture distillation\n";
                str += "Esc\t\t-\t Clear all selection marks\n";
                MessageBox.Show(str,"Help - Commands",MessageBoxButtons.OK);
            }
            //else
            //{
            //    Vector2 adjustment = e.Shift ? Vector2.One : Vector2.Zero;

            //    if (e.KeyCode == Keys.Left)
            //    {
            //        if (e.Shift) adjustment.X /= 1.2f;
            //        else adjustment.X += 0.1f;
            //    }
            //    if (e.KeyCode == Keys.Right)
            //    {
            //        if (e.Shift) adjustment.X *= 1.2f;
            //        else adjustment.X -= 0.1f;
            //    }
            //    if (e.KeyCode == Keys.Down)
            //    {
            //        if (e.Shift) adjustment.Y /= 1.2f;
            //        else adjustment.Y += 0.1f;
            //    }
            //    if (e.KeyCode == Keys.Up)
            //    {
            //        if (e.Shift) adjustment.Y *= 1.2f;
            //        else adjustment.Y -= 0.1f;
            //    }

            //    if (e.Shift) _graph.AdjustScale(adjustment);
            //    else _graph.AdjustPosition(adjustment);
            //}

            glControl.Invalidate();
        }

        private void GlControlMouseMove(object sender, MouseEventArgs e)
        {
            if (_loaded && _bound)
            {
                FrameElement tmp = _graph.SetMousePosition(new Vector2(e.X, e.Y));
                if (tmp != null && tmp != _currentSelection)
                {
                    _currentSelection = tmp;
                    if (SelectionChanged != null) SelectionChanged(_currentSelection);
                }
                glControl.Invalidate();
            }
        }

        private void GlControlDoubleClick(object sender, EventArgs e)
        {
            if (_loaded && _bound)
            {
                _graph.DrillLocation();
                glControl.Invalidate();
            }
            glControl.Invalidate();
        }

        public MarkList SelectionMarks { get { return CaptureGraph.MarkList; } }

        private void GraphControlResize(object sender, EventArgs e)
        {
            glControl.Size = this.Size;
            CanvasManager.ChangeViewportSize(glControl.Size);
        }

        //public static Vector4 UnProject(ref Matrix4 projection, Matrix4 view, Size viewport, Vector2 mouse)
        //{
        //    Vector4 vec;

        //    vec.X = 2.0f * mouse.X / (float)viewport.Width - 1;
        //    vec.Y = -(2.0f * mouse.Y / (float)viewport.Height - 1);
        //    vec.Z = 0;
        //    vec.W = 1.0f;

        //    Matrix4 viewInv = Matrix4.Invert(view);
        //    Matrix4 projInv = Matrix4.Invert(projection);

        //    Vector4.Transform(ref vec, ref projInv, out vec);
        //    Vector4.Transform(ref vec, ref viewInv, out vec);

        //    if (vec.W > float.Epsilon || vec.W < float.Epsilon)
        //    {
        //        vec.X /= vec.W;
        //        vec.Y /= vec.W;
        //        vec.Z /= vec.W;
        //    }

        //    return vec;
        //}
    }
}