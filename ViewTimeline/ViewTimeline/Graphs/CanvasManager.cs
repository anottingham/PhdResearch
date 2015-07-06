using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using NetMQ;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using ViewTimeline.GLControls;

namespace ViewTimeline.Graphs
{
    public static class CanvasManager
    {
        public delegate void GraphChangedeventHandler();
        public static event GraphChangedeventHandler GraphChanged;
        
        public static ServerSocket ServerSocket { get; private set; }
        public static FileManager FileManager { get; private set; }

        public static TimeFrameTree TimeTree { get; private set; }

        public static SimpleShader2D SimpleShader2D { get; private set; }

        public static bool Initialised { get; set; }

        public static int SelectedGpuIndex { get; set; }

        //graph canvas properties
        public static FrameNodeLevel MajorTickDepth { get; set; }

        public static FrameNodeLevel RenderUnit { get; set; }

        public static FrameNodeLevel CurrentDepth { get; private set; }

        private static CanvasData _currentCanvasData;

        public static CanvasData CurrentCanvasData
        {
            get { return _currentCanvasData; }
            private set
            {
                _currentCanvasData = value;
                if (GraphChanged != null) GraphChanged();
            }
        }

        public static Vector2 PixelSize { get; private set; }

        public static Matrix4 ViewportTransform { get; private set; }

        public static int TextSize { get; set; }

        public static int BorderSize { get; set; }

        public static int TickSize { get; set; }

        /// <summary>
        /// The width of the glControl in pixels
        /// </summary>
        public static int ControlWidth { get; private set; }

        /// <summary>
        /// The height of the glControl in pixels
        /// </summary>
        public static int ControlHeight { get; private set; }

        public static float CanvasWidth { get { return ControlWidth - 2 * (BorderSize + TickSize) - TextSize; } }

        public static float CanvasHeight { get { return ControlHeight - 2 * (BorderSize + TickSize); } }

        public static float CanvasLocationX { get { return BorderSize + TickSize + TextSize; } }

        public static float CanvasLocationY { get { return BorderSize + TickSize; } }

        public static Stack<CanvasData> UndoStack { get; private set; }

        private static Matrix4 _projection;

        static CanvasManager()
        {
            Initialised = false;
            MajorTickDepth = FrameNodeLevel.Month;
            RenderUnit = FrameNodeLevel.Day;
            CurrentDepth = FrameNodeLevel.Root;
            SelectedGpuIndex = 0;
            PixelSize = Vector2.One;
            ViewportTransform = Matrix4.Identity;

            UndoStack = new Stack<CanvasData>();
        }

        public static void Initialise(ViewFormSetup setup, ViewForm ui)
        {
            //DateTime begin = DateTime.Now;
            Size viewportSize = ui.ViewPortSize;

            SimpleShader2D = new SimpleShader2D();
            ControlWidth = viewportSize.Width;
            ControlHeight = viewportSize.Height;
            BorderSize = 20;
            TickSize = 10;
            TextSize = 100;
            SetViewportTransfrom();

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            _projection = Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, -10, 10);
            //0, _graph.Bounds.X, 0, _graph.Bounds.Y, -10, 10);
            GL.MultMatrix(ref _projection);

            ServerSocket = new ServerSocket(setup.Socket);
            FileManager = new FileManager(setup.Project);

            Initialised = true;
            int filterCount = setup.Project.FilterFiles.Count;
            TimeTree = new TimeFrameTree(FileManager, filterCount);
            TimeTree.Fill();

            FrameElement tmp = TimeTree;
            while (tmp.Children.Count == 1 && tmp.Level != FrameNodeLevel.Minute)
            {
                tmp = tmp.Children[0];
                tmp.Fill();
            }

            MajorTickDepth = (FrameNodeLevel)(((int)tmp.Level) + 1);
            if ((int)MajorTickDepth >= (int)FrameNodeLevel.Minute) RenderUnit = FrameNodeLevel.Second;
            else if ((int)MajorTickDepth <= (int)FrameNodeLevel.Month) RenderUnit = (FrameNodeLevel)(((int)MajorTickDepth) + 2);
            else RenderUnit = (FrameNodeLevel)(((int)MajorTickDepth) + 1);

