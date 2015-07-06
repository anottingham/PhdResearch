using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using ViewTimeline.Graphs;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using BufferUsageHint = OpenTK.Graphics.OpenGL.BufferUsageHint;
using GL = OpenTK.Graphics.OpenGL.GL;

namespace ViewTimeline.Graphs
{
    /// <summary>
    /// The shared background of all visible graph canvas objects
    /// </summary>
    public class GraphBackground
    {
        private readonly int _uniformTransform;
        private readonly int _uniformColor;

        private GLSLVertexBuffer<Vector2> _selectionBlock;
        private Vector2[] _selectionData;
        private Matrix4 _initialTransform;
        private Matrix4 _shiftMatrix;
        private float _XShift;
        private List<float> _majorOffsets;

        private Point _lastMousePosition;
        private GraphScaleData _scaleData;
        private float _startPosition;
        private float _endPosition;

        public MarkList MarkList { get; private set; }

        public GraphBackground(SimpleShader2D shaderProgram)
        {
            _uniformTransform = shaderProgram.UniformTransform;
            _uniformColor = shaderProgram.UniformColor;

            _selectionBlock = new GLSLVertexBuffer<Vector2>();
            _selectionBlock.SetAttributeInfo(shaderProgram.AttributeCoord2D, 2);
            _selectionData = new Vector2[4];

            _selectionData[0] = new Vector2(0, -1);
            _selectionData[1] = new Vector2(0, 1);
            _selectionData[2] = new Vector2(1, -1);
            _selectionData[3] = new Vector2(1, 1);

            _selectionBlock.BindData(_selectionData, BufferUsageHint.StaticDraw);

            SelectorColor = Color4.LightSkyBlue;
            SelectionColor = Color4.LightGray;
            _shiftMatrix = Matrix4.Identity;
            _XShift = 0;

            _lastMousePosition = new Point(0, 0);
            Enabled = true;

            MarkList = new MarkList();
        }

        public float EndPosition
        {
            get { return _endPosition; }
        }

        public float StartPosition
        {
            get { return _startPosition; }
        }

        public bool Enabled { get; set; }

        public void SelectRegion(float zeroPosition, float startPosition, float endPosition)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;
            _initialTransform = Matrix4.Scale(endPosition - startPosition, 1, 1) * Matrix4.CreateTranslation(startPosition, 0, 0);
        }

        //public void ShiftPosition(float xShift)
        //{
        //    _XShift += xShift;
        //    _shiftMatrix = Matrix4.CreateTranslation(_XShift, 0, 0);
        //}

        public Color4 SelectorColor { get; set; }

        public Color4 SelectionColor { get; set; }

        public void AddSelectionMark(DateTime start, DateTime end)
        {
            MarkList.Add(start, end);
        }

        public void ClearSelectionMarks()
        {
            MarkList.Clear();
        }

        public void UndoClearSelectionMarks()
        {
            MarkList.UndoClear();
        }

        public void Draw(float xShift)
        {
            if (!Enabled) return;

            Matrix4 tmp = Matrix4.CreateTranslation(xShift, 0, 0) *
                          Matrix4.Scale(2f / CanvasManager.CurrentCanvasData.ScaleData.BaseScale, 1, 1) *
                          Matrix4.CreateTranslation(-1, 0, 0);
            Matrix4 transform;
            GL.Uniform4(_uniformColor, SelectionColor);

            //Draw Marks
            foreach (var mark in MarkList.Marks)
            {
                transform = mark.InitialTransform * tmp;
                GL.UniformMatrix4(_uniformTransform, false, ref transform);

                _selectionBlock.BeginDraw();
                _selectionBlock.Draw(BeginMode.TriangleStrip);
                _selectionBlock.EndDraw();
            }

            transform = _initialTransform * tmp;

            GL.UniformMatrix4(_uniformTransform, false, ref transform);
            GL.Uniform4(_uniformColor, SelectorColor);

            _selectionBlock.BeginDraw();
            _selectionBlock.Draw(BeginMode.TriangleStrip);
            _selectionBlock.EndDraw();
        }
    }
}

public class MarkList
{
    private List<CanvasMark> _marks, _undo;

    public MarkList()
    {
        _marks = new List<CanvasMark>();
        _undo = null;
    }

    public List<CanvasMark> Marks
    {
        get { return _marks; }
    }

    public void Clear()
    {
        if (_undo != null) _undo.Clear();
        _undo = _marks;
        _marks = new List<CanvasMark>();
    }

    public void Add(DateTime start, DateTime end)
    {
        _marks.Add(new CanvasMark(start, end));
    }

    public void UndoClear()
    {
        if (_undo != null)
        {
            var tmp = _undo;
            _undo = _marks;
            _marks = tmp;
        }
    }
}

public struct CanvasMark : IComparable<CanvasMark>
{
    private readonly long _startIndex;
    private readonly long _endIndex;
    private readonly float _xShift;
    private readonly float _width;
    private readonly Matrix4 _transform;

    public long StartIndex { get { return _startIndex; } }

    public long EndIndex { get { return _endIndex; } }

    public float XShift { get { return _xShift; } }

    public float Width { get { return _width; } }

    public Matrix4 InitialTransform { get { return _transform; } }

    public CanvasMark(DateTime start, DateTime end)
    {
        CanvasManager.FileManager.TimeFile.GetPacketIndices(start, end, out _startIndex, out _endIndex);
        _xShift = CanvasManager.TimeToOffset(start);
        _width = CanvasManager.TimeToOffset(end) - _xShift;
        _transform = Matrix4.Scale(_width, 1, 1) * Matrix4.CreateTranslation(_xShift, 0, 0);
    }

    private CanvasMark(long startIndex, long endIndex)
    {
        _startIndex = startIndex;
        _endIndex = endIndex;
        _xShift = 0;
        _width = 0;
        _transform = new Matrix4();
    }

    public List<CanvasMark> Split(long size, int target)
    {
        double chunks = 1.5 * size / target; //over estimate avg index size by ~10%
        int avgCount = (int)Math.Ceiling(((EndIndex - StartIndex) / chunks));
        List<CanvasMark> marks = new List<CanvasMark>();

        for (int k = 0; k < chunks; k++)
        {
            long start, end;
            long srt_idx = _startIndex + k * avgCount;
            long end_idx = srt_idx + avgCount;
            if (end_idx >= _endIndex)
            {
                end_idx = _endIndex;
            }

            CanvasManager.FileManager.IndexFile.GetDataIndices(srt_idx, end_idx, out start, out end);

             if (end - start >= target)
             {
                 marks.AddRange(new CanvasMark(srt_idx, end_idx).Split(end - start, target));
             }
             else
             {
                 marks.Add(new CanvasMark(srt_idx, end_idx));
             }
        }

        
        //do
        //{
        //    long start, end;

        //    CanvasManager.FileManager.IndexFile.GetDataIndices(currIndex, currIndex + avgCount, out start, out end);

        //    if (end - start >= target)
        //    {
        //        var mark = new CanvasMark(currIndex, currIndex + avgCount);
        //        marks.AddRange(mark.Split(end - start, target));
        //        currIndex += avgCount;
        //    }
        //    else
        //    {
        //        marks.Add(new CanvasMark(currIndex, currIndex + avgCount));
        //        currIndex += avgCount;
        //    }
        //    remaining -= (end - start);

        //    //if (end - start ==0)// patch method  -need to fix root problem
        //    //{
        //    //    break;
        //    //}

        //} while (remaining > 0);
        


        return marks;
    }

    public int CompareTo(CanvasMark other)
    {
        return _startIndex.CompareTo(other.StartIndex);
    }
}