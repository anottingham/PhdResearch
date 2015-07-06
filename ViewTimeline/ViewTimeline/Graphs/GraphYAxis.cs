using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using ViewTimeline.GLControls;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using BufferUsageHint = OpenTK.Graphics.OpenGL.BufferUsageHint;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using GL = OpenTK.Graphics.OpenGL.GL;

namespace ViewTimeline.Graphs
{
    public class GraphYAxis
    {
        private SimpleShader2D _shaderProgram;

        public GLSLVertexBuffer<Vector2> YTicks { get; private set; }

        public Vector2[] YTicksData { get; private set; }

        public Vector2[] YTicksIndex { get; private set; }

        public GraphYAxis(SimpleShader2D shaderProgram)
        {
            _shaderProgram = shaderProgram;

            YTicks = new GLSLVertexBuffer<Vector2>();

            YTicks.SetAttributeInfo(shaderProgram.AttributeCoord2D, 2);
        }

        public void GenerateAxis(long height)
        {
            //generate y ticks data
            //Note - need allow different axis modes (count, data)
            var digits = (int)Math.Floor(Math.Log10(height));
            var yTickSpacing = (int)Math.Pow(10, digits - 1);
            var max = (int)((((height / Math.Pow(10, digits)) + 1) * Math.Pow(10, digits)) / yTickSpacing);
            var data = new List<Vector2>();
            for (var k = 0; k <= max; k++)
            {
                var tickScale = 0.5f;
                var y = k * yTickSpacing;
                if (k % 10 == 0) tickScale = 1;
                data.Add(new Vector2(-1, y));
                data.Add(new Vector2(-1 - CanvasManager.TickSize * tickScale * CanvasManager.PixelSize.X, y));
            }
            YTicksData = data.ToArray();
            YTicks.BindData(YTicksData, BufferUsageHint.StaticDraw);
        }

        public void Draw(float max)
        {
            Matrix4 transform = CanvasManager.ViewportTransform;

            GL.Enable(EnableCap.ScissorTest);

            transform = Matrix4.Scale(1, 2 / max, 1) * Matrix4.CreateTranslation(0, -1, 0) *
                CanvasManager.ViewportTransform;
            GL.UniformMatrix4(_shaderProgram.UniformTransform, false, ref transform);
            GL.Uniform4(_shaderProgram.UniformColor, Color4.Gray);
            //GL.LineWidth(1f);

            GL.Disable(EnableCap.ScissorTest);

            //transform = Matrix4.CreateTranslation(0, pos.Y, 0) * Matrix4.Scale(1, scale.Y, 1) * CanvasManager.ViewportTransform;
            //GL.UniformMatrix4(_uniformTransform, false, ref transform);
            GL.LineWidth(2f);

            GL.Scissor(CanvasManager.TickSize, CanvasManager.BorderSize + CanvasManager.TickSize - 1, CanvasManager.ControlWidth - CanvasManager.TickSize,
                       CanvasManager.ControlHeight - 2 * (CanvasManager.BorderSize + CanvasManager.TickSize) + 2);
            GL.Enable(EnableCap.ScissorTest);

            YTicks.BeginDraw();
            YTicks.Draw(BeginMode.Lines);
            YTicks.EndDraw();

            GL.Disable(EnableCap.ScissorTest);
        }
    }
}