            var start = TimeTree.StartTime;
            var end = TimeTree.EndTime;
            //CurrentCanvasData = TimeTree.GetCanvasData(new DateTime(start.Year, start.Month, 1), new DateTime(start.Year, start.Month + 1, 1), FrameNodeLevel.Day, FrameNodeLevel.Month);
            CurrentCanvasData = TimeTree.GetCanvasData(start, end, RenderUnit, FrameNodeLevel.Root);

           // TimeSpan total = DateTime.Now.Subtract(begin);
           // MessageBox.Show("Setup Time (ms): " + total.TotalMilliseconds);
        }


        public static void ChangeViewportSize(Size size)
        {
            ControlWidth = size.Width;
            ControlHeight = size.Height;
            SetViewportTransfrom();
        }

        public static void IncreaseDrawDepth()
        {
            if (RenderUnit == FrameNodeLevel.Second) return;
            RenderUnit = (FrameNodeLevel)((int)RenderUnit + 1);
            CurrentCanvasData = TimeTree.GetCanvasData(CurrentCanvasData.StartTime, CurrentCanvasData.EndTime, RenderUnit, CurrentCanvasData.ScaleData.DisplayUnit);
        }

        public static void ReduceDrawDepth()
        {
            if (RenderUnit == FrameNodeLevel.Root || RenderUnit == FrameNodeLevel.Year || (int)CurrentCanvasData.ScaleData.DisplayUnit >= (int)RenderUnit + 1) return;
            RenderUnit = (FrameNodeLevel)((int)RenderUnit - 1);
            CurrentCanvasData = TimeTree.GetCanvasData(CurrentCanvasData.StartTime, CurrentCanvasData.EndTime, RenderUnit, CurrentCanvasData.ScaleData.DisplayUnit);
        }

