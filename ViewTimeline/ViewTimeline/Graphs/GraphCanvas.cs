using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using GL = OpenTK.Graphics.OpenGL.GL;

namespace ViewTimeline.Graphs
{
    public class GraphCanvas
    {
        private GLSLVertexBuffer<Vector2> _frontBuffer;
        private GLSLVertexBuffer<Vector2> _backBuffer;
        private BeginMode _drawMode;
        private Vector2[] _frontData;
        private Vector2[] _backData;

        private float _alphaBlend;
        private int _drawLength;

        public string Name { get; set; }

        private SimpleShader2D _shaderProgram;

        public GraphCanvas(string name, SimpleShader2D shaderProgram)
        {
            _shaderProgram = shaderProgram;
            Color = Color4.LightSteelBlue;
            _alphaBlend = 1f;

            _frontBuffer = new GLSLVertexBuffer<Vector2>();
            _backBuffer = new GLSLVertexBuffer<Vector2>();
            GraphType = GraphType.LineGraph;
            ScaleFunction = new GraphScaleConfig(GraphScaleFunction.Maximum, GraphScaleTarget.DataVolume);
            Hide = false;
            Name = name;
        }

        public float AlphaBlend
        {
            get { return _alphaBlend; }
            set
            {
                _alphaBlend = value;
                if (_alphaBlend > 1f) _alphaBlend = 1f;
                else if (_alphaBlend < 0f) _alphaBlend = 0f;
            }
        }

        public bool Hide { get; set; }

        /// <summary>
        /// Binds data to the front and back buffers. If the front buffer is empty, writes to front buffer. Otherwise writes to back buffer.
        /// </summary>
        /// <param name="data">The GraphData object to bind</param>
        public void BindData(GraphData data)
        {
            if (_frontData == null)
            {
                _frontData = data.VertexData.ToArray();
                _frontBuffer.BindData(_frontData);
            }
            else
            {
                _backData = data.VertexData.ToArray();
                _backBuffer.BindData(_backData);
            }
        }

        /// <summary>
        /// Swaps the back buffer and front buffer, and empties the new back buffer (old front buffer)
        /// </summary>
        public void SwapBuffers()
        {
            if (_backData == null) return;
            var tmp = _frontBuffer;
            _frontBuffer = _backBuffer;
            _frontData = _backData;
            _backBuffer = tmp;
            _backData = null;
        }

        public GraphScaleConfig ScaleFunction { get; set; }

        public Color4 Color { get; set; }

        private GraphType _graphType;

        public GraphType GraphType
        {
            get { return _graphType; }
            set
            {
                _graphType = value;
                switch (_graphType)
                {
                    case GraphType.LineGraph:
                        _drawMode = BeginMode.LineStrip;
                        _frontBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);
                        _backBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);
                        break;
                    case GraphType.ScatterPlot:
                        _drawMode = BeginMode.Points;
                        _frontBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);
                        _backBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);

                        break;
                    case GraphType.SolidGraph:
                        _drawMode = BeginMode.TriangleStrip;
                        _frontBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2);
                        _backBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Draw(GraphScaleData scaleData)
        {
            if (Hide) return;
            float height = ScaleFunction.ScaledHeight(scaleData);

            Matrix4 transform = Matrix4.CreateTranslation(scaleData.XShift, 0, 0) * Matrix4.Scale(2 / scaleData.BaseScale, 2 / height, 1) * Matrix4.CreateTranslation(-1, -1, 0);
            GL.UniformMatrix4(_shaderProgram.UniformTransform, false, ref transform);

            if (_drawMode == BeginMode.Lines || _drawMode == BeginMode.LineLoop || _drawMode == BeginMode.LineStrip)
            {
                GL.LineWidth(2f);
            }
            else if (_drawMode == BeginMode.Points)
            {
                GL.PointSize(2f);
            }

            if (_alphaBlend < 1)
            {
                Color4 col = Color;
                col.A = _alphaBlend;
                GL.Uniform4(_shaderProgram.UniformColor, col);
            }
            else
            {
                GL.Uniform4(_shaderProgram.UniformColor, Color);
            }

            if (_graphType == GraphType.SolidGraph)
            {
                _frontBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2);

                _frontBuffer.BeginDraw();
                _frontBuffer.Draw(_drawMode);
                _frontBuffer.EndDraw();

                _frontBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);
                GL.Uniform4(_shaderProgram.UniformColor, Color4.Black);
                GL.LineWidth(1f);

                _frontBuffer.BeginDraw();
                _frontBuffer.Draw(BeginMode.LineStrip, _frontData.Length / 2);
                _frontBuffer.EndDraw();
            }
            else
            {
                _frontBuffer.BeginDraw();
                _frontBuffer.Draw(_drawMode, _frontData.Length / 2);
                _frontBuffer.EndDraw();
            }
        }

        public void DrawBackBuffer(GraphScaleData scaleData)
        {
            if (Hide || _backData == null) return;
            float height = ScaleFunction.ScaledHeight(scaleData);

            Matrix4 transform = Matrix4.CreateTranslation(scaleData.XShift, 0, 0) * Matrix4.Scale(2 / scaleData.BaseScale, 2 / height, 1) * Matrix4.CreateTranslation(-1, -1, 0);
            GL.UniformMatrix4(_shaderProgram.UniformTransform, false, ref transform);

            if (_drawMode == BeginMode.Lines || _drawMode == BeginMode.LineLoop || _drawMode == BeginMode.LineStrip)
            {
                GL.LineWidth(2f);
            }
            else if (_drawMode == BeginMode.Points)
            {
                GL.PointSize(2f);
            }

            if (_alphaBlend > 0)
            {
                Color4 col = Color;
                col.A = 1 - _alphaBlend;
                GL.Uniform4(_shaderProgram.UniformColor, col);
            }
            else
            {
                GL.Uniform4(_shaderProgram.UniformColor, Color);
            }

            if (_graphType == GraphType.SolidGraph)
            {
                _backBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2);

                _backBuffer.BeginDraw();
                _backBuffer.Draw(_drawMode);
                _backBuffer.EndDraw();

                _backBuffer.SetAttributeInfo(_shaderProgram.AttributeCoord2D, 2, false, 2 * Vector2.SizeInBytes);
                GL.Uniform4(_shaderProgram.UniformColor, Color4.Black);
                GL.LineWidth(1f);

                _backBuffer.BeginDraw();
                _backBuffer.Draw(BeginMode.LineStrip, _backData.Length / 2);
                _backBuffer.EndDraw();
            }
            else
            {
                _backBuffer.BeginDraw();
                _backBuffer.Draw(_drawMode, _backData.Length / 2);
                _backBuffer.EndDraw();
            }
        }
    }
}