        public static void Drill(float startOffset, float endOffset)
        {
            TimeSpan span = TimeSpan.FromSeconds(endOffset - startOffset);
            FrameNodeLevel canvasSpan, renderUnit;
            DateTime startTime;
            DateTime endTime = TimeTree.StartTime.AddSeconds(endOffset);

            if (span > TimeSpan.FromDays(32)) //span is one year
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Day != 1)
                {
                    startTime = startTime.AddYears(1);
                }
                startTime = new DateTime(startTime.Year, 1, 1);
                endTime = startTime.AddYears(1);
                canvasSpan = FrameNodeLevel.Year;
                renderUnit = FrameNodeLevel.Day;
            }
            else if (span > TimeSpan.FromDays(2)) //span is one month
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Day != 1)
                {
                    startTime = startTime.AddMonths(1);
                }
                startTime = new DateTime(startTime.Year, startTime.Month, 1);
                endTime = startTime.AddMonths(1);
                canvasSpan = FrameNodeLevel.Month;
                renderUnit = FrameNodeLevel.Hour;
            }

            else if (span > TimeSpan.FromHours(2)) //span is one day
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Hour != 0)
                {
                    startTime = startTime.AddDays(1);
                }
                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                endTime = startTime.AddDays(1);
                canvasSpan = FrameNodeLevel.Day;
                renderUnit = FrameNodeLevel.PartHour;
            }
            else if (span > TimeSpan.FromMinutes(20)) //span is one hour
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Minute % 6 != 0)
                {
                    startTime = startTime.AddHours(1);
                }
                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);
                endTime = startTime.AddHours(1);
                canvasSpan = FrameNodeLevel.Hour;
                renderUnit = FrameNodeLevel.Minute;
            }
            else if (span > TimeSpan.FromMinutes(2)) //span is one part hour
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Minute != 0)
                {
                    startTime = startTime.AddMinutes(10);
                }
                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute - (startTime.Minute%5), 0);
                endTime = startTime.AddMinutes(5);
                canvasSpan = FrameNodeLevel.PartHour;
                renderUnit = FrameNodeLevel.Second;
            }
            else //span is one minute
            {
                startTime = TimeTree.StartTime.AddSeconds(startOffset);
                if (startTime.Second != 0)
                {
                    startTime = startTime.AddMinutes(1);
                }
                startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0);
                endTime = startTime.AddMinutes(1);
                canvasSpan = FrameNodeLevel.Minute;
                renderUnit = FrameNodeLevel.Second;
            }

            UndoStack.Push(CurrentCanvasData);
            CurrentCanvasData = TimeTree.GetCanvasData(startTime, endTime, renderUnit, canvasSpan);
        }

        public static void UndoDrill()
        {
            if (UndoStack.Count < 1) return;
            CurrentCanvasData = UndoStack.Pop();
        }

        public static FrameElement FindFrameElement(float startOffset, FrameNodeLevel level)
        {
            return FindFrameElement(startOffset, FileManager.StartTime, level);
        }

        public static FrameElement FindFrameElement(float startOffset, DateTime zeroTime, FrameNodeLevel level)
        {
            return TimeTree.GetFrameElement(zeroTime.AddSeconds(startOffset), level);
        }

        public static FrameElement FindFrameElement(DateTime start, FrameNodeLevel level)
        {
            return TimeTree.GetFrameElement(start, level);
        }

        public static void SetViewportTransfrom(int controlWidth, int controlHeight)
        {
            var offsetX = (2f * CanvasLocationX + (CanvasWidth - controlWidth)) / controlWidth;
            var offsetY = (2f * CanvasLocationY + (CanvasHeight - controlHeight)) / controlHeight;

            //the amount to scale the x and y values
            var scaleX = CanvasWidth / ControlWidth;
            var scaleY = CanvasHeight / ControlHeight;

            PixelSize = Vector2.Divide(new Vector2(2, 2), new Vector2(CanvasWidth, CanvasHeight));

            ViewportTransform = Matrix4.Scale(scaleX, scaleY, 1) * Matrix4.CreateTranslation(offsetX, offsetY, 0);
        }

        public static void SetViewportTransfrom()
        {
            SetViewportTransfrom(ControlWidth, ControlHeight);
        }

        public static TimeType DateInfo(DateTime time)
        {
            int timeType = 1;                   //second (1) : 1 + 6 = 7
            if (time.Second == 0)
            {
                timeType++;                     //Minute (2) : 2 + 5 = 7
                if (time.Minute == 0)
                {
                    timeType++;                 //Hour   (3) : 3 + 4 = 7
                    if (time.Hour == 0)
                    {
                        timeType++;             //day    (4) : 4 + 3 = 7
                        if (time.Day == 1)
                        {
                            timeType++;         //month  (5) : 5 + 2 = 7
                            if (time.Month == 1)
                            {
                                timeType++;     //year   (6) : 6 + 1 = 7
                            }
                        }
                    }
                }
            }
            return (TimeType)timeType;
        }

        public static float TimeToOffset(DateTime offsetTime)
        {
            Debug.Assert(Initialised);
            return (float)offsetTime.Subtract(FileManager.StartTime).TotalSeconds;
        }

        public static float TimeToOffset(DateTime offsetTime, DateTime zeroTime)
        {
            Debug.Assert(Initialised);
            return (float)offsetTime.Subtract(zeroTime).TotalSeconds;
        }

        public static Vector4 UnProject(Matrix4 view, Vector2 mouse)
        {
            Vector4 vec;

            vec.X = 2.0f * mouse.X / ControlWidth - 1;
            vec.Y = -(2.0f * mouse.Y / ControlHeight - 1);
            vec.Z = 0;
            vec.W = 1.0f;

            Matrix4 viewInv = Matrix4.Invert(view);
            Matrix4 projInv = Matrix4.Invert(_projection);

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);

            if (vec.W > float.Epsilon || vec.W < float.Epsilon)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec;
        }

    }

    public class ServerSocket
    {
        private readonly NetMQSocket _socket;
        private readonly Mutex _mutex;

        public ServerSocket(NetMQSocket socket)
        {
            _socket = socket;
            _mutex = new Mutex();
        }

        ~ServerSocket()
        {
            if (_socket != null)
            {
                while(!_mutex.WaitOne()) { Thread.Sleep(1);}
                _socket.Send(BitConverter.GetBytes(2), sizeof(int));
                _socket.Dispose();
                _mutex.Dispose();
            }
        }

        public NetMQSocket GetSocket(ServerSocketType type)
        {
            while(!_mutex.WaitOne())
            {
                Thread.Sleep(50);
            }
            _socket.Send(BitConverter.GetBytes((int)type), sizeof(int));
            return _socket;
        }

        public void ReturnSocket(ref NetMQSocket socket)
        {
            socket = null;
            _mutex.ReleaseMutex();
        }
    }

    public enum ServerSocketType
    {
        Count = 0,
        Distill = 1
    }